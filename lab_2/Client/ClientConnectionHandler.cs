using System.Net;
using System.Net.Sockets;
using System.Text;
using lab_2.Protocol;
using Newtonsoft.Json.Linq;
using System.Collections.Concurrent;

namespace lab_2.Client
{
    public class ClientConnectionHandler
    {
        public async Task Handle(MessageFramer framer, int clientId, TaskCompletionSource buzzer)
        {
            while (true)
            {
                NetworkMessage? message = await framer.ReceiveMessageAsync();
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
                            MenuResponse payload = message.Payload.ToObject<MenuResponse>();
                            Console.WriteLine($"[Client #{clientId}] Current menu: {string.Join(", ", payload.Menu.Select(kvp => $"{kvp.Key}: {kvp.Value}"))} ");
                            break;
                        }
                    case MessageTypes.OrderResponse:
                        {
                            OrderResponse payload = message.Payload.ToObject<OrderResponse>();
                            if (payload.Success)
                            {
                                Console.WriteLine($"[Client #{clientId}] Order #{payload.OrderId} time ({payload.PreparationTime}s)");
                            }
                            else
                            {
                                Console.WriteLine($"[Client #{clientId}] {payload.ErrorMessage}");
                                Console.WriteLine($"[Client #{clientId}] remaining stock: {string.Join(", ", payload.RemainingStock.Select(kvp => $"{kvp.Key}: {kvp.Value}"))}");
                                buzzer.SetResult();
                            }
                            break;
                        }
                    case MessageTypes.OrderReady:
                        {
                            OrderReady payload = message.Payload.ToObject<OrderReady>();
                            Console.WriteLine($"[Client #{clientId}] {payload.OrderDoneMessage}");
                            buzzer.SetResult();
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