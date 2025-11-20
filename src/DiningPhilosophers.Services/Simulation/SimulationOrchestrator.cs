using System.Collections.Generic;
using System.Linq;
using DiningPhilosophers.Core.Contracts.Simulation;
using DiningPhilosophers.Core.Models;

namespace DiningPhilosophers.Services.Simulation
{
    public class SimulationOrchestrator : ISimulationOrchestrator
    {
        private readonly IPhilosopherStateProcessor _stateProcessor;
        private readonly ForkAcquisitionManager _acquisitionManager;

        public SimulationOrchestrator(IPhilosopherStateProcessor stateProcessor, ForkAcquisitionManager acquisitionManager)
        {
            _stateProcessor = stateProcessor;
            _acquisitionManager = acquisitionManager;
        }

        public void ExecuteStep(int step, IList<Philosopher> philosophers, IList<Fork> forks)
        {
            for (int i = 0; i < philosophers.Count; i++)
            {
                var philosopher = philosophers[i];
                var leftFork = GetLeftFork(forks, i);
                var rightFork = GetRightFork(forks, i);

                _stateProcessor.ProcessState(philosopher, leftFork, rightFork, step);
            }
        }

        public bool CheckDeadlock(IList<Philosopher> philosophers)
        {
            if (philosophers.Count == 0) return false;
            
            return philosophers.All(p => p.State == PhilosopherState.Hungry) &&
                   philosophers.All(p => p.HasLeftFork ^ p.HasRightFork);
        }

        private Fork GetLeftFork(IList<Fork> forks, int philosopherIndex)
        {
            return forks[(philosopherIndex + forks.Count - 1) % forks.Count];
        }

        private Fork GetRightFork(IList<Fork> forks, int philosopherIndex)
        {
            return forks[philosopherIndex % forks.Count];
        }
    }
}