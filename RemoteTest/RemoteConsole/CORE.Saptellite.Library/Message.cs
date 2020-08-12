﻿using System;
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
    
    public class ResponseIdentification
    {
        public string Role { get; set; }

        public string UserName { get; set; }

        public int[] Ports { get; set; }
    }
}
