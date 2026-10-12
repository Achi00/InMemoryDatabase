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


            Assert.Equal(3, _fixture.Server.ActiveConnectionCount);
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
    }
}
