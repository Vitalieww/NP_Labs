using System.Net;
using System.Net.Sockets;
using System.Text;
using lab_2.Protocol;
using Newtonsoft.Json.Linq;

namespace lab_2.Server
{
    public class ConnectionHandler(Inventory inventory)
    {
        public async Task Handle(Socket clientSocket)
        {
            var framer = new MessageFramer(clientSocket);

            while (true)
            {
                NetworkMessage? message = framer.ReceiveMessage();
                if (message == null)
                {
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
                    framer.SendMessage(networkMessage);
                }

                if (message.Type == MessageTypes.OrderRequest)
                {
                    OrderRequest orderRequest = message.Payload.ToObject<OrderRequest>();
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
                        framer.SendMessage(networkMessage);
                        Console.WriteLine($"Fail: Order -> {orderRequest.Order}...{clientSocket.RemoteEndPoint}");
                    }
                    else
                    {
                        orderResponse.OrderId = inventory.GetId();
                        orderResponse.Success = true;
                        orderResponse.PreparationTime = inventory.PrepareOrder(orderRequest.Order);
                        orderResponse.RemainingStock = inventory.GetStock();
                        networkMessage.Payload = JToken.FromObject(orderResponse);
                        framer.SendMessage(networkMessage);
                        Console.WriteLine($"Success: Order {orderResponse.OrderId}, prep time:{orderResponse.PreparationTime}...{clientSocket.RemoteEndPoint}");
                        _ = Task.Run(async () =>
                        {
                            await Task.Delay(orderResponse.PreparationTime * 1000);
                            OrderReady orderReady = new()
                            {
                                OrderId = orderResponse.OrderId,
                            };
                            orderReady.OrderDoneMessage = $"Order with orderID {orderReady.OrderId}";
                            NetworkMessage messageOrderReady = new()
                            {
                                Type = MessageTypes.OrderReady,
                                Payload = JToken.FromObject(orderReady)
                            };
                            framer.SendMessage(messageOrderReady);
                            Console.WriteLine($"Order {orderReady.OrderId} is ready. ... {clientSocket.RemoteEndPoint}");
                        });
                    }

                }

                if (message.Type == MessageTypes.ProviderDelivery)
                { }

            }
        }
    }
}