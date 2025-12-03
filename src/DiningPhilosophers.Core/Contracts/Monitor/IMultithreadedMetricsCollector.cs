using DiningPhilosophers.Core.Models;

namespace DiningPhilosophers.Core.Contracts.Monitor
{
    public interface IMultithreadedMetricsCollector
    {
        MultithreadedPhilosopherMetrics GetPhilosopherMetrics(string name);
        ForkMetrics GetForkMetrics(int forkId);

        void StartWaiting(string name);
        void StopWaiting(string name);
        void IncrementMeal(string name);

        void UpdateMetrics();
        void Reset();

        long GetTotalObservations();
    }
}