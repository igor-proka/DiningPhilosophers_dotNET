using System.Collections.Generic;
using DiningPhilosophers.Core.Models;
using DiningPhilosophers.Services.Simulation;
using Xunit;

namespace DiningPhilosophers.Tests.Deadlock
{
    // Юнит-тесты DeadlockChecker — простой логический проверяющий класс,
    // определяющий дедлок по двум условиям:
    //   1) Все философы Hungry
    //   2) Каждый держит ровно одну вилку (левая XOR правая)
    public class DeadlockCheckerTests
    {
        private readonly DeadlockChecker _checker = new DeadlockChecker();

        // Сценарий: классический дедлок.
        // Все философы — Hungry, каждый держит одну левую вилку.
        // Ожидание: CheckDeadlock возвращает true.
        [Fact]
        public void CheckDeadlock_ReturnsTrue_WhenAllHungryAndEachHoldsOneLeftFork()
        {
            var philosophers = new List<Philosopher>
            {
                new Philosopher("P1") { State = PhilosopherState.Hungry, HasLeftFork = true },
                new Philosopher("P2") { State = PhilosopherState.Hungry, HasLeftFork = true },
                new Philosopher("P3") { State = PhilosopherState.Hungry, HasLeftFork = true },
                new Philosopher("P4") { State = PhilosopherState.Hungry, HasLeftFork = true },
                new Philosopher("P5") { State = PhilosopherState.Hungry, HasLeftFork = true }
            };

            Assert.True(_checker.CheckDeadlock(philosophers));
        }

        // Сценарий: Аналогично предыдущему, только все держат правую вилку.
        [Fact]
        public void CheckDeadlock_ReturnsTrue_WhenAllHungryAndEachHoldsOneRightFork()
        {
            var philosophers = new List<Philosopher>
            {
                new Philosopher("P1") { State = PhilosopherState.Hungry, HasRightFork = true },
                new Philosopher("P2") { State = PhilosopherState.Hungry, HasRightFork = true },
                new Philosopher("P3") { State = PhilosopherState.Hungry, HasRightFork = true },
                new Philosopher("P4") { State = PhilosopherState.Hungry, HasRightFork = true },
                new Philosopher("P5") { State = PhilosopherState.Hungry, HasRightFork = true }
            };

            Assert.True(_checker.CheckDeadlock(philosophers));
        }

        // Сценарий: один философ не Hungry → дедлока нет.
        [Fact]
        public void CheckDeadlock_ReturnsFalse_WhenNotAllHungry()
        {
            var philosophers = new List<Philosopher>
            {
                new Philosopher("P1") { State = PhilosopherState.Thinking, HasLeftFork = true },
                new Philosopher("P2") { State = PhilosopherState.Hungry, HasLeftFork = true },
                new Philosopher("P3") { State = PhilosopherState.Hungry, HasLeftFork = true },
                new Philosopher("P4") { State = PhilosopherState.Hungry, HasLeftFork = true },
                new Philosopher("P5") { State = PhilosopherState.Hungry, HasLeftFork = true }
            };

            Assert.False(_checker.CheckDeadlock(philosophers));
        }

        // Сценарий: один философ держит обе вилки → не подходит под XOR правило,
        // следовательно дедлока нет.
        [Fact]
        public void CheckDeadlock_ReturnsFalse_WhenSomeHoldBothForks()
        {
            var philosophers = new List<Philosopher>
            {
                new Philosopher("P1") { State = PhilosopherState.Hungry, HasLeftFork = true, HasRightFork = true },
                new Philosopher("P2") { State = PhilosopherState.Hungry, HasLeftFork = true },
                new Philosopher("P3") { State = PhilosopherState.Hungry, HasLeftFork = true },
                new Philosopher("P4") { State = PhilosopherState.Hungry, HasLeftFork = true },
                new Philosopher("P5") { State = PhilosopherState.Hungry, HasLeftFork = true }
            };

            Assert.False(_checker.CheckDeadlock(philosophers));
        }

        // Сценарий: один философ держит 0 вилок → дедлока нет.
        [Fact]
        public void CheckDeadlock_ReturnsFalse_WhenSomeHoldNoForks()
        {
            var philosophers = new List<Philosopher>
            {
                new Philosopher("P1") { State = PhilosopherState.Hungry, HasLeftFork = false, HasRightFork = false },
                new Philosopher("P2") { State = PhilosopherState.Hungry, HasLeftFork = true },
                new Philosopher("P3") { State = PhilosopherState.Hungry, HasLeftFork = true },
                new Philosopher("P4") { State = PhilosopherState.Hungry, HasLeftFork = true },
                new Philosopher("P5") { State = PhilosopherState.Hungry, HasLeftFork = true }
            };

            Assert.False(_checker.CheckDeadlock(philosophers));
        }

        // Сценарий: список пустой → дедлока нет.
        [Fact]
        public void CheckDeadlock_ReturnsFalse_WhenEmptyList()
        {
            var philosophers = new List<Philosopher>();

            Assert.False(_checker.CheckDeadlock(philosophers));
        }
    }
}
