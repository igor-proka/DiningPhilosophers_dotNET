using DiningPhilosophers.Core.Contracts.Strategies;
using DiningPhilosophers.Core.Models;
using DiningPhilosophers.Services.Simulation.Multithreaded;
using DiningPhilosophers.Strategies;
using MassTransit;
using Microservices.Shared;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

var builder = Host.CreateApplicationBuilder(args);

// Конфиг
builder.Configuration.AddJsonFile("appsettings.json");

// Инициализируем философов и вилки (статически)
var names = new[] { "Платон", "Аристотель", "Сократ", "Декарт", "Кант" };
var philosophers = names.Select(n => new Philosopher(n)).ToList();
var forks = Enumerable.Range(1, names.Length).Select(i => new ThreadSafeFork(i)).ToList();

var philosophersDict = philosophers.ToDictionary(p => p.Name, p => p);

builder.Services.AddSingleton<IReadOnlyDictionary<string, Philosopher>>(philosophersDict);

// Регистрируем координатор
builder.Services.AddSingleton<ICoordinator>(sp =>
{
    var factory = new ThreadSafeStrategyFactory();
    var (_, coordinator) = factory.Create(true, CoordinatorType.Semaphore, philosophers, forks);
    if (coordinator == null)
    {
        throw new InvalidOperationException("Coordinator not created");
    }
    return coordinator;
});

// MassTransit
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<HungryConsumer>();
    x.AddConsumer<FinishedConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host("rabbitmq", "/", h =>
        {
            h.Username("guest");
            h.Password("guest");
        });

        // Очередь для голодных философов
        cfg.ReceiveEndpoint("hungry-queue", e =>
        {
            e.ConfigureConsumer<HungryConsumer>(context);
        });

        // Очередь для завершивших философов
        cfg.ReceiveEndpoint("finished-queue", e =>
        {
            e.ConfigureConsumer<FinishedConsumer>(context);
        });
    });
});

builder.Services.AddSingleton<ICoordinatorService, CoordinatorServiceImpl>();
builder.Services.AddSingleton<ILoggerFactory, LoggerFactory>();

var host = builder.Build();
await host.RunAsync();