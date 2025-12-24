using System;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;
using DiningPhilosophers.Persistence.Tests.Helpers;

namespace DiningPhilosophers.Persistence.Tests.Integration.Repositories
{
    public class RunTests : IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly DbContextOptions<DiningPhilosophers.Persistence.SimulationDbContext> _options;
        private readonly TestDbContextFactory _factory;
        private readonly DiningPhilosophers.Persistence.SimulationPersistence _persistence;

        public RunTests()
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

        // Тест проверяет, что CreateRunAsync создаёт run и возвращает непустой Guid.
        // Вход: пустая БД. Выход: run сохранён, OptionsJson совпадает.
        [Fact]
        public async Task CreateRunAsync_CreatesRunAndReturnsGuid()
        {
            var runId = await _persistence.CreateRunAsync("{\"test\":true}");

            Assert.NotEqual(Guid.Empty, runId);

            using var ctx = _factory.CreateDbContext();
            var run = await ctx.Runs.FindAsync(runId);
            Assert.NotNull(run);
            Assert.Equal("{\"test\":true}", run.OptionsJson);
            Assert.True(run.StartedAtUtc <= DateTime.UtcNow);
        }

        // Тест проверяет, что SetRunFinishedAsync устанавливает FinishedAtUtc и что метод идемпотентен (второй вызов не ломает).
        // Вход: созданный run. Выход: FinishedAtUtc установлен и не равен null после первого и второго вызова.
        [Fact]
        public async Task SetRunFinishedAsync_SetsFinishedAtUtc_And_IsIdempotent()
        {
            var runId = await _persistence.CreateRunAsync(null);

            await _persistence.SetRunFinishedAsync(runId);

            using (var ctx = _factory.CreateDbContext())
            {
                var r = await ctx.Runs.FindAsync(runId);
                Assert.NotNull(r);
                Assert.NotNull(r.FinishedAtUtc);
            }

            // Второй вызов не должен кинуть исключение, и FinishedAtUtc останется заданным (или обновится, но не будет null)
            await _persistence.SetRunFinishedAsync(runId);

            using (var ctx = _factory.CreateDbContext())
            {
                var r2 = await ctx.Runs.FindAsync(runId);
                Assert.NotNull(r2);
                Assert.NotNull(r2.FinishedAtUtc);
            }
        }
    }
}
