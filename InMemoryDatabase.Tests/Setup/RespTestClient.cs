using System.Net;
using System.Net.Sockets;
using System.Text;

namespace InMemoryDatabase.Tests.Setup
{
    public class RespTestClient : IAsyncDisposable
    {
        private readonly TcpClient _client;
        private readonly NetworkStream _stream;

        private RespTestClient(TcpClient client)
        {
            _client = client;
            _stream = client.GetStream();
        }

        public static async Task<RespTestClient> ConnectAsync(int port)
        {
            var client = new TcpClient();
            await client.ConnectAsync(IPAddress.Loopback, port);

            return new RespTestClient(client);
        }

        public async Task SendAsync(string rawResp)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(rawResp);
            await _stream.WriteAsync(bytes);
        }
        public async Task<string> ReadRawAsync(int expectedByteCountHint = 4096)
        {
            byte[] buffer = new byte[expectedByteCountHint];
            // read from stream we write into
            int read = await _stream.ReadAsync(buffer);

            return Encoding.UTF8.GetString(buffer, 0, read);
        }

        public ValueTask DisposeAsync()
        {
            _stream.Dispose();
            _client.Dispose();
            return ValueTask.CompletedTask;
        }
    }
}
