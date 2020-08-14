using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using SAP2000v1;

namespace WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public cOAPI SapObjectClient { get; set; }

        public cHelper HelperClient { get; set; }
        public cSapModel SapModelClient { get; set; }

        private SapellitePipeline _sapellitePipeline;

        public MainWindow()
        {
            InitializeComponent();
            _sapellitePipeline = new SapellitePipeline();
            this.Closing += MainWindow_Closing;

            //Current SAP22 load cases
            List<LoadCase> LoadCases = GetCurrentLoadCases();

            //Populate initial datagrid
            foreach(LoadCase lc in LoadCases)
            {
                InfillCases.Items.Add(lc);
            }

            //Get Potential machine locations here?
            //MachineLocation ml1 = new MachineLocation("IL1W015", 11650);
            //MachineLocation ml2 = new MachineLocation("IL1W015", 49150);

            //MachineLocation.Items.Add("Client");
            //MachineLocation.Items.Add(ml1.ToString());
            //MachineLocation.Items.Add(ml2.ToString());
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            this._sapellitePipeline.ShutDown();
            //throw new NotImplementedException();
        }

        //Can move this somewhere else
        public List<LoadCase> GetCurrentLoadCases()
        {
            //Retrieve current instance
            SapObjectClient = null;
            HelperClient = new Helper();
            SapObjectClient = HelperClient.GetObject("CSI.SAP2000.API.SapObject");
            SapModelClient = SapObjectClient.SapModel;

            int numberofitems = 0;
            string[] casenames = null;
            int[] status = null;

            int numberofitemsrun = 0;
            string[] casenamesrun = null;
            bool[] run = null;

            int ret = SapModelClient.Analyze.GetCaseStatus(ref numberofitems, ref casenames, ref status);
            ret = SapModelClient.Analyze.GetRunCaseFlag(ref numberofitemsrun, ref casenamesrun, ref run);

            List<int> statuslist = new List<int>();
            List<LoadCase> loadcases = new List<LoadCase>();

            for (int i = 0; i < numberofitems;i++)
            {
                LoadCase lc = new LoadCase(casenames[i], status[i], true, "Client");
                loadcases.Add(lc);
            }
          
            return loadcases;
        }
    

        /// <summary>
        /// Assign selected grid to a specific drop down
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Confirmbutton_Click(object sender, RoutedEventArgs e)
        {
            string currentdropdown = MachineLocation.Text;

            var selecteditems = InfillCases.SelectedItems;

            foreach (LoadCase lc in selecteditems)
            {
                lc._action = "Run";
                lc._location = currentdropdown;
            }

            InfillCases.Items.Refresh();
        }

        /// <summary>
        /// Assign all to a specific drop down
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DoNotRunButton_Click(object sender, RoutedEventArgs e)
        {
            var selecteditems = InfillCases.SelectedItems;

            foreach (LoadCase lc in selecteditems)
            {
                lc._action = "Do Not Run";
                lc._location = "NA";
            }

            InfillCases.Items.Refresh();
        }

        private MachineLocation ParseMachineInfo(string location)
        {
            //string location = group.First()._location;
            var splitchar = location.Split('[');
            string machinenumber = splitchar[0];
            string tcpportstr = splitchar[1].TrimEnd(']');
            int tcpport = System.Convert.ToInt32(tcpportstr);

            return new MachineLocation(machinenumber, tcpport);

        }

        /// <summary>
        /// Trigger Run Sequence
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RunAnalysis_Click(object sender, RoutedEventArgs e)
        {
            //save model on p drive
            string originalFileFullPath = SapModelClient.GetModelFilename();
            string originalFileName = SapModelClient.GetModelFilename(false);
            string batchRunGuid = Guid.NewGuid().ToString();
            var basePath = @"P:\_Corp\CORE\Public\9_Temp\SAPELLITE";

            //List<List<LoadCase>> groupeduploadcases = new List<List<LoadCase>>();
            List<LoadCase> groupeduploadcases = new List<LoadCase>();
            foreach (LoadCase lc in InfillCases.Items)
            {
                groupeduploadcases.Add(lc);
            }

            var newgroup = groupeduploadcases.GroupBy(x => x._location);

            //Make this a parallel loop after reviewed
            List<string> modelsToMerge = new List<string>();

            foreach(var group in newgroup)
            {
                if (group.Key == "Client")
                {
                    //Retrieves current instance of SAP on CLIENT Computer

                    //cOAPI SapObjectServer = null;
                    //cHelper HelperServer = new Helper();
                    //SapObjectServer = HelperServer.GetObject("CSI.SAP2000.API.SapObject");

                    //AnalysisProcess ap = new AnalysisProcess(SapObjectServer);
                    //ap.RunProcess(group.ToList(), newfilename, newname, true);
                }
                else
                {

                    string location = group.First()._location;
                    MachineLocation loc = ParseMachineInfo(location);
                    string machinenumber = loc._MachineNumber;
                    int tcpport = loc._Port;
                    
                    string clientModelDirectory = basePath + @"\" + batchRunGuid + @"\" + loc._MachineNumber;

                    System.IO.Directory.CreateDirectory(clientModelDirectory);

                    string machineSpecificModelOnPdrive = System.IO.Path.Combine(clientModelDirectory, originalFileName);

                    System.IO.File.Copy(originalFileFullPath, machineSpecificModelOnPdrive);

                    //start instance
                    //send to a machine
                    cOAPI SapObjectServer = null;
                    cHelper HelperServer = new Helper();
                    SapObjectServer = HelperServer.CreateObjectProgIDHostPort(machinenumber, tcpport, "CSI.SAP2000.API.SapObject");

                    AnalysisProcess ap = new AnalysisProcess(SapObjectServer);
                    ap.RunProcess(group.ToList(), machineSpecificModelOnPdrive, false);
                    modelsToMerge.Add(machineSpecificModelOnPdrive);
                }             
            }


            //Open the analyzed model on the client side.
            //int ret = SapModelClient.File.OpenFile(pDriveClientFilename); // this could be a copy if opening on the pdrive takes forever

            //open original
            //ret = SapModelClient.File.OpenFile(originalFileFullPath); // this could be a copy if opening on the pdrive takes forever


            //original is opened - let's save it
            int type = 0, proctype = 0, numcores = 0;
            string stiffcase = "";
            int ret = SapModelClient.Analyze.GetSolverOption_2(ref type, ref proctype, ref numcores, ref stiffcase);
            ret = SapModelClient.Analyze.SetSolverOption_2(type, 2, 8);
            ret = SapModelClient.File.Save(originalFileFullPath);
            //merge.

            //string temp = @"P:\_Corp\CORE\Public\9_Temp\SAPELLITE\6685132b-63b4-4b04-a62e-6a83707ac16a\8.10.20_6685132b-63b4-4b04-a62e-6a83707ac16a.sdb";
            //ret = SapModelClient.Analyze.MergeAnalysisResults(temp);
            string pDriveClientFilename = modelsToMerge[0];

            ret = SapModelClient.Analyze.MergeAnalysisResults(pDriveClientFilename);




            //Open up the merged version after all is completed:

            //Save back to initial filepath:
            //SapModelClient.File.Save(originalFileFullPath);

            //Review Results.

        }

        //EMIL DO STUFF HERE!
        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            MachineLocation.Items.Clear();

            var machines = this._sapellitePipeline.GetAvailableMachines();
            foreach (var machine in machines)
            {
                MachineLocation.Items.Add(machine);
            }
        }
    }
}
