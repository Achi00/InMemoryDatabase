using InMemoryDatabase.Commands;
using InMemoryDatabase.Commands.Handlers;
using InMemoryDatabase.Exucutors;
using InMemoryDatabase.Servers;
using InMemoryDatabase.Storage;

namespace InMemoryDatabase.Tests.Setup
{
    // each test should have own RespServer to avoid shared state while testing
    public class TestServerFixture : IAsyncDisposable
    {
        private readonly RespServer _server;
        private readonly CancellationTokenSource _cts = new();
        private readonly Task _serverTask;

        public int Port { get; }

        public RespServer Server { get { return _server; } }

        public TestServerFixture()
        {
            Port = TcpPortHelper.GetFreePort();

            var store = new RespStore();
            var handlers = new ICommandHandler[]
            {
                new PingCommandHandler(),
                new SetCommandHandler(store),
                new GetCommandHandler(store),
                new DelCommandHandler(store),
                new ExistsCommandHandler(store),
                new ExpireCommandHandler(store),
                new TtlCommandHandler(store),
                new IncrCommandHandler(store),
                new DecrCommandHandler(store),
                new IncrByCommandHandler(store),
                new DecrByCommandHandler(store),
            };

            var executor = new RespCommandExecutor(handlers);

            _server = new RespServer(Port, executor);
            _serverTask = _server.RunAsync(_cts.Token);
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
    }
}
