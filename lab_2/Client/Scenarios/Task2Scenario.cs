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
                    CoffeeShopClient client = new(clientId);
                    _ = Task.Run(() => client.ListenAsync());
                    var buzzer = new TaskCompletionSource();
                    await client.MakeOrder(CreateRandomOrder());
                    await client.RequestMenu();
                    await client.OrderBuzzer.Task;
                }));
            }
            await Task.WhenAll(clientTasks);
            Console.WriteLine("Now running last croissant battle");
            await RunLastCroissantBattle();
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
        private async Task RunLastCroissantBattle()
        {
            // We made an order to have all the croissants in the menu
            CoffeeShopClient client = new(0);
            await client.RequestMenu();
            NetworkMessage? message = await client.Framer.ReceiveMessageAsync();
            MenuResponse? payload = message!.Payload?.ToObject<MenuResponse>();
            int croissantCount = payload!.Menu["Croissant"];
            Dictionary<string, int> order0 = new()
            { ["Croissant"] = croissantCount - 1 };
            await client.MakeOrder(order0);
            await client.RequestMenu();
            // ----------------------------------- 

            //Run 2 clients at same time to fight over last croissant
            var startingGate = new TaskCompletionSource();
            Dictionary<string, int> orderCroissant = new()
            { ["Croissant"] = 1 };
            List<Task> clientTasks = [];
            for (int i = 1; i < 3; i++)
            {
                int clientId = i;
                clientTasks.Add(Task.Run(async () =>
                {
                    var client = new CoffeeShopClient(clientId);
                    var buzzer = new TaskCompletionSource();
                    _ = Task.Run(() => client.ListenAsync());
                    await startingGate.Task;
                    await client.MakeOrder(orderCroissant);
                    await client.OrderBuzzer.Task;
                }));
            }
            await Task.Delay(50); // Let both runners line up
            startingGate.SetResult();
            await Task.WhenAll(clientTasks);



        }
    }
}