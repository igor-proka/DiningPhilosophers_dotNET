using System;
using System.Collections.Generic;
using DiningPhilosophers.Core.Models;
using DiningPhilosophers.Core.Contracts.Strategies;
using DiningPhilosophers.Services.Simulation;
using DiningPhilosophers.Services.Simulation.Multithreaded;

namespace DiningPhilosophers.Tests.Helpers
{
    // Фабрика для создания философов, вилок и процессоров для интеграционных тестов.
    // Параметры подобраны так, чтобы повысить шанс появления дедлок-паттерна (для Naive).
    public static class SimulationTestFactory
    {
        public static List<Philosopher> CreatePhilosophers5()
        {
            return new List<Philosopher>
            {
                new Philosopher("P1"),
                new Philosopher("P2"),
                new Philosopher("P3"),
                new Philosopher("P4"),
                new Philosopher("P5")
            };
        }

        public static List<ThreadSafeFork> CreateForks5()
        {
            return new List<ThreadSafeFork>
            {
                new ThreadSafeFork(1),
                new ThreadSafeFork(2),
                new ThreadSafeFork(3),
                new ThreadSafeFork(4),
                new ThreadSafeFork(5)
            };
        }

        // Создаёт процессоры. Параметры конфигурации настроены для тестов:
        // - очень короткое время думания (чтобы все одновременно стали Hungry),
        // - небольшое время взятия вилки (реализует небольшую задержку, но TryAcquire — мгновенный),
        // - детерминированный Random (new Random(seed)).
        public static List<MultithreadedPhilosopherStateProcessor> CreateProcessors(
            List<Philosopher> philosophers,
            List<ThreadSafeFork> forks,
            IPhilosopherStrategy strategy)
        {
            var collector = new FakeMetricsCollector();
            var processors = new List<MultithreadedPhilosopherStateProcessor>();

            for (int i = 0; i < philosophers.Count; i++)
            {
                var left = forks[i];
                var right = forks[(i + 1) % forks.Count];

                var config = new MultithreadedSimulationConfig
                {
                    // Минимальное и синхронное время думания — ускоряет попадание в hungry у всех одновременно
                    ThinkingTimeMin = 1,
                    ThinkingTimeMax = 1,

                    // Небольшое время еды
                    EatingTimeMin = 3,
                    EatingTimeMax = 3,

                    // Небольшое время захвата (имитируем небольшую задержку после TryAcquire)
                    ForkAcquisitionTime = 1
                };

                var processor = new MultithreadedPhilosopherStateProcessor(
                    philosophers[i],
                    left,
                    right,
                    config,
                    strategy,
                    new ThreadSafeForkAcquisitionManager(config.ForkAcquisitionTime),
                    collector,
                    new Random(0) // фиксируем seed для воспроизводимости
                );

                processors.Add(processor);
            }

            return processors;
        }
    }
}
