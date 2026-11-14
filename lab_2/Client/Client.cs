using System.Net;
using System.Net.Sockets;
using System.Text;
using lab_2.Protocol;
using Newtonsoft.Json.Linq;
using System.Collections.Concurrent;

namespace lab_2.Client
{
    public class Client
    {
        public static MessageFramer CreateClientFramer()
        {
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.Connect(new IPEndPoint(IPAddress.Loopback, 5050));
            MessageFramer messageFramer = new(socket);
            return messageFramer;

        }

        public static async Task RequestMenu(MessageFramer messageFramer)
        {
            NetworkMessage networkMessage = new()
            {
                Type = MessageTypes.MenuRequest
            };
            await messageFramer.SendMessageAsync(networkMessage);
        }

        public static async Task MakeOrder(MessageFramer messageFramer, Dictionary<string, int> order)
        {
            OrderRequest orderRequest = new()
            {
                Order = order
            };
            NetworkMessage networkMessageOrderRequest = new()
            {
                Type = MessageTypes.OrderRequest,
                Payload = JToken.FromObject(orderRequest)
            };
            await messageFramer.SendMessageAsync(networkMessageOrderRequest);
        }
    }
}