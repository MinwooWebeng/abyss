namespace AbyssCLI.Tool
{
    public class Contexted
    {
        public Contexted()
        {
            CancellationTokenSource = new();
            _root_context = CancellationTokenSource.Token;
        }
        public Contexted(Contexted base_context)
        {
            CancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
                base_context.CancellationTokenSource.Token);
            _root_context = CancellationTokenSource.Token;
        }
        public Task Activate()
        {
            return Task.Run(async () =>
            {
                //prevent duplicated activation.
                if (Interlocked.CompareExchange(ref _state, 1, 0) != 0)
                    return;

                try
                {
                    await ActivateCallback(_root_context); //synchronize execution context to catch exception.
                }
                catch (Exception ex)
                {
                    if (ex is not TaskCanceledException)
                    {
                        ErrorCallback(ex);
                    }
                    SafeDecease();
                }
            }
            , _root_context);
        }
        public void RunAction(Action<CancellationToken> action)
        {
            var action_token = CancellationTokenSource.Token;
            Task.Run(() => { action(action_token); }, action_token);
        }
        public void Close()
        {
            CloseAsync();
        }
        public Task<Exception> CloseAsync()
        {
            return Task.Run(async () =>
            {
                try
                {
                    CancellationTokenSource.Cancel();

                    SafeDecease();
                    //decease termination barrier
                    _decease_sema.WaitOne();
                    _decease_sema.Release();
                    //

                    await CancellationTokenSource.CancelAsync();
                    if (Interlocked.CompareExchange(ref _state, 3, 2) == 2)
                    {
                        CleanupCallback();
                        _cleanup_sema.Release();
                    }
                    //cleanup termination barrier
                    _cleanup_sema.WaitOne();
                    _cleanup_sema.Release();
                }
                catch (Exception e)
                {
                    return e; //non-recoverable
                }
                return null;
            });
        }
        private void SafeDecease()
        {
            //decease before activation
            if (Interlocked.CompareExchange(ref _state, 2, 0) == 0)
            {
                _decease_sema.Release();
                return;
            }

            if (Interlocked.CompareExchange(ref _state, 2, 1) == 1)
            {
                DeceaseCallback();
                _decease_sema.Release();
                return;
            }

            //previously deceased, or cleanuped.
        }

        protected virtual Task ActivateCallback(CancellationToken token) { return Task.CompletedTask; }
        //ActivateCallback is guranteed to be called only once
        protected virtual void ErrorCallback(Exception e) { throw new NotImplementedException(); }
        //ErrorCallback must be thread safe
        protected virtual void DeceaseCallback() { return; }
        //DeceaseCallback is guaranteed to be called only once, only after ActivateCallback is finished or throwed
        protected virtual void CleanupCallback() { throw new NotImplementedException(); }
        //CleanupCallback is guaranteed to be called only once, after DeceaseCallback is finished.
        // **IMPORTANT** 
        // when overriding CleanupCallback,
        // it must override CleanupAsyncCallback
        // it is safe to override CleanupAsyncCallback alone

        public CancellationTokenSource CancellationTokenSource { get; private set; }
        private readonly CancellationToken _root_context;
        private readonly Semaphore _decease_sema = new(0, 1);
        private readonly Semaphore _cleanup_sema = new(0, 1);
        private int _state = 0; //0: non-activated, 1: activated, 2:decease called, 3: cleanup called
    }
}
