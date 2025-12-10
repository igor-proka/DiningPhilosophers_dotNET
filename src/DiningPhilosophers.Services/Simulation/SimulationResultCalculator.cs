using System;
using System.Collections.Generic;
using System.Linq;
using DiningPhilosophers.Core.Contracts.Monitor;
using DiningPhilosophers.Core.Models;
using DiningPhilosophers.Services.Simulation.Multithreaded;

namespace DiningPhilosophers.Services.Simulation
{
    public class SimulationResultCalculator
    {
        public SimulationResult CalculateForStepByStep(
            IMetricsCollector metrics,
            IList<Philosopher> philosophers,
            IList<Fork> forks,
            int totalSteps)
        {
            var result = new SimulationResult();
            result.TotalSteps = totalSteps;

            // Meals
            result.TotalMeals = philosophers.Sum(p => metrics.GetPhilosopherMetrics(p.Name).MealsEaten);
            result.ThroughputPer1000 = result.TotalMeals * 1000.0 / Math.Max(1, totalSteps);

            // Waiting times
            foreach (var p in philosophers)
            {
                var pm = metrics.GetPhilosopherMetrics(p.Name);
                result.WaitingTimes[p.Name] = pm.WaitingSteps;
                result.WaitingEpisodes[p.Name] = 0; // Пошаговая версия не считает эпизоды
            }

            // Fork utilization
            CalculateForkUtilization(forks, metrics, result);

            return result;
        }

        public SimulationResult CalculateForMultithreaded(
            IMultithreadedMetricsCollector metrics,
            IList<Philosopher> philosophers,
            IList<ThreadSafeFork> forks,
            long totalMilliseconds)
        {
            var result = new SimulationResult();
            result.TotalSteps = (int)(totalMilliseconds / 1000);

            // Meals
            result.TotalMeals = philosophers.Sum(p => metrics.GetPhilosopherMetrics(p.Name).MealsEaten);
            result.ThroughputPer1000 = result.TotalMeals * 1000.0 / Math.Max(1, totalMilliseconds);

            // Waiting times
            foreach (var p in philosophers)
            {
                var pm = metrics.GetPhilosopherMetrics(p.Name);
                result.WaitingTimes[p.Name] = pm.TotalWaitingTimeMs;
                result.WaitingEpisodes[p.Name] = pm.HungerEpisodes;
            }

            // Fork utilization
            CalculateForkUtilization(forks, metrics, result, totalMilliseconds);

            return result;
        }

        private void CalculateForkUtilization(
            IList<Fork> forks,
            IMetricsCollector metrics,
            SimulationResult result)
        {
            foreach (var f in forks)
            {
                var fm = metrics.GetForkMetrics(f.Id);
                CalculateAndAddForkUtilization(f.Id, fm, result);
            }
        }

        private void CalculateForkUtilization(
            IList<ThreadSafeFork> forks,
            IMultithreadedMetricsCollector metrics,
            SimulationResult result,
            long totalMilliseconds)
        {
            const double stepDurationMs = 10.0;

            foreach (var f in forks)
            {
                var fm = metrics.GetForkMetrics(f.Id);

                double freeMs = fm.StepsFree * stepDurationMs;
                double blockedMs = fm.StepsBlocked * stepDurationMs;
                double inUseMs = fm.StepsInUse * stepDurationMs;

                double totalMs = totalMilliseconds;

                double pctFree = (freeMs / totalMs) * 100.0;
                double pctBlocked = (blockedMs / totalMs) * 100.0;
                double pctInUse = (inUseMs / totalMs) * 100.0;

                // Нормализация
                double sum = pctFree + pctBlocked + pctInUse;
                if (sum > 0)
                {
                    pctFree = pctFree * 100.0 / sum;
                    pctBlocked = pctBlocked * 100.0 / sum;
                    pctInUse = pctInUse * 100.0 / sum;
                }

                result.ForkUtilizations[f.Id] = new ForkUtilizationInfo
                {
                    FreePct = pctFree,
                    BlockedPct = pctBlocked,
                    InUsePct = pctInUse
                };
            }
        }

        private void CalculateAndAddForkUtilization(int forkId, ForkMetrics fm, SimulationResult result)
        {
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

            result.ForkUtilizations[forkId] = new ForkUtilizationInfo
            {
                FreePct = pctFree,
                BlockedPct = pctBlocked,
                InUsePct = pctInUse
            };
        }
    }
}