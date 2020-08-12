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
        public MainWindow()
        {
            InitializeComponent();
            

            InfillCases.Items.Add(lc);


            //write to list





            //Retrieve current instance
            //cOAPI SapObjectClient = null;
            //cHelper HelperClient = new Helper();
            //SapObjectClient = HelperClient.GetObject("CSI.SAP2000.API.SapObject");
            //cSapModel SapModelClient = SapObjectClient.SapModel;
        }


        public static List<LoadCase> PopulateGrid()
        {
            //Retrieve current instance
            cOAPI SapObjectClient = null;
            cHelper HelperClient = new Helper();
            SapObjectClient = HelperClient.GetObject("CSI.SAP2000.API.SapObject");
            cSapModel SapModelClient = SapObjectClient.SapModel;

            int numberofitems = 0;
            string[] casenames = null;
            int[] status = null;

            int numberofitemsrun = 0;
            string[] casenamesrun = null;
            bool[] run = null;

            int ret = SapModelClient.Analyze.GetCaseStatus(ref numberofitems, ref casenames, ref status);
            ret = SapModelClient.Analyze.GetRunCaseFlag(ref numberofitemsrun, ref casenamesrun, ref run);

            List<int> statuslist = new List<int>();

            for (int i = 0; i < numberofitems;i++)
            {
                LoadCase lc = new LoadCase(casenames[i], status[i], true, "Client");
                
            }
            InfillCases.Items.Add(lc);



        }

        public static void PopulateDropdown()
        {

        }

        /// <summary>
        /// Assign selected grid to a specific drop down
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Confirmbutton_Click(object sender, RoutedEventArgs e)
        {

        }

        /// <summary>
        /// Assign all to a specific drop down
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DoNotRunButton_Click(object sender, RoutedEventArgs e)
        {

        }

        /// <summary>
        /// Trigger Run Sequence
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RunAnalysis_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
