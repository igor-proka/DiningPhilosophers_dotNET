using System;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;
using DiningPhilosophers.Persistence.Tests.Helpers;
using System.Threading.Tasks;
using System.Linq;

namespace DiningPhilosophers.Persistence.Tests.Integration.Repositories
{
    public class CutoffBeforeEventsTests : IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly DbContextOptions<DiningPhilosophers.Persistence.SimulationDbContext> _options;
        private readonly TestDbContextFactory _factory;
        private readonly DiningPhilosophers.Persistence.SimulationPersistence _persistence;
        private readonly Guid _runId;

        public CutoffBeforeEventsTests()
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

        // Тест проверяет, что если cutoff раньше всех событий, то GetLatestPhilosopherStatesAtAsync возвращает пустую коллекцию.
        // Вход: события со временем T1 > cutoff. Выход: пустая коллекция.
        [Fact]
        public async Task CutoffBeforeAllEvents_ReturnsEmpty()
        {
            var now = DateTime.UtcNow;

            using (var ctx = _factory.CreateDbContext())
            {
                ctx.PhilosopherStateEvents.Add(new DiningPhilosophers.Persistence.Entities.PhilosopherStateEvent
                {
                    RunId = _runId,
                    PhilosopherName = "Зара",
                    State = "Thinking",
                    TimestampUtc = now.AddSeconds(10) // в будущем относительно cutoff
                });

                await ctx.SaveChangesAsync();
            }

            var cutoff = now; // до события
            var result = await _persistence.GetLatestPhilosopherStatesAtAsync(_runId, cutoff);

            Assert.NotNull(result);
            Assert.Empty(result);
        }
    }
}
