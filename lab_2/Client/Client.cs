using System.Net;
using System.Net.Sockets;
using System.Text;
using lab_2.Protocol;
using Newtonsoft.Json.Linq;
using System.Collections.Concurrent;
using System.Globalization;

namespace lab_2.Client
{
    public class CoffeeShopClient
    {
        public MessageFramer Framer;
        private readonly int _clientId;
        public TaskCompletionSource OrderBuzzer { get; } = new();
        public CoffeeShopClient(int id) : this("127.0.0.1", 5050, id)
        {
        }

        public CoffeeShopClient(string ip, int port) : this(ip, port, 0)
        {
        }

        private CoffeeShopClient(string ip, int port, int id)
        {
            Framer = CreateClientFramer(ip, port);
            _clientId = id;
        }
        private MessageFramer CreateClientFramer(string ip, int port)
        {
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.Connect(new IPEndPoint(IPAddress.Parse(ip), port));
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
            try
            {
                while (true)
                {
                    NetworkMessage? message = await Framer.ReceiveMessageAsync();
                    if (message == null)
                    {
                        Console.WriteLine("Server disconnected!");
                        break;
                    }
                    string prefix = _clientId > 0 ? $"[Client #{_clientId}] " : "";
                    switch (message.Type)
                    {
                        case MessageTypes.MenuResponse:
                            {
                                MenuResponse? payload = message.Payload?.ToObject<MenuResponse>();
                                if (payload == null) break;
                                Console.WriteLine($"{prefix}Current menu: {string.Join(", ", payload.Menu.Select(kvp => $"{kvp.Key}: {kvp.Value}"))}");
                                break;
                            }
                        case MessageTypes.OrderResponse:
                            {
                                OrderResponse? payload = message.Payload?.ToObject<OrderResponse>();
                                if (payload == null) break;
                                if (payload.Success)
                                {
                                    Console.WriteLine($"{prefix}Order #{payload.OrderId} accepted (Estimated time: {payload.PreparationTime}s)");
                                }
                                else
                                {
                                    Console.WriteLine($"{prefix}{payload.ErrorMessage}");
                                    Console.WriteLine($"{prefix}Remaining stock: {string.Join(", ", payload.RemainingStock.Select(kvp => $"{kvp.Key}: {kvp.Value}"))}");
                                    OrderBuzzer.SetResult();
                                }
                                break;
                            }
                        case MessageTypes.OrderReady:
                            {
                                OrderReady? payload = message.Payload?.ToObject<OrderReady>();
                                if (payload == null) break;
                                Console.WriteLine($"{prefix}{payload.OrderDoneMessage}");
                                OrderBuzzer.SetResult();
                                break;
                            }
                        case MessageTypes.Error:
                            {
                                Error? payload = message.Payload?.ToObject<Error>();
                                if (payload == null) break;
                                Console.WriteLine($"{prefix}Error: {payload.ErrorMessage}");
                                break;
                            }
                        default:
                            Console.WriteLine($"{prefix}Unrecognized message type: {message.Type}");
                            break;
                    }
                }
            }
            catch (SocketException)
            {
                Console.WriteLine("Server disconnected!");
            }
            catch (ObjectDisposedException)
            {
                // The interactive loop closed the connection.
            }
        }

        public async Task RunInteractiveAsync()
        {
            try
            {
                while (true)
                {
                    Console.Write("> ");
                    string? input = await Console.In.ReadLineAsync();
                    if (input == null || input.Equals("exit", StringComparison.OrdinalIgnoreCase))
                    {
                        Framer.Disconnect();
                        return;
                    }

                    string[] parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length == 0)
                    {
                        continue;
                    }

                    if (parts.Length == 1 && parts[0].Equals("menu", StringComparison.OrdinalIgnoreCase))
                    {
                        await RequestMenu();
                        continue;
                    }

                    if (parts.Length >= 3 && parts[0].Equals("order", StringComparison.OrdinalIgnoreCase))
                    {
                        if (parts.Length % 2 == 0)
                        {
                            Console.WriteLine("Usage: order <item> <quantity> [item quantity...]");
                            continue;
                        }

                        Dictionary<string, int> order = new(StringComparer.OrdinalIgnoreCase);
                        bool valid = true;
                        for (int i = 1; i < parts.Length; i += 2)
                        {
                            if (!int.TryParse(parts[i + 1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int quantity) ||
                                quantity <= 0)
                            {
                                valid = false;
                                break;
                            }
                            order[parts[i]] = quantity;
                        }

                        if (valid)
                        {
                            await MakeOrder(order);
                        }
                        else
                        {
                            Console.WriteLine("Quantities must be positive integers.");
                        }
                        continue;
                    }

                    Console.WriteLine("Commands: menu, order <item> <quantity> [...], exit");
                }
            }
            catch (SocketException)
            {
                Console.WriteLine("Server disconnected!");
            }
            catch (ObjectDisposedException)
            {
                // Disconnected
            }
        }
        }
}