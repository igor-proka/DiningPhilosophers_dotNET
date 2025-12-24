using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System;

namespace DiningPhilosophers.Persistence.Tests.Helpers
{
    // Тестовая фабрика DbContext, возвращает новый SimulationDbContext с заданными опциями.
    // Используется во всех тестах для создания контекстов, основанных на единой in-memory SQLite-связи.
    public class TestDbContextFactory : IDbContextFactory<DiningPhilosophers.Persistence.SimulationDbContext>
    {
        private readonly DbContextOptions<DiningPhilosophers.Persistence.SimulationDbContext> _options;

        public TestDbContextFactory(DbContextOptions<DiningPhilosophers.Persistence.SimulationDbContext> options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public DiningPhilosophers.Persistence.SimulationDbContext CreateDbContext()
        {
            return new DiningPhilosophers.Persistence.SimulationDbContext(_options);
        }
    }
}
