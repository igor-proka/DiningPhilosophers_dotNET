using System.Collections.Generic;
using DiningPhilosophers.Core.Models;

namespace DiningPhilosophers.Services.Simulation
{
    public class ForkAcquisitionManager
    {
        private readonly Dictionary<Philosopher, int> _leftAcquireProgress = new();
        private readonly Dictionary<Philosopher, int> _rightAcquireProgress = new();
        private readonly int _acquisitionTime;

        public ForkAcquisitionManager(int acquisitionTime)
        {
            _acquisitionTime = acquisitionTime;
        }

        public void InitializePhilosopher(Philosopher philosopher)
        {
            _leftAcquireProgress[philosopher] = 0;
            _rightAcquireProgress[philosopher] = 0;
        }

        public void ProcessLeftForkAcquisition(Philosopher philosopher, Fork leftFork)
        {
            // Если вилка уже зарезервирована под этого философа (координатор пометил Owner),
            // позволяем менеджеру продолжить прогресс захвата и в конце установить HasLeftFork = true.
            if (leftFork.Owner == philosopher.Name)
            {
                // Продолжаем прогресс (также учитываем, что состояние может уже быть InUse)
                _leftAcquireProgress[philosopher]++;
                if (_leftAcquireProgress[philosopher] >= _acquisitionTime)
                {
                    // Разрешаем философу считать, что он взял вилку
                    philosopher.HasLeftFork = true;

                    // Убедимся, что вилка помечена как InUse и владельцем — философ
                    leftFork.State = ForkState.InUse;
                    leftFork.Owner = philosopher.Name;

                    _leftAcquireProgress[philosopher] = 0;
                }
                return;
            }

            // Обычное поведение: вилка свободна => начинаем прогресс
            if (leftFork.State == ForkState.Available)
            {
                _leftAcquireProgress[philosopher]++;
                if (_leftAcquireProgress[philosopher] >= _acquisitionTime)
                {
                    if (TryTakeFork(leftFork, philosopher.Name))
                    {
                        philosopher.HasLeftFork = true;
                    }
                    _leftAcquireProgress[philosopher] = 0;
                }
            }
            else
            {
                _leftAcquireProgress[philosopher] = 0;
            }
        }

        public void ProcessRightForkAcquisition(Philosopher philosopher, Fork rightFork)
        {
            if (rightFork.Owner == philosopher.Name)
            {
                _rightAcquireProgress[philosopher]++;
                if (_rightAcquireProgress[philosopher] >= _acquisitionTime)
                {
                    philosopher.HasRightFork = true;
                    rightFork.State = ForkState.InUse;
                    rightFork.Owner = philosopher.Name;
                    _rightAcquireProgress[philosopher] = 0;
                }
                return;
            }

            if (rightFork.State == ForkState.Available)
            {
                _rightAcquireProgress[philosopher]++;
                if (_rightAcquireProgress[philosopher] >= _acquisitionTime)
                {
                    if (TryTakeFork(rightFork, philosopher.Name))
                    {
                        philosopher.HasRightFork = true;
                    }
                    _rightAcquireProgress[philosopher] = 0;
                }
            }
            else
            {
                _rightAcquireProgress[philosopher] = 0;
            }
        }

        public void ResetProgress(Philosopher philosopher)
        {
            _leftAcquireProgress[philosopher] = 0;
            _rightAcquireProgress[philosopher] = 0;
        }

        private bool TryTakeFork(Fork fork, string philosopherName)
        {
            if (fork.State == ForkState.Available)
            {
                fork.State = ForkState.InUse;
                fork.Owner = philosopherName;
                return true;
            }
            return false;
        }
    }
}