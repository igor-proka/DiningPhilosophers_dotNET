using System.Collections.Generic;
using DiningPhilosophers.Core.Models;

namespace DiningPhilosophers.Core.Contracts.Monitor
{
    public interface IMonitor
    {
        void DisplayStep(int step, IEnumerable<Philosopher> philosophers, IEnumerable<Fork> forks, IMetricsCollector metrics);

        void DisplaySummary(IMetricsCollector metrics, SimulationResult result);
    }
}
