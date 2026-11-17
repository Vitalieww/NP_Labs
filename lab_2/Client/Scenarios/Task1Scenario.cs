using System.Net;
using System.Net.Sockets;
using System.Text;
using lab_2.Client;
using lab_2.Protocol;
using Newtonsoft.Json.Linq;


namespace lab_2.Client
{
    public class Task1
    {
        public static async Task Run()
        {
            CoffeeShopClient client = new(0);
            await client.RequestMenu();
            await client.ListenAsync();

            Dictionary<string, int> order = new()
            {
                ["Coffee"] = 1,
                ["Croissant"] = 2
            };
            await client.MakeOrder(order);
        }
    }
}