using InMemoryDatabase.Tests.Setup;
using System.Diagnostics;

namespace InMemoryDatabase.Tests
{
    public class RespServerTests : IAsyncLifetime
    {
        private TestServerFixture _fixture;

        public Task InitializeAsync()
        {
            _fixture = new TestServerFixture();
            return Task.CompletedTask;
        }
        public async Task DisposeAsync()
        {
            await _fixture.DisposeAsync();
        }

        [Fact]
        public async Task Connect_ShouldIncrementCount()
        {
            // connect 3 clients
            await using var client = await RespTestClient.ConnectAsync(_fixture.Port);
            await using var client2 = await RespTestClient.ConnectAsync(_fixture.Port);
            await using var client3 = await RespTestClient.ConnectAsync(_fixture.Port);

            await WaitUntilAsync(() => _fixture.Server.ActiveConnectionCount == 3);

            Assert.Equal(3, _fixture.Server.ActiveConnectionCount);
        }

        [Fact]
        public async Task Disconnect_OneClinet_ShouldDropCount()
        {
            var client = await RespTestClient.ConnectAsync(_fixture.Port);
            await using var client2 = await RespTestClient.ConnectAsync(_fixture.Port);

            await WaitUntilAsync(() => _fixture.Server.ActiveConnectionCount == 2);

            await client2.DisposeAsync();

            await WaitUntilAsync(() => _fixture.Server.ActiveConnectionCount == 1);

            Assert.Equal(1, _fixture.Server.ActiveConnectionCount);
        }

        [Fact]
        public async Task WaitForConnections_ClientStillOpen_ReturnsFalse()
        {
            await using var client1 = await RespTestClient.ConnectAsync(_fixture.Port);
            await using var client2 = await RespTestClient.ConnectAsync(_fixture.Port);
            await WaitUntilAsync(() => _fixture.Server.ActiveConnectionCount == 2);

            Assert.Equal(2, _fixture.Server.ActiveConnectionCount);
            
            bool allClosed = await _fixture.Server.WaitForConnectionsAsync(TimeSpan.FromMilliseconds(200));

            Assert.False(allClosed);
        }

        [Fact]
        public async Task WaitForConnections_ClientsClose_ReturnsTrue()
        {
            var client1 = await RespTestClient.ConnectAsync(_fixture.Port);
            var client2 = await RespTestClient.ConnectAsync(_fixture.Port);

            await WaitUntilAsync(() => _fixture.Server.ActiveConnectionCount == 2);

            Task<bool> waiting = _fixture.Server.WaitForConnectionsAsync(TimeSpan.FromMilliseconds(200));

            await client1.DisposeAsync();
            await client2.DisposeAsync();

            Assert.True(await waiting);
        }

        [Fact]
        public async Task WaitForConnections_ThenCloseAll_ShouldMakeCountZero()
        {
            var client1 = await RespTestClient.ConnectAsync(_fixture.Port);
            var client2 = await RespTestClient.ConnectAsync(_fixture.Port);
            var client3 = await RespTestClient.ConnectAsync(_fixture.Port);
            await WaitUntilAsync(() => _fixture.Server.ActiveConnectionCount == 3);

            Assert.Equal(3, _fixture.Server.ActiveConnectionCount);

            await client1.DisposeAsync();
            await client2.DisposeAsync();
            await client3.DisposeAsync();

            await WaitUntilAsync(() => _fixture.Server.ActiveConnectionCount == 0);

            Assert.Equal(0, _fixture.Server.ActiveConnectionCount);
        }


        // shutdown tests
        [Fact]
        public async Task IdleConnection_ShutDown_ShouldCloseAll()
        {
            await using var client1 = await RespTestClient.ConnectAsync(_fixture.Port);
            await using var client2 = await RespTestClient.ConnectAsync(_fixture.Port);
            await using var client3 = await RespTestClient.ConnectAsync(_fixture.Port);
            await WaitUntilAsync(() => _fixture.Server.ActiveConnectionCount == 3);

            var result = await _fixture.Server.ShutdownConnectionsAsync(TimeSpan.FromSeconds(5));

            Assert.True(result);

            string afterShutdown = await client1.ReadRawAsync();
            Assert.Equal("", afterShutdown);
        }

        [Theory]
        [MemberData(nameof(Runs))]
        public async Task Shutdown_NeverExecutesWithoutReplying(int run)
        {
            // runs enumerable of 30, only used to repeat the test
            _ = run;

            await using var client = await RespTestClient.ConnectAsync(_fixture.Port);
            await WaitUntilAsync(() => _fixture.Server.ActiveConnectionCount == 1);

            // send a command and start shutdown at the same moment
            await client.SendAsync("SET foo bar\r\n");
            Task<bool> shutdown = _fixture.Server.ShutdownConnectionsAsync(TimeSpan.FromSeconds(2));

            string? reply;
            try
            {
                reply = await client.ReadRawAsync();
            }
            catch (Exception ex) when (ex is InvalidOperationException or IOException)
            {
                // server closed the connection without replying
                reply = null;
            }

            await shutdown;

            bool executed = _fixture.Store.TryGet("foo", out _);
            bool replied = reply == "+OK\r\n";

            Assert.True(executed == replied,
                $"executed: {executed}, replied: {replied}, reply: {reply ?? "<connection closed>"}");
        }

        // helper, created delay for test so os can have time to return all tasks so we can collect it, otherwise we allways have 0 connections on server side
        private static async Task WaitUntilAsync(Func<bool> condition, int timeoutMs = 2000)
        {
            var stopwatch = Stopwatch.StartNew();

            while (!condition())
            {
                if (stopwatch.ElapsedMilliseconds > timeoutMs)
                    throw new TimeoutException("Condition was not met in time.");

                await Task.Delay(10);
            }
        }

        // helper, returns 30 new object for single test, used to make xUnit run test 30 times, all should run its own client/server
        public static IEnumerable<object[]> Runs => Enumerable.Range(0, 30).Select(i => new object[] { i });
    }
}
