using Xunit;
using DiningPhilosophers.Services.Simulation.Multithreaded;
using DiningPhilosophers.Core.Models;

namespace DiningPhilosophers.Tests.Simulation
{
    public class ThreadSafeForkTests
    {
        // Тест проверяет, что одну и ту же вилку нельзя захватить дважды разными философами.
        // Шаги:
        // 1) Создаём вилку.
        // 2) Первый философ успешно захватывает вилку.
        // 3) Второй философ не может её захватить пока первая не отпустит.
        [Fact]
        public void TryAcquire_Fails_WhenAlreadyTaken()
        {
            var fork = new ThreadSafeFork(1);

            var ok1 = fork.TryAcquire("A");
            var ok2 = fork.TryAcquire("B");

            Assert.True(ok1);
            Assert.False(ok2);
            Assert.Equal(ForkState.InUse, fork.State);
            Assert.Equal("A", fork.Owner);
        }

        // Тест проверяет, что после Release вилку можно захватить снова.
        // Шаги:
        // 1) Захватываем вилку.
        // 2) Освобождаем.
        // 3) Другой философ успешно захватывает.
        [Fact]
        public void TryAcquire_Succeeds_AfterRelease()
        {
            var fork = new ThreadSafeFork(2);

            var ok1 = fork.TryAcquire("A");
            Assert.True(ok1);

            fork.Release();

            var ok2 = fork.TryAcquire("B");
            Assert.True(ok2);
            Assert.Equal("B", fork.Owner);
        }
    }
}
