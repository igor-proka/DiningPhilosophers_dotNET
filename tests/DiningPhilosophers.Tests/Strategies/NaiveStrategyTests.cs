using DiningPhilosophers.Core.Models;
using DiningPhilosophers.Strategies;
using Xunit;

namespace DiningPhilosophers.Tests.Strategies
{
    // Тесты наивной стратегии (NaiveStrategy).
    // Логика стратегии:
    //  - если философ не голоден → действие None
    //  - если философ не держит вилок → пытается взять левую
    //  - если философ держит левую, но правая свободна → берет правую
    //  - если философ держит левую, а правая занята → отпускает левую
    //  - если обе заняты → None
    public class NaiveStrategyTests
    {
        // Проверяет сценарий:
        // 1. Философ голоден и не держит ни одной вилки.
        // 2. Обе вилки свободны.
        // 3. Ожидаем, что наивная стратегия скажет взять левую вилку.
        [Fact]
        public void Decide_TakeLeftFirst_WhenNoForksHeld()
        {
            var strategy = new NaiveStrategy();
            var philosopher = new Philosopher("Test") { State = PhilosopherState.Hungry };
            var leftFork = new Fork(1) { State = ForkState.Available };
            var rightFork = new Fork(2) { State = ForkState.Available };

            var action = strategy.Decide(philosopher, leftFork, rightFork);

            Assert.Equal(PhilosopherAction.TakeLeftFork, action);
        }

        // Проверяет сценарий:
        // 1. Философ голоден и уже держит левую вилку.
        // 2. Правая занята другим.
        // 3. Ожидаем, что стратегия скажет отпустить левую вилку,
        //    так как взять правую невозможно.
        // [Fact]
        // public void Decide_ReleaseLeft_WhenLeftHeldAndRightInUse()
        // {
        //     var strategy = new NaiveStrategy();
        //     var philosopher = new Philosopher("Test")
        //     {
        //         State = PhilosopherState.Hungry,
        //         HasLeftFork = true
        //     };
        //     var leftFork = new Fork(1) { State = ForkState.InUse };
        //     var rightFork = new Fork(2) { State = ForkState.InUse };

        //     var action = strategy.Decide(philosopher, leftFork, rightFork);

        //     Assert.Equal(PhilosopherAction.ReleaseLeftFork, action);
        // }

        // Проверяет сценарий:
        // 1. Философ голоден и уже держит левую вилку.
        // 2. Правая свободна.
        // 3. Ожидаем, что стратегия скажет взять правую вилку.
        [Fact]
        public void Decide_TakeRight_WhenLeftHeldAndRightAvailable()
        {
            var strategy = new NaiveStrategy();
            var philosopher = new Philosopher("Test")
            {
                State = PhilosopherState.Hungry,
                HasLeftFork = true
            };
            var leftFork = new Fork(1) { State = ForkState.InUse };
            var rightFork = new Fork(2) { State = ForkState.Available };

            var action = strategy.Decide(philosopher, leftFork, rightFork);

            Assert.Equal(PhilosopherAction.TakeRightFork, action);
        }

        // Проверяет сценарий:
        // 1. Философ НЕ голоден (например, Thinking).
        // 2. Ожидаем действие None.
        [Fact]
        public void Decide_None_WhenNotHungry()
        {
            var strategy = new NaiveStrategy();
            var philosopher = new Philosopher("Test") { State = PhilosopherState.Thinking };
            var leftFork = new Fork(1);
            var rightFork = new Fork(2);

            var action = strategy.Decide(philosopher, leftFork, rightFork);

            Assert.Equal(PhilosopherAction.None, action);
        }

        // Проверяет сценарий:
        // 1. Философ держит ПРАВУЮ вилку.
        // 2. Левая свободна.
        // 3. Наивная стратегия должна сказать "взять левую".
        [Fact]
        public void Decide_TakeLeft_WhenRightHeld_AndLeftAvailable()
        {
            var strategy = new NaiveStrategy();
            var philosopher = new Philosopher("Test")
            {
                State = PhilosopherState.Hungry,
                HasRightFork = true
            };
            var leftFork = new Fork(1) { State = ForkState.Available };
            var rightFork = new Fork(2) { State = ForkState.InUse };

            var action = strategy.Decide(philosopher, leftFork, rightFork);

            Assert.Equal(PhilosopherAction.TakeLeftFork, action);
        }

        // Проверяет сценарий:
        // 1. Философ голоден и НЕ держит левую вилку.
        // 2. Левая вилка занята (у другого философа), правая — свободна.
        // 3. Наивная стратегия в этой реализации НЕ пробует брать правую, если левая недоступна,
        //    поэтому ожидаем действие None.
        // 
        // Примечание:
        // философ сначала всегда пробует левую; если левую взять нельзя, он не переключается на правую.
        [Fact]
        public void Decide_None_WhenLeftInUse_AndRightAvailable_NaiveBehavior()
        {
            var strategy = new NaiveStrategy();
            var philosopher = new Philosopher("Test")
            {
                State = PhilosopherState.Hungry
                // HasLeftFork = false по умолчанию
            };
            var leftFork = new Fork(1) { State = ForkState.InUse };     // левая занята другим
            var rightFork = new Fork(2) { State = ForkState.Available }; // правая свободна

            var action = strategy.Decide(philosopher, leftFork, rightFork);

            Assert.Equal(PhilosopherAction.None, action);
        }

        // Проверяет сценарий:
        // 1. Философ уже держит обе вилки.
        // 2. Это означает, что он должен перейти в Eating.
        // 3. Стратегия должна вернуть None — никаких новых действий не нужно.
        [Fact]
        public void Decide_None_WhenPhilosopherAlreadyHasBothForks()
        {
            var strategy = new NaiveStrategy();
            var philosopher = new Philosopher("Test")
            {
                State = PhilosopherState.Eating,
                HasLeftFork = true,
                HasRightFork = true
            };
            var leftFork = new Fork(1) { State = ForkState.InUse };
            var rightFork = new Fork(2) { State = ForkState.InUse };

            var action = strategy.Decide(philosopher, leftFork, rightFork);

            Assert.Equal(PhilosopherAction.None, action);
        }
    }
}
