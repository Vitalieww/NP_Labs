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
            var cts = new CancellationTokenSource();
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
                    cts.Cancel();
                    break;
                }
                else
                {
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

                    else if (message.Type == MessageTypes.OrderRequest)
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
                                Items = orderRequest.Order,
                                CancelToken = cts.Token
                            });
                            //Now check if there are any items < 5
                            await CheckLowStock();
                        }
                        ;

                    }

                    else if (message.Type == MessageTypes.RegisterProvider)
                    {
                        _providerFramer = framer;
                        isProvider = true;
                        await CheckLowStock();
                    }

                    else if (message.Type == MessageTypes.ProviderDelivery)
                    {
                        if (isProvider)
                        {
                            ProviderDelivery? delivery = message.Payload?.ToObject<ProviderDelivery>();
                            Dictionary<string, int>? products = delivery?.Products;
                            if (products == null) break;
                            inventory.TryAdd(products);
                            Console.WriteLine($"[Delivery] Restocked products from {clientSocket.RemoteEndPoint}");
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
                    else
                    {
                        Error invalidCommand = new()
                        {
                            ErrorMessage = "Unknown command!"
                        };
                        NetworkMessage networkMessage = new()
                        {
                            Type = MessageTypes.Error,
                            Payload = JToken.FromObject(invalidCommand)
                        };
                        await framer.SendMessageAsync(networkMessage);
                    }
                }
            }
        }

        private async Task CheckLowStock()
        {
            //Now check if there are any items < 5
            foreach (KeyValuePair<string, int> item in inventory.GetStock())
            {
                if (item.Value < 5)
                {
                    if (_providerFramer != null)
                    {
                        LowStockAlert lowStockAlert = new()
                        {
                            RemainingStock = inventory.GetStock()
                        };
                        NetworkMessage messageLowStock = new()
                        {
                            Type = MessageTypes.LowStockAlert,
                            Payload = JToken.FromObject(lowStockAlert)
                        };
                        await _providerFramer.SendMessageAsync(messageLowStock);
                        break;
                    }
                }
            }
        }
    }
}