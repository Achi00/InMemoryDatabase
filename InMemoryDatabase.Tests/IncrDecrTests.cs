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

        // increments
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

        [Fact]
        public async Task Incr_WhenKeyExistsAndIsNotInteger_ShouldReturnErr()
        {
            await using var client = await RespTestClient.ConnectAsync(_fixture.Port);

            await client.SendAsync("*3\r\n$3\r\nSET\r\n$3\r\nfoo\r\n$2\r\nhi\r\n"); ;
            string setResponse = await client.ReadRawAsync();

            await client.SendAsync("*2\r\n$4\r\nINCR\r\n$3\r\nfoo\r\n");
            string incrResponse = await client.ReadRawAsync();

            Assert.Equal("+OK\r\n", setResponse);
            Assert.Equal("-ERR value is not an integer or out of range\r\n", incrResponse);
        }

        [Fact]
        public async Task Incr_WhenKeyNotExists_ShouldCreateNew()
        {
            await using var client = await RespTestClient.ConnectAsync(_fixture.Port);

            // no set, straight increment
            await client.SendAsync("*2\r\n$4\r\nINCR\r\n$3\r\nfoo\r\n");
            string incrResponse = await client.ReadRawAsync();

            await client.SendAsync("*2\r\n$3\r\nGET\r\n$3\r\nfoo\r\n");
            string getResponse = await client.ReadRawAsync();

            Assert.Equal(":1\r\n", incrResponse);
            Assert.Equal("$1\r\n1\r\n", getResponse);
        }

        // decrements
        [Fact]
        public async Task Decr_WhenKeyExistsAndIsInteger_ShouldReturnInteger()
        {
            await using var client = await RespTestClient.ConnectAsync(_fixture.Port);

            // set integer value
            await client.SendAsync("*3\r\n$3\r\nSET\r\n$3\r\nfoo\r\n$2\r\n10\r\n");
            var setResponse = await client.ReadRawAsync();

            // decrement by 10
            await client.SendAsync("*2\r\n$4\r\nDECR\r\n$3\r\nfoo\r\n");

            string response = await client.ReadRawAsync();

            Assert.Equal(":9\r\n", response);
        }

        [Fact]
        public async Task Decr_WhenKeyExistsAndIsNotInteger_ShouldReturnErr()
        {
            await using var client = await RespTestClient.ConnectAsync(_fixture.Port);

            await client.SendAsync("*3\r\n$3\r\nSET\r\n$3\r\nfoo\r\n$2\r\nhi\r\n"); ;
            string setResponse = await client.ReadRawAsync();

            await client.SendAsync("*2\r\n$4\r\nDECR\r\n$3\r\nfoo\r\n");
            string decrResponse = await client.ReadRawAsync();

            Assert.Equal("+OK\r\n", setResponse);
            Assert.Equal("-ERR value is not an integer or out of range\r\n", decrResponse);
        }

        [Fact]
        public async Task Decr_WhenKeyNotExists_ShouldCreateNew()
        {
            await using var client = await RespTestClient.ConnectAsync(_fixture.Port);

            // no set, straight increment
            await client.SendAsync("*2\r\n$4\r\nDECR\r\n$3\r\nfoo\r\n");
            string decrResponse = await client.ReadRawAsync();

            await client.SendAsync("*2\r\n$3\r\nGET\r\n$3\r\nfoo\r\n");
            string getResponse = await client.ReadRawAsync();

            Assert.Equal(":-1\r\n", decrResponse);
            Assert.Equal("$2\r\n-1\r\n", getResponse);
        }

        // inline command tests
        [Fact]
        public async Task InlineCommand_Incr_WhenKeyExistsAndIsInteger_ShouldReturnInteger()
        {
            await using var client = await RespTestClient.ConnectAsync(_fixture.Port);

            // set integer value
            await client.SendAsync("SET foo 10\r\n");
            var setResponse = await client.ReadRawAsync();

            // increment by 10
            await client.SendAsync("INCR foo\r\n");

            string response = await client.ReadRawAsync();

            Assert.Equal(":11\r\n", response);
        }

        [Fact]
        public async Task InlineCommand_Incr_WhenKeyExistsAndIsNotInteger_ShouldReturnErr()
        {
            await using var client = await RespTestClient.ConnectAsync(_fixture.Port);

            // set integer value
            await client.SendAsync("SET foo bar\r\n");
            var setResponse = await client.ReadRawAsync();

            // increment by 10
            await client.SendAsync("INCR foo\r\n");

            string incrResponse = await client.ReadRawAsync();

            Assert.Equal("+OK\r\n", setResponse);
            Assert.Equal("-ERR value is not an integer or out of range\r\n", incrResponse);
        }

        [Fact]
        public async Task InlineCommand_Incr_WhenKeyNotExists_ShouldCreateNew()
        {
            await using var client = await RespTestClient.ConnectAsync(_fixture.Port);

            // no set, straight increment
            await client.SendAsync("INCR foo\r\n");
            string incrResponse = await client.ReadRawAsync();

            await client.SendAsync("GET foo\r\n");
            string getResponse = await client.ReadRawAsync();

            Assert.Equal(":1\r\n", incrResponse);
            Assert.Equal("$1\r\n1\r\n", getResponse);
        }

        // decrements
        [Fact]
        public async Task InlineCommand_Decr_WhenKeyExistsAndIsInteger_ShouldReturnInteger()
        {
            await using var client = await RespTestClient.ConnectAsync(_fixture.Port);

            // set integer value
            await client.SendAsync("SET foo 10\r\n");
            var setResponse = await client.ReadRawAsync();

            // decrement by 10
            await client.SendAsync("DECR foo\r\n");

            string response = await client.ReadRawAsync();

            Assert.Equal(":9\r\n", response);
        }

        [Fact]
        public async Task InlineCommand_Decr_WhenKeyExistsAndIsNotInteger_ShouldReturnErr()
        {
            await using var client = await RespTestClient.ConnectAsync(_fixture.Port);

            await client.SendAsync("SET foo hi\r\n"); ;
            string setResponse = await client.ReadRawAsync();

            await client.SendAsync("DECR foo\r\n");
            string decrResponse = await client.ReadRawAsync();

            Assert.Equal("+OK\r\n", setResponse);
            Assert.Equal("-ERR value is not an integer or out of range\r\n", decrResponse);
        }

        [Fact]
        public async Task InlineCommand_Decr_WhenKeyNotExists_ShouldCreateNew()
        {
            await using var client = await RespTestClient.ConnectAsync(_fixture.Port);

            // no set, straight increment
            await client.SendAsync("DECR foo\r\n");
            string decrResponse = await client.ReadRawAsync();

            await client.SendAsync("GET foo\r\n");
            string getResponse = await client.ReadRawAsync();

            Assert.Equal(":-1\r\n", decrResponse);
            Assert.Equal("$2\r\n-1\r\n", getResponse);
        }
    }
}
