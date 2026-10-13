using InMemoryDatabase.Exucutors;
using InMemoryDatabase.Handlers;
using System.Collections.Concurrent;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;

namespace InMemoryDatabase.Servers
{
    public class RespServer
    {
        private readonly TcpListener _listener;
        private readonly RespCommandExecutor _executor;

        // connection tracking
        private readonly ConcurrentDictionary<long, Task> _connections = new();
        private long _nextConnectionId;
        internal int ActiveConnectionCount => _connections.Count;

        // tokens for soft and immediate shutdown
        private readonly CancellationTokenSource _stopReadingCts = new();
        private readonly CancellationTokenSource _abortCts = new();

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

                // should happend as single operation
                long id = Interlocked.Increment(ref _nextConnectionId);
                var connectionTask = HandleClientAsync(client);

                _connections[id] = connectionTask;
                _ = connectionTask.ContinueWith(_ => _connections.TryRemove(id, out _));
            }
        }
        // track state of all connections with deadline, for graceful shutdown
        internal async Task<bool> WaitForConnectionsAsync(TimeSpan timeout)
        {
            // run collected live connection Tasks, waits for all to return/finish
            Task allDone = Task.WhenAll(_connections.Values);
            // if timeout exhaust before all live connections
            Task first = await Task.WhenAny(allDone, Task.Delay(timeout));
            // true = all finished in time, false = deadline hit, no overloaded comparison only references compare
            return first == allDone;   
        }

        private async Task HandleClientAsync(TcpClient client)
        {
            using (client)
            {
                NetworkStream stream = client.GetStream();
                PipeReader reader = PipeReader.Create(stream);
                PipeWriter writer = PipeWriter.Create(stream);

                try
                {
                    await RespConnectionHandler.ProcessAsync(reader, writer, _executor, _stopReadingCts.Token, _abortCts.Token);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Connection error: {ex.Message}");
                }
            }
        }
    }
}
