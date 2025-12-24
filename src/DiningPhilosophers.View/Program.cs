using System;
using System.Threading.Tasks;
using DiningPhilosophers.Persistence;
using DiningPhilosophers.View.CommandLine;
using DiningPhilosophers.View.Services;
using DiningPhilosophers.View.Renderers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DiningPhilosophers.View
{
    internal class Program
    {
        static async Task<int> Main(string[] args)
        {
            try
            {
                Console.OutputEncoding = System.Text.Encoding.UTF8;
                
                // 1. Парсим аргументы командной строки
                var parser = new CommandLineParser();
                var arguments = parser.Parse(args);
                
                if (!arguments.IsValid)
                {
                    parser.DisplayUsage();
                    return 1;
                }
                
                // 2. Проверка аргументов
                var validator = new CommandLineValidator();
                var validation = validator.Validate(arguments);
                if (!validation.IsValid)
                {
                    Console.Error.WriteLine($"Error: {validation.ErrorMessage}");
                    return 2;
                }

                // 3. Configure services
                var configuration = new ConfigurationBuilder()
                    .AddJsonFile("appsettings.json", optional: true)
                    .AddEnvironmentVariables()
                    .Build();

                var connectionString = configuration.GetConnectionString("SimulationDb") 
                    ?? Environment.GetEnvironmentVariable("SIM_DB");

                if (string.IsNullOrEmpty(connectionString))
                {
                    Console.Error.WriteLine("Error: Database connection string not found.");
                    Console.Error.WriteLine("Set in appsettings.json (ConnectionStrings:SimulationDb) or environment variable SIM_DB.");
                    return 3;
                }

                var services = new ServiceCollection();
                services.AddDiningPhilosophersPersistence(connectionString);
                services.AddScoped<ISimulationStateService, SimulationStateService>();
                services.AddScoped<ISimulationStateRenderer, ConsoleSimulationStateRenderer>();
                
                using var serviceProvider = services.BuildServiceProvider();

                // 4. Получаем состояние симуляции
                using var scope = serviceProvider.CreateScope();
                var stateService = scope.ServiceProvider.GetRequiredService<ISimulationStateService>();
                
                var result = await stateService.GetStateAsync(arguments.RunId, arguments.DelaySeconds);
                
                if (!result.Success)
                {
                    Console.Error.WriteLine($"Error: {result.ErrorMessage}");
                    return 4;
                }

                // 5. Обновить аргументы с учетом фактической задержки
                arguments.StartedAtUtc = result.Run!.StartedAtUtc;
                
                // 6. Отоьражаем в консоли состояние стола
                var renderer = scope.ServiceProvider.GetRequiredService<ISimulationStateRenderer>();
                renderer.Render(arguments, result.Run, result.Philosophers, result.Forks, result.WarningMessage);

                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Unexpected error: {ex.Message}");
                Console.Error.WriteLine($"Stack trace: {ex.StackTrace}");
                return 99;
            }
        }
    }
}