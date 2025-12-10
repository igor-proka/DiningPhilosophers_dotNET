using System.Threading;
using System.Threading.Tasks;
using DiningPhilosophers.Core.Contracts.Monitor;
using DiningPhilosophers.Core.Contracts.Strategies;
using DiningPhilosophers.Core.Models;
using DiningPhilosophers.Hosted.Interfaces;
using DiningPhilosophers.Services.Simulation.Multithreaded;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace DiningPhilosophers.Hosted.Services.Philosophers
{
    public class PhilosopherHostedService : BackgroundService
    {
        private readonly MultithreadedPhilosopherStateProcessor _processor;

        public PhilosopherHostedService(
            ITableManager tableManager,
            IOptions<MultithreadedSimulationConfig> configOptions,
            IPhilosopherStrategy strategy,
            ThreadSafeForkAcquisitionManager acquisitionManager,
            IMultithreadedMetricsCollector metrics,
            string philosopherName)
        {
            var philosopher = tableManager.GetPhilosopher(philosopherName);
            var leftFork = tableManager.GetLeftFork(philosopherName);
            var rightFork = tableManager.GetRightFork(philosopherName);
            var config = configOptions.Value;

            _processor = new MultithreadedPhilosopherStateProcessor(
                philosopher, leftFork, rightFork, config, strategy, acquisitionManager, metrics);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await _processor.RunAsync(stoppingToken);
        }
    }
}