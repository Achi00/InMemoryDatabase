using InMemoryDatabase.Servers;
using System.Net;
using System.Net.Sockets;

namespace InMemoryDatabase.Tests
{
    // each test should have own RespServer to avoid shared state while testing
    public class TestServerFixture : IAsyncDisposable
    {
        private readonly RespServer _server;
        private readonly CancellationTokenSource _cts = new();
        private readonly Task _serverTask;

        public int Port { get; }

        public TestServerFixture()
        {
            Port = GetFreePort();
        }

        public async ValueTask DisposeAsync()
        {
            _cts.Cancel();

            try
            {
                await _serverTask;
            }
            catch (OperationCanceledException)
            {
            }
        }
        private int GetFreePort()
        {
            // post 0 = OS should pick free one
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }
    }
}
