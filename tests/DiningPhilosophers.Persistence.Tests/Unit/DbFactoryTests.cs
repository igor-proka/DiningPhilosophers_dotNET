using System;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;
using DiningPhilosophers.Persistence.Tests.Helpers;

namespace DiningPhilosophers.Persistence.Tests.Unit
{
    public class DbFactoryTests : IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly DbContextOptions<DiningPhilosophers.Persistence.SimulationDbContext> _options;

        public DbFactoryTests()
        {
            // Проверяем, что фабрика контекстов корректно создаёт контексты для in-memory Sqlite
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            _options = new DbContextOptionsBuilder<DiningPhilosophers.Persistence.SimulationDbContext>()
                .UseSqlite(_connection)
                .Options;

            using var ctx = new DiningPhilosophers.Persistence.SimulationDbContext(_options);
            ctx.Database.EnsureCreated();
        }

        public void Dispose()
        {
            _connection?.Dispose();
        }

        // Тест проверяет, что TestDbContextFactory создаёт работоспособный контекст.
        // Вход: корректные DbContextOptions (in-memory sqlite). 
        // Выход: экземпляр контекста, на котором можно выполнить миграцию/EnsureCreated.
        // Ожидается: создание контекста без исключений.
        [Fact]
        public void TestDbContextFactory_CreatesContext()
        {
            var factory = new TestDbContextFactory(_options);
            using var ctx = factory.CreateDbContext();
            // Если контекст создан — можно вызвать Database.CanConnect
            Assert.True(ctx.Database.CanConnect());
        }
    }
}
