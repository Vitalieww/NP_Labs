using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using lab_2.Protocol;
using System.Collections.Concurrent;
using Newtonsoft.Json.Linq;

namespace lab_2.Server
{
    public class CoffeeShopServer
    {

        const int PORT = 5050;
        private readonly Inventory _inventory;
        private readonly ServerConnectionHandler _connectionHandler;
        private readonly BlockingCollection<OrderJob> _orderQueue = new();

        public CoffeeShopServer()
        {
            _inventory = new Inventory();
            _connectionHandler = new ServerConnectionHandler(_inventory, _orderQueue);

        }
        public async Task RunListener()
        {
            Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listener.Bind(new IPEndPoint(IPAddress.Loopback, PORT));
            listener.Listen();
            int workerCount = Environment.ProcessorCount;
            for (int i = 0; i < workerCount; i++)
            {
                int workerId = i + 1;
                _ = Task.Run(() => WorkerLoop(workerId));
            }
            while (true)
            {
                Socket clientSocket = listener.Accept();
                Console.WriteLine($"Client connected from {clientSocket.RemoteEndPoint}");
                _ = Task.Run(() => _connectionHandler.Handle(clientSocket));
            }
        }

        private async Task WorkerLoop(int workerId)
        {
            while (true)
            {
                OrderJob job = _orderQueue.Take();
                Console.WriteLine($"[Worker {workerId}] Preparing Order #{job.OrderId} ({job.PreparationTime}s)...");
                Thread.Sleep(job.PreparationTime * 1000);
                OrderReady orderReady = new()
                {
                    OrderId = job.OrderId,
                    OrderDoneMessage = $"Order #{job.OrderId} is ready!"
                };
                NetworkMessage message = new()
                {
                    Type = MessageTypes.OrderReady,
                    Payload = JToken.FromObject(orderReady)
                };
                if (job.UserFramer == null) break;
                await job.UserFramer.SendMessageAsync(message);
                Console.WriteLine($"[Worker {workerId}] Order #{job.OrderId} is ready and delivered!");
            }
        }


    }

    public class OrderJob
    {
        public int OrderId { get; set; }
        public int PreparationTime { get; set; }
        public MessageFramer? UserFramer { get; set; }
        public Dictionary<string, int> Items { get; set; } = [];
    }
}