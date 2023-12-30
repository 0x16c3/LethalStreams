using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace SocketIOClient
{
    public class SocketIOResponse
    {
        public SocketIOResponse(IList<JObject> array, SocketIO socket)
        {
            _array = array;
            InComingBytes = new List<byte[]>();
            SocketIO = socket;
            PacketId = -1;
        }

        readonly IList<JObject> _array;

        public List<byte[]> InComingBytes { get; }
        public SocketIO SocketIO { get; }
        public int PacketId { get; set; }

        public JObject GetValue(int index = 0) => _array[index];

        public int Count => _array.Count;

        public override string ToString()
        {
            var builder = new StringBuilder();
            builder.Append('[');
            foreach (var item in _array)
            {
                builder.Append(item.ToString());
                if (_array.IndexOf(item) < _array.Count - 1)
                {
                    builder.Append(',');
                }
            }
            builder.Append(']');
            return builder.ToString();
        }
    }
}
