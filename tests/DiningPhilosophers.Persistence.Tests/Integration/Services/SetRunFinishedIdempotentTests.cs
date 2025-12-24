using System;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;
using DiningPhilosophers.Persistence.Tests.Helpers;
using System.Threading.Tasks;

namespace DiningPhilosophers.Persistence.Tests.Integration.Services
{
    public class SetRunFinishedIdempotentTests : IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly DbContextOptions<DiningPhilosophers.Persistence.SimulationDbContext> _options;
        private readonly TestDbContextFactory _factory;
        private readonly DiningPhilosophers.Persistence.SimulationPersistence _persistence;

        public SetRunFinishedIdempotentTests()
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
        }

        public void Dispose()
        {
            _connection?.Dispose();
        }

        // Тест проверяет идемпотентность SetRunFinishedAsync: два вызова — результат не ломается и FinishedAtUtc установлен.
        // Вход: существующий run. Выход: FinishedAtUtc != null, оба вызова проходят без исключений.
        [Fact]
        public async Task SetRunFinished_IsIdempotent()
        {
            var runId = await _persistence.CreateRunAsync(null);

            await _persistence.SetRunFinishedAsync(runId);
            await _persistence.SetRunFinishedAsync(runId); // второй вызов не должен бросать

            using var ctx = _factory.CreateDbContext();
            var run = await ctx.Runs.FindAsync(runId);
            Assert.NotNull(run);
            Assert.NotNull(run.FinishedAtUtc);
        }
    }
}
