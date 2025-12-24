namespace DiningPhilosophers.Core.Models
{
    public class Philosopher
    {
        private readonly object _lock = new object();

        public string Name { get; }
        private PhilosopherState _state = PhilosopherState.Thinking;
        public PhilosopherState State
        {
            get { lock (_lock) { return _state; } }
            set { lock (_lock) { _state = value; } }
        }

        // Оставшиеся шаги/мс в текущем состоянии (thinking/eating)
        private int _stepsRemaining = 0;
        public int StepsRemaining
        {
            get { lock (_lock) { return _stepsRemaining; } }
            set { lock (_lock) { _stepsRemaining = value; } }
        }
        
        // Флаги владения вилками (модель знает только о своих вилках)
        private bool _hasLeftFork = false;
        public bool HasLeftFork
        {
            get { lock (_lock) { return _hasLeftFork; } }
            set { lock (_lock) { _hasLeftFork = value; } }
        }

        private bool _hasRightFork = false;
        public bool HasRightFork
        {
            get { lock (_lock) { return _hasRightFork; } }
            set { lock (_lock) { _hasRightFork = value; } }
        }

        // Сейчас выбранное действие (устанавливается стратегией/координатором)
        private PhilosopherAction _currentAction = PhilosopherAction.None;
        public PhilosopherAction CurrentAction
        {
            get { lock (_lock) { return _currentAction; } }
            set { lock (_lock) { _currentAction = value; } }
        }

        public Philosopher(string name) => Name = name ?? throw new ArgumentNullException(nameof(name));
    }
}