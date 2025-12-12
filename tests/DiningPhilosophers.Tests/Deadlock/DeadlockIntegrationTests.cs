using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DiningPhilosophers.Services.Simulation;
using DiningPhilosophers.Strategies;
using DiningPhilosophers.Tests.Helpers;
using Xunit;

namespace DiningPhilosophers.Tests.Simulation
{
    // Интеграционные тесты дедлока для многопоточной симуляции.
    // Требование задачи:
    //
    // 1) Наивная стратегия должна приводить к дедлоку.
    // 2) Иерархическая стратегия должна предотвращать дедлок.
    //
    // Эти тесты запускают реальных философов в отдельных потоках и
    // анализируют их состояние через DeadlockChecker.
    public class DeadlockIntegrationTests
    {
        // Наивная стратегия:
        // Все философы сначала пытаются взять левую вилку → образуется цикл владения.
        // Через 200–300 мс обязательный дедлок.
        [Fact]
        public async Task NaiveStrategy_LeadsToDeadlock()
        {
            var philosophers = SimulationTestFactory.CreatePhilosophers5();
            var forks = SimulationTestFactory.CreateForks5();
            var processors = SimulationTestFactory.CreateProcessors(philosophers, forks, new NaiveStrategy());

            using var cts = new CancellationTokenSource(500);

            foreach (var processor in processors)
                _ = Task.Run(() => processor.RunAsync(cts.Token));

            // Даём время симуляции войти в дедлок
            await Task.Delay(300);

            var checker = new DeadlockChecker();
            bool deadlock = checker.CheckDeadlock(philosophers);

            Assert.True(deadlock);
        }

        // Стратегия иерархии ресурсов:
        // Философы всегда захватывают вилки в порядке возрастания ID.
        // Это *гарантированно* предотвращает дедлок.
        [Fact]
        public async Task HierarchyStrategy_DoesNotLeadToDeadlock()
        {
            var philosophers = SimulationTestFactory.CreatePhilosophers5();
            var forks = SimulationTestFactory.CreateForks5();
            var processors = SimulationTestFactory.CreateProcessors(philosophers, forks, new HierarchyStrategy());

            using var cts = new CancellationTokenSource(500);

            foreach (var processor in processors)
                _ = Task.Run(() => processor.RunAsync(cts.Token));

            // Даем симуляции поработать, как и в предыдущем тесте
            await Task.Delay(300);

            var checker = new DeadlockChecker();
            bool deadlock = checker.CheckDeadlock(philosophers);

            Assert.False(deadlock);
        }
    }
}
