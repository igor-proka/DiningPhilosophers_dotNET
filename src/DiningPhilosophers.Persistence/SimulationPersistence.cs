using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DiningPhilosophers.Persistence.Entities;

namespace DiningPhilosophers.Persistence
{
    public class SimulationPersistence : ISimulationPersistence
    {
        private readonly IDbContextFactory<SimulationDbContext> _factory;
        private readonly ILogger<SimulationPersistence>? _logger;

        public SimulationPersistence(IDbContextFactory<SimulationDbContext> factory, ILogger<SimulationPersistence>? logger = null)
        {
            _factory = factory;
            _logger = logger;
        }

        public async Task<Guid> CreateRunAsync(string? optionsJson = null)
        {
            using var db = _factory.CreateDbContext();
            var run = new SimulationRun
            {
                StartedAtUtc = DateTime.UtcNow,
                OptionsJson = optionsJson
            };

            db.Runs.Add(run);
            await db.SaveChangesAsync();
            _logger?.LogInformation("Created run {RunId}", run.Id);
            return run.Id;
        }

        public async Task SetRunFinishedAsync(Guid runId)
        {
            using var db = _factory.CreateDbContext();
            var run = await db.Runs.FirstOrDefaultAsync(r => r.Id == runId);
            if (run == null) return;
            run.FinishedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync();
            _logger?.LogInformation("Run finished {RunId}", runId);
        }

        public async Task LogPhilosopherEventAsync(Guid runId, PhilosopherStateEvent evt)
        {
            using var db = _factory.CreateDbContext();
            evt.RunId = runId;
            evt.TimestampUtc = DateTime.UtcNow;
            db.PhilosopherStateEvents.Add(evt);
            await db.SaveChangesAsync();
        }

        public async Task LogForkEventAsync(Guid runId, ForkStateEvent evt)
        {
            using var db = _factory.CreateDbContext();
            evt.RunId = runId;
            evt.TimestampUtc = DateTime.UtcNow;
            db.ForkStateEvents.Add(evt);
            await db.SaveChangesAsync();
        }

        public async Task<SimulationRun?> GetRunAsync(Guid runId)
        {
            using var db = _factory.CreateDbContext();
            return await db.Runs.AsNoTracking().FirstOrDefaultAsync(r => r.Id == runId);
        }

        public async Task<IReadOnlyList<PhilosopherStateEvent>> GetLatestPhilosopherStatesAtAsync(Guid runId, DateTime cutoffUtc)
        {
            using var db = _factory.CreateDbContext();
            var q = db.PhilosopherStateEvents
                .AsNoTracking()
                .Where(e => e.RunId == runId && e.TimestampUtc <= cutoffUtc)
                .GroupBy(e => e.PhilosopherName)
                .Select(g => g.OrderByDescending(e => e.TimestampUtc).FirstOrDefault());

            var list = await q.ToListAsync();
            return list.Where(x => x != null).ToList()!;
        }

        public async Task<IReadOnlyList<ForkStateEvent>> GetLatestForkStatesAtAsync(Guid runId, DateTime cutoffUtc)
        {
            using var db = _factory.CreateDbContext();
            var q = db.ForkStateEvents
                .AsNoTracking()
                .Where(e => e.RunId == runId && e.TimestampUtc <= cutoffUtc)
                .GroupBy(e => e.ForkNumber)
                .Select(g => g.OrderByDescending(e => e.TimestampUtc).FirstOrDefault());

            var list = await q.ToListAsync();
            return list.Where(x => x != null).ToList()!;
        }
    }
}