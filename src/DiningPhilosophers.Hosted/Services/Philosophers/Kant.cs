using DiningPhilosophers.Core.Contracts.Monitor;
using DiningPhilosophers.Core.Contracts.Strategies;
using DiningPhilosophers.Core.Models;
using DiningPhilosophers.Hosted.Interfaces;
using DiningPhilosophers.Services.Simulation.Multithreaded;
using Microsoft.Extensions.Options;

namespace DiningPhilosophers.Hosted.Services.Philosophers
{
    public class Kant : PhilosopherHostedService
    {
        public Kant(ITableManager tableManager,
                    IOptions<MultithreadedSimulationConfig> configOptions,
                    IPhilosopherStrategy strategy,
                    ThreadSafeForkAcquisitionManager acquisitionManager,
                    IMultithreadedMetricsCollector metrics)
            : base(tableManager, configOptions, strategy, acquisitionManager, metrics, "Кант")
        {
        }
    }
}