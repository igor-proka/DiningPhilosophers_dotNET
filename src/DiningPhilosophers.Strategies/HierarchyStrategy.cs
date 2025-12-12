using DiningPhilosophers.Core.Contracts.Strategies;
using DiningPhilosophers.Core.Models;

namespace DiningPhilosophers.Strategies
{
    public class HierarchyStrategy : IPhilosopherStrategy
    {
        public PhilosopherAction Decide(Philosopher philosopher, Fork leftFork, Fork rightFork)
        {
            if (philosopher.State != PhilosopherState.Hungry)
                return PhilosopherAction.None;

            if (philosopher.HasLeftFork && philosopher.HasRightFork)
                return PhilosopherAction.None;

            // Определяем нижнюю и верхнюю вилки по ID
            Fork lowerFork = leftFork.Id < rightFork.Id ? leftFork : rightFork;
            Fork higherFork = leftFork.Id < rightFork.Id ? rightFork : leftFork;

            bool hasLower = lowerFork == leftFork ? philosopher.HasLeftFork : philosopher.HasRightFork;
            bool hasHigher = higherFork == leftFork ? philosopher.HasLeftFork : philosopher.HasRightFork;

            PhilosopherAction lowerAction = lowerFork == leftFork ? PhilosopherAction.TakeLeftFork : PhilosopherAction.TakeRightFork;
            PhilosopherAction higherAction = higherFork == leftFork ? PhilosopherAction.TakeLeftFork : PhilosopherAction.TakeRightFork;
            PhilosopherAction releaseLower = lowerFork == leftFork ? PhilosopherAction.ReleaseLeftFork : PhilosopherAction.ReleaseRightFork;

            // 1. Если нет нижней вилки — пытаемся взять её, если доступна
            if (!hasLower)
            {
                if (lowerFork.State == ForkState.Available)
                    return lowerAction;
                return PhilosopherAction.None;
            }

            // 2. Если есть нижняя, но нет верхней
            if (hasLower && !hasHigher)
            {
                if (higherFork.State == ForkState.Available)
                    return higherAction;
                else
                    return releaseLower; // отпускаем нижнюю, если верхняя занята
            }

            return PhilosopherAction.None;
        }
    }
}