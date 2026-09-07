using System.Net;
using System.Net.Sockets;
using System.Text;

namespace InMemoryDatabase.TCP
{
    internal class TCPServer
    {
        private const int Port = 8888;

        // TODO: add pipelines and ReadOnlySequence in future
        public static async Task Start(CancellationToken cancellationToken)
        {
            // listens all available network interfaces
            var server = new TcpListener(IPAddress.Any, Port);

            try
            {
                server.Start();

                Console.WriteLine($"[SERVER] Started, Listening on port {Port}");

                while (!cancellationToken.IsCancellationRequested)
                {
                    TcpClient? client = null;
                    try
                    {
                        client = await server.AcceptTcpClientAsync();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[SERVER ERROR] {ex.Message}");
                    }

                    if (client != null)
                    {
                        Console.WriteLine($"[SERVER] Client connected from: {client.Client.RemoteEndPoint}");
                        // handle each client in seperate task to avoid blocking new connections
                        _ = Task.Run(() => HandleClientAsync(client));
                    }
                }
            }
            finally
            {
                server.Stop();
            }
        }

        private static async Task HandleClientAsync(TcpClient client)
        {
            using(client)
            using (NetworkStream stream = client.GetStream())
            {
                var buffer = new byte[1024];
                int byteRead;

                try
                {
                    // read data continuosly from client stream
                    while ((byteRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        string receivedMessage = Encoding.UTF8.GetString(buffer, 0, byteRead);
                        Console.WriteLine($"[RECEIVED]: {receivedMessage}");

                        // echo back, simple response for testing
                        string response = $"Server echoed: {receivedMessage}";
                        byte[] responseBytes = Encoding.UTF8.GetBytes(response);

                        // write data back to client
                        await stream.WriteAsync(responseBytes, 0, responseBytes.Length);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[CLIENT ERROR] {ex.Message}");
                    throw;
                }
            }

            Console.WriteLine("[SERVER] Client disconnected.");
        }
    }
}
