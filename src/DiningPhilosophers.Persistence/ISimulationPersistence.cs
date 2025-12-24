using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DiningPhilosophers.Persistence.Entities;

namespace DiningPhilosophers.Persistence
{
    public interface ISimulationPersistence
    {
        // Создает и возвращает новый идентификатор запуска для симуляции
        Task<Guid> CreateRunAsync(string? optionsJson = null);

        // Помечает выполнение симуляции как завершенное и выводит окончательную информацию
        Task SetRunFinishedAsync(Guid runId);

        // Зарегистрировать событие для состояния философа (атомарное сохранение)
        Task LogPhilosopherEventAsync(Guid runId, PhilosopherStateEvent evt);

        // Зарегистрировать событие для состояния форка (атомарное сохранение)
        Task LogForkEventAsync(Guid runId, ForkStateEvent evt);

        // Вспомогательные функции, используемые DiningPhilosophers.View
        Task<SimulationRun?> GetRunAsync(Guid runId);
        Task<IReadOnlyList<PhilosopherStateEvent>> GetLatestPhilosopherStatesAtAsync(Guid runId, DateTime cutoffUtc);
        Task<IReadOnlyList<ForkStateEvent>> GetLatestForkStatesAtAsync(Guid runId, DateTime cutoffUtc);
    }
}
