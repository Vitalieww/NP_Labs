using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace lab_2.Protocol
{
    // The Envelope that wraps every message sent over the wire
    public class NetworkMessage
    {
        [JsonProperty("t")]
        public string Type { get; set; } = string.Empty;

        [JsonProperty("p")]
        public JToken? Payload { get; set; }
    }

    public class MessageFramer(Socket socket)
    {
        private readonly Socket _socket = socket;
        private StringBuilder _stringBuffer = new();

        public void SendMessage(NetworkMessage msg)
        {
            string messageString = JsonConvert.SerializeObject(msg);
            messageString = string.Concat(messageString, "\n");
            byte[] messageBytes = Encoding.UTF8.GetBytes(messageString);
            _socket.Send(messageBytes);
        }

        public NetworkMessage? ReceiveMessage()
        {
            byte[] byteBuffer = new byte[1024];
            string message;
            while (true)
            {
                int index = CheckForNewLine();
                if (index != -1)
                {
                    message = _stringBuffer.ToString(0, index);
                    NetworkMessage npMessage = JsonConvert.DeserializeObject<NetworkMessage>(message);
                    _stringBuffer = _stringBuffer.Remove(0, index + 1);
                    return npMessage;
                }
                int bytesRead = _socket.Receive(byteBuffer);
                if (bytesRead == 0)
                {
                    return null;
                }
                _stringBuffer.Append(Encoding.UTF8.GetString(byteBuffer, 0, bytesRead));
            }

        }
        private int CheckForNewLine()
        {
            int index = -1;
            // Search for '\n' in the StringBuilder
            for (int i = 0; i < _stringBuffer.Length; i++)
            {
                if (_stringBuffer[i] == '\n')
                {
                    index = i;
                    break;
                }
            }
            return index;
        }
    }

    public static class MessageTypes
    {
        public const string MenuRequest = "MENU_REQ";
        public const string MenuResponse = "MENU_RES";
        public const string OrderRequest = "ORDER_REQ";
        public const string OrderResponse = "ORDER_RES";
        public const string OrderReady = "ORDER_RED";
        public const string ProviderDelivery = "PROV_DELIV";
        public const string LowStockAlert = "LOW_STOCK";

    }
    public class OrderRequest
    {
        public Dictionary<string, int> Order { get; set; } = [];
    }

    public class MenuResponse
    {
        public Dictionary<string, int> Menu { get; set; } = [];

    }

    public class OrderResponse : Error
    {
        public bool Success;
        public int OrderId;
        public int PreparationTime;
        public Dictionary<string, int> RemainingStock { get; set; } = [];
    }

    public class OrderReady
    {
        public int OrderId;
        public string OrderDoneMessage;
    }

    public class ProviderDelivery
    {
        public Dictionary<string, int> Products { get; set; } = [];

    }

    public class LowStockAlert
    {
        public Dictionary<string, int> RemainingStock { get; set; } = [];
    }

    public class Error
    {
        public string ErrorMessage;
    }
}