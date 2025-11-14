using DiningPhilosophers.Core.Models;

namespace DiningPhilosophers.Core.Contracts.Simulation
{
    public interface IPhilosopherStateProcessor
    {
        void ProcessState(Philosopher philosopher, Fork leftFork, Fork rightFork, int step);
    }
}