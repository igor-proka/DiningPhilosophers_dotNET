using DiningPhilosophers.Core.Contracts.Configuration;
using DiningPhilosophers.Core.Contracts.Monitor;
using DiningPhilosophers.Core.Contracts.Strategies;
using DiningPhilosophers.Core.Models;
using DiningPhilosophers.Hosted.Interfaces;
using DiningPhilosophers.Hosted.Services;
using DiningPhilosophers.Services.Configuration;
using DiningPhilosophers.Services.Metrics;
using DiningPhilosophers.Services.Simulation.Multithreaded;
using DiningPhilosophers.Strategies;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Collections.Generic;
using System.Linq;

var builder = WebApplication.CreateBuilder(args);

// Добавляем сервисы
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Инициализация философов и вилок
var names = new[] { "Платон", "Аристотель", "Сократ", "Декарт", "Кант" };
var philosophers = names.Select(n => new Philosopher(n)).ToList();
var forks = Enumerable.Range(1, names.Length).Select(i => new ThreadSafeFork(i)).ToList();

// Регистрация зависимостей
builder.Services.AddSingleton<IPhilosopherNamesProvider>(new StaticPhilosopherNamesProvider(names));
builder.Services.AddSingleton<ITableManager, TableManager>();
builder.Services.AddSingleton<IPhilosopherStrategy, HierarchyStrategy>();

// Метрики
builder.Services.AddSingleton<IMultithreadedMetricsCollector>(sp =>
{
    var tableManager = sp.GetRequiredService<ITableManager>();
    return new MultithreadedMetricsCollector(tableManager.GetPhilosophers(), tableManager.GetForks());
});

// Для совместимости
builder.Services.AddSingleton<DiningPhilosophers.Core.Contracts.Monitor.IMetricsCollector>(sp =>
{
    var multiMetrics = sp.GetRequiredService<IMultithreadedMetricsCollector>();
    return new MultithreadedToMetricsAdapter(multiMetrics);
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();
app.MapControllers();

app.Run();

// Простой провайдер имен философов
public class StaticPhilosopherNamesProvider : IPhilosopherNamesProvider
{
    private readonly string[] _names;

    public StaticPhilosopherNamesProvider(string[] names)
    {
        _names = names;
    }

    public IEnumerable<string> GetNames() => _names;
}