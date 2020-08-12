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

namespace CORE.Sapellite.Client
{
    //https://stackoverflow.com/questions/43431196/c-sharp-tcp-ip-simple-chat-with-multiple-clients
    class Program
    {
        static void Main(string[] args)
        {
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

        private static void HandleRequestIdentification(TcpClient client)
        {
            ResponseIdentification response = new ResponseIdentification()
            {
                UserName = Environment.UserName,
                Role = MsgNames.ServerRole,
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