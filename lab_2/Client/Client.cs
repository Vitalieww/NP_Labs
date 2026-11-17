using System.Net;
using System.Net.Sockets;
using System.Text;
using lab_2.Protocol;
using Newtonsoft.Json.Linq;
using System.Collections.Concurrent;

namespace lab_2.Client
{
    public class CoffeeShopClient
    {
        public MessageFramer Framer;
        private readonly int _clientId;
        public TaskCompletionSource OrderBuzzer { get; } = new();
        public CoffeeShopClient(int id)
        {
            Framer = CreateClientFramer();
            _clientId = id;
        }
        private MessageFramer CreateClientFramer()
        {
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.Connect(new IPEndPoint(IPAddress.Loopback, 5050));
            MessageFramer messageFramer = new(socket);
            return messageFramer;

        }

        public async Task RequestMenu()
        {
            NetworkMessage networkMessage = new()
            {
                Type = MessageTypes.MenuRequest
            };
            await Framer.SendMessageAsync(networkMessage);
        }
        public async Task MakeOrder(Dictionary<string, int> order)
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
            await Framer.SendMessageAsync(networkMessageOrderRequest);
        }

        public async Task ListenAsync()
        {
            while (true)
            {
                NetworkMessage? message = await Framer.ReceiveMessageAsync();
                if (message == null)
                {
                    // disconnected!
                    Console.WriteLine("Server disconnected!");
                    break;
                }
                switch (message.Type)
                {
                    case MessageTypes.MenuResponse:
                        {
                            MenuResponse? payload = message.Payload?.ToObject<MenuResponse>();
                            if (payload == null) break;
                            Console.WriteLine($"[Client #{_clientId}] Current menu: {string.Join(", ", payload.Menu.Select(kvp => $"{kvp.Key}: {kvp.Value}"))} ");
                            break;
                        }
                    case MessageTypes.OrderResponse:
                        {
                            OrderResponse? payload = message.Payload?.ToObject<OrderResponse>();
                            if (payload == null) break;
                            if (payload.Success)
                            {
                                Console.WriteLine($"[Client #{_clientId}] Order #{payload.OrderId} time ({payload.PreparationTime}s)");
                            }
                            else
                            {
                                Console.WriteLine($"[Client #{_clientId}] {payload.ErrorMessage}");
                                Console.WriteLine($"[Client #{_clientId}] remaining stock: {string.Join(", ", payload.RemainingStock.Select(kvp => $"{kvp.Key}: {kvp.Value}"))}");
                                OrderBuzzer.SetResult();
                            }
                            break;
                        }
                    case MessageTypes.OrderReady:
                        {
                            OrderReady? payload = message.Payload?.ToObject<OrderReady>();
                            if (payload == null) break;
                            Console.WriteLine($"[Client #{_clientId}] {payload.OrderDoneMessage}");
                            OrderBuzzer.SetResult();
                            break;
                        }
                    default:
                        {
                            Console.WriteLine("Unrecognized message type!");
                            break;
                        }
                }

            }
        }
    }
}