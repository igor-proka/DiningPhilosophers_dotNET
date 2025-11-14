using System;
using System.Collections.Generic;
using System.Linq;
using DiningPhilosophers.Core.Contracts.Simulation;
using DiningPhilosophers.Core.Contracts.Monitor;
using DiningPhilosophers.Core.Models;

namespace DiningPhilosophers.Services.Simulation
{
    public class SimulationEngine : ISimulation
    {
        private readonly SimulationConfig _config;
        private readonly IMonitor _monitor;
        private readonly IMetricsCollector _metrics;
        private readonly ISimulationOrchestrator _orchestrator;
        private SimulationResult _result = new();

        public SimulationEngine(
            SimulationConfig config,
            IMonitor monitor,
            IMetricsCollector metrics,
            ISimulationOrchestrator orchestrator)
        {
            _config = config;
            _monitor = monitor;
            _metrics = metrics;
            _orchestrator = orchestrator;
        }

        public void Run(IEnumerable<Philosopher> philosophersEnum, IList<Fork> forks)
        {
            var philosophers = philosophersEnum.ToList();
            InitializePhilosophers(philosophers);

            for (int step = 1; step <= _config.TotalSteps; step++)
            {
                _orchestrator.ExecuteStep(step, philosophers, forks);

                // Новый вызов — передаём всю информацию
                foreach (var fork in forks)
                    _metrics.RecordForkUsage(fork, philosophers);

                if (ShouldDisplayStep(step))
                    _monitor.DisplayStep(step, philosophers, forks, _metrics);

                if (_orchestrator.CheckDeadlock(philosophers))
                {
                    HandleDeadlock(step, philosophers, forks);
                    break;
                }
            }

            FinalizeSimulation(philosophers, forks);
        }

        public SimulationResult GetResult() => _result;

        private void InitializePhilosophers(IList<Philosopher> philosophers)
        {
            var random = new Random();
            foreach (var philosopher in philosophers)
            {
                philosopher.State = PhilosopherState.Thinking;
                philosopher.StepsRemaining =
                    random.Next(_config.ThinkingTimeMin, _config.ThinkingTimeMax + 1);
            }
        }

        private bool ShouldDisplayStep(int step)
        {
            return step % _config.DisplayInterval == 0
                || step == 1
                || step == _config.TotalSteps;
        }

        private void HandleDeadlock(
            int step,
            IList<Philosopher> philosophers,
            IList<Fork> forks)
        {
            _result.DeadlockInfo = $"DEADLOCK detected at step {step}";
            Console.WriteLine($"\n{_result.DeadlockInfo}: all philosophers hungry and each holds exactly one fork.");
            _monitor.DisplayStep(step, philosophers, forks, _metrics);
        }

        private void FinalizeSimulation(IList<Philosopher> philosophers, IList<Fork> forks)
        {
            _result.TotalSteps = _config.TotalSteps;

            _result.TotalMeals =
                philosophers.Sum(p => _metrics.GetPhilosopherMetrics(p.Name).MealsEaten);

            _result.ThroughputPer1000 =
                _result.TotalMeals * 1000.0 / Math.Max(1, _result.TotalSteps);

            foreach (var p in philosophers)
            {
                _result.WaitingTimes[p.Name] =
                    _metrics.GetPhilosopherMetrics(p.Name).WaitingSteps;
            }

            foreach (var f in forks)
            {
                var fm = _metrics.GetForkMetrics(f.Id);
                double util = fm.TotalObservedSteps == 0
                    ? 0
                    : 100.0 * fm.StepsInUse / fm.TotalObservedSteps;

                _result.ForkUtilizations[f.Id] = util;
            }
        }
    }
}
