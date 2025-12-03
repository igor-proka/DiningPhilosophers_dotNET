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

            // Meals
            _result.TotalMeals =
                philosophers.Sum(p => _metrics.GetPhilosopherMetrics(p.Name).MealsEaten);

            _result.ThroughputPer1000 =
                _result.TotalMeals * 1000.0 / Math.Max(1, _result.TotalSteps);

            // Waiting times
            foreach (var p in philosophers)
            {
                var pm = _metrics.GetPhilosopherMetrics(p.Name);
                _result.WaitingTimes[p.Name] = pm.WaitingSteps;

                // Пошаговая версия эпизоды голода не считает
                _result.WaitingEpisodes[p.Name] = 0;
            }

            // === Расчёт утилизации вилок ===
            foreach (var f in forks)
            {
                var fm = _metrics.GetForkMetrics(f.Id);

                long total = Math.Max(1, fm.TotalObservedSteps);

                double pctFree = 100.0 * fm.StepsFree / total;
                double pctBlocked = 100.0 * fm.StepsBlocked / total;
                double pctInUse = 100.0 * fm.StepsInUse / total;

                // Нормализация
                double sum = pctFree + pctBlocked + pctInUse;
                if (sum > 0)
                {
                    pctFree = pctFree * 100.0 / sum;
                    pctBlocked = pctBlocked * 100.0 / sum;
                    pctInUse = pctInUse * 100.0 / sum;
                }

                _result.ForkUtilizations[f.Id] = new ForkUtilizationInfo
                {
                    FreePct = pctFree,
                    BlockedPct = pctBlocked,
                    InUsePct = pctInUse
                };
            }
        }
    }
}
