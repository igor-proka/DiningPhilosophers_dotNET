using System;
using System.Collections.Generic;
using DiningPhilosophers.Core.Contracts.Monitor;
using DiningPhilosophers.Core.Models;
using DiningPhilosophers.Services.Simulation.Multithreaded;

namespace DiningPhilosophers.Tests.Helpers
{
    // Простая реализация IMultithreadedMetricsCollector для тестов.
    // Не проводит реальных расчётов, но:
    // - возвращает корректные объекты MultithreadedPhilosopherMetrics / ForkMetrics,
    // - поддерживает StartWaiting/StopWaiting/IncrementMeal/UpdateMetrics/Reset/GetTotalObservations,
    // - потокобезопасность не критична для наших коротких интеграционных тестов.
    // 
    // Эта заглушка достаточна, чтобы создать MultithreadedPhilosopherStateProcessor в тестах
    // и не требовать полноценной реализации метрик.
    public class FakeMetricsCollector : IMultithreadedMetricsCollector
    {
        private readonly Dictionary<string, MultithreadedPhilosopherMetrics> _philos =
            new(StringComparer.OrdinalIgnoreCase);

        private readonly Dictionary<int, ForkMetrics> _forks = new();

        private long _totalObservations = 0;

        public FakeMetricsCollector()
        {
            // Пусто — конкретные записи будут добавляться через EnsurePhilosopher/EnsureFork
        }

        private void EnsurePhilosopher(string name)
        {
            if (!_philos.ContainsKey(name))
                _philos[name] = new MultithreadedPhilosopherMetrics();
        }

        private void EnsureFork(int id)
        {
            if (!_forks.ContainsKey(id))
                _forks[id] = new ForkMetrics();
        }

        // Возвращаем объект метрик философа (может быть пустой, но не null).
        public MultithreadedPhilosopherMetrics GetPhilosopherMetrics(string name)
        {
            EnsurePhilosopher(name);
            return _philos[name];
        }

        // Возвращаем объект метрик вилки (может быть пустой, но не null).
        public ForkMetrics GetForkMetrics(int forkId)
        {
            EnsureFork(forkId);
            return _forks[forkId];
        }

        // Начало ожидания — пометим время старта (для простоты в заглушке).
        public void StartWaiting(string name)
        {
            EnsurePhilosopher(name);
            var m = _philos[name];
            if (!m.HungerStartTime.HasValue)
            {
                m.HungerStartTime = DateTime.Now;
                m.HungerEpisodes++;
            }
        }

        // Остановка ожидания — накопим время ожидания.
        public void StopWaiting(string name)
        {
            EnsurePhilosopher(name);
            var m = _philos[name];
            if (m.HungerStartTime.HasValue)
            {
                var delta = DateTime.Now - m.HungerStartTime.Value;
                m.TotalWaitingTimeMs += (long)delta.TotalMilliseconds;
                m.HungerStartTime = null;
            }
        }

        // Инкремент количества приёмов пищи
        public void IncrementMeal(string name)
        {
            EnsurePhilosopher(name);
            _philos[name].MealsEaten++;
        }

        // Обновление метрик — в заглушке просто считаем наблюдение и помечаем вилки как свободные
        public void UpdateMetrics()
        {
            _totalObservations++;
            // для тестов не нужно детального подсчёта, но гарантируем, что метод работает
        }

        // Сброс всех накопленных данных
        public void Reset()
        {
            _philos.Clear();
            _forks.Clear();
            _totalObservations = 0;
        }

        // Общее число наблюдений
        public long GetTotalObservations() => _totalObservations;
    }
}
