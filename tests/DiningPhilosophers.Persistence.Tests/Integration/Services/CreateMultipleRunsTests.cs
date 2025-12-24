using System;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;
using DiningPhilosophers.Persistence.Tests.Helpers;
using System.Threading.Tasks;
using System.Linq;

namespace DiningPhilosophers.Persistence.Tests.Integration.Services
{
    public class CreateMultipleRunsTests : IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly DbContextOptions<DiningPhilosophers.Persistence.SimulationDbContext> _options;
        private readonly TestDbContextFactory _factory;
        private readonly DiningPhilosophers.Persistence.SimulationPersistence _persistence;

        public CreateMultipleRunsTests()
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

        // Тест проверяет, что создание нескольких запусков даёт уникальные runId.
        // Вход: N вызовов CreateRunAsync. Выход: N уникальных Guid.
        [Fact]
        public async Task CreateMultipleRuns_UniqueIds()
        {
            var ids = new Guid[10];
            for (int i = 0; i < ids.Length; i++)
            {
                ids[i] = await _persistence.CreateRunAsync($"{{\"run\":{i}}}");
            }

            Assert.Equal(ids.Length, ids.Distinct().Count());
        }
    }
}
