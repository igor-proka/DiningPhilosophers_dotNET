using DiningPhilosophers.Core.Contracts.Strategies;
using DiningPhilosophers.Core.Models;

namespace DiningPhilosophers.Strategies
{
    public class CoordinatorStrategy : IPhilosopherStrategy
    {
        public PhilosopherAction Decide(Philosopher philosopher, Fork leftFork, Fork rightFork)
        {
            // Просто возвращаем текущее действие, которое установил координатор через событие
            return philosopher.CurrentAction;
        }
    }
}