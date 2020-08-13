using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace CORE.Saptellite.Library
{
    public static class Utils
    {
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

        public static void SendMessage(TcpClient client, TcpMessage msg)
        {
            var json = JsonConvert.SerializeObject(msg);
            NetworkStream ns = client.GetStream();
            byte[] buffer = Encoding.ASCII.GetBytes(json);
            ns.Write(buffer, 0, buffer.Length);
        }

        public static void RequestDisconnection(TcpClient client, string role)
        {
            RequestDisconnection content = new RequestDisconnection()
            {
                MachineName = Environment.MachineName,
                Role = role
            };

            TcpMessage msg = new TcpMessage(MsgNames.RequestDisconnection)
            {
                Content = JsonConvert.SerializeObject(content)
            };
            SendMessage(client, msg);
        }

    }
}
