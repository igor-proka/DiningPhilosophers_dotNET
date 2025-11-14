using System;
using System.Collections.Generic;
using DiningPhilosophers.Core.Contracts.Monitor;
using DiningPhilosophers.Core.Models;
using System.Linq;

namespace DiningPhilosophers.Services.Metrics
{
    public class MetricsCollector : IMetricsCollector
    {
        private readonly Dictionary<string, PhilosopherMetrics> _philos = new(StringComparer.Ordinal);
        private readonly Dictionary<int, ForkMetrics> _forks = new();

        public MetricsCollector(IEnumerable<Philosopher> philosophers, IEnumerable<Fork> forks)
        {
            foreach (var p in philosophers)
                _philos[p.Name] = new PhilosopherMetrics();

            foreach (var f in forks)
                _forks[f.Id] = new ForkMetrics();
        }

        public PhilosopherMetrics GetPhilosopherMetrics(string name)
            => _philos[name];

        public ForkMetrics GetForkMetrics(int forkId)
            => _forks[forkId];

        public void IncrementWaiting(string name)
        {
            _philos[name].WaitingSteps++;
        }

        public void IncrementMeal(string name)
        {
            _philos[name].MealsEaten++;
        }

        public void RecordForkUsage(Fork fork, IEnumerable<Philosopher> philosophers)
        {
            var fm = _forks[fork.Id];

            if (fork.State == Core.Models.ForkState.Available)
            {
                fm.StepsFree++;
                return;
            }

            // fork.State == InUse
            if (string.IsNullOrEmpty(fork.Owner))
            {
                // нет владельца, но InUse — считаем как blocked
                fm.StepsBlocked++;
                return;
            }

            // Найдём философа-владельца и проверим его состояние
            var owner = philosophers.FirstOrDefault(p => p.Name == fork.Owner);
            if (owner == null)
            {
                // если не нашли — учитываем как blocked
                fm.StepsBlocked++;
                return;
            }

            if (owner.State == PhilosopherState.Eating)
                fm.StepsInUse++;
            else
                fm.StepsBlocked++;
        }

        public void Reset()
        {
            foreach (var p in _philos.Values) p.Reset();
            foreach (var f in _forks.Values) f.Reset();
        }
    }
}
