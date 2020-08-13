using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SAP2000v1;

namespace WPF
{
    public class AnalysisProcess
    {
        public cOAPI _SapObjectServer { get; set; }
        public cSapModel _SapModelServer { get; set; }

        public AnalysisProcess(cOAPI SapObjectServer)
        {

            _SapModelServer = SapObjectServer.SapModel;
        }


        /// <summary>
        /// Process to run for each sapobject thats connected
        /// </summary>
        /// <returns></returns>
        public void RunProcess(List<LoadCase> lcs, string newfilename, string currentfilename, bool current)
        {
            //wrap in a try catch and return false on catch

            //openfile
            if (!current)
            {
                _SapObjectServer.ApplicationStart(eUnits.kip_in_F, true, newfilename);

                //Save locally somewhere
                string newlocalfilename = System.IO.Path.Combine(@"Documents\", currentfilename);
                int ret = _SapModelServer.File.Save(newlocalfilename);
            }           

            //Set all load cases to false
            _SapModelServer.Analyze.SetRunCaseFlag("", false, true);

            foreach (LoadCase lc in lcs)
            {
                _SapModelServer.Analyze.SetRunCaseFlag(lc._name, true);
            }

            //Use all 8 cores on each machine
            int type = 0;
            int proctype = 0;
            int numcores = 0;
            string stiffcase = "";

            _SapModelServer.Analyze.GetSolverOption_2(ref type, ref proctype, ref numcores, ref stiffcase);
            _SapModelServer.Analyze.SetSolverOption_2(type, 2, 8);
            _SapModelServer.Analyze.RunAnalysis();

            //Merge to original
            _SapModelServer.Analyze.MergeAnalysisResults(newfilename);

            //Exit SAP
            if (!current)
            {
                _SapObjectServer.ApplicationExit(false);
            }
        }

    }
}
