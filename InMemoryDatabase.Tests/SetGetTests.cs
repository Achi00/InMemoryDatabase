using InMemoryDatabase.Tests.Setup;

namespace InMemoryDatabase.Tests
{
    public class SetGetTests : IAsyncLifetime
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
        public async Task Set_Then_Get_ReturnsStoredValue()
        {
            await using var client = await RespTestClient.ConnectAsync(_fixture.Port);

            // set
            await client.SendAsync("*3\r\n$3\r\nSET\r\n$3\r\nfoo\r\n$3\r\nbar\r\n");
            var setResponse = await client.ReadRawAsync();

            //get
            await client.SendAsync("*2\r\n$3\r\nGET\r\n$3\r\nfoo\r\n");
            
            string getResponse = await client.ReadRawAsync();

            Assert.Equal("+OK\r\n", setResponse);
            Assert.Equal("$3\r\nbar\r\n", getResponse);
        }

        [Fact]
        public async Task Get_NonExistentKey_ReturnsNullBulkString()
        {
            await using var client = await RespTestClient.ConnectAsync(_fixture.Port);

            await client.SendAsync("*2\r\n$3\r\nGET\r\n$7\r\nmissing\r\n");

            string getResponse = await client.ReadRawAsync();

            Assert.Equal("$-1\r\n", getResponse);
        }
    }
}
