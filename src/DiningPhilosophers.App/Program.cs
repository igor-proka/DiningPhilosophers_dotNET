using System;
using System.Linq;
using DiningPhilosophers.Core.Contracts.Configuration;
using DiningPhilosophers.Core.Contracts.Simulation;
using DiningPhilosophers.Core.Contracts.Monitor;
using DiningPhilosophers.Core.Contracts.Strategies;
using DiningPhilosophers.Core.Models;
using DiningPhilosophers.Services.Configuration;
using DiningPhilosophers.Services.Simulation;
using DiningPhilosophers.Services.Monitor;
using DiningPhilosophers.Services.Metrics;
using DiningPhilosophers.Strategies;

namespace DiningPhilosophers.App
{
    internal class Program
    {
        static void Main(string[] args)
        {
            try
            {
                RunSimulation();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка: {ex.Message}");
            }

            Console.WriteLine("Simulation finished. Press any key to exit...");
            Console.ReadKey();
        }

        private static void RunSimulation()
        {
            IPhilosopherNamesProvider namesProvider =
                new FilePhilosopherNamesProvider("philosophers.txt");

            var names = namesProvider.GetNames().ToArray();

            // Доменные объекты
            var philosophers = names.Select(n => new Philosopher(n)).ToList();
            var forks = Enumerable.Range(1, names.Length).Select(i => new Fork(i)).ToList();

            // Конфигурация симуляции
            var config = new SimulationConfig
            {
                UseCoordinator = true,
                CoordinatorType = CoordinatorType.Semaphore
            };

            var strategyFactory = new StrategyFactory();
            var (strategy, coordinator) =
                strategyFactory.Create(config.UseCoordinator, config.CoordinatorType, philosophers, forks);

            var monitor = new ConsoleMonitor();
            var metrics = new MetricsCollector(philosophers, forks);

            // Менеджер захвата вилок
            var acquisitionManager = new ForkAcquisitionManager(config.ForkAcquisitionTime);
            foreach (var philosopher in philosophers)
                acquisitionManager.InitializePhilosopher(philosopher);

            var stateProcessor = new PhilosopherStateProcessor(
                config, strategy, coordinator, acquisitionManager, metrics);

            var orchestrator = new SimulationOrchestrator(stateProcessor, acquisitionManager);

            var engine = new SimulationEngine(config, monitor, metrics, orchestrator);

            engine.Run(philosophers, forks);

            // Финальный вывод
            var result = engine.GetResult();
            monitor.DisplaySummary(metrics, result);
        }
    }
}
