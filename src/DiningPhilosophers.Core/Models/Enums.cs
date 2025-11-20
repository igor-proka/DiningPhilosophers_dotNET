namespace DiningPhilosophers.Core.Models
{
    [System.Flags]
    public enum PhilosopherAction
    {
        None = 0,
        TakeLeftFork = 1 << 0,
        TakeRightFork = 1 << 1,
        ReleaseLeftFork = 1 << 2,
        ReleaseRightFork = 1 << 3
    }

    public enum PhilosopherState
    {
        Thinking,
        Hungry,
        Eating
    }

    public enum ForkState
    {
        Available,
        InUse
    }

    public enum CoordinatorType
    {
        Stupid,      // Специально создаёт дедлок
        Semaphore
    }
}
