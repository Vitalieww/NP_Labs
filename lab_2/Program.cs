using System;
using lab_2.Server;
using lab_2.Client;
using lab_2.Provider;

namespace lab_2
{
    class Program
    {
        static async Task Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Usage: server [port], client [ip] [port], or provider [ip] [port]");
                return;
            }

            if (args[0].Equals("server", StringComparison.OrdinalIgnoreCase))
            {
                int port = args.Length > 1 ? int.Parse(args[1]) : 5050;
                await new CoffeeShopServer(port).RunListener();
            }
            else if (args[0].Equals("client", StringComparison.OrdinalIgnoreCase))
            {
                if (args.Length > 1 && args[1].Equals("task1", StringComparison.OrdinalIgnoreCase))
                {
                    await Task1.Run();
                }
                else if (args.Length > 1 && args[1].Equals("task2", StringComparison.OrdinalIgnoreCase))
                {
                    await new Task2().Run();
                }
                else
                {
                    string ip = args.Length > 1 ? args[1] : "127.0.0.1";
                    int port = args.Length > 2 ? int.Parse(args[2]) : 5050;
                    var client = new CoffeeShopClient(ip, port);
                    Task listener = client.ListenAsync();
                    Task interactive = client.RunInteractiveAsync();
                    Task completed = await Task.WhenAny(listener, interactive);
                    if (completed == listener)
                    {
                        client.Framer.Disconnect();
                        Environment.Exit(0);
                    }
                    else
                    {
                        await listener;
                    }
                }
            }
            else if (args[0].Equals("provider", StringComparison.OrdinalIgnoreCase))
            {
                string ip = args.Length > 1 ? args[1] : "127.0.0.1";
                int port = args.Length > 2 ? int.Parse(args[2]) : 5050;
                var provider = new CoffeeShopProvider(ip, port);
                await provider.LogProvider();
                await provider.WaitForAlert();
            }
        }
    }
}
