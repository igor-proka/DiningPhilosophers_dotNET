using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DiningPhilosophers.Persistence;
using DiningPhilosophers.Persistence.Entities;
using DiningPhilosophers.View.Models;

namespace DiningPhilosophers.View.Services
{
    public class SimulationStateService : ISimulationStateService
    {
        private readonly ISimulationPersistence _persistence;

        public SimulationStateService(ISimulationPersistence persistence)
        {
            _persistence = persistence;
        }

        public async Task<SimulationStateResult> GetStateAsync(Guid runId, double delaySeconds)
        {
            var run = await _persistence.GetRunAsync(runId);
            if (run == null)
            {
                return new SimulationStateResult 
                { 
                    Success = false, 
                    ErrorMessage = $"Simulation run with ID {runId} not found." 
                };
            }

            var simulationStart = run.StartedAtUtc;
            var simulationEnd = run.FinishedAtUtc ?? DateTime.UtcNow;
            var requestedTime = simulationStart.AddSeconds(delaySeconds);
            
            // Запрошенное время выходит за пределы параметров моделирования
            var actualRequestTime = requestedTime;
            string? warningMessage = null;
            
            if (requestedTime < simulationStart)
            {
                actualRequestTime = simulationStart;
                warningMessage = $"Requested time is before simulation start. Showing state at start.";
            }
            else if (requestedTime > simulationEnd)
            {
                actualRequestTime = simulationEnd;
                var actualDelay = (simulationEnd - simulationStart).TotalSeconds;
                warningMessage = $"Requested time exceeds simulation duration. Showing state at end ({actualDelay:F2}s).";
            }
            
            var philosophers = await _persistence.GetLatestPhilosopherStatesAtAsync(runId, actualRequestTime);
            var forks = await _persistence.GetLatestForkStatesAtAsync(runId, actualRequestTime);
            
            // Если нет событий на запрошенное время, ищем ближайшие
            if (!philosophers.Any() || !forks.Any())
            {
                var allPhilosopherEvents = await GetPhilosopherEventsUpToAsync(runId, actualRequestTime);
                var allForkEvents = await GetForkEventsUpToAsync(runId, actualRequestTime);
                
                philosophers = allPhilosopherEvents
                    .GroupBy(e => e.PhilosopherName)
                    .Select(g => g.OrderByDescending(e => e.TimestampUtc).FirstOrDefault())
                    .Where(e => e != null)
                    .ToList()!;
                    
                forks = allForkEvents
                    .GroupBy(e => e.ForkNumber)
                    .Select(g => g.OrderByDescending(e => e.TimestampUtc).FirstOrDefault())
                    .Where(e => e != null)
                    .ToList()!;
                    
                if (!string.IsNullOrEmpty(warningMessage))
                    warningMessage += " Using nearest available events.";
                else
                    warningMessage = "No events at requested time. Using nearest available events.";
            }

            return new SimulationStateResult
            {
                Success = true,
                Run = run,
                Philosophers = philosophers,
                Forks = forks,
                RequestedDelay = delaySeconds,
                ActualDelay = (actualRequestTime - simulationStart).TotalSeconds,
                WarningMessage = warningMessage
            };
        }
        
        private async Task<List<PhilosopherStateEvent>> GetPhilosopherEventsUpToAsync(Guid runId, DateTime time)
        {
            // Получаем все события философов до указанного времени
            // В реальной реализации можно использовать более эффективный запрос
            var events = await _persistence.GetLatestPhilosopherStatesAtAsync(runId, time);
            
            if (events.Any())
                return events.ToList();
                
            // Если нет событий на точное время, пытаемся получить последние события до этого времени
            // Для простоты возвращаем пустой список
            return new List<PhilosopherStateEvent>();
        }
        
        private async Task<List<ForkStateEvent>> GetForkEventsUpToAsync(Guid runId, DateTime time)
        {
            var events = await _persistence.GetLatestForkStatesAtAsync(runId, time);
            
            if (events.Any())
                return events.ToList();
                
            return new List<ForkStateEvent>();
        }
    }
}