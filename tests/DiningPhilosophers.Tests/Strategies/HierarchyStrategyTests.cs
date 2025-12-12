using DiningPhilosophers.Core.Models;
using DiningPhilosophers.Strategies;
using Xunit;

namespace DiningPhilosophers.Tests.Strategies
{
    // Тесты для стратегии иерархии ресурсов (HierarchyStrategy).
    //
    // Стратегия:
    //  - Определяет нижнюю вилку (lowerFork) по меньшему ID.
    //  - Сначала пытается взять нижнюю вилку.
    //  - Лишь после её взятия философ может пытаться взять верхнюю.
    //  - Если нижняя уже в руках, а верхняя занята → философ отпускает нижнюю.
    //  - Если философ не голоден или имеет обе вилки → действие None.
    public class HierarchyStrategyTests
    {
        // Сценарий:
        // 1. Lower = левая (1), Higher = правая (2).
        // 2. Философ голоден и не держит вилок.
        // 3. Обе вилки доступны.
        // Ожидание: стратегия должна взять нижнюю вилку — левую.
        [Fact]
        public void Decide_TakeLowerFirst_WhenLowerIsLeftAndAvailable()
        {
            var strategy = new HierarchyStrategy();
            var philosopher = new Philosopher("Test") { State = PhilosopherState.Hungry };
            var leftFork = new Fork(1) { State = ForkState.Available }; // lower
            var rightFork = new Fork(2) { State = ForkState.Available }; // higher

            var action = strategy.Decide(philosopher, leftFork, rightFork);

            Assert.Equal(PhilosopherAction.TakeLeftFork, action);
        }

        // Сценарий:
        // 1. Lower = правая (ID = 1), Higher = левая (ID = 2)
        // 2. Обе вилки доступны.
        // Ожидание: стратегия должна взять нижнюю вилку — правую.
        [Fact]
        public void Decide_TakeLowerFirst_WhenLowerIsRightAndAvailable()
        {
            var strategy = new HierarchyStrategy();
            var philosopher = new Philosopher("Test") { State = PhilosopherState.Hungry };
            var leftFork = new Fork(2) { State = ForkState.Available }; // higher
            var rightFork = new Fork(1) { State = ForkState.Available }; // lower

            var action = strategy.Decide(philosopher, leftFork, rightFork);

            Assert.Equal(PhilosopherAction.TakeRightFork, action);
        }

        // Сценарий:
        // 1. Философ держит нижнюю вилку.
        // 2. Верхняя занята.
        // Ожидание: стратегия должна приказать отпустить нижнюю вилку.
        [Fact]
        public void Decide_ReleaseLower_WhenLowerHeldAndHigherInUse()
        {
            var strategy = new HierarchyStrategy();
            var philosopher = new Philosopher("Test")
            {
                State = PhilosopherState.Hungry,
                HasLeftFork = true // эта вилка нижняя — ID 1
            };
            var leftFork = new Fork(1) { State = ForkState.InUse }; // lower
            var rightFork = new Fork(2) { State = ForkState.InUse }; // higher, occupied by someone else

            var action = strategy.Decide(philosopher, leftFork, rightFork);

            Assert.Equal(PhilosopherAction.ReleaseLeftFork, action);
        }

        // Сценарий:
        // 1. У философа есть нижняя вилка.
        // 2. Верхняя свободна.
        // Ожидание: стратегия должна приказать взять верхнюю.
        [Fact]
        public void Decide_TakeHigher_WhenLowerHeldAndHigherAvailable()
        {
            var strategy = new HierarchyStrategy();
            var philosopher = new Philosopher("Test")
            {
                State = PhilosopherState.Hungry,
                HasLeftFork = true // lower
            };
            var leftFork = new Fork(1) { State = ForkState.InUse };
            var rightFork = new Fork(2) { State = ForkState.Available }; // higher

            var action = strategy.Decide(philosopher, leftFork, rightFork);

            Assert.Equal(PhilosopherAction.TakeRightFork, action);
        }

        // Сценарий:
        // Философ не голоден → стратегия всегда None.
        [Fact]
        public void Decide_None_WhenNotHungry()
        {
            var strategy = new HierarchyStrategy();
            var philosopher = new Philosopher("Test") { State = PhilosopherState.Thinking };
            var leftFork = new Fork(1);
            var rightFork = new Fork(2);

            var action = strategy.Decide(philosopher, leftFork, rightFork);

            Assert.Equal(PhilosopherAction.None, action);
        }

        // Сценарий:
        // 1. Нижняя вилка занята другим философом.
        // 2. Верхняя свободна.
        // Ожидание: стратегия НЕ должна брать верхнюю,
        //           так как сначала всегда берём lower.
        //           => действие None.
        [Fact]
        public void Decide_None_WhenLowerInUse_AndHigherAvailable()
        {
            var strategy = new HierarchyStrategy();
            var philosopher = new Philosopher("Test") { State = PhilosopherState.Hungry };

            var leftFork = new Fork(1) { State = ForkState.InUse }; // lower (busy)
            var rightFork = new Fork(2) { State = ForkState.Available }; // higher free

            var action = strategy.Decide(philosopher, leftFork, rightFork);

            Assert.Equal(PhilosopherAction.None, action);
        }

        // Сценарий:
        // 1. Философ держит ВЕРХНЮЮ вилку
        // 2. Нижняя свободна.
        // Ожидание: стратегия должна приказать взять нижнюю,
        //           т.к. она всегда приоритетнее.
        [Fact]
        public void Decide_TakeLower_WhenHigherHeld_AndLowerAvailable()
        {
            var strategy = new HierarchyStrategy();
            var philosopher = new Philosopher("Test")
            {
                State = PhilosopherState.Hungry,
                HasRightFork = true // допустим, это higher
            };

            var leftFork = new Fork(1) { State = ForkState.Available }; // lower
            var rightFork = new Fork(2) { State = ForkState.InUse }; // higher

            var action = strategy.Decide(philosopher, leftFork, rightFork);

            Assert.Equal(PhilosopherAction.TakeLeftFork, action);
        }

        // Сценарий:
        // Если философ держит обе вилки,
        // стратегия должна вернуть None.
        [Fact]
        public void Decide_None_WhenPhilosopherHasBothForks()
        {
            var strategy = new HierarchyStrategy();
            var philosopher = new Philosopher("Test")
            {
                State = PhilosopherState.Hungry,
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
