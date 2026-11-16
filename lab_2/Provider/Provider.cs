using System.Net;
using System.Net.Sockets;
using System.Text;
using lab_2.Protocol;
using Newtonsoft.Json.Linq;
using System.Collections.Concurrent;

namespace lab_2.Provider
{
    public class Provider
    {
        public MessageFramer Framer;

        public Provider()
        {
            Framer = CreateProviderFramer();
        }


        public MessageFramer CreateProviderFramer()
        {
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.Connect(new IPEndPoint(IPAddress.Loopback, 5050));
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
            while (true)
            {
                NetworkMessage? message = await Framer.ReceiveMessageAsync();
                if (message == null)
                {
                    // Server disconnected!
                    break;
                }

                if (message.Type == MessageTypes.LowStockAlert)
                {
                    Dictionary<string, int> remainingStock = message.Payload.ToObject<LowStockAlert>().RemainingStock;
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
                }
            }
        }
    }
}