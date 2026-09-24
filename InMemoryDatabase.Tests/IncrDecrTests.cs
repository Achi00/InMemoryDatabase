using InMemoryDatabase.Tests.Setup;

namespace InMemoryDatabase.Tests
{
    public class IncrDecrTests : IAsyncLifetime
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
        public async Task Incr_WhenKeyExistsAndIsInteger_ShouldReturnInteger()
        {
            await using var client = await RespTestClient.ConnectAsync(_fixture.Port);

            // set integer value
            await client.SendAsync("*3\r\n$3\r\nSET\r\n$3\r\nfoo\r\n$2\r\n10\r\n");
            var setResponse = await client.ReadRawAsync();

            // increment by 10
            await client.SendAsync("*2\r\n$4\r\nINCR\r\n$3\r\nfoo\r\n");

            string response = await client.ReadRawAsync();

            Assert.Equal(":11\r\n", response);
        }
    }
}
