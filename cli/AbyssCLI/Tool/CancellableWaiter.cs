//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace AbyssCLI.Tool
//{
//    internal class VariableHolder<T>(T init_value)
//    {
//        public T Value { get; set; } = init_value;
//    }
//    internal class CancellableWaitResult<T>
//    {
//        public T GetValue()
//        {
//            return _waiter.GetValue();
//        }
//        public void CancelWithValue(T value)
//        {
//            if(_waiter.IsFirstAccess())
//            {
//                _waiter.SetValue(value);
//            }
//        }
//        private Waiter<T> _waiter = new();
//    }
//    internal class CancellableWaiter<T>(T default_value)
//    {
//        private readonly VariableHolder<T> result = new(default_value);
//        private readonly Semaphore semaphore = new(0, 1);
//        private int state = 0; //0: init, 1: loading, 2: loaded (no need to check sema)
//        public bool IsFirstAccess() => Interlocked.CompareExchange(ref state, 1, 0) == 0;
//        public void SetValue(T t) //this must be called only once.
//        {
//            if (state != 0) throw new Exception("fatal: Waiter.SetValue() called twice");
//            result.Value = t;
//            state = 2;
//            semaphore.Release();  
//        }
//        public T GetValue()
//        {
//            if (state < 2)
//            {
//                semaphore.WaitOne();
//                semaphore.Release();
//                return result;
//            }
//            return result;
//        }
//    }
//}
