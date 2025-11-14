using System;
using DiningPhilosophers.Core.Models;

namespace DiningPhilosophers.Core.Contracts.Strategies
{
    public interface ICoordinator
    {
        // Философ сообщает координатору, что он голоден
        void NotifyHungry(Philosopher philosopher);

        // Философ сообщает, что закончил есть (вилки можно освободить)
        void NotifyFinished(Philosopher philosopher);

        // Координатор сообщает философу, что можно взять вилку/вилки или ничего не делать
        event Action<Philosopher, PhilosopherAction>? DecisionEvent;
    }
}
