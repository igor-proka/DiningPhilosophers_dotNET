using System;
using System.Linq;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;
using DiningPhilosophers.Persistence.Tests.Helpers;
using System.Threading.Tasks;

namespace DiningPhilosophers.Persistence.Tests.Integration.Repositories
{
    public class ForkBehaviorTests : IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly DbContextOptions<DiningPhilosophers.Persistence.SimulationDbContext> _options;
        private readonly TestDbContextFactory _factory;
        private readonly DiningPhilosophers.Persistence.SimulationPersistence _persistence;
        private readonly Guid _runId;

        public ForkBehaviorTests()
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

        // Тест проверяет, что логирование нескольких событий одной вилки оставляет последним ожидаемое состояние.
        // Вход: несколько событий для Fork 1 с разными Timestamp. 
        // Выход: последний статус — тот, у которого максимальный Timestamp <= cutoff.
        [Fact]
        public async Task ForkEvents_MultipleTimestamps_LastWins()
        {
            var now = DateTime.UtcNow;

            using (var ctx = _factory.CreateDbContext())
            {
                ctx.ForkStateEvents.AddRange(
                    new DiningPhilosophers.Persistence.Entities.ForkStateEvent { RunId = _runId, ForkNumber = 1, State = "Available", Owner = null, TimestampUtc = now.AddSeconds(-60) },
                    new DiningPhilosophers.Persistence.Entities.ForkStateEvent { RunId = _runId, ForkNumber = 1, State = "InUse", Owner = "A", TimestampUtc = now.AddSeconds(-30) },
                    new DiningPhilosophers.Persistence.Entities.ForkStateEvent { RunId = _runId, ForkNumber = 1, State = "Available", Owner = null, TimestampUtc = now.AddSeconds(-10) }
                );
                await ctx.SaveChangesAsync();
            }

            var latest = await _persistence.GetLatestForkStatesAtAsync(_runId, DateTime.UtcNow);

            var fork1 = latest.FirstOrDefault(f => f.ForkNumber == 1);
            Assert.NotNull(fork1);
            Assert.Equal("Available", fork1.State);
            Assert.Null(fork1.Owner);
        }

        // Тест проверяет поведение при одинаковых Timestamp для вилок — допускаем оба варианта.
        // Вход: два события с одинаковым Timestamp. Выход: последнее состояние — одно из ожидаемых.
        [Fact]
        public async Task ForkEvents_SameTimestamp_Tolerant()
        {
            var now = DateTime.UtcNow;

            using (var ctx = _factory.CreateDbContext())
            {
                ctx.ForkStateEvents.AddRange(
                    new DiningPhilosophers.Persistence.Entities.ForkStateEvent { RunId = _runId, ForkNumber = 2, State = "Available", Owner = null, TimestampUtc = now },
                    new DiningPhilosophers.Persistence.Entities.ForkStateEvent { RunId = _runId, ForkNumber = 2, State = "InUse", Owner = "B", TimestampUtc = now }
                );
                await ctx.SaveChangesAsync();
            }

            var latest = await _persistence.GetLatestForkStatesAtAsync(_runId, now);

            var fork2 = latest.FirstOrDefault(f => f.ForkNumber == 2);
            Assert.NotNull(fork2);
            Assert.Contains(fork2.State, new[] { "Available", "InUse" });
        }
    }
}
