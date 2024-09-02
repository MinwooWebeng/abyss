using System;
using System.Collections.Generic;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static AbyssCLI.Content.ResourceLoader;

namespace AbyssCLI.Tool
{
    internal class Waiter<T>
    {
        private T result;
        private readonly Semaphore semaphore = new(0, 1);
        private int state = 0; //0: init, 1: loading, 2: loaded (no need to check sema)
        public bool IsFirstAccess() => Interlocked.CompareExchange(ref state, 1, 0) == 0;
        public void SetValue(T t) //this must be called only once.
        {
            if (state == 2) throw new Exception("fatal: Waiter.SetValue() called twice");
            result = t;
            state = 2;
            semaphore.Release();
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
    }
}
