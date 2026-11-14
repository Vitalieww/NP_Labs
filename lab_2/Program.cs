using System;
using lab_2.Server;
using lab_2.Client;

namespace lab_2
{
    class Program
    {
        static async Task Main(string[] args)
        {
            if (args[0] == "server")
            {
                var server = new CoffeeShopServer();
                await server.RunListener();
            }

            else if (args[0] == "client")
            {
                if (args[1] == "task1")
                {
                    await Task1.Run();
                }
                else if (args[1] == "task2")
                {
                    var task2 = new Task2();
                    await task2.Run();
                }
            }
        }
    }
}
