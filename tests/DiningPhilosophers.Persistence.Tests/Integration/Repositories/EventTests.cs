using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;
using DiningPhilosophers.Persistence.Tests.Helpers;

namespace DiningPhilosophers.Persistence.Tests.Integration.Repositories
{
    public class EventTests : IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly DbContextOptions<DiningPhilosophers.Persistence.SimulationDbContext> _options;
        private readonly TestDbContextFactory _factory;
        private readonly DiningPhilosophers.Persistence.SimulationPersistence _persistence;
        private readonly Guid _runId;

        public EventTests()
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

            _runId = _persistence.CreateRunAsync(null).GetAwaiter().GetResult();
        }

        public void Dispose()
        {
            _connection?.Dispose();
        }

        // Тест проверяет, что LogPhilosopherEventAsync сохраняет событие философа.
        // Вход: PhilosopherStateEvent без Timestamp (метод может проставить автоматически).
        // Выход: запись присутствует и имеет TimestampUtc.
        [Fact]
        public async Task LogPhilosopherEventAsync_SavesEvent()
        {
            var evt = new DiningPhilosophers.Persistence.Entities.PhilosopherStateEvent
            {
                PhilosopherName = "ТестФилософ",
                State = "Hungry",
                StepsRemaining = 0,
                HasLeftFork = false,
                HasRightFork = false,
                CurrentAction = "None",
                StepNumber = 42
            };

            await _persistence.LogPhilosopherEventAsync(_runId, evt);

            using var ctx = _factory.CreateDbContext();
            var persisted = await ctx.PhilosopherStateEvents
                .Where(e => e.RunId == _runId && e.PhilosopherName == "ТестФилософ")
                .FirstOrDefaultAsync();

            Assert.NotNull(persisted);
            Assert.Equal("Hungry", persisted.State);
            Assert.Equal(42, persisted.StepNumber);
            Assert.True((DateTime.UtcNow - persisted.TimestampUtc).TotalSeconds < 60); // timestamp проставлен недавно
        }

        // Тест проверяет, что GetLatestPhilosopherStatesAtAsync возвращает последние состояния по философам.
        // Вход: несколько событий с разным Timestamp; 
        // Выход: по каждому философу возвращается событие с максимальным Timestamp <= cutoff.
        [Fact]
        public async Task GetLatestPhilosopherStatesAtAsync_ReturnsLatestPerPhilosopher()
        {
            var now = DateTime.UtcNow;

            using (var ctx = _factory.CreateDbContext())
            {
                ctx.PhilosopherStateEvents.AddRange(
                    new DiningPhilosophers.Persistence.Entities.PhilosopherStateEvent { RunId = _runId, PhilosopherName = "A", State = "Thinking", TimestampUtc = now.AddSeconds(-10) },
                    new DiningPhilosophers.Persistence.Entities.PhilosopherStateEvent { RunId = _runId, PhilosopherName = "A", State = "Hungry", TimestampUtc = now.AddSeconds(-5) },
                    new DiningPhilosophers.Persistence.Entities.PhilosopherStateEvent { RunId = _runId, PhilosopherName = "B", State = "Thinking", TimestampUtc = now.AddSeconds(-8) },
                    new DiningPhilosophers.Persistence.Entities.PhilosopherStateEvent { RunId = _runId, PhilosopherName = "B", State = "Eating", TimestampUtc = now.AddSeconds(-1) }
                );
                await ctx.SaveChangesAsync();
            }

            var cutoff = DateTime.UtcNow;
            var latest = await _persistence.GetLatestPhilosopherStatesAtAsync(_runId, cutoff);

            Assert.Equal(2, latest.Count);
            var a = latest.First(x => x.PhilosopherName == "A");
            var b = latest.First(x => x.PhilosopherName == "B");
            Assert.Equal("Hungry", a.State);
            Assert.Equal("Eating", b.State);
        }

        // Тест проверяет, что логирование событий вилок сохраняется, 
        // и GetLatestForkStatesAtAsync возвращает последние состояния.
        // Вход: seed-события в БД + LogForkEventAsync; Выход: последние события для каждой вилки корректны.
        [Fact]
        public async Task ForkEvents_LogAndGetLatestWork()
        {
            var now = DateTime.UtcNow;
            using (var ctx = _factory.CreateDbContext())
            {
                ctx.ForkStateEvents.AddRange(
                    new DiningPhilosophers.Persistence.Entities.ForkStateEvent { RunId = _runId, ForkNumber = 1, State = "Available", Owner = null, TimestampUtc = now.AddSeconds(-30) },
                    new DiningPhilosophers.Persistence.Entities.ForkStateEvent { RunId = _runId, ForkNumber = 1, State = "InUse", Owner = "A", TimestampUtc = now.AddSeconds(-10) },
                    new DiningPhilosophers.Persistence.Entities.ForkStateEvent { RunId = _runId, ForkNumber = 2, State = "Available", Owner = null, TimestampUtc = now.AddSeconds(-20) }
                );
                await ctx.SaveChangesAsync();
            }

            var newEvt = new DiningPhilosophers.Persistence.Entities.ForkStateEvent { ForkNumber = 2, State = "InUse", Owner = "B" };
            await _persistence.LogForkEventAsync(_runId, newEvt);

            var cutoff = DateTime.UtcNow;
            var latest = await _persistence.GetLatestForkStatesAtAsync(_runId, cutoff);

            Assert.Contains(latest, e => e.ForkNumber == 1);
            Assert.Contains(latest, e => e.ForkNumber == 2);

            var fork1 = latest.First(e => e.ForkNumber == 1);
            var fork2 = latest.First(e => e.ForkNumber == 2);

            Assert.Equal("InUse", fork1.State);
            Assert.Equal("A", fork1.Owner);

            Assert.Equal("InUse", fork2.State);
            Assert.Equal("B", fork2.Owner);
        }

        // Тест проверяет поведение при одинаковых TimestampUtc: допускается любой из значений.
        // Пояснение: поскольку Timestamp одинаков, БД может вернуть либо первую, либо вторую запись; тест учитывает это.
        // Вход: два события с одинаковым Timestamp. Выход: state — одно из ожидаемых значений.
        [Fact]
        public async Task MultipleEvents_SameTimestamp_LastInsertedWins_Tolerant()
        {
            var now = DateTime.UtcNow;

            using (var ctx = _factory.CreateDbContext())
            {
                ctx.PhilosopherStateEvents.AddRange(
                    new DiningPhilosophers.Persistence.Entities.PhilosopherStateEvent
                    {
                        RunId = _runId,
                        PhilosopherName = "Сократ",
                        State = "Thinking",
                        TimestampUtc = now
                    },
                    new DiningPhilosophers.Persistence.Entities.PhilosopherStateEvent
                    {
                        RunId = _runId,
                        PhilosopherName = "Сократ",
                        State = "Hungry",
                        TimestampUtc = now
                    }
                );
                await ctx.SaveChangesAsync();
            }

            var result = await _persistence.GetLatestPhilosopherStatesAtAsync(_runId, now);

            Assert.Single(result);
            Assert.Contains(result[0].State, new[] { "Thinking", "Hungry" });
        }

        // Тест проверяет массовое логирование: после N логов количество записей в таблице равно N.
        // Вход: N вызовов LogPhilosopherEventAsync. Выход: N записей в БД.
        [Fact]
        public async Task BulkLogging_PreservesAllEvents()
        {
            const int eventsCount = 50;

            for (int i = 0; i < eventsCount; i++)
            {
                await _persistence.LogPhilosopherEventAsync(_runId,
                    new DiningPhilosophers.Persistence.Entities.PhilosopherStateEvent
                    {
                        PhilosopherName = "Платон",
                        State = "Thinking",
                        StepNumber = i
                    });
            }

            using var ctx = _factory.CreateDbContext();
            var count = await ctx.PhilosopherStateEvents.CountAsync();

            Assert.Equal(eventsCount, count);
        }
    }
}
