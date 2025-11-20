using DiningPhilosophers.Core.Contracts.Strategies;
using DiningPhilosophers.Core.Models;

namespace DiningPhilosophers.Strategies
{
    public class NaiveStrategy : IPhilosopherStrategy
    {
        public PhilosopherAction Decide(Philosopher philosopher, Fork leftFork, Fork rightFork)
        {
            if (philosopher.State != PhilosopherState.Hungry)
                return PhilosopherAction.None;

            if (philosopher.HasLeftFork && philosopher.HasRightFork)
                return PhilosopherAction.None;

            // 1. Если у философа нет левой вилки — пытаемся взять её
            if (!philosopher.HasLeftFork)
            {
                if (leftFork.State == ForkState.Available)
                    return PhilosopherAction.TakeLeftFork;
                return PhilosopherAction.None;
            }

            // 2. Если есть левая, но нет правой
            if (philosopher.HasLeftFork && !philosopher.HasRightFork)
            {
                if (rightFork.State == ForkState.Available)
                    return PhilosopherAction.TakeRightFork;
                else
                    return PhilosopherAction.ReleaseLeftFork; // отпускаем левую, если правая занята
            }

            return PhilosopherAction.None;
        }
    }
}
