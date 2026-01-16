using DiningPhilosophers.Core.Contracts.Monitor;
using DiningPhilosophers.Core.Contracts.Strategies;
using DiningPhilosophers.Core.Models;
using DiningPhilosophers.Services.Metrics;
using DiningPhilosophers.Services.Simulation.Multithreaded;
using DiningPhilosophers.Strategies;
using MassTransit;
using Microservices.Shared;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System;
using System.Net.Http;
using System.Threading.Tasks;

var builder = Host.CreateApplicationBuilder(args);

/*
 Не добавляем AddJsonFile вручную — Host уже загрузил appsettings.json,
 а переменные окружения из Docker будут иметь приоритет.
Если очень нужно явно грузить JSON → добавлять ДО AddEnvironmentVariables()
*/

//builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
//builder.Configuration.AddEnvironmentVariables();

// Читаем конфиг строго из env/docker-compose
var philosopherName = builder.Configuration["PhilosopherName"]
    ?? throw new InvalidOperationException("PhilosopherName not set");

var leftForkId = int.Parse(builder.Configuration["LeftForkId"]
    ?? throw new InvalidOperationException("LeftForkId not set"));

var rightForkId = int.Parse(builder.Configuration["RightForkId"]
    ?? throw new InvalidOperationException("RightForkId not set"));

// HttpClient для стола
builder.Services.AddHttpClient("Table", client =>
{
    var tableUrl = builder.Configuration["TableServiceUrl"] ?? "http://table-service:8080";
    client.BaseAddress = new Uri(tableUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

// Создаем философа и вилки
var philosopher = new Philosopher(philosopherName);
var leftFork = new ThreadSafeFork(leftForkId);
var rightFork = new ThreadSafeFork(rightForkId);

// Загружаем конфиг симуляции
builder.Services.Configure<MultithreadedSimulationConfig>(builder.Configuration.GetSection("Simulation"));

// Метрики
builder.Services.AddSingleton<IMultithreadedMetricsCollector>(_ =>
    new MultithreadedMetricsCollector(new[] { philosopher }, new[] { leftFork, rightFork }));

// Стратегия взаимодействия с координатором
builder.Services.AddSingleton<IPhilosopherStrategy, CoordinatorStrategy>();

builder.Services.AddSingleton<ThreadSafeForkAcquisitionManager>(sp =>
{
    var config = sp.GetRequiredService<IOptions<MultithreadedSimulationConfig>>().Value;
    return new ThreadSafeForkAcquisitionManager(config.ForkAcquisitionTime);
});

// Обработчик логики философа
builder.Services.AddSingleton<MultithreadedPhilosopherStateProcessor>(sp =>
{
    var config = sp.GetRequiredService<IOptions<MultithreadedSimulationConfig>>().Value;
    return new MultithreadedPhilosopherStateProcessor(
        philosopher,
        leftFork,
        rightFork,
        config,
        sp.GetRequiredService<IPhilosopherStrategy>(),
        sp.GetRequiredService<ThreadSafeForkAcquisitionManager>(),
        sp.GetRequiredService<IMultithreadedMetricsCollector>()
    );
});

// --- MassTransit/RabbitMQ ---
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<DecisionConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host("rabbitmq", "/", h =>
        {
            h.Username("guest");
            h.Password("guest");
        });

        // --- FIX: очереди теперь ASCII-валидные ---
        string Normalize(string s) =>
            System.Text.Encoding.ASCII.GetString(System.Text.Encoding.ASCII.GetBytes(s))
                .ToLower()
                .Replace("?", string.Empty);

        var queueSafeName = $"decision-{Normalize(philosopherName)}-queue";

        cfg.ReceiveEndpoint(queueSafeName, e =>
        {
            e.ConfigureConsumer<DecisionConsumer>(context);
        });
    });
});


// Фоновый сервис-философ
builder.Services.AddHostedService<PhilosopherSimulator>();

await builder.Build().RunAsync();
