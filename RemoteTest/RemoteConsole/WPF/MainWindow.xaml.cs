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

        public MainWindow()
        {
            InitializeComponent();

            //Current SAP22 load cases
            List<LoadCase> LoadCases = GetCurrentLoadCases();

            //Populate initial datagrid
            foreach(LoadCase lc in LoadCases)
            {
                InfillCases.Items.Add(lc);
            }

            //Get Potential machine locations here?
            MachineLocation ml1 = new MachineLocation("IL1M113", 11650);
            MachineLocation ml2 = new MachineLocation("IL1M113", 49150);

            MachineLocation.Items.Add("Client");
            MachineLocation.Items.Add(ml1.ToString());
            MachineLocation.Items.Add(ml2.ToString());
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

        /// <summary>
        /// Trigger Run Sequence
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RunAnalysis_Click(object sender, RoutedEventArgs e)
        {
            //save model on p drive
            string initialfilepath = SapModelClient.GetModelFilename();
            string filename = SapModelClient.GetModelFilename(false);
            string guid = Guid.NewGuid().ToString();
            string appendedfilepath = @"\\windows.thorntontomasetti.com\FileSys\_Corp\CORE\Public\9_Temp\SAPELLITE\CLIENT_MODELS\" + guid;
            //string appendedfilepath = @"P:\_Corp\CORE\Public\9_Temp\SAPELLITE\CLIENT_MODELS\" + guid;
            System.IO.Directory.CreateDirectory(appendedfilepath);
            string newfilename = System.IO.Path.Combine(appendedfilepath, filename);
            System.IO.File.Copy(initialfilepath, newfilename);

            //List<List<LoadCase>> groupeduploadcases = new List<List<LoadCase>>();
            List<LoadCase> groupeduploadcases = new List<LoadCase>();
            foreach (LoadCase lc in InfillCases.Items)
            {
                groupeduploadcases.Add(lc);
            }

            var newgroup = groupeduploadcases.GroupBy(x => x._location);

            //Make this a parallel loop after reviewed
            foreach(var group in newgroup)
            {
                if (group.Key == "Client")
                {
                    //Retrieves current instance of SAP on CLIENT Computer

                    cOAPI SapObjectServer = null;
                    cHelper HelperServer = new Helper();
                    SapObjectServer = HelperServer.GetObject("CSI.SAP2000.API.SapObject");

                    AnalysisProcess ap = new AnalysisProcess(SapObjectServer);
                    ap.RunProcess(group.ToList(), newfilename, filename, true);
                }
                else
                {
                    //Retrieves groups and ports on SERVER Computers

                    //location and tcp port
                    string location = group.First()._location;
                    var splitchar = location.Split('[');
                    string machinenumber = splitchar[0];
                    string tcpportstr = splitchar[1].TrimEnd(']');
                    int tcpport = System.Convert.ToInt32(tcpportstr);

                    //start instance
                    //send to a machine
                    cOAPI SapObjectServer = null;
                    cHelper HelperServer = new Helper();
                    SapObjectServer = HelperServer.CreateObjectProgIDHostPort(machinenumber, tcpport, "CSI.SAP2000.API.SapObject");

                    AnalysisProcess ap = new AnalysisProcess(SapObjectServer);
                    ap.RunProcess(group.ToList(), newfilename, filename, false);
                }             
            }

            //Open up the merged version after all is completed:
            SapModelClient.File.OpenFile(newfilename); // this could be a copy if opening on the pdrive takes forever

            //Save back to initial filepath:
            SapModelClient.File.Save(initialfilepath);

            //Review Results.

        }
    }
}
