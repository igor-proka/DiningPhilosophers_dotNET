using System;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;
using DiningPhilosophers.Persistence.Tests.Helpers;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace DiningPhilosophers.Persistence.Tests.Integration.Services
{
    public class ConcurrentLoggingTests : IDisposable
    {
        private readonly string _connectionString;
        private readonly SqliteConnection _masterConnection;
        private readonly DbContextOptions<DiningPhilosophers.Persistence.SimulationDbContext> _factoryOptions;
        private readonly TestDbContextFactory _factory;
        private readonly DiningPhilosophers.Persistence.SimulationPersistence _persistence;
        private readonly Guid _runId;

        public ConcurrentLoggingTests()
        {
            // Используем уникальное имя in-memory файла, чтобы параллельные запуски тестов не пересекались.
            // Полезная форма: file:memdb_<guid>?mode=memory&cache=shared
            _connectionString = $"Data Source=file:memdb_{Guid.NewGuid():N}?mode=memory&cache=shared";

            // Открываем "мастер"-соединение и на нём создаём схему. Это поддерживает жизнь in-memory DB.
            _masterConnection = new SqliteConnection(_connectionString);
            _masterConnection.Open();

            // На мастере сразу включим WAL и зададим busy_timeout — это помогает параллельным writes.
            using (var cmd = _masterConnection.CreateCommand())
            {
                cmd.CommandText = "PRAGMA journal_mode = WAL;";
                cmd.ExecuteNonQuery();

                cmd.CommandText = "PRAGMA synchronous = NORMAL;";
                cmd.ExecuteNonQuery();

                cmd.CommandText = "PRAGMA busy_timeout = 5000;"; // 5 сек
                cmd.ExecuteNonQuery();
            }

            // Создаём контекст, используя мастер-соединение, и создаём схему
            var masterOptions = new DbContextOptionsBuilder<DiningPhilosophers.Persistence.SimulationDbContext>()
                .UseSqlite(_masterConnection)
                .Options;

            using (var ctx = new DiningPhilosophers.Persistence.SimulationDbContext(masterOptions))
            {
                ctx.Database.EnsureCreated();
            }

            // Фабричные опции — НЕ reuse мастера: каждый DbContext будет открывать своё соединение к same shared memory.
            _factoryOptions = new DbContextOptionsBuilder<DiningPhilosophers.Persistence.SimulationDbContext>()
                .UseSqlite(_connectionString)
                .Options;

            _factory = new TestDbContextFactory(_factoryOptions);
            _persistence = new DiningPhilosophers.Persistence.SimulationPersistence(_factory);

            // Создадим run
            _runId = _persistence.CreateRunAsync(null).GetAwaiter().GetResult();
        }

        public void Dispose()
        {
            try
            {
                _masterConnection?.Close();
                _masterConnection?.Dispose();
            }
            catch { /* ignore */ }
        }

        // Тест проверяет корректность параллельного логирования: N логов параллельно приводят к N записям.
        // Вход: N параллельных вызовов LogPhilosopherEventAsync.
        // Выход: N записей в БД и отсутствие исключений.
        //
        // Реализация: используем shared in-memory DB (file:...;mode=memory&cache=shared) и включаем WAL + busy_timeout,
        // чтобы уменьшить вероятность блокировок при параллельных записях в SQLite.
        [Fact]
        public async Task ConcurrentLogging_NoDataLoss()
        {
            // PRE-WARM: инициализация EF Core / SQLite в одном потоке до параллельного доступа.
            // Это предотвращает редкую гонку инициализации провайдера.
            using (var warmCtx = _factory.CreateDbContext())
            {
                await warmCtx.PhilosopherStateEvents.AnyAsync();
            }

            const int tasksCount = 30;
            var tasks = new List<Task>();

            for (int i = 0; i < tasksCount; i++)
            {
                int local = i;
                tasks.Add(Task.Run(async () =>
                {
                    // Каждый DbContext открывает собственное соединение к общей in-memory БД благодаря строке соединения.
                    await _persistence.LogPhilosopherEventAsync(_runId,
                        new DiningPhilosophers.Persistence.Entities.PhilosopherStateEvent
                        {
                            PhilosopherName = $"P{local}",
                            State = "Thinking",
                            StepNumber = local
                        });
                }));
            }

            await Task.WhenAll(tasks);

            using var verifyCtx = _factory.CreateDbContext();
            var count = await verifyCtx.PhilosopherStateEvents.CountAsync();
            Assert.Equal(tasksCount, count);
        }
    }
}
