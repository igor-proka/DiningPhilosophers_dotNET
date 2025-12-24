using System;
using System.Linq;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;
using DiningPhilosophers.Persistence.Tests.Helpers;
using System.Threading.Tasks;

namespace DiningPhilosophers.Persistence.Tests.Integration.Repositories
{
    public class MultipleForksSnapshotTests : IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly DbContextOptions<DiningPhilosophers.Persistence.SimulationDbContext> _options;
        private readonly TestDbContextFactory _factory;
        private readonly DiningPhilosophers.Persistence.SimulationPersistence _persistence;
        private readonly Guid _runId;

        public MultipleForksSnapshotTests()
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

        // Тест проверяет, что GetLatestForkStatesAtAsync возвращает snapshot для всех вилок, если есть события для каждой вилки.
        // Вход: события для 5 вилок с различными Timestamp. Выход: 5 последних записей (по одной на вилку).
        [Fact]
        public async Task GetLatestForkStatesAtAsync_ReturnsAllForksSnapshot()
        {
            var now = DateTime.UtcNow;

            using (var ctx = _factory.CreateDbContext())
            {
                // создаём для 5 вилок по 2 события — старое и более свежее
                for (int fork = 1; fork <= 5; fork++)
                {
                    ctx.ForkStateEvents.Add(new DiningPhilosophers.Persistence.Entities.ForkStateEvent
                    {
                        RunId = _runId,
                        ForkNumber = fork,
                        State = "Available",
                        Owner = null,
                        TimestampUtc = now.AddSeconds(-20)
                    });

                    ctx.ForkStateEvents.Add(new DiningPhilosophers.Persistence.Entities.ForkStateEvent
                    {
                        RunId = _runId,
                        ForkNumber = fork,
                        State = "InUse",
                        Owner = $"P{fork}",
                        TimestampUtc = now.AddSeconds(-5)
                    });
                }
                await ctx.SaveChangesAsync();
            }

            var latest = await _persistence.GetLatestForkStatesAtAsync(_runId, DateTime.UtcNow);

            Assert.Equal(5, latest.Count);
            for (int fork = 1; fork <= 5; fork++)
            {
                var f = latest.FirstOrDefault(x => x.ForkNumber == fork);
                Assert.NotNull(f);
                Assert.Equal("InUse", f.State);
                Assert.Equal($"P{fork}", f.Owner);
            }
        }
    }
}
