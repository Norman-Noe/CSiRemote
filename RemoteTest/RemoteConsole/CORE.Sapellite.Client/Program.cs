using CORE.Saptellite.Library;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace CORE.Sapellite.Client
{
    class Program
    {
        static ConsoleEventDelegate handler;   // Keeps it from getting garbage collected
                                               // Pinvoke
        private delegate bool ConsoleEventDelegate(int eventType);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetConsoleCtrlHandler(ConsoleEventDelegate callback, bool add);

        static bool ConsoleEventCallback(int eventType)
        {
            if (eventType == 2)
            {
                Utils.RequestDisconnection(client, MsgNames.ClientRole);
                //Thread.Sleep(1000);
                client?.Close();

            }
            return false;
        }
        static TcpClient client;
        static void Main(string[] args)
        {
            handler = new ConsoleEventDelegate(ConsoleEventCallback);
            SetConsoleCtrlHandler(handler, true);

            string configIp = Connection.ConnectAddress;
            IPAddress ip = IPAddress.Parse(configIp);
            int port = Connection.Port;
            client = new TcpClient();

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
            Console.WriteLine("Successfully connected to orchestrator.");
            NetworkStream ns = client.GetStream();
            Thread thread = new Thread(o => ReceiveData((TcpClient)o));

            thread.Start(client);

            string s;
            while (!string.IsNullOrEmpty((s = Console.ReadLine())))
            {
                s = CreateServerInfoRequest();
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

        private static string CreateServerInfoRequest()
        {
            TcpMessage msg = new TcpMessage(MsgNames.RequestServerInfo);
            return JsonConvert.SerializeObject(msg);
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
                case MsgNames.ResponseServerInfo:
                    HandleResponseServerInfo(client, msg);
                    break;
                default:
                    break;
            }
        }

        private static void HandleResponseServerInfo(TcpClient client, TcpMessage msg)
        {
            ResponseServerInfo info = JsonConvert.DeserializeObject<ResponseServerInfo>(msg.Content);

            StringBuilder builder = new StringBuilder();
            foreach (var ep in info.SapEndpoints)
            {
                builder.AppendLine($"{ep.Machine}:{ep.Port}");
            }
            Console.WriteLine(builder.ToString());
        }

        private static void HandleRequestIdentification(TcpClient client)
        {
            //List<int> activePorts = Program.SapProcesses.Select(sp => sp.Port).ToList();

            ResponseIdentification response = new ResponseIdentification()
            {
                UserName = Environment.UserName,
                Role = MsgNames.ClientRole,
                //Ports = activePorts,
                MachineName = Environment.MachineName,
                IpAdress = Utils.GetLocalIPAddress()
            };

            JsonConvert.SerializeObject(response);

            TcpMessage msg = new TcpMessage(MsgNames.ResponseIdentification)
            {
                Content = JsonConvert.SerializeObject(response)
            };

            Utils.SendMessage(client, msg);
            //var json = JsonConvert.SerializeObject(msg);
            //NetworkStream ns = client.GetStream();
            //byte[] buffer = Encoding.ASCII.GetBytes(json);
            //ns.Write(buffer, 0, buffer.Length);
        }


    }
}
