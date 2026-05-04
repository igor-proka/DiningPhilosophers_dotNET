# Dining Philosophers in C#/.NET

Учебный проект по классической задаче об обедающих философах. Репозиторий вырос из набора лабораторных работ (курс "Введение в C# и платформу .Net" - 2025, НГУ ФИТ 4 курс): от простой пошаговой симуляции до многопоточного приложения, persistence-слоя и микросервисной версии с RabbitMQ.

Исходные формулировки заданий находятся в [TASKS.md](TASKS.md).

## Что реализовано

- Пошаговая консольная симуляция без многопоточности.
- Многопоточная симуляция, где каждый философ работает независимо.
- Стратегии захвата вилок: наивная, иерархия ресурсов, стратегия с координатором.
- Проверка deadlock-состояний и сбор метрик по ожиданию, throughput и использованию вилок.
- Вариант на `.NET Generic Host` с `BackgroundService`, DI, конфигурацией через `appsettings.json` и graceful shutdown.
- Persistence-слой на PostgreSQL и Entity Framework Core: хранение запусков, событий философов и состояний вилок.
- Консольная утилита `DiningPhilosophers.View` для просмотра состояния симуляции на заданный момент времени.
- Микросервисная версия через Docker Compose: Table Service, пять Philosopher Service, Coordinator Service и RabbitMQ.
- Unit- и integration-тесты для стратегий, симуляции, метрик, deadlock-проверок и persistence-слоя.

## Стек

- C# 12 / .NET 8
- ASP.NET Core Web API
- .NET Generic Host, BackgroundService, Dependency Injection, Options
- Entity Framework Core, PostgreSQL, SQLite InMemory для тестов
- Docker, Docker Compose
- RabbitMQ, MassTransit
- xUnit, Moq, coverlet

## Архитектура решения

```text
src/
  DiningPhilosophers.Core          Доменные модели и контракты
  DiningPhilosophers.Services      Логика симуляции, метрики, deadlock checker
  DiningPhilosophers.Strategies    Стратегии поведения философов и координаторы
  DiningPhilosophers.App           Простая консольная точка входа
  DiningPhilosophers.Hosted        Generic Host версия с BackgroundService
  DiningPhilosophers.Persistence   EF Core, PostgreSQL, миграции, репозитории
  DiningPhilosophers.View          CLI для просмотра сохраненного состояния
  Microservices/
    TableService                   HTTP API состояния стола и метрик
    PhilosopherService             Сервис отдельного философа
    CoordinatorService             Координация через события RabbitMQ
    Microservices.Shared           Общие контракты сообщений и DTO

tests/
  DiningPhilosophers.Tests
  DiningPhilosophers.Persistence.Tests
```

Основная идея разделения: доменная модель и контракты не зависят от инфраструктуры, стратегии вынесены отдельно, симуляция живет в сервисном слое, а способы запуска представлены отдельными приложениями. Это позволяет сравнивать разные модели исполнения одной и той же задачи: пошаговую, многопоточную, hosted и микросервисную.

## Как запустить

### Требования

- .NET SDK 8
- Docker и Docker Compose
- PostgreSQL, если запускается версия с сохранением состояния

### Сборка и тесты

```powershell
dotnet restore DiningPhilosophers.sln
dotnet build DiningPhilosophers.sln
dotnet test DiningPhilosophers.sln
```

### Пошаговая симуляция

```powershell
dotnet run --project src/DiningPhilosophers.App
```

### Многопоточная симуляция

```powershell
dotnet run --project src/DiningPhilosophers.App -- multithreaded
```

### Generic Host + PostgreSQL

Для hosted-версии нужна строка подключения к PostgreSQL. Ее можно передать через переменную окружения `SIM_DB`; это удобнее, чем менять локальный `appsettings.json`.

```powershell
$env:SIM_DB="Host=localhost;Port=5432;Database=dining_philosophers;Username=postgres;Password=postgres"
dotnet run --project src/DiningPhilosophers.Hosted
```

При старте приложение применяет EF Core migrations, создает новый `runId`, выводит его в консоль и сохраняет события симуляции в БД.

### Просмотр состояния сохраненного запуска

```powershell
$env:SIM_DB="Host=localhost;Port=5432;Database=dining_philosophers;Username=postgres;Password=postgres"
dotnet run --project src/DiningPhilosophers.View -- --runId <run-guid> --delay 5.5
```

`--delay` задает смещение в секундах от начала симуляции. Утилита восстанавливает состояние философов и вилок на этот момент.

### Микросервисы через Docker Compose

```powershell
docker compose up --build
```

Поднимаются:

- RabbitMQ: `localhost:5672`, management UI `http://localhost:15672`
- Table Service: `http://localhost:8080`
- Coordinator Service: `http://localhost:8086`
- Philosopher Service instances: `http://localhost:8081` ... `http://localhost:8085`

Полезные endpoint'ы Table Service:

```text
GET  http://localhost:8080/api/health
GET  http://localhost:8080/api/forks
GET  http://localhost:8080/api/metrics
GET  http://localhost:8080/api/metrics/summary
POST http://localhost:8080/api/forks/action
```

## Что показывает проект

- Работа с многопоточностью и синхронизацией доступа к общим ресурсам.
- Выделение доменной логики, сервисного слоя и инфраструктуры.
- Использование DI, конфигурации, hosted services и cancellation tokens.
- Проектирование REST API и межсервисного взаимодействия.
- Использование брокера сообщений для event-driven координации.
- Работа с EF Core migrations и интеграционными тестами persistence-слоя.
- Покрытие поведения тестами, включая конкурентные сценарии.
