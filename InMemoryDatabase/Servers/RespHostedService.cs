using Microsoft.Extensions.Hosting;

namespace InMemoryDatabase.Servers
{
    public class RespHostedService : BackgroundService
    {
        private readonly RespServer _server;

        public RespHostedService(RespServer server)
        {
            _server = server;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await _server.RunAsync(stoppingToken);
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            await base.StopAsync(cancellationToken);

            bool graceful = await _server.ShutdownConnectionsAsync(TimeSpan.FromSeconds(10));

            Console.WriteLine(graceful
                ? "All connections closed gracefully."
                : "Grace period expired; remaining connections were aborted.");
        }
    }
}
