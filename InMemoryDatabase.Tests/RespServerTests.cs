using InMemoryDatabase.Tests.Setup;

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
    }
}
