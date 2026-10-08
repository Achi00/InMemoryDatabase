using InMemoryDatabase.Tests.Setup;

namespace InMemoryDatabase.Tests
{
    public class ExpireTests : IAsyncLifetime
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
        public async Task Expire_WhenKeyExists_ShouldReturnInteger()
        {
            await using var client = await RespTestClient.ConnectAsync(_fixture.Port);

            // set
            await client.SendAsync("*3\r\n$3\r\nSET\r\n$3\r\nfoo\r\n$3\r\nbar\r\n");
            var setResponse = await client.ReadRawAsync();

            // expire
            await client.SendAsync("*3\r\n$6\r\nEXPIRE\r\n$3\r\nfoo\r\n$2\r\n60\r\n");

            string response = await client.ReadRawAsync();

            Assert.Equal(":1\r\n", response);
        }

        [Fact]
        public async Task Expire_WhenKeyNotExists_ShouldReturnInteger()
        {
            await using var client = await RespTestClient.ConnectAsync(_fixture.Port);

            // no set

            // expire
            await client.SendAsync("*3\r\n$6\r\nEXPIRE\r\n$3\r\nfoo\r\n$2\r\n60\r\n");

            string response = await client.ReadRawAsync();

            Assert.Equal(":0\r\n", response);
        }

        // inline commands
        [Fact]
        public async Task Set_WithEx_TtlIsPositive()
        {
            await using var client = await RespTestClient.ConnectAsync(_fixture.Port);

            await client.SendAsync("SET foo bar EX 10\r\n");
            string setResult = await client.ReadRawAsync();
            Assert.Equal("+OK\r\n", setResult);

            await client.SendAsync("TTL foo\r\n");
            string ttlResult = await client.ReadRawAsync();

            string ttlText = ttlResult.TrimEnd('\r', '\n');
            Assert.StartsWith(":", ttlText);
            long ttl = long.Parse(ttlText[1..]);
            Assert.InRange(ttl, 9, 10);
        }
    }
}
