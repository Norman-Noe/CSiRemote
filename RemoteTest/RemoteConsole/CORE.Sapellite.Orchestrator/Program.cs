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

namespace CORE.Sapellite.Orchestrator
{

    public class SapelliteTcpClient
    {
        public TcpClient TcpClient { get; set; }
        public ResponseIdentification ResponseIdentification { get; set; }

        public SapelliteTcpClient(TcpClient tcpClient, ResponseIdentification id)
        {
            this.TcpClient = tcpClient;
            this.ResponseIdentification = id;
        }
    }


    //https://stackoverflow.com/questions/43431196/c-sharp-tcp-ip-simple-chat-with-multiple-clients
    class Program
    {
        static readonly object _lock = new object();
        static readonly Dictionary<int, TcpClient> list_clients = new Dictionary<int, TcpClient>();

        static readonly List<SapelliteTcpClient> SapelliteClients = new List<SapelliteTcpClient>();
        static readonly List<SapelliteTcpClient> SapelliteServers = new List<SapelliteTcpClient>();

        static void Main(string[] args)
        {

            Console.WriteLine("Launching Orchestrator");

            int count = 1;

            TcpListener ServerSocket = new TcpListener(IPAddress.Any, 5000);
            ServerSocket.Start();

            while (true)
            {
                TcpClient client = ServerSocket.AcceptTcpClient();
                lock (_lock) list_clients.Add(count, client);
                //Console.WriteLine("Someone connected!!");

                Thread t = new Thread(handle_clients);
                t.Start(count);
                count++;
                RequestIdentification(client);
            }
        }


        public static void RequestIdentification(TcpClient client)
        {
            TcpMessage m = new TcpMessage("RequestIdentification");
            
            broadcast(client, m);
        }

        public static void handle_clients(object o)
        {
            int id = (int)o;
            TcpClient client;

            lock (_lock) client = list_clients[id];

            while (true)
            {
                try
                {
                    NetworkStream stream = client.GetStream();
                    byte[] buffer = new byte[1024];

                    int byte_count = stream.Read(buffer, 0, buffer.Length);
                    if (byte_count == 0)
                    {
                        break;
                    }
                    string data = Encoding.ASCII.GetString(buffer, 0, byte_count);
                    TcpMessage msg = JsonConvert.DeserializeObject<TcpMessage>(data);
                    HandleServerMessage(client, msg);
                }
                catch (Exception e)
                {
                    client.Close();
                    Console.WriteLine(e.Message);
                    //throw e;
                }


            }

            lock (_lock) list_clients.Remove(id);
            client.Client.Shutdown(SocketShutdown.Both);
            client.Close();
        }

        private static void HandleServerMessage(TcpClient client, TcpMessage msg)
        {
            switch (msg.Name)
            {
                case MsgNames.ResponseIdentification:
                    HandleResponseIdentification(client, msg);
                    break;
                case MsgNames.RequestServerInfo:
                    HandleRequestServerInfo(client);
                    break;
                case MsgNames.RequestDisconnection:
                    HandleRequestDisconnection(client, msg);
                    break;
                default:
                    break;
            }
        }

        private static void HandleRequestDisconnection(TcpClient client, TcpMessage msg)
        {
            RequestDisconnection req = JsonConvert.DeserializeObject<RequestDisconnection>(msg.Content);

            if (req.Role == MsgNames.ClientRole)
            {
                SapelliteTcpClient node = Program.SapelliteClients.Where(sc => sc.ResponseIdentification.MachineName == req.MachineName).FirstOrDefault();
                if(node != null)
                {
                    Console.WriteLine($"Client {node.ResponseIdentification.MachineName} disconnected.");
                    Program.SapelliteClients.Remove(node);
                }
            }
            else if (req.Role == MsgNames.ServerRole)
            {
                SapelliteTcpClient node = Program.SapelliteServers.Where(sc => sc.ResponseIdentification.MachineName == req.MachineName).FirstOrDefault();
                if (node != null)
                {
                    Console.WriteLine($"Server {node.ResponseIdentification.MachineName} disconnected.");
                    Program.SapelliteServers.Remove(node);
                }
            }
        }

        private static void HandleRequestServerInfo(TcpClient client)
        {
            ResponseServerInfo info = new ResponseServerInfo();
            
            foreach (var sap in Program.SapelliteServers.Select(ss => ss.ResponseIdentification))
            {
                foreach (var port in sap.Ports)
                {
                    SapEndpoint ep = new SapEndpoint()
                    {
                        Machine = sap.MachineName,
                        Port = port
                    };
                    info.SapEndpoints.Add(ep);
                }
            }

            TcpMessage msg = new TcpMessage(MsgNames.ResponseServerInfo);
            msg.Content = JsonConvert.SerializeObject(info);

            broadcast(client, msg);


            //throw new NotImplementedException();
        }

        private static void HandleResponseIdentification(TcpClient client, TcpMessage msg)
        {
            var id = JsonConvert.DeserializeObject<ResponseIdentification>(msg.Content);
            var newConnection = new SapelliteTcpClient(client, id);

            if (id.Role == MsgNames.ServerRole)
            {
                SapelliteServers.Add(newConnection);
                //Console.WriteLine($"New {id.Role} connected: {id.UserName}");
                StringBuilder builder = new StringBuilder();
                builder.AppendLine($"New {id.Role} connected:");
                builder.AppendLine($"User:    {id.UserName}");
                builder.AppendLine($"IP:      {id.IpAdress}");
                builder.AppendLine($"Ports:   {string.Join(",", id.Ports)}");
                builder.AppendLine($"Machine: {id.MachineName}");
                Console.WriteLine(builder.ToString());
            }
            else if (id.Role == MsgNames.ClientRole)
            {
                SapelliteClients.Add(newConnection);
                StringBuilder builder = new StringBuilder();
                builder.AppendLine($"New {id.Role} connected:");
                builder.AppendLine($"User:    {id.UserName}");
                builder.AppendLine($"IP:      {id.IpAdress}");
                //builder.AppendLine($"Ports:   {string.Join(",", id.Ports)}");
                builder.AppendLine($"Machine: {id.MachineName}");
                Console.WriteLine(builder.ToString());
            }
        }


        public static void broadcast(TcpClient client, TcpMessage data)
        {
            var json = JsonConvert.SerializeObject(data);

            byte[] buffer = Encoding.ASCII.GetBytes(json);//data + Environment.NewLine);

            lock (_lock)
            {
                NetworkStream stream = client.GetStream();
                stream.Write(buffer, 0, buffer.Length);
            }
        }

        
        public static void broadcast(string data)
        {
            byte[] buffer = Encoding.ASCII.GetBytes(data + Environment.NewLine);

            lock (_lock)
            {
                foreach (TcpClient c in list_clients.Values)
                {
                    NetworkStream stream = c.GetStream();
                    stream.Write(buffer, 0, buffer.Length);
                }
            }
        }

    }
}
