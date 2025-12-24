using System;
using System.Threading.Tasks;
using DiningPhilosophers.View.Models;

namespace DiningPhilosophers.View.Services
{
    public interface ISimulationStateService
    {
        Task<SimulationStateResult> GetStateAsync(Guid runId, double delaySeconds);
    }
}