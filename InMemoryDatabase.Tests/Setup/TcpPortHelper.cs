using System.Net;
using System.Net.Sockets;

namespace InMemoryDatabase.Tests.Setup
{
    internal static class TcpPortHelper
    {
        public static int GetFreePort()
        {
            // post 0 = OS should pick free one
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }
    }
}
