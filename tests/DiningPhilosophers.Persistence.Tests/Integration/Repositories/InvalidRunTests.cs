using System;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;
using DiningPhilosophers.Persistence.Tests.Helpers;
using System.Threading.Tasks;

namespace DiningPhilosophers.Persistence.Tests.Integration.Repositories
{
    public class InvalidRunTests : IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly DbContextOptions<DiningPhilosophers.Persistence.SimulationDbContext> _options;
        private readonly DiningPhilosophers.Persistence.SimulationPersistence _persistence;

        public InvalidRunTests()
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            _options = new DbContextOptionsBuilder<DiningPhilosophers.Persistence.SimulationDbContext>()
                .UseSqlite(_connection)
                .Options;

            using var ctx = new DiningPhilosophers.Persistence.SimulationDbContext(_options);
            ctx.Database.EnsureCreated();

            _persistence = new DiningPhilosophers.Persistence.SimulationPersistence(new TestDbContextFactory(_options));
        }

        public void Dispose()
        {
            _connection?.Dispose();
        }

        // Тест проверяет, что при запросе с несуществующим runId методы возвращают пустые коллекции, а не выбрасывают исключение.
        // Вход: случайный Guid. Выход: пустые коллекции.
        [Fact]
        public async Task QueryWithInvalidRunId_ReturnsEmpty()
        {
            var fakeRunId = Guid.NewGuid();

            var philosophers = await _persistence.GetLatestPhilosopherStatesAtAsync(fakeRunId, DateTime.UtcNow);
            var forks = await _persistence.GetLatestForkStatesAtAsync(fakeRunId, DateTime.UtcNow);

            Assert.NotNull(philosophers);
            Assert.Empty(philosophers);

            Assert.NotNull(forks);
            Assert.Empty(forks);
        }
    }
}
