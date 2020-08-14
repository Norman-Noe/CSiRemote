using System;
using System.Collections.Generic;
using System.IO;
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
        private async void RunAnalysis_Click(object sender, RoutedEventArgs e)
        {
            //string test = @"P:\_Corp\CORE\Public\9_Temp\SAPELLITE\176d1db6-0dab-4724-b308-b7539f97de60\NY1W125_11653\8.10.20.sdb";
            //CopyAnalysisResultsToClientMachine(test);
            //return;

            //save model on p drive
            string originalFileFullPath = SapModelClient.GetModelFilename();
            string originalFileName = SapModelClient.GetModelFilename(false);
            string batchRunGuid = Guid.NewGuid().ToString();
            var basePath = @"P:\_Corp\CORE\Public\9_Temp\SAPELLITE";
            
            List<LoadCase> allLoadcases = new List<LoadCase>();
            foreach (LoadCase lc in InfillCases.Items)
            {
                allLoadcases.Add(lc);
            }

            var loadcasesGroupedByServerHost = allLoadcases.GroupBy(x => x._location);

            //List to collect all locations.
            List<string> modelsToMerge = new List<string>();

            List<Task> analysisTasks = new List<Task>();
            foreach(var group in loadcasesGroupedByServerHost)
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
                    //The machine name and TCP port from the UI string
                    string location = group.First()._location;

                    //Create the task which will perform the analysis.
                    Task t = CreateAnalysisTask(location, basePath, batchRunGuid, originalFileName, originalFileFullPath,
                        group.ToList(), modelsToMerge);
                    analysisTasks.Add(t);

                }             
            }
            
            //Start all tasks
            foreach (var task in analysisTasks) task.Start();
            
            //Now wait for all of them to finish
            Task.WaitAll(analysisTasks.ToArray());

            string localFolder = CopyAnalysisResultsToClientMachine(modelsToMerge.FirstOrDefault());

            //Merge the results back into the client model
            foreach (var pDriveClientFilename in modelsToMerge)
            {
                string local = GetLocalMirrorName(pDriveClientFilename);

                int ret = SapModelClient.Analyze.MergeAnalysisResults(local);
            }
            DeleteLocalResults(localFolder);


        }

        //Creates the task which will run the analysis on the server.
        Task CreateAnalysisTask(string location, string basePath, string batchRunGuid, string originalFileName, 
            string originalFileFullPath, List<LoadCase> groups, List<string> modelsToMerge)
        {
            Task t1 = new Task(() =>
            {
                MachineLocation loc = ParseMachineInfo(location);
                string machinenumber = loc._MachineNumber;
                int tcpport = loc._Port;

                string clientModelDirectory = basePath + @"\" + batchRunGuid + @"\" + loc._MachineNumber + $"_{loc._Port}";

                System.IO.Directory.CreateDirectory(clientModelDirectory);

                string machineSpecificModelOnPdrive = System.IO.Path.Combine(clientModelDirectory, originalFileName);

                System.IO.File.Copy(originalFileFullPath, machineSpecificModelOnPdrive);

                //start instance
                //send to a machine
                cOAPI SapObjectServer = null;
                cHelper HelperServer = new Helper();
                SapObjectServer = HelperServer.CreateObjectProgIDHostPort(machinenumber, tcpport, "CSI.SAP2000.API.SapObject");

                AnalysisProcess ap = new AnalysisProcess(SapObjectServer);
                ap.RunProcess(groups, machineSpecificModelOnPdrive, false);
                modelsToMerge.Add(machineSpecificModelOnPdrive);

            });
            return t1;
        }

        private string GetLocalMirrorName(string pathToSapFile)
        {
            var appdataFolder = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"Computers and Structures\CORE\SAPELLITE");

            //P:\_Corp\CORE\Public\9_Temp\SAPELLITE\176d1db6-0dab-4724-b308-b7539f97de60\NY1W125_11653\8.10.20.sdb

            //NY1W125_11653
            //string machineAndPort = System.IO.Path.GetFileName(System.IO.Path.GetDirectoryName(pathToSapFile));
            string fileName = System.IO.Path.GetFileName(pathToSapFile);

            //P:\_Corp\CORE\Public\9_Temp\SAPELLITE\176d1db6-0dab-4724-b308-b7539f97de60\NY1W125_11653
            string machineAndPort = Directory.GetParent(pathToSapFile).FullName;
            //NY1W125_11653
            machineAndPort = System.IO.Path.GetFileName(machineAndPort);

            //P:\_Corp\CORE\Public\9_Temp\SAPELLITE\176d1db6-0dab-4724-b308-b7539f97de60
            string batchFolder = Directory.GetParent(pathToSapFile).Parent.FullName;
            //176d1db6-0dab-4724-b308-b7539f97de60
            string guid = System.IO.Path.GetFileName(batchFolder);
            
            string fullLocalFolder = System.IO.Path.Combine(appdataFolder, guid, machineAndPort, fileName);
            return fullLocalFolder;
        }

        private string CopyAnalysisResultsToClientMachine(string pathToSapFile)
        {

            //string fu
            string fullLocalFolder = System.IO.Path.GetDirectoryName((GetLocalMirrorName(pathToSapFile)));
            fullLocalFolder = System.IO.Directory.GetParent(fullLocalFolder).FullName;

            string batchFolder = Directory.GetParent(pathToSapFile).Parent.FullName;
            
            Directory.CreateDirectory(fullLocalFolder);

            CloneFolder(batchFolder, fullLocalFolder);

            return fullLocalFolder;

            //string localFilelocation = System.IO.Path.Combine(fullLocalFolder, );

            //return new Tuple<string, string>(fullLocalFolder, "");
        }

        private void DeleteLocalResults(string s)
        {
            Directory.Delete(s, true);
        }

        private void CloneFolder(string SourcePath, string DestinationPath)
        {
            //Now Create all of the directories
            foreach (string dirPath in Directory.GetDirectories(SourcePath, "*",
                SearchOption.AllDirectories))
                Directory.CreateDirectory(dirPath.Replace(SourcePath, DestinationPath));

            //Copy all the files & Replaces any files with the same name
            foreach (string newPath in Directory.GetFiles(SourcePath, "*.*",
                SearchOption.AllDirectories))
                File.Copy(newPath, newPath.Replace(SourcePath, DestinationPath), true);

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
