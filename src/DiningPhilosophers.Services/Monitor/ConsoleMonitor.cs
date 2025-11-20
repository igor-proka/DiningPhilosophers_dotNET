using System;
using System.Collections.Generic;
using System.Linq;
using DiningPhilosophers.Core.Contracts.Monitor;
using DiningPhilosophers.Core.Models;

namespace DiningPhilosophers.Services.Monitor
{
    public class ConsoleMonitor : IMonitor
    {
        public void DisplayStep(int step, IEnumerable<Philosopher> philosophers, IEnumerable<Fork> forks, IMetricsCollector metrics)
        {
            Console.WriteLine($"\n===== ШАГ {step} =====");
            Console.WriteLine("\nФилософы:");

            foreach (var p in philosophers)
            {
                string stateDesc = p.State switch
                {
                    PhilosopherState.Thinking => $"Thinking ({p.StepsRemaining} steps left)",
                    PhilosopherState.Hungry => $"Hungry (Action = {p.CurrentAction})",
                    PhilosopherState.Eating => $"Eating ({p.StepsRemaining} steps left)",
                    _ => p.State.ToString()
                };

                var pm = metrics.GetPhilosopherMetrics(p.Name);
                long meals = pm.MealsEaten;
                Console.WriteLine($"  {p.Name}: {stateDesc}, съедено: {meals}");
            }

            Console.WriteLine("\nВилки:");
            foreach (var f in forks)
            {
                Console.WriteLine($"  Fork-{f.Id}: {f.State}" + (f.Owner != null ? $" (используется {f.Owner})" : ""));
            }
        }

        public void DisplaySummary(IMetricsCollector metrics, SimulationResult result)
        {
            Console.WriteLine("\n" + new string('=', 50));
            Console.WriteLine("ФИНАЛЬНЫЕ РЕЗУЛЬТАТЫ");
            Console.WriteLine(new string('=', 50));

            Console.WriteLine($"Общее количество шагов: {result.TotalSteps}");
            Console.WriteLine($"Всего съедено: {result.TotalMeals}");
            Console.WriteLine($"Пропускная способность (всего): {result.ThroughputPer1000:0.###} meals per 1000 steps");

            // Пропускная способность по каждому философу + среднее
            Console.WriteLine("\nПропускная способность по философам (meals per 1000 steps):");
            var throughputs = new List<double>();
            foreach (var kv in result.WaitingTimes)
            {
                var name = kv.Key;
                var pm = metrics.GetPhilosopherMetrics(name);
                double tp = pm.MealsEaten * 1000.0 / Math.Max(1, result.TotalSteps);
                throughputs.Add(tp);
                Console.WriteLine($"  {name}: {tp:0.###}");
            }

            double avgThroughput = throughputs.Count == 0 ? 0.0 : throughputs.Average();
            Console.WriteLine($"Средняя пропускная способность: {avgThroughput:0.###} meals per 1000 steps");

            // Время ожидания: по философу, среднее, максимальное + кто
            Console.WriteLine("\nВремя ожидания (в шагах):");
            var waits = new List<long>();
            foreach (var kv in result.WaitingTimes)
            {
                Console.WriteLine($"  {kv.Key}: {kv.Value} steps");
                waits.Add(kv.Value);
            }

            double avgWait = waits.Count == 0 ? 0.0 : waits.Average();
            var maxWait = waits.Count == 0 ? 0L : waits.Max();
            var whoMax = result.WaitingTimes.FirstOrDefault(k => k.Value == maxWait).Key ?? string.Empty;
            Console.WriteLine($"\nСреднее время ожидания: {avgWait:0.##} steps");
            Console.WriteLine($"Максимальное время ожидания: {maxWait} steps (философ: {whoMax})");

            // Утилизация вилок: сколько процентов времени вилка свободна/заблокирована/используется для еды (free/blocked/inuse)
            Console.WriteLine("\nКоэффициенты утилизации вилок (проценты времени):");
            foreach (var kv in result.ForkUtilizations.OrderBy(k => k.Key))
            {
                int forkId = kv.Key;
                var fm = metrics.GetForkMetrics(forkId);
                double total = Math.Max(1.0, fm.TotalObservedSteps);
                double pctFree = 100.0 * fm.StepsFree / total;
                double pctBlocked = 100.0 * fm.StepsBlocked / total;
                double pctInUse = 100.0 * fm.StepsInUse / total;

                Console.WriteLine($"  Fork-{forkId}: free={pctFree:0.00}% blocked={pctBlocked:0.00}% eating={pctInUse:0.00}% (Observed={fm.TotalObservedSteps})");
            }

            if (!string.IsNullOrEmpty(result.DeadlockInfo))
                Console.WriteLine($"\n⚠️  {result.DeadlockInfo}");
        }
    }
}
