using InMemoryDatabase.Commands;
using InMemoryDatabase.Commands.Handlers;
using InMemoryDatabase.Commands.Handlers.Connection;
using InMemoryDatabase.Commands.Handlers.KeySpace;
using InMemoryDatabase.Commands.Handlers.String;
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

        public RespStore Store {  get; }

        public RespServer Server { get { return _server; } }

        public TestServerFixture()
        {
            Port = TcpPortHelper.GetFreePort();

            Store = new RespStore();
            var handlers = new ICommandHandler[]
            {
                new PingCommandHandler(),
                new SetCommandHandler(Store),
                new GetCommandHandler(Store),
                new DelCommandHandler(Store),
                new ExistsCommandHandler(Store),
                new ExpireCommandHandler(Store),
                new TtlCommandHandler(Store),
                new IncrCommandHandler(Store),
                new DecrCommandHandler(Store),
                new IncrByCommandHandler(Store),
                new DecrByCommandHandler(Store),
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
