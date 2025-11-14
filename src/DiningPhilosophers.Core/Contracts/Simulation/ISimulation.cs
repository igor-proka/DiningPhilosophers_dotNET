using System.Collections.Generic;
using DiningPhilosophers.Core.Models;

namespace DiningPhilosophers.Core.Contracts.Simulation
{
    public interface ISimulation
    {
        void Run(IEnumerable<Philosopher> philosophers, IList<Fork> forks);
        SimulationResult GetResult();
    }
}