using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DiningPhilosophers.Core.Contracts.Monitor;
using DiningPhilosophers.Core.Models;
using DiningPhilosophers.Hosted.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace DiningPhilosophers.Hosted.Services
{
    public class DisplayService : BackgroundService
    {
        private readonly IMonitor _monitor;
        private readonly ITableManager _tableManager;
        private readonly IMetricsCollector _metricsAdapter;
        private readonly MultithreadedSimulationConfig _config;

        public DisplayService(
            IMonitor monitor,
            ITableManager tableManager,
            IMetricsCollector metricsAdapter,
            IOptions<MultithreadedSimulationConfig> configOptions)
        {
            _monitor = monitor;
            _tableManager = tableManager;
            _metricsAdapter = metricsAdapter;
            _config = configOptions.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Initial display at 0 ms
            if (!stoppingToken.IsCancellationRequested)
            {
                _monitor.DisplayStep(0, _tableManager.GetPhilosophers(), 
                    _tableManager.GetForks().Select(f => new Fork(f.Id) { State = f.State, Owner = f.Owner }).ToList(), 
                    _metricsAdapter);
            }

            var startTime = DateTime.Now;
            int displayCount = 1;
            while (!stoppingToken.IsCancellationRequested)
            {
                var elapsed = DateTime.Now - startTime;

                var nextDisplayTime = displayCount * _config.DisplayInterval;
                var currentTime = (int)elapsed.TotalMilliseconds;
                var waitTime = Math.Max(0, nextDisplayTime - currentTime);

                if (waitTime > 0)
                {
                    try
                    {
                        await Task.Delay(waitTime, stoppingToken);
                    }
                    catch (TaskCanceledException)
                    {
                        break;
                    }
                }

                if (stoppingToken.IsCancellationRequested) break;

                // Отображаем
                _monitor.DisplayStep(displayCount, _tableManager.GetPhilosophers(), 
                    _tableManager.GetForks().Select(f => new Fork(f.Id) { State = f.State, Owner = f.Owner }).ToList(), 
                    _metricsAdapter);

                displayCount++;
            }
        }
    }
}