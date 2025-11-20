using System.Collections.Generic;
using DiningPhilosophers.Core.Models;

namespace DiningPhilosophers.Core.Contracts.Simulation
{
    public interface ISimulationOrchestrator
    {
        void ExecuteStep(int step, IList<Philosopher> philosophers, IList<Fork> forks);
        bool CheckDeadlock(IList<Philosopher> philosophers);
    }
}