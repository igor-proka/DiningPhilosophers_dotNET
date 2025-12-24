using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DiningPhilosophers.Persistence
{
    // Design-time factory for EF Core migrations.
    // Used by dotnet-ef tools.
    public class SimulationDbContextFactory
        : IDesignTimeDbContextFactory<SimulationDbContext>
    {
        public SimulationDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<SimulationDbContext>();

            // ВАЖНО:
            // Это connection string используется ТОЛЬКО для миграций
            // Runtime-строка берётся из appsettings.json / DI
            var connectionString =
                "Host=localhost;Port=5432;Database=dining_philosophers;Username=postgres;Password=Stiknoob1";

            optionsBuilder.UseNpgsql(connectionString);

            return new SimulationDbContext(optionsBuilder.Options);
        }
    }
}
