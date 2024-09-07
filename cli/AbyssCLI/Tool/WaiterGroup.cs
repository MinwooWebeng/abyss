namespace AbyssCLI.Tool
{
    class WaiterGroup<T>
    {
        public bool TryFinalizeValue(T value)
        {
            lock (_waiters)
            {
                if (!finished)
                {
                    result = value;
                    finished = true;
                    foreach (var waiter in _waiters)
                    {
                        waiter.SetterFinalize(value);
                    }
                    _waiters.Clear();
                    return true;
                }
            }
            return false;
        }
        public bool TryGetValueOrWaiter(out T value, out Waiter<T> waiter)
        {
            lock (_waiters)
            {
                if (finished)
                {
                    value = result;
                    waiter = null;
                    return true;
                }

                value = default;
                waiter = new Waiter<T>();
                waiter.TryClaimSetter(); //always success
                _waiters.Add(waiter);
                return false;
            }
        }
        public T GetValue() => result;

        [Obsolete]
        public void FinalizeValue(T value)
        {
            TryFinalizeValue(value);
        }
        public bool IsFinalized { get { return finished; } }
        private T result;
        private bool finished = false; //0: init, 1: loading, 2: loaded (no need to check sema)
        private readonly HashSet<Waiter<T>> _waiters = [];
    }
}
