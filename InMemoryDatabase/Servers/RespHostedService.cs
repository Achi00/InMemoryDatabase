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
    }
}
