using System;
using System.Collections.Generic;
using System.Linq;
using DiningPhilosophers.Core.Contracts.Monitor;
using DiningPhilosophers.Core.Models;
using DiningPhilosophers.Services.Metrics;

namespace DiningPhilosophers.Services.Monitor
{
    public class MultithreadedConsoleMonitor : IMonitor
    {
        private readonly int _displayInterval;

        public MultithreadedConsoleMonitor(int displayInterval)
        {
            _displayInterval = displayInterval;
        }

        public void DisplayStep(int step, IEnumerable<Philosopher> philosophers, IEnumerable<Fork> forks, IMetricsCollector metrics)
        {
            var elapsedTime = step * _displayInterval;
            Console.WriteLine($"\n===== ВРЕМЯ {elapsedTime} мс =====");
            Console.WriteLine("\nФилософы:");

            foreach (var p in philosophers)
            {
                string stateDesc = p.State switch
                {
                    PhilosopherState.Thinking => $"Thinking ({p.StepsRemaining} ms left)",
                    PhilosopherState.Hungry => $"Hungry (Action = {p.CurrentAction})",
                    PhilosopherState.Eating => $"Eating ({p.StepsRemaining} ms left)",
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
            Console.WriteLine("ФИНАЛЬНЫЕ РЕЗУЛЬТАТЫ МНОГОПОТОЧНОЙ СИМУЛЯЦИИ");
            Console.WriteLine(new string('=', 50));

            long totalMs = result.TotalMilliseconds; // Используем реальное время в мс
            double totalSec = totalMs / 1000.0;
            Console.WriteLine($"Общее время симуляции: {totalSec:0.##} сек ({totalMs} мс)");
            Console.WriteLine($"Всего съедено: {result.TotalMeals}");

            // --- Пропускная способность ---
            double throughputMs = result.TotalMeals / (double)totalMs;
            double throughputSec = result.TotalMeals / totalSec;

            Console.WriteLine($"\n>>> Пропускная способность");
            Console.WriteLine($"  meals/ms:  {throughputMs:0.#####}");
            Console.WriteLine($"  meals/sec: {throughputSec:0.###}");

            // --- Пропускная способность по философам ---
            Console.WriteLine("\nПропускная способность по философам (meals/ms):");
            var perPhilosopher = new List<double>();

            foreach (var name in result.WaitingTimes.Keys)
            {
                var pm = metrics.GetPhilosopherMetrics(name);
                double tp = pm.MealsEaten / (double)totalMs;
                perPhilosopher.Add(tp);

                Console.WriteLine($"  {name}: {tp:0.#####}");
            }

            double avgThroughput = perPhilosopher.Count == 0 ? 0 : perPhilosopher.Average();
            Console.WriteLine($"Средняя пропускная способность: {avgThroughput:0.#####} meals/ms");

            // === Среднее время ожидания для КАЖДОГО философа ===
            Console.WriteLine("\nСреднее время ожидания для каждого философа (ms):");
            foreach (var kv in result.WaitingTimes)
            {
                string name = kv.Key;
                long totalWait = kv.Value;
                int episodes = result.WaitingEpisodes.TryGetValue(name, out var e) ? e : 0;

                double avgWait = episodes > 0 ? totalWait / (double)episodes : totalWait;
                Console.WriteLine($"  {name}: {avgWait:0.##} ms  (эпизодов: {episodes})");
            }

            // === Общее время ожидания ===
            Console.WriteLine("\nОбщее время ожидания (ms):");
            var waits = result.WaitingTimes.Values.ToList();
            foreach (var kv in result.WaitingTimes)
                Console.WriteLine($"  {kv.Key}: {kv.Value} ms");

            long maxWait = waits.Count == 0 ? 0 : waits.Max();
            string whoMax = result.WaitingTimes.FirstOrDefault(kv => kv.Value == maxWait).Key ?? string.Empty;
            double avgWaitAll = waits.Count == 0 ? 0 : waits.Average();

            Console.WriteLine($"\nСреднее время ожидания (по всем философам): {avgWaitAll:0.##} ms");
            Console.WriteLine($"Максимальное время ожидания: {maxWait} ms (философ: {whoMax})");

            // --- Утилизация вилок ---
            Console.WriteLine("\nКоэффициенты утилизации вилок:");
            foreach (var kv in result.ForkUtilizations.OrderBy(k => k.Key))
            {
                int forkId = kv.Key;
                var util = kv.Value;

                Console.WriteLine(
                    $"  Fork-{forkId}: free={util.FreePct:0.00}%  blocked={util.BlockedPct:0.00}%  eating={util.InUsePct:0.00}%");
            }

            if (!string.IsNullOrWhiteSpace(result.DeadlockInfo))
                Console.WriteLine($"\n⚠️  {result.DeadlockInfo}");
        }
    }
}