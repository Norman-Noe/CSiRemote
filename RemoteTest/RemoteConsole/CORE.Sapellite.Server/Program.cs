using CORE.Saptellite.Library;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
//using SAP2000v1;

namespace CORE.Sapellite.Server
{
    public class SapProcess
    {
        public SapProcess(System.Diagnostics.Process process, int port)
        {
            this.Process = process;
            this.Port = port;
        }

        public System.Diagnostics.Process Process { get; set; }
        public int Port { get; set; }

        public void Kill()
        {
            this.Process.Kill();
        }
    }


    //https://stackoverflow.com/questions/43431196/c-sharp-tcp-ip-simple-chat-with-multiple-clients
    class Program
    {

        private static int minLimit = 1;
        private static int maxLimit = 5;
        private static List<SapProcess> SapProcesses { get; set; } = new List<SapProcess>();

        public static int GetNumberOfDesiredInstances()
        {
            Console.Write("Please enter how many SAP instances you would like to make available: ");

            int numInstances = -1;
            bool validAnswer = false;
            while (!validAnswer)
            {
                var input = Console.ReadLine();
                bool successfulParse = int.TryParse(input, out numInstances);
                if (successfulParse && (numInstances >= minLimit && numInstances <= maxLimit))
                {
                    validAnswer = true;
                }
                else
                {
                    Console.WriteLine("The input must be an integer between 1 and 5. Please try again.");
                }
            }
            return numInstances;
        }


        public static List<SapProcess> StartSapInstances(int count)
        {
            List<SapProcess> sapProcesses = new List<SapProcess>();
            int lastPort = 11650; //default SAP port
            for (int i = 0; i < count; i++)
            {
                int port = GetAvailablePort(lastPort);
                var sapProcess = StartSapInstance(port);
                sapProcesses.Add(sapProcess);
                lastPort++;
            }
            return sapProcesses;
        }

        static bool ConsoleEventCallback(int eventType)
        {
            if (eventType == 2)
            {
                Console.WriteLine("Closing down SAP instances");
                foreach (SapProcess process in Program.SapProcesses)
                {
                    process.Kill();
                }
            }
            return false;
        }

        static ConsoleEventDelegate handler;   // Keeps it from getting garbage collected
                                               // Pinvoke
        private delegate bool ConsoleEventDelegate(int eventType);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetConsoleCtrlHandler(ConsoleEventDelegate callback, bool add);

        public static SapProcess StartSapInstance(int port)
        {

            //"C:\Program Files\Computers and Structures\SAP2000 22\CSiAPIService.exe"
            var p = new System.Diagnostics.Process();
            p.StartInfo.FileName = @"C:\Program Files\Computers and Structures\SAP2000 22\CSiAPIService.exe";
            p.StartInfo.Arguments = $"-p {port}"; //"/c echo Foo && echo Bar";
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.CreateNoWindow = true;

            p.OutputDataReceived += new DataReceivedEventHandler(OutputHandler);
            p.ErrorDataReceived += new DataReceivedEventHandler(OutputHandler);
            //* Start process and handlers
            p.Start();
            p.BeginOutputReadLine();
            Thread.Sleep(500); //let it prep for some time.
            //p.BeginErrorReadLine();

            //p.StandardOutput.ReadToEnd()
            //p.StandardOutput.ReadToEnd().Dump();
            SapProcess sapProcess = new SapProcess(p, port);
            return sapProcess;
        }

        static void OutputHandler(object sendingProcess, DataReceivedEventArgs outLine)
        {
            //* Do your stuff with the output (write to console/log/StringBuilder)
            Console.WriteLine(outLine.Data);
        }

        public static int GetAvailablePort(int startingPort)
        {
            var properties = IPGlobalProperties.GetIPGlobalProperties();

            //getting active connections
            var tcpConnectionPorts = properties.GetActiveTcpConnections()
                                .Where(n => n.LocalEndPoint.Port >= startingPort)
                                .Select(n => n.LocalEndPoint.Port);

            //getting active tcp listners - WCF service listening in tcp
            var tcpListenerPorts = properties.GetActiveTcpListeners()
                                .Where(n => n.Port >= startingPort)
                                .Select(n => n.Port);

            //getting active udp listeners
            var udpListenerPorts = properties.GetActiveUdpListeners()
                                .Where(n => n.Port >= startingPort)
                                .Select(n => n.Port);

            var port = Enumerable.Range(startingPort, ushort.MaxValue)
                .Where(i => !tcpConnectionPorts.Contains(i))
                .Where(i => !tcpListenerPorts.Contains(i))
                .Where(i => !udpListenerPorts.Contains(i))
                .FirstOrDefault();

            return port;
        }



        static void Main(string[] args)
        {
            Console.WriteLine("Starting Sapellite server");
            //AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit; //new EventHandler(CurrentDomain_ProcessExit);
            handler = new ConsoleEventDelegate(ConsoleEventCallback);
            SetConsoleCtrlHandler(handler, true);

            var numInstances = GetNumberOfDesiredInstances();
            var sapInstances = StartSapInstances(numInstances);
            Program.SapProcesses.AddRange(sapInstances);
            
            //return;

            IPAddress ip = IPAddress.Parse("127.0.0.1");
            //IPAddress ip = IPAddress.Parse("10.1.12.126");

            int port = 5000;
            TcpClient client = new TcpClient();
            //client.
            try
            {
                client.Connect(ip, port);
            }
            catch (SocketException e)
            {
                Console.WriteLine("Failed to connect to orchestrator. Make sure you are on VPN and that the orchestrator is running. \n");
                Console.WriteLine($"Full error message: {e.Message}");
                Console.ReadKey();
                return;
            }
            //client.Connect()
            Console.WriteLine("client connected!!");
            NetworkStream ns = client.GetStream();
            Thread thread = new Thread(o => ReceiveData((TcpClient)o));

            thread.Start(client);

            string s;
            while (!string.IsNullOrEmpty((s = Console.ReadLine())))
            {
                byte[] buffer = Encoding.ASCII.GetBytes(s);
                ns.Write(buffer, 0, buffer.Length);
            }

            client.Client.Shutdown(SocketShutdown.Send);
            thread.Join();
            ns.Close();
            client.Close();
            Console.WriteLine("disconnect from server!!");
            Console.ReadKey();
        }

        static void ReceiveData(TcpClient client)
        {
            NetworkStream ns = client.GetStream();
            byte[] receivedBytes = new byte[1024];
            int byte_count;

            while ((byte_count = ns.Read(receivedBytes, 0, receivedBytes.Length)) > 0)
            {
                string rawMessage = Encoding.ASCII.GetString(receivedBytes, 0, byte_count);
                TcpMessage msg = JsonConvert.DeserializeObject<TcpMessage>(rawMessage);

                HandleServerMessage(client, msg);

                //Console.Write();
            }
        }

        private static void HandleServerMessage(TcpClient client, TcpMessage msg)
        {
            switch (msg.Name)
            {
                case MsgNames.RequestIdentification:
                    HandleRequestIdentification(client);
                    break;
                default:
                    break;
            }
        }

        //https://stackoverflow.com/questions/6803073/get-local-ip-address
        public static string GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            throw new Exception("No network adapters with an IPv4 address in the system!");
        }

        private static void HandleRequestIdentification(TcpClient client)
        {
            List<int> activePorts = Program.SapProcesses.Select(sp => sp.Port).ToList();

            ResponseIdentification response = new ResponseIdentification()
            {
                UserName = Environment.UserName,
                Role = MsgNames.ServerRole,
                Ports = activePorts,
                MachineName = Environment.MachineName,
                IpAdress = GetLocalIPAddress()
            };

            JsonConvert.SerializeObject(response);

            TcpMessage msg = new TcpMessage(MsgNames.ResponseIdentification)
            {
                Content = JsonConvert.SerializeObject(response)
            };

            var json = JsonConvert.SerializeObject(msg);
            NetworkStream ns = client.GetStream();
            byte[] buffer = Encoding.ASCII.GetBytes(json);
            ns.Write(buffer, 0, buffer.Length);
        }
    }
}