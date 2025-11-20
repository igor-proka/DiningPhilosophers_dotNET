using System.Collections.Generic;
using DiningPhilosophers.Core.Models;

namespace DiningPhilosophers.Core.Contracts.Monitor
{
    public interface IMetricsCollector
    {
        PhilosopherMetrics GetPhilosopherMetrics(string name);
        ForkMetrics GetForkMetrics(int forkId);

        void IncrementWaiting(string name);
        void IncrementMeal(string name);

        void RecordForkUsage(Fork fork, IEnumerable<Philosopher> philosophers);

        void Reset();
    }
}
