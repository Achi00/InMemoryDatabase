using InMemoryDatabase.Exucutors;
using InMemoryDatabase.Handlers;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;

namespace InMemoryDatabase.Servers
{
    public class RespServer
    {
        private readonly TcpListener _listener;
        private readonly RespCommandExecutor _executor;

        public RespServer(int port, RespCommandExecutor executor)
        {
            _listener = new TcpListener(IPAddress.Loopback, port);
            _executor = executor;
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
                PipeWriter writer = PipeWriter.Create(stream);

                try
                {
                    await RespConnectionHandler.ProcessAsync(reader, writer, _executor, ct);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Connection error: {ex.Message}");
                }
            }
        }
    }
}
