using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using SAP2000v1;


namespace RemoteConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            cHelper SapHelper = new Helper();
            int ret = 0;

            //create Sap2000 object
            cOAPI SapObject = SapHelper.CreateObject(@"C:\Program Files\Computers and Structures\SAP2000 22\SAP2000.exe");

            //start Sap2000 application
            SapObject.ApplicationStart();

            //create SapModel object
            cSapModel SapModel = SapObject.SapModel;

            //initialize model
            ret = SapModel.InitializeNewModel();

            //create model from template
            ret = SapModel.File.New2DFrame(e2DFrameType.PortalFrame, 3, 124, 3, 200);

            //save model
            ret = SapModel.File.Save(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\SapAPI\source\model.sdb");

            //run model (this will create the analysis model)
            ret = SapModel.Analyze.RunAnalysis();

            //initialize a new model
            ret = SapModel.InitializeNewModel();

            //create the same model from template
            ret = SapModel.File.New2DFrame(e2DFrameType.PortalFrame, 3, 124, 3, 200);

            //merge analysis results
            ret = SapModel.Analyze.SetSolverOption_2(1, 2, 0);

            ret = SapModel.File.Save(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) +  @"\SapAPI\target\model.sdb");
            ret = SapModel.Analyze.MergeAnalysisResults(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\SapAPI\source\model.sdb");


        }
    }
}
