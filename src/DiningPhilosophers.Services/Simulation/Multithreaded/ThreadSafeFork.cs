using System.Threading;
using System.Threading.Tasks;
using DiningPhilosophers.Core.Models;

namespace DiningPhilosophers.Services.Simulation.Multithreaded
{
    public class ThreadSafeFork
    {
        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();
        private ForkState _state = ForkState.Available;
        private string? _owner;

        public int Id { get; }
        
        public ForkState State 
        {
            get
            {
                _lock.EnterReadLock();
                try { return _state; }
                finally { _lock.ExitReadLock(); }
            }
        }

        public string? Owner
        {
            get
            {
                _lock.EnterReadLock();
                try { return _owner; }
                finally { _lock.ExitReadLock(); }
            }
        }

        public ThreadSafeFork(int id)
        {
            Id = id;
        }

        public bool TryAcquire(string philosopherName)
        {
            _lock.EnterWriteLock();
            try
            {
                if (_state == ForkState.Available)
                {
                    _state = ForkState.InUse;
                    _owner = philosopherName;
                    return true;
                }
                return false;
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public void Release()
        {
            _lock.EnterWriteLock();
            try
            {
                _state = ForkState.Available;
                _owner = null;
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public void UpdateState(ForkState state, string owner)
        {
            _lock.EnterWriteLock();
            try
            {
                _state = state;
                _owner = owner;
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }
    }
}