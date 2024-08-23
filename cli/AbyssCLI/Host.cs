using AbyssCLI.Tool;
using System.Collections.Concurrent;

namespace AbyssCLI
{
    public class Host
    {
        public Host(string hash, string h3_server_root_dir)
        {
            _localHash = hash;
            _netHost = new AbyssLib.AbyssHost(_localHash, h3_server_root_dir);
        }
        public void Start()
        {
            _netHost.AndOpenWorld("/", string.Concat("abyst:", _localHash + "/home"));
            new Thread(ProvideAndService).Start();
            new Thread(ProvideSomService).Start();
            new Thread(() =>
            {
                while(true)
                {
                    var host_err = _netHost.WaitError();
                    WriteLog(new Tool.StringError(host_err.ToString()));
                }
            }).Start();
        }
        public bool TryPopError(out Tool.IError err)
        {
            return _log.TryDequeue(out err);
        }
        public void URLAction(string aurl)
        {
            //_netHost.AndCloseWorld("/");
            //_netHost.AndJoin("/", aurl);
        }

        private void ProvideAndService()
        {
            while (true)
            {
                var and_event = _netHost.AndWaitEvent();
            }
        }
        private void ProvideSomService()
        {
            while (true)
            {
                var som_event = _netHost.SomWaitEvent();
            }
        }
        private void WriteLog(IError error)
        {
            _log.Enqueue(error);
        }

        private readonly string _localHash;
        private readonly AbyssLib.AbyssHost _netHost;
        private readonly ConcurrentQueue<Tool.IError> _log;
    }
}
