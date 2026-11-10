using System.Net;
using System.Net.Sockets;
using System.Text;
using lab_2.Protocol;
using Newtonsoft.Json.Linq;


namespace lab_2.Client
{
    public class Task1
    {
        public static void Run()
        {
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.Connect(new IPEndPoint(IPAddress.Loopback, 5050));
            MessageFramer messageFramer = new(socket);
            NetworkMessage networkMessage = new()
            {
                Type = MessageTypes.MenuRequest
            };
            messageFramer.SendMessage(networkMessage);
            NetworkMessage messageResponse = messageFramer.ReceiveMessage();
            MenuResponse menuResponse = messageResponse.Payload.ToObject<MenuResponse>();
            foreach (var kvp in menuResponse.Menu)
            {
                Console.WriteLine($"Key: {kvp.Key}, Value: {kvp.Value}");
            }

            OrderRequest orderRequest = new()
            {
                Order =
                {
                    ["Coffee"] = 1,
                    ["Croissant"] = 2
                }
            };
            NetworkMessage networkMessageOrderRequest = new()
            {
                Type = MessageTypes.OrderRequest,
                Payload = JToken.FromObject(orderRequest)
            };
            messageFramer.SendMessage(networkMessageOrderRequest);
            Console.WriteLine(messageFramer.ReceiveMessage().Payload);
            Console.WriteLine(messageFramer.ReceiveMessage().Payload);


        }
    }
}