using System.Net;
using System.Net.Sockets;
using System.Text;
using lab_2.Protocol;
using Newtonsoft.Json.Linq;
using System.Collections.Concurrent;

namespace lab_2.Server
{
    public class ServerConnectionHandler(Inventory inventory, BlockingCollection<OrderJob> orderQueue)
    {
        private MessageFramer? _providerFramer;
        public async Task Handle(Socket clientSocket)
        {
            var framer = new MessageFramer(clientSocket);
            bool isProvider = false;

            while (true)
            {
                NetworkMessage? message = await framer.ReceiveMessageAsync();
                if (message == null)
                {
                    if (isProvider) //Provider disconnected
                    {
                        _providerFramer = null;
                        isProvider = false;
                        Console.WriteLine("Provider disconnected.");
                    }
                    // Client disconnected!
                    break;
                }

                if (message.Type == MessageTypes.MenuRequest)
                {
                    MenuResponse menuResponse = new()
                    {
                        Menu = inventory.GetStock()
                    };
                    NetworkMessage networkMessage = new()
                    {
                        Type = MessageTypes.MenuResponse,
                        Payload = JToken.FromObject(menuResponse)
                    };
                    await framer.SendMessageAsync(networkMessage);
                }

                if (message.Type == MessageTypes.OrderRequest)
                {
                    OrderRequest? orderRequest = message.Payload?.ToObject<OrderRequest>();
                    if (orderRequest == null) continue;
                    bool responseSuccess = inventory.TryDeduct(orderRequest.Order);
                    OrderResponse orderResponse = new();
                    NetworkMessage networkMessage = new()
                    {
                        Type = MessageTypes.OrderResponse
                    };
                    if (!responseSuccess)
                    {
                        orderResponse.Success = false;
                        orderResponse.ErrorMessage = "Order refused: Insufficient stock or invalid item";
                        orderResponse.RemainingStock = inventory.GetStock();
                        networkMessage.Payload = JToken.FromObject(orderResponse);
                        await framer.SendMessageAsync(networkMessage);
                        Console.WriteLine($"The menu: {string.Join(", ", orderResponse.RemainingStock.Select(kvp => $"{kvp.Key}: {kvp.Value}"))}");
                        Console.WriteLine($"Fail: Order -> {string.Join(", ", orderRequest.Order.Select(kvp => $"{kvp.Key}: {kvp.Value}"))}...{clientSocket.RemoteEndPoint}");
                    }
                    else
                    {
                        orderResponse.OrderId = inventory.GetId();
                        orderResponse.Success = true;
                        orderResponse.PreparationTime = inventory.PrepareOrder(orderRequest.Order);
                        orderResponse.RemainingStock = inventory.GetStock();
                        networkMessage.Payload = JToken.FromObject(orderResponse);
                        await framer.SendMessageAsync(networkMessage);
                        orderQueue.Add(new OrderJob
                        {
                            OrderId = orderResponse.OrderId,
                            PreparationTime = orderResponse.PreparationTime,
                            UserFramer = framer,
                            Items = orderRequest.Order
                        });
                    }
                    ;

                }

                if (message.Type == MessageTypes.RegisterProvider)
                {
                    _providerFramer = framer;
                    isProvider = true;
                }

                if (message.Type == MessageTypes.ProviderDelivery)
                {
                    if (isProvider)
                    {
                        ProviderDelivery? delivery = message.Payload?.ToObject<ProviderDelivery>();
                        Dictionary<string, int>? products = delivery?.Products;
                        inventory.TryAdd(products);
                    }
                    else
                    {
                        Error errorProviderMessage = new()
                        { ErrorMessage = "Not a provider!" };
                        NetworkMessage networkMessage = new()
                        {
                            Type = MessageTypes.Error,
                            Payload = JToken.FromObject(errorProviderMessage)
                        };
                        await framer.SendMessageAsync(networkMessage);
                    }
                }

            }
        }
    }
}