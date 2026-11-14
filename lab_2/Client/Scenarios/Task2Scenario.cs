using System.Net;
using System.Net.Sockets;
using lab_2.Protocol;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace lab_2.Client
{
    public class Task2
    {
        public async Task Run()
        {
            List<Task> clientTasks = [];
            int clientCount = Environment.ProcessorCount + 4;
            for (int i = 1; i <= clientCount; i++)
            {
                int clientId = i;
                clientTasks.Add(Task.Run(async () =>
                {
                    MessageFramer messageFramer = Client.CreateClientFramer();
                    var buzzer = new TaskCompletionSource();
                    ClientConnectionHandler connectionHandler = new();
                    _ = Task.Run(() => connectionHandler.Handle(messageFramer, clientId, buzzer));
                    // await Client.RequestMenu(messageFramer);
                    await Client.MakeOrder(messageFramer, CreateRandomOrder());
                    await Client.RequestMenu(messageFramer);
                    await buzzer.Task;
                }));
            }
            await Task.WhenAll(clientTasks);
        }


        private Dictionary<string, int> CreateRandomOrder()
        {
            Dictionary<string, int> order = new();
            int coffeeAmount = Random.Shared.Next(0, 4);
            int teaAmount = Random.Shared.Next(0, 4);
            int croissantAmount = Random.Shared.Next(1, 4);
            if (coffeeAmount > 0)
            {
                order.Add("Coffee", coffeeAmount);
            }
            if (teaAmount > 0)
            {
                order.Add("Tea", teaAmount);
            }
            order.Add("Croissant", croissantAmount);

            return order;
        }
    }
}