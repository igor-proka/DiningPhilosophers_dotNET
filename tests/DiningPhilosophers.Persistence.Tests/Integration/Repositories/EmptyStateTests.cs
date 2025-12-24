using System;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;
using DiningPhilosophers.Persistence.Tests.Helpers;
using System.Threading.Tasks;

namespace DiningPhilosophers.Persistence.Tests.Integration.Repositories
{
    public class EmptyStateTests : IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly DbContextOptions<DiningPhilosophers.Persistence.SimulationDbContext> _options;
        private readonly TestDbContextFactory _factory;
        private readonly DiningPhilosophers.Persistence.SimulationPersistence _persistence;
        private readonly Guid _runId;

        public EmptyStateTests()
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

        // Тест проверяет, что GetLatestPhilosopherStatesAtAsync для нового run возвращает пустую коллекцию.
        // Вход: новый run без событий. Выход: пустая коллекция (не null).
        [Fact]
        public async Task GetLatestPhilosopherStatesAtAsync_NoEvents_ReturnsEmpty()
        {
            var result = await _persistence.GetLatestPhilosopherStatesAtAsync(_runId, DateTime.UtcNow);
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        // Тест проверяет, что GetLatestForkStatesAtAsync для нового run возвращает пустую коллекцию.
        // Вход: новый run без событий. Выход: пустая коллекция.
        [Fact]
        public async Task GetLatestForkStatesAtAsync_NoEvents_ReturnsEmpty()
        {
            var result = await _persistence.GetLatestForkStatesAtAsync(_runId, DateTime.UtcNow);
            Assert.NotNull(result);
            Assert.Empty(result);
        }
    }
}
