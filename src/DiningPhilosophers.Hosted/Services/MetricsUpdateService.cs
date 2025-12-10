using System.Threading;
using System.Threading.Tasks;
using DiningPhilosophers.Core.Contracts.Monitor;
using Microsoft.Extensions.Hosting;

namespace DiningPhilosophers.Hosted.Services
{
    public class MetricsUpdateService : BackgroundService
    {
        private readonly IMultithreadedMetricsCollector _metrics;

        public MetricsUpdateService(IMultithreadedMetricsCollector metrics)
        {
            _metrics = metrics;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _metrics.UpdateMetrics();
                await Task.Delay(10, stoppingToken);
            }
        }
    }
}