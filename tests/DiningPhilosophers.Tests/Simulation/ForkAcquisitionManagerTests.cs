using System;
using DiningPhilosophers.Core.Models;
using DiningPhilosophers.Services.Simulation;
using Xunit;

namespace DiningPhilosophers.Tests.Simulation
{
    // Тесты для менеджера захвата вилок в пошаговой симуляции.
    // Проверяет логику постепенного взятия вилок с учетом времени приобретения.
    public class ForkAcquisitionManagerTests
    {
        private readonly ForkAcquisitionManager _manager;
        private const int AcquisitionTime = 2; // 2 шага на взятие вилки

        public ForkAcquisitionManagerTests()
        {
            _manager = new ForkAcquisitionManager(AcquisitionTime);
        }

        // Тест: Проверяет постепенное взятие левой вилки.
        // Что проверяем:
        // 1. Прогресс увеличивается на 1 за каждый шаг
        // 2. Вилка берется только после достижения AcquisitionTime
        // 3. Состояние вилки и владелец обновляются корректно
        [Fact]
        public void ProcessLeftForkAcquisition_AcquiresAfterFullProgress()
        {
            // Arrange - подготовка
            var philosopher = new Philosopher("ТестовыйФилософ");
            var fork = new Fork(1) { State = ForkState.Available };
            _manager.InitializePhilosopher(philosopher);

            // Act & Assert - проверяем пошаговый прогресс
            
            // Шаг 1: Прогресс = 1, вилка еще не взята
            _manager.ProcessLeftForkAcquisition(philosopher, fork);
            Assert.False(philosopher.HasLeftFork, 
                "После первого шага вилка еще не должна быть взята");
            Assert.Equal(ForkState.Available, fork.State);
            Assert.Null(fork.Owner);

            // Шаг 2: Прогресс = 2 (достигнут AcquisitionTime), вилка должна быть взята
            _manager.ProcessLeftForkAcquisition(philosopher, fork);
            Assert.True(philosopher.HasLeftFork, 
                "После достижения времени захвата вилка должна быть взята");
            Assert.Equal(ForkState.InUse, fork.State);
            Assert.Equal("ТестовыйФилософ", fork.Owner);
        }

        // Тест: Проверяет взятие правой вилки.
        // Что проверяем:
        // 1. Аналогичная логика как для левой вилки
        // 2. Независимость прогресса для левой и правой вилок
        [Fact]
        public void ProcessRightForkAcquisition_AcquiresAfterFullProgress()
        {
            // Arrange
            var philosopher = new Philosopher("ТестовыйФилософ");
            var fork = new Fork(1) { State = ForkState.Available };
            _manager.InitializePhilosopher(philosopher);

            // Act & Assert
            _manager.ProcessRightForkAcquisition(philosopher, fork); // Прогресс 1
            Assert.False(philosopher.HasRightFork);

            _manager.ProcessRightForkAcquisition(philosopher, fork); // Прогресс 2
            Assert.True(philosopher.HasRightFork);
            Assert.Equal(ForkState.InUse, fork.State);
            Assert.Equal("ТестовыйФилософ", fork.Owner);
        }

        // Тест: Проверяет сброс прогресса.
        // Что проверяем:
        // 1. ResetProgress обнуляет прогресс для обеих вилок
        // 2. После сброса нужно начинать захват заново
        [Fact]
        public void ResetProgress_ResetsBothLeftAndRightProgress()
        {
            // Arrange
            var philosopher = new Philosopher("ТестовыйФилософ");
            var leftFork = new Fork(1) { State = ForkState.Available };
            var rightFork = new Fork(2) { State = ForkState.Available };
            
            _manager.InitializePhilosopher(philosopher);
            
            // Делаем прогресс для обеих вилок
            _manager.ProcessLeftForkAcquisition(philosopher, leftFork);  // Левый прогресс 1
            _manager.ProcessRightForkAcquisition(philosopher, rightFork); // Правый прогресс 1

            // Act - сбрасываем прогресс
            _manager.ResetProgress(philosopher);

            // Assert - после сброса вилки не должны быть взяты
            Assert.False(philosopher.HasLeftFork);
            Assert.False(philosopher.HasRightFork);

            // Проверяем, что нужно начинать заново
            _manager.ProcessLeftForkAcquisition(philosopher, leftFork); // Снова прогресс 1
            Assert.False(philosopher.HasLeftFork, 
                "После сброса прогресса вилка не должна браться за 1 шаг");
            
            _manager.ProcessLeftForkAcquisition(philosopher, leftFork); // Прогресс 2
            Assert.True(philosopher.HasLeftFork, 
                "После повторного прогресса вилка должна быть взята");
        }

        // Тест: Проверяет независимость прогресса для разных философов.
        // Что проверяем:
        // 1. Каждый философ имеет свой независимый прогресс
        // 2. Прогресс одного не влияет на другого
        [Fact]
        public void ProcessAcquisition_IndependentProgressPerPhilosopher()
        {
            // Arrange
            var philosopher1 = new Philosopher("Философ1");
            var philosopher2 = new Philosopher("Философ2");
            var fork = new Fork(1) { State = ForkState.Available };
            
            _manager.InitializePhilosopher(philosopher1);
            _manager.InitializePhilosopher(philosopher2);

            // Act - философ1 делает 1 шаг прогресса
            _manager.ProcessLeftForkAcquisition(philosopher1, fork);

            // Assert - философ2 не должен иметь прогресса
            _manager.ProcessLeftForkAcquisition(philosopher2, fork);
            Assert.False(philosopher1.HasLeftFork);
            Assert.False(philosopher2.HasLeftFork);

            // Философ1 завершает взятие
            _manager.ProcessLeftForkAcquisition(philosopher1, fork);
            Assert.True(philosopher1.HasLeftFork);
            Assert.Equal("Философ1", fork.Owner);

            // Философ2 не может взять занятую вилку
            _manager.ProcessLeftForkAcquisition(philosopher2, fork);
            _manager.ProcessLeftForkAcquisition(philosopher2, fork);
            Assert.False(philosopher2.HasLeftFork, 
                "Философ2 не должен взять вилку, занятую философом1");
        }

        // Тест: Проверяет взятие вилки, предварительно зарезервированной координатором.
        // Что проверяем:
        // 1. Если координатор установил Owner, философ может взять вилку
        // 2. Прогресс работает как обычно
        // 3. Состояние вилки обновляется корректно
        [Fact]
        public void ProcessAcquisition_RespectsCoordinatorReservation()
        {
            // Arrange
            var philosopher = new Philosopher("ТестовыйФилософ");
            var fork = new Fork(1) 
            { 
                State = ForkState.Available,
                Owner = "ТестовыйФилософ" // Координатор зарезервировал
            };
            
            _manager.InitializePhilosopher(philosopher);

            // Act & Assert
            // Шаг 1: Прогресс с учетом резервации
            _manager.ProcessLeftForkAcquisition(philosopher, fork);
            Assert.False(philosopher.HasLeftFork, 
                "После 1 шага даже с резервацией вилка еще не взята");

            // Шаг 2: Завершение взятия
            _manager.ProcessLeftForkAcquisition(philosopher, fork);
            Assert.True(philosopher.HasLeftFork, 
                "После достижения времени вилка должна быть взята");
            Assert.Equal(ForkState.InUse, fork.State);
            Assert.Equal("ТестовыйФилософ", fork.Owner);
        }

        // Тест: Проверяет сброс прогресса при занятой вилке.
        // Что проверяем:
        // 1. Если вилка занята другим философом, прогресс сбрасывается
        // 2. Философ не может взять занятую вилку
        [Fact]
        public void ProcessAcquisition_ResetsProgress_WhenForkInUseByOther()
        {
            // Arrange
            var philosopher1 = new Philosopher("Философ1");
            var philosopher2 = new Philosopher("Философ2");
            var fork = new Fork(1) 
            { 
                State = ForkState.InUse, 
                Owner = "Философ1" // Занята другим философом
            };
            
            _manager.InitializePhilosopher(philosopher2);

            // Act - философ2 пытается взять занятую вилку
            _manager.ProcessLeftForkAcquisition(philosopher2, fork); // Прогресс должен сброситься
            _manager.ProcessLeftForkAcquisition(philosopher2, fork); // Снова сбросится

            // Assert
            Assert.False(philosopher2.HasLeftFork, 
                "Не должен взять вилку, занятую другим философом");
            Assert.Equal("Философ1", fork.Owner); // Владелец должен остаться прежним
        }

        // Тест: Проверяет одновременное взятие левой и правой вилок.
        // Что проверяем:
        // 1. Независимый прогресс для левой и правой вилок
        // 2. Каждая вилка берется отдельно после своего прогресса
        // 3. Можно взять одну вилку, пока ждешь другую
        [Fact]
        public void ProcessAcquisition_SimultaneousLeftAndRightAcquisition()
        {
            // Arrange
            var philosopher = new Philosopher("ТестовыйФилософ");
            var leftFork = new Fork(1) { State = ForkState.Available };
            var rightFork = new Fork(2) { State = ForkState.Available };
            
            _manager.InitializePhilosopher(philosopher);

            // Act & Assert - берем обе вилки поочередно
            
            // Шаг 1: Прогресс для обеих вилок
            _manager.ProcessLeftForkAcquisition(philosopher, leftFork);
            _manager.ProcessRightForkAcquisition(philosopher, rightFork);
            Assert.False(philosopher.HasLeftFork);
            Assert.False(philosopher.HasRightFork);

            // Шаг 2: Завершаем взятие левой вилки, правая еще в процессе
            _manager.ProcessLeftForkAcquisition(philosopher, leftFork);
            Assert.True(philosopher.HasLeftFork, "Левая вилка должна быть взята");
            Assert.False(philosopher.HasRightFork, "Правая вилка еще в процессе");

            // Шаг 3: Завершаем взятие правой вилки
            _manager.ProcessRightForkAcquisition(philosopher, rightFork);
            Assert.True(philosopher.HasRightFork, "Теперь и правая вилка должна быть взята");

            // Проверяем состояние вилок
            Assert.Equal(ForkState.InUse, leftFork.State);
            Assert.Equal(ForkState.InUse, rightFork.State);
            Assert.Equal("ТестовыйФилософ", leftFork.Owner);
            Assert.Equal("ТестовыйФилософ", rightFork.Owner);
        }

        // Тест: Проверяет обработку случая, когда вилка уже взята этим философом.
        // Что проверяем:
        // 1. Если философ уже имеет вилку, повторные вызовы не должны ломать состояние
        // 2. Прогресс не должен накапливаться для уже взятой вилки
        [Fact]
        public void ProcessAcquisition_NoEffect_WhenPhilosopherAlreadyHasFork()
        {
            // Arrange
            var philosopher = new Philosopher("ТестовыйФилософ") 
            { 
                HasLeftFork = true // Уже имеет вилку
            };
            
            var fork = new Fork(1) 
            { 
                State = ForkState.InUse, 
                Owner = "ТестовыйФилософ" 
            };
            
            _manager.InitializePhilosopher(philosopher);

            // Act - пытаемся "взять" уже взятую вилку
            _manager.ProcessLeftForkAcquisition(philosopher, fork);
            _manager.ProcessLeftForkAcquisition(philosopher, fork);

            // Assert - состояние не должно измениться
            Assert.True(philosopher.HasLeftFork);
            Assert.Equal(ForkState.InUse, fork.State);
            Assert.Equal("ТестовыйФилософ", fork.Owner);
        }

        // Тест: Проверяет метод TryTakeFork через публичные методы.
        // Что проверяем:
        // 1. Вилка берется только если Available
        // 2. Если вилка уже InUse, возвращается false
        // 3. Состояние и владелец обновляются корректно
        [Fact]
        public void ProcessAcquisition_TryTakeFork_RespectsForkState()
        {
            // Arrange - два философа и одна вилка
            var philosopher1 = new Philosopher("Философ1");
            var philosopher2 = new Philosopher("Философ2");
            var fork = new Fork(1) { State = ForkState.Available };
            
            _manager.InitializePhilosopher(philosopher1);
            _manager.InitializePhilosopher(philosopher2);

            // Act - философ1 берет вилку
            for (int i = 0; i < AcquisitionTime; i++)
            {
                _manager.ProcessLeftForkAcquisition(philosopher1, fork);
            }

            // Assert - вилка у философа1
            Assert.True(philosopher1.HasLeftFork);
            Assert.Equal(ForkState.InUse, fork.State);
            Assert.Equal("Философ1", fork.Owner);

            // Попытка философа2 взять уже занятую вилку
            for (int i = 0; i < AcquisitionTime; i++)
            {
                _manager.ProcessLeftForkAcquisition(philosopher2, fork);
            }
            
            Assert.False(philosopher2.HasLeftFork, 
                "Философ2 не должен взять уже занятую вилку");
        }

        // Тест: Проверяет корректность инициализации философа.
        // Что проверяем:
        // 1. InitializePhilosopher устанавливает прогресс в 0
        // 2. Неинициализированный философ вызывает исключение (опционально)
        [Fact]
        public void InitializePhilosopher_SetsProgressToZero()
        {
            // Arrange
            var philosopher = new Philosopher("НовыйФилософ");
            var fork = new Fork(1) { State = ForkState.Available };

            // Act - инициализируем и сразу пытаемся взять
            _manager.InitializePhilosopher(philosopher);
            _manager.ProcessLeftForkAcquisition(philosopher, fork);

            // Assert - после инициализации прогресс должен быть 1, не 0
            // (Менеджер должен обрабатывать философа)
            // Для проверки нужен второй вызов
            _manager.ProcessLeftForkAcquisition(philosopher, fork);
            Assert.True(philosopher.HasLeftFork, 
                "После инициализации и двух шагов вилка должна быть взята");
        }
    }
}