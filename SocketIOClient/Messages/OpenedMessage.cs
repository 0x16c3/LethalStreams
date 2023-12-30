using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using SocketIOClient.Transport;

namespace SocketIOClient.Messages
{
    public class OpenedMessage : IMessage
    {
        public MessageType Type => MessageType.Opened;

        public string Sid { get; set; }

        public string Namespace { get; set; }

        public List<string> Upgrades { get; private set; }

        public int PingInterval { get; private set; }

        public int PingTimeout { get; private set; }

        public List<byte[]> OutgoingBytes { get; set; }

        public List<byte[]> IncomingBytes { get; set; }

        public int BinaryCount { get; }

        public int Eio { get; set; }

        public TransportProtocol Protocol { get; set; }
        
        public void Read(string msg)
        {
            JObject doc = JObject.Parse(msg);
            var root = doc.Root;
            Sid = root.SelectToken("sid").Value<string>();

            // get int32 value from json element using newtonsoft.json
            PingInterval = root.SelectToken("pingInterval").Value<int>();
            PingTimeout = root.SelectToken("pingTimeout").Value<int>();
            
            //PingInterval = GetInt32FromJsonElement(root, msg, "pingInterval");
            //PingTimeout = GetInt32FromJsonElement(root, msg, "pingTimeout");

            Upgrades = new List<string>();
            var upgradesArray = JArray.Parse(root.SelectToken("upgrades").ToString());
            foreach (var item in upgradesArray)
            {
                Upgrades.Add(item.Value<string>());
            }
        }

        public string Write()
        {
            //var builder = new StringBuilder();
            //builder.Append("40");
            //if (!string.IsNullOrEmpty(Namespace))
            //{
            //    builder.Append(Namespace).Append(',');
            //}
            //return builder.ToString();
            throw new NotImplementedException();
        }
    }
}
