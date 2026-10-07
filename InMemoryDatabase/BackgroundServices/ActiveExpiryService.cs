using InMemoryDatabase.Storage;
using Microsoft.Extensions.Hosting;
using System.Diagnostics;

namespace InMemoryDatabase.BackgroundServices
{
    public sealed class ActiveExpiryService : BackgroundService
    {
        private static readonly TimeSpan Interval = TimeSpan.FromMilliseconds(100);
        private static readonly long CycleBudgetTicks = Stopwatch.Frequency / 100;
        private readonly RespStore _store;

        public ActiveExpiryService(RespStore store)
        {
            _store = store;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var timer = new PeriodicTimer(Interval);

            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                RunCycle();
            }
        }

        private void RunCycle()
        {
            long deadline = Stopwatch.GetTimestamp() + CycleBudgetTicks;

            while (true)
            {
                var (sample, expired) = _store.ExpireSample();

                if (sample == 0)
                {
                    return;
                }
                // if 25% or less is expired, memory is fine, no actions
                if (expired * 4 <= sample)
                {
                    return;
                }

                if (Stopwatch.GetTimestamp() >= deadline)
                {
                    return;
                }
            }
        }
    }
}
