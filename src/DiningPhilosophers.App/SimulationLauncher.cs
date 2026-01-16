using System;
using System.Linq;
using System.Threading.Tasks;
using DiningPhilosophers.Core.Contracts.Configuration;
using DiningPhilosophers.Core.Models;
using DiningPhilosophers.Services.Configuration;
using DiningPhilosophers.Services.Simulation;
using DiningPhilosophers.Services.Simulation.Multithreaded;
using DiningPhilosophers.Services.Monitor;
using DiningPhilosophers.Strategies;

namespace DiningPhilosophers.App
{
    public static class SimulationLauncher
    {
        public static void RunStepByStepSimulation()
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
                UseCoordinator = false,
                CoordinatorType = CoordinatorType.Semaphore
            };

            var strategyFactory = new StrategyFactory();
            var (strategy, coordinator) =
                strategyFactory.Create(config.UseCoordinator, config.CoordinatorType, philosophers, forks);

            var monitor = new ConsoleMonitor();
            var metrics = new Services.Metrics.MetricsCollector(philosophers, forks);

            // Менеджер захвата вилок
            var acquisitionManager = new ForkAcquisitionManager(config.ForkAcquisitionTime);
            foreach (var philosopher in philosophers)
                acquisitionManager.InitializePhilosopher(philosopher);

            var stateProcessor = new PhilosopherStateProcessor(
                config, strategy, coordinator, acquisitionManager, metrics);

            var orchestrator = new SimulationOrchestrator(stateProcessor, acquisitionManager);

            var engine = new SimulationEngine(config, monitor, metrics, orchestrator);

            engine.Run(philosophers, forks);

            // Итоговый вывод
            var result = engine.GetResult();
            monitor.DisplaySummary(metrics, result);
        }

        public static async Task RunMultithreadedSimulationAsync()
        {
            IPhilosopherNamesProvider namesProvider =
                new FilePhilosopherNamesProvider("philosophers.txt");

            var names = namesProvider.GetNames().ToArray();

            // Доменные объекты с потокобезопасными вилками
            var philosophers = names.Select(n => new Philosopher(n)).ToList();
            var forks = Enumerable.Range(1, names.Length).Select(i => new ThreadSafeFork(i)).ToList();

            // Конфигурация многопоточной симуляции
            var config = new MultithreadedSimulationConfig();

            // В многопоточной версии не используем координатор
            var strategy = new HierarchyStrategy();

            var monitor = new MultithreadedConsoleMonitor(config.DisplayInterval);

            var engine = new MultithreadedSimulationEngine(
                config, monitor, strategy, philosophers, forks);

            var result = await engine.RunAsync();

            // Финальный вывод через адаптер
            monitor.DisplaySummary(engine.GetMetricsAdapter(), result);
        }
    }
}
