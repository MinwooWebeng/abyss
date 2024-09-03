using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbyssCLI.Tool
{
    class WaiterGroup<T>
    {
        public void FinalizeValue(T value)
        {
            lock(_waiters)
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
                    return;
                }
            }
        }
        public bool TryGetValueOrWaiter(out T value, out Waiter<T> waiter)
        {
            lock(_waiters)
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
        public bool IsFinalized { get { return finished; } }
        private T result;
        private bool finished = false; //0: init, 1: loading, 2: loaded (no need to check sema)
        private readonly HashSet<Waiter<T>> _waiters = [];
    }
}
