using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CORE.Saptellite.Library
{

    public static class MsgNames
    {
        public const string RequestIdentification = "RequestIdentification";
        public const string ResponseIdentification = "ResponseIdentification";

        public const string RequestDisconnection = "RequestDisconnection";
        public const string RequestServerInfo = "RequestServerInfo";
        public const string ResponseServerInfo = "ResponseServerInfo";
        
        public const string ClientRole = "Client";
        public const string ServerRole = "Server";

    }

    public class TcpMessage
    {
        public TcpMessage()
            :this("")
        {
        }

        public TcpMessage(string name)
        {
            this.Name = name;
        }

        public string Name { get; set; }

        public string Sender { get; set; }

        /// <summary>
        /// Json content
        /// </summary>
        public string Content { get; set; }
    }

    public class ResponseServerInfo
    {
        public ResponseServerInfo()
        {
            this.SapEndpoints = new List<SapEndpoint>();
        }
        public List<SapEndpoint> SapEndpoints { get; set; }
    }

    public class SapEndpoint
    {
        public string Machine { get; set; }
        public int Port { get; set; }

    }

    public class RequestDisconnection
    {
        public string Role { get; set; }
        public string MachineName { get; set; }
    }

    public class ResponseIdentification
    {
        public string Role { get; set; }

        public string UserName { get; set; }

        public string IpAdress { get; set; }

        public string MachineName { get; set; }

        public List<int> Ports { get; set; }
    }
}
