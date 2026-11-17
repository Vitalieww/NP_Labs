using System.Net;
using System.Net.Sockets;
using System.Text;
using lab_2.Protocol;
using Newtonsoft.Json.Linq;
using System.Collections.Concurrent;

namespace lab_2.Provider
{
    public class CoffeeShopProvider
    {
        public MessageFramer Framer;

        public CoffeeShopProvider(string ip = "127.0.0.1", int port = 5050)
        {
            Framer = CreateProviderFramer(ip, port);
        }


        private MessageFramer CreateProviderFramer(string ip, int port)
        {
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.Connect(new IPEndPoint(IPAddress.Parse(ip), port));
            MessageFramer messageFramer = new(socket);
            return messageFramer;
        }

        public async Task LogProvider()
        {
            NetworkMessage message = new()
            {
                Type = MessageTypes.RegisterProvider,
            };
            await Framer.SendMessageAsync(message);
        }

        public async Task WaitForAlert()
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

                    if (message.Type == MessageTypes.LowStockAlert)
                    {
                        LowStockAlert? lowStockAlert = message.Payload?.ToObject<LowStockAlert>();
                        Dictionary<string, int>? remainingStock = lowStockAlert?.RemainingStock;
                        if (remainingStock == null) break;
                        Dictionary<string, int> stockFill = [];
                        foreach (KeyValuePair<string, int> item in remainingStock)
                        {
                            if (item.Value < 5)
                            {
                                stockFill[item.Key] = 20;
                            }
                        }
                        ProviderDelivery delivery = new()
                        {
                            Products = stockFill
                        };
                        NetworkMessage sendRestockMessage = new()
                        {
                            Type = MessageTypes.ProviderDelivery,
                            Payload = JToken.FromObject(delivery)
                        };
                        await Framer.SendMessageAsync(sendRestockMessage);
                        Console.WriteLine($"[Provider] Low stock detected. Restocked items: {string.Join(", ", stockFill.Select(kvp => $"{kvp.Key}: +{kvp.Value}"))}");
                    }
                    else if (message.Type == MessageTypes.Error)
                    {
                        Error? err = message.Payload?.ToObject<Error>();
                        if (err != null)
                        {
                            Console.WriteLine($"[Provider] Error: {err.ErrorMessage}");
                        }
                    }
                }
            }
            catch (SocketException)
            {
                Console.WriteLine("Server disconnected!");
            }
            catch (ObjectDisposedException)
            {
                // The connection was closed.
            }
        }
    }
}