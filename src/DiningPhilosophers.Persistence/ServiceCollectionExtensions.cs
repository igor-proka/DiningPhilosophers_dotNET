using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DiningPhilosophers.Persistence
{
    public static class ServiceCollectionExtensions
    {
        // Register Persistence layer. Пример:
        // services.AddDiningPhilosophersPersistence(configuration.GetConnectionString("SimulationDb"));
        public static IServiceCollection AddDiningPhilosophersPersistence(this IServiceCollection services, string connectionString)
        {
            services.AddDbContext<SimulationDbContext>(options => options.UseNpgsql(connectionString));
            services.AddDbContextFactory<SimulationDbContext>(options => options.UseNpgsql(connectionString));
            services.AddSingleton<ISimulationPersistence, SimulationPersistence>();
            return services;
        }
    }
}
