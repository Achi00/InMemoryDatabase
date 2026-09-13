using System.Net;
using System.Net.Sockets;

namespace InMemoryDatabase.Servers
{
    internal class RespServer
    {
        private readonly TcpListener _listener;

        public RespServer(int port)
        {
            _listener = new TcpListener(IPAddress.Loopback, port);
        }

        public async Task RunAsync(CancellationToken ct)
        {
            _listener.Start();
            Console.WriteLine("Server listening...");
        }
    }
}
