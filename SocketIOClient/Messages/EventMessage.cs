using System;
using SocketIOClient.Transport;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.Linq;
using Newtonsoft.Json;

namespace SocketIOClient.Messages
{
    public class EventMessage : IMessage
    {
        public MessageType Type => MessageType.EventMessage;

        public string Namespace { get; set; }

        public string Event { get; set; }

        public int Id { get; set; }

        public List<JObject> JsonElements { get; set; }

        public string Json { get; set; }

        public List<byte[]> OutgoingBytes { get; set; }

        public List<byte[]> IncomingBytes { get; set; }

        public int BinaryCount { get; }

        public int Eio { get; set; }

        public TransportProtocol Protocol { get; set; }

        public void Read(string msg)
        {
            // sanitize line breaks
            msg = msg.Replace("\n", "\\n").Replace("\r", "\\r");
            
            // sanitize empty characters
            msg = msg.Replace("\0", "");
            
            // deserialize json using JObject
            
            var doc = JArray.Parse(msg);
            
            // convert to list of json objects
            var root = doc.Root;
            Event = root[0].ToString();

            // the rest of the elements are json objects
            JsonElements = new List<JObject>();
            for (int i = 1; i < root.Count(); i++)
            {
                JsonElements.Add(root[i].Value<JObject>());
            }
        }

        public string Write()
        {
            var builder = new StringBuilder();
            builder.Append("42");
            if (!string.IsNullOrEmpty(Namespace))
            {
                builder.Append(Namespace).Append(',');
            }
            if (string.IsNullOrEmpty(Json))
            {
                builder.Append("[\"").Append(Event).Append("\"]");
            }
            else
            {
                string data = Json.Insert(1, $"\"{Event}\",");
                builder.Append(data);
            }
            return builder.ToString();
        }
    }
}
