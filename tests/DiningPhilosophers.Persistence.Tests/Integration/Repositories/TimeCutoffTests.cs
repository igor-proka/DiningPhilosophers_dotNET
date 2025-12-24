using System;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;
using DiningPhilosophers.Persistence.Tests.Helpers;
using System.Threading.Tasks;

namespace DiningPhilosophers.Persistence.Tests.Integration.Repositories
{
    public class TimeCutoffTests : IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly DbContextOptions<DiningPhilosophers.Persistence.SimulationDbContext> _options;
        private readonly TestDbContextFactory _factory;
        private readonly DiningPhilosophers.Persistence.SimulationPersistence _persistence;
        private readonly Guid _runId;

        public TimeCutoffTests()
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            _options = new DbContextOptionsBuilder<DiningPhilosophers.Persistence.SimulationDbContext>()
                .UseSqlite(_connection)
                .Options;

            using var ctx = new DiningPhilosophers.Persistence.SimulationDbContext(_options);
            ctx.Database.EnsureCreated();

            _factory = new TestDbContextFactory(_options);
            _persistence = new DiningPhilosophers.Persistence.SimulationPersistence(_factory);

            _runId = _persistence.CreateRunAsync(null).GetAwaiter().GetResult();
        }

        public void Dispose()
        {
            _connection?.Dispose();
        }

        // Тест проверяет, что события, у которых TimestampUtc > cutoff, не возвращаются.
        // Вход: события до и после cutoff. Выход: только события до cutoff.
        [Fact]
        public async Task Cutoff_ExcludesFutureEvents()
        {
            var now = DateTime.UtcNow;

            using (var ctx = _factory.CreateDbContext())
            {
                ctx.PhilosopherStateEvents.Add(new DiningPhilosophers.Persistence.Entities.PhilosopherStateEvent
                {
                    RunId = _runId,
                    PhilosopherName = "Кант",
                    State = "Hungry",
                    TimestampUtc = now.AddSeconds(-10)
                });

                ctx.PhilosopherStateEvents.Add(new DiningPhilosophers.Persistence.Entities.PhilosopherStateEvent
                {
                    RunId = _runId,
                    PhilosopherName = "Кант",
                    State = "Eating",
                    TimestampUtc = now.AddSeconds(+10)
                });

                await ctx.SaveChangesAsync();
            }

            var result = await _persistence.GetLatestPhilosopherStatesAtAsync(_runId, now);

            Assert.Single(result);
            Assert.Equal("Hungry", result[0].State);
        }
    }
}
