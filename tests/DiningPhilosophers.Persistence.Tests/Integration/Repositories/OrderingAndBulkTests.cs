using System;
using System.Linq;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;
using DiningPhilosophers.Persistence.Tests.Helpers;
using System.Threading.Tasks;

namespace DiningPhilosophers.Persistence.Tests.Integration.Repositories
{
    public class OrderingAndBulkTests : IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly DbContextOptions<DiningPhilosophers.Persistence.SimulationDbContext> _options;
        private readonly TestDbContextFactory _factory;
        private readonly DiningPhilosophers.Persistence.SimulationPersistence _persistence;
        private readonly Guid _runId;

        public OrderingAndBulkTests()
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

        /// <summary>
        /// Тест проверяет, что GetLatestPhilosopherStatesAtAsync корректно выбирает максимальные Timestamp для нескольких философов.
        /// Вход: события с разными Timestamp для философов A и B. Выход: возвращаются последние состояния.
        /// </summary>
        [Fact]
        public async Task GetLatestPhilosopherStatesAtAsync_MultiplePhilosophers_Sorted()
        {
            var now = DateTime.UtcNow;

            using (var ctx = _factory.CreateDbContext())
            {
                ctx.PhilosopherStateEvents.AddRange(
                    new DiningPhilosophers.Persistence.Entities.PhilosopherStateEvent { RunId = _runId, PhilosopherName = "A", State = "Thinking", TimestampUtc = now.AddSeconds(-20) },
                    new DiningPhilosophers.Persistence.Entities.PhilosopherStateEvent { RunId = _runId, PhilosopherName = "A", State = "Hungry", TimestampUtc = now.AddSeconds(-5) },
                    new DiningPhilosophers.Persistence.Entities.PhilosopherStateEvent { RunId = _runId, PhilosopherName = "B", State = "Thinking", TimestampUtc = now.AddSeconds(-15) },
                    new DiningPhilosophers.Persistence.Entities.PhilosopherStateEvent { RunId = _runId, PhilosopherName = "B", State = "Eating", TimestampUtc = now.AddSeconds(-2) }
                );
                await ctx.SaveChangesAsync();
            }

            var latest = await _persistence.GetLatestPhilosopherStatesAtAsync(_runId, DateTime.UtcNow);

            Assert.Equal(2, latest.Count);
            Assert.Equal("Hungry", latest.First(x => x.PhilosopherName == "A").State);
            Assert.Equal("Eating", latest.First(x => x.PhilosopherName == "B").State);
        }

        // Тест проверяет массовое логирование событий вилок через LogForkEventAsync: N событий → N записей.
        // Вход: N вызовов LogForkEventAsync. Выход: N записей в таблице ForkStateEvents.
        [Fact]
        public async Task BulkForkLogging_PreservesAllForkEvents()
        {
            const int eventsCount = 40;

            for (int i = 0; i < eventsCount; i++)
            {
                await _persistence.LogForkEventAsync(_runId,
                    new DiningPhilosophers.Persistence.Entities.ForkStateEvent
                    {
                        ForkNumber = (i % 5) + 1,
                        State = i % 2 == 0 ? "Available" : "InUse",
                        Owner = i % 2 == 0 ? null : $"P{i}"
                    });
            }

            using var ctx = _factory.CreateDbContext();
            var count = await ctx.ForkStateEvents.CountAsync();

            Assert.Equal(eventsCount, count);
        }
    }
}
