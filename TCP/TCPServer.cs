using System.Net;
using System.Net.Sockets;

namespace InMemoryDatabase.TCP
{
    internal class TCPServer
    {
        private const int Port = 8888;

        public static async Task Start(string[] args)
        {
            // listens all available network interfaces
            var server = new TcpListener(IPAddress.Any, Port);

            try
            {
                server.Start();

                Console.WriteLine($"[SERVER] Started, Listening on port {Port}");

                while (true)
                {
                    var client = await server.AcceptTcpClientAsync();
                    Console.WriteLine($"[SERVER] Client connected from: {client.Client.RemoteEndPoint}");

                    // handle each client in seperate task to avoid blocking new connections
                    _ = Task.Run(() => HandleClientAsync(client));
                }
            }
            catch (Exception)
            {
                Console.WriteLine($"[SERVER ERROR] {ex.Message}");
            }
            finally
            {
                server.Stop();
            }
        }

        private static void HandleClientAsync(TcpClient client)
        {
            throw new NotImplementedException();
        }
    }
}
