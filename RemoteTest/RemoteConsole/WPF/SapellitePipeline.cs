using CORE.Saptellite.Library;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WPF
{
    public class SapellitePipeline
    {
        private TcpClient client;
        private Thread thread;
        private bool responseReceived;
        private List<MachineLocation> _machines;

        public SapellitePipeline()
        {
            Initialize();
            this._machines = new List<MachineLocation>();
        }

        public void Initialize()
        {
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
                //TODO: ERROR MESSAGE
                //Failed to connect to orchestrator. Make sure you are on VPN and that the orchestrator is running.
                throw new Exception();
            }
            //TODO: "Successfully connected to orchestrator."

            //NetworkStream ns = client.GetStream();
            this.thread = new Thread(o => ReceiveData((TcpClient)o));

            thread.Start(client);

            //string s;
            //while (!string.IsNullOrEmpty((s = Console.ReadLine())))
            //{
            //    s = CreateServerInfoRequest();
            //    byte[] buffer = Encoding.ASCII.GetBytes(s);
            //    ns.Write(buffer, 0, buffer.Length);
            //}

            //client.Client.Shutdown(SocketShutdown.Send);
            //thread.Join();
            //ns.Close();
            //client.Close();

            //Console.WriteLine("disconnect from server!!");
            //Console.ReadKey();
        }

        public void ShutDown()
        {
            Utils.RequestDisconnection(client, MsgNames.ClientRole);

            NetworkStream ns = client.GetStream();
            client.Client.Shutdown(SocketShutdown.Send);
            thread.Join();
            ns.Close();
            client.Close();
        }

        public List<MachineLocation> GetAvailableMachines()
        {
            responseReceived = false;
            this._machines.Clear();

            NetworkStream ns = client.GetStream();
            string s = CreateServerInfoRequest();
            byte[] buffer = Encoding.ASCII.GetBytes(s);
            ns.Write(buffer, 0, buffer.Length);

            int bailout = 10000;  //10 sec
            int step = 100;
            int current = 0;
            while(!responseReceived)
            {
                Thread.Sleep(step);
                current += step;
                if(current > bailout)
                {
                    break;
                }
            }
            return _machines;
        }

        private void ReceiveData(TcpClient client)
        {
            NetworkStream ns = client.GetStream();
            byte[] receivedBytes = new byte[1024];
            int byte_count;

            while ((byte_count = ns.Read(receivedBytes, 0, receivedBytes.Length)) > 0)
            {
                string rawMessage = Encoding.ASCII.GetString(receivedBytes, 0, byte_count);
                TcpMessage msg = JsonConvert.DeserializeObject<TcpMessage>(rawMessage);

                HandleServerMessage(client, msg);
            }
        }

        private static string CreateServerInfoRequest()
        {
            TcpMessage msg = new TcpMessage(MsgNames.RequestServerInfo);
            return JsonConvert.SerializeObject(msg);
        }

        private void HandleServerMessage(TcpClient client, TcpMessage msg)
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

        private void HandleResponseServerInfo(TcpClient client, TcpMessage msg)
        {
            ResponseServerInfo info = JsonConvert.DeserializeObject<ResponseServerInfo>(msg.Content);

            StringBuilder builder = new StringBuilder();
            foreach (var ep in info.SapEndpoints)
            {
                this._machines.Add(new MachineLocation(ep.Machine, ep.Port));
            }
            this.responseReceived = true;
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
        }
    }
}
