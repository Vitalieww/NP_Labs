using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
namespace lab_2.Server
{
    public class CoffeeShopServer
    {

        const int PORT = 5050;
        private readonly Inventory _inventory;
        private readonly ConnectionHandler _connectionHandler;

        public CoffeeShopServer()
        {
            _inventory = new Inventory();
            _connectionHandler = new ConnectionHandler(_inventory);
        }
        public async Task RunListener()
        {
            Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listener.Bind(new IPEndPoint(IPAddress.Loopback, PORT));
            listener.Listen();
            while (true)
            {
                Socket clientSocket = listener.Accept();
                Console.WriteLine($"Client connected from {clientSocket.RemoteEndPoint}");
                _ = Task.Run(() => _connectionHandler.Handle(clientSocket));
            }
        }
        private async Task Prepare(string item, int prepareTime)
        {
            Console.WriteLine($"Preparing {item}");
            await Task.Delay(prepareTime);
            Console.WriteLine($"{item} done!");
        }

    }
}