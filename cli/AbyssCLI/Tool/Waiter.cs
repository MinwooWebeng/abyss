﻿namespace AbyssCLI.Tool
{
    internal class Waiter<T>
    {
        //every method is safe to call multiple times
        public bool TryClaimSetter() => Interlocked.CompareExchange(ref state, 1, 0) == 0;
        public void SetterFinalize(T value)
        {
            if (Interlocked.CompareExchange(ref state, 2, 1) == 1)
            {
                result = value;
                semaphore.Release();
                return;
            }
        }
        public void CancelWithValue(T value)
        {
            if(Interlocked.CompareExchange(ref state, 2, 0) == 0)
            {
                result = value;
                semaphore.Release();
                return;
            }

            if(Interlocked.CompareExchange(ref state, 2, 1) == 1)
            {
                result = value;
                semaphore.Release();
                return;
            }
        }
        public T GetValue()
        {
            if (state < 2)
            {
                semaphore.WaitOne();
                semaphore.Release();
                return result;
            }
            return result;
        }

        private T result;
        private readonly Semaphore semaphore = new(0, 1);
        private int state = 0; //0: init, 1: loading, 2: loaded (no need to check sema)

        [Obsolete("IsFirstAccess is deprecated, please use TryClaimSetter instead.")]
        public bool IsFirstAccess() => Interlocked.CompareExchange(ref state, 1, 0) == 0;
        [Obsolete("SetValue is deprecated, please use SetterFinalize/CancelWithValue instead.")]
        public void SetValue(T t) //deprecated
        {
            if (state == 2) throw new Exception("fatal: Waiter.SetValue() called twice");
            result = t;
            state = 2;
            semaphore.Release();
        }
    }
}