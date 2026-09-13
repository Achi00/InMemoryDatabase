using InMemoryDatabase.Handlers;
using System.IO.Pipelines;
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

            while (!ct.IsCancellationRequested)
            {
                TcpClient client = await _listener.AcceptTcpClientAsync(ct);
                // fire and forget per connection
                _ = HandleClientAsync(client, ct);
            }
        }

        private async Task HandleClientAsync(TcpClient client, CancellationToken ct)
        {
            using (client)
            {
                NetworkStream stream = client.GetStream();
                PipeReader reader = PipeReader.Create(stream);

                try
                {
                    await RespConnectionHandler.ProcessAsync(reader, ct);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Connection error: {ex.Message}");
                }
            }
        }
    }
}
