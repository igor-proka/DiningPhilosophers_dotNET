using DiningPhilosophers.Core.Contracts.Monitor;
using DiningPhilosophers.Core.Models;

namespace DiningPhilosophers.Services.Metrics
{
    public class MultithreadedToMetricsAdapter : IMetricsCollector
    {
        private readonly IMultithreadedMetricsCollector _multithreadedMetrics;

        public MultithreadedToMetricsAdapter(IMultithreadedMetricsCollector multithreadedMetrics)
        {
            _multithreadedMetrics = multithreadedMetrics;
        }

        public PhilosopherMetrics GetPhilosopherMetrics(string name)
        {
            var multiMetrics = _multithreadedMetrics.GetPhilosopherMetrics(name);
            return new PhilosopherMetrics
            {
                MealsEaten = multiMetrics.MealsEaten,
                WaitingSteps = multiMetrics.TotalWaitingTimeMs
            };
        }

        public ForkMetrics GetForkMetrics(int forkId)
        {
            return _multithreadedMetrics.GetForkMetrics(forkId);
        }

        public void IncrementWaiting(string name)
        {
            // Не используется в адаптере
        }

        public void IncrementMeal(string name)
        {
            // Не используется в адаптере
        }

        public void RecordForkUsage(Fork fork, IEnumerable<Philosopher> philosophers)
        {
            // Не используется в адаптере
        }

        public void Reset()
        {
            _multithreadedMetrics.Reset();
        }
    }
}