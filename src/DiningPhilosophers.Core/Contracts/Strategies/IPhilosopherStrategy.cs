using DiningPhilosophers.Core.Models;

namespace DiningPhilosophers.Core.Contracts.Strategies
{
    public interface IPhilosopherStrategy
    {
        PhilosopherAction Decide(Philosopher philosopher, Fork leftFork, Fork rightFork);
    }
}