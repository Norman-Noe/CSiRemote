﻿using System;
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
            cOAPI coapi = null;
            cHelper helper = new Helper();
            int ret = 0;
            int[] tpc = new int[] { 11650, 49151 };

            //Try and run multiple instance on IL1M113 using different TPC ports
            //Parallel.For(0, 2, i =>
            //  {
            //      //IF I WANT TO RUN MULTIPLE INSTANCES ON THE SAME COMPUTER CAN I LOOK UP THE AVAILABLE TCP PORTS ON THAT COMPUTER?  
            //      //HOW DOES THIS WORK?


            coapi = helper.CreateObjectProgIDHostPort("IL1W015", tpc[0], "CSI.SAP2000.API.SapObject");
            ret = coapi.ApplicationStart();
            cSapModel SapModel = coapi.SapModel;
            ret = SapModel.InitializeNewModel();

            //initialize model
            //  }
            //);

            //Try and run multiple instances on my own computer
            //Parallel.For(0, 2, i =>
            //{
            //IF I WANT TO RUN MULTIPLE INSTANCES ON THE SAME COMPUTER CAN I LOOK UP THE AVAILABLE TCP PORTS ON THAT COMPUTER?  
            //HOW DOES THIS WORK?



            //Looks like you cant have more then 1 sapv22 model at a time on a computer.  Will have to go the mass computer route.

        }
    }
}
