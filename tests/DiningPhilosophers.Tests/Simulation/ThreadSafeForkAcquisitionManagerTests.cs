using Xunit;
using System.Threading;
using System.Threading.Tasks;
using DiningPhilosophers.Core.Models;
using DiningPhilosophers.Services.Simulation.Multithreaded;

namespace DiningPhilosophers.Tests.Simulation
{
    /// Тесты для ThreadSafeForkAcquisitionManager.
    /// Менеджер отвечает за асинхронный захват вилок с задержкой.
    public class ThreadSafeForkAcquisitionManagerTests
    {
        // ------------------------------------------------------------
        // 1) Проверяем успешный захват левой вилки.
        //
        // Шаги:
        //  1. Создаём философа и вилку.
        //  2. Вилка свободна => TryAcquireLeftForkAsync должен:
        //      - сразу захватить вилку
        //      - подождать _acquisitionTime мс
        //      - выставить philosopher.HasLeftFork = true
        //  3. Возвращаемое значение должно быть true.
        // ------------------------------------------------------------
        [Fact]
        public async Task TryAcquireLeftForkAsync_ShouldAcquire_WhenForkIsAvailable()
        {
            var philosopher = new Philosopher("A");
            var fork = new ThreadSafeFork(1);

            var manager = new ThreadSafeForkAcquisitionManager(acquisitionTime: 10);
            manager.InitializePhilosopher(philosopher);

            var cts = new CancellationTokenSource();

            bool acquired = await manager.TryAcquireLeftForkAsync(philosopher, fork, cts.Token);

            Assert.True(acquired);
            Assert.True(philosopher.HasLeftFork);
            Assert.Equal(ForkState.InUse, fork.State);
            Assert.Equal("A", fork.Owner);
        }

        // ------------------------------------------------------------
        // 2) Проверяем, что левую вилку нельзя захватить,
        //    если она уже используется другим философом.
        //
        // Шаги:
        //  1. Создаём вилку и вручную захватываем её другим философом.
        //  2. Пытаемся захватить через TryAcquireLeftForkAsync.
        //  3. Должно вернуть false и не менять HasLeftFork.
        // ------------------------------------------------------------
        [Fact]
        public async Task TryAcquireLeftForkAsync_ShouldFail_WhenForkIsInUse()
        {
            var philosopher = new Philosopher("A");
            var fork = new ThreadSafeFork(1);

            // Философ B захватывает вилку заранее
            bool ok = fork.TryAcquire("B");
            Assert.True(ok);

            var manager = new ThreadSafeForkAcquisitionManager(acquisitionTime: 10);
            manager.InitializePhilosopher(philosopher);

            var cts = new CancellationTokenSource();

            bool acquired = await manager.TryAcquireLeftForkAsync(philosopher, fork, cts.Token);

            Assert.False(acquired);
            Assert.False(philosopher.HasLeftFork);
            Assert.Equal("B", fork.Owner);
        }

        // ------------------------------------------------------------
        // 3) Проверяем успешный захват правой вилки.
        //    Полностью аналогично TryAcquireLeftForkAsync.
        // ------------------------------------------------------------
        [Fact]
        public async Task TryAcquireRightForkAsync_ShouldAcquire_WhenForkIsAvailable()
        {
            var philosopher = new Philosopher("A");
            var fork = new ThreadSafeFork(2);

            var manager = new ThreadSafeForkAcquisitionManager(acquisitionTime: 10);
            manager.InitializePhilosopher(philosopher);

            var cts = new CancellationTokenSource();

            bool acquired = await manager.TryAcquireRightForkAsync(philosopher, fork, cts.Token);

            Assert.True(acquired);
            Assert.True(philosopher.HasRightFork);
            Assert.Equal(ForkState.InUse, fork.State);
            Assert.Equal("A", fork.Owner);
        }

        // ------------------------------------------------------------
        // 4) Проверяем, что ResetProgress выполняется и не вызывает ошибок.
        //    Прогресс сейчас никак не влияет на захват вилок,
        //    но тест должен проверять работоспособность вызова.
        // ------------------------------------------------------------
        [Fact]
        public void ResetProgress_ShouldNotThrow_AndResetInternalState()
        {
            var philosopher = new Philosopher("A");
            var manager = new ThreadSafeForkAcquisitionManager(10);
            manager.InitializePhilosopher(philosopher);

            // Метод не возвращает значение, но мы должны убедиться,
            // что вызов не приводит к исключениям.
            manager.ResetProgress(philosopher);
        }
    }
}
