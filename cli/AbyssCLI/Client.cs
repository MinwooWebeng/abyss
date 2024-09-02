using AbyssCLI.ABI;
using AbyssCLI.Content;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace AbyssCLI
{
    public class Client
    {
        public Client()
        {
            _cin = new BinaryReader(Console.OpenStandardInput());
            _cout = new RenderActionWriter(Console.OpenStandardOutput());
            _cerr = new StreamWriter(Stream.Synchronized(Console.OpenStandardError()))
            {
                AutoFlush = true
            };
            _worlds = [];
        }
        public async Task RunAsync()
        {
            try
            {
                //Host Initialization
                var init_msg = ReadProtoMessage();
                if (init_msg.InnerCase != UIAction.InnerOneofCase.Init)
                {
                    throw new Exception("fatal: host not initialized");
                }

                _host = new AbyssLib.AbyssHost(init_msg.Init.LocalHash, init_msg.Init.Http3RootDir);
                _ = Task.Run(AndHandleFunc);
                _ = Task.Run(SomHandleFunc);
                _ = Task.Run(ErrorHandleFunc);

                await Task.Run(MainHandleFunc);
            }
            catch (Exception ex)
            {
                _cerr.WriteLine(ex.ToString());
            }
        }
        private UIAction ReadProtoMessage()
        {
            int length = _cin.ReadInt32();
            if (length <= 0)
            {
                throw new Exception("invalid length message");
            }
            byte[] data = _cin.ReadBytes(length);
            if (data.Length != length)
            {
                throw new Exception("invalid length message");
            }
            return UIAction.Parser.ParseFrom(data);
        }
        private void AndHandleFunc()
        {
            _cerr.WriteLine("AndHandle!");
            while (true)
            {
                var ev_a = _host.AndWaitEvent();
                _cerr.WriteLine($"AND: {JsonSerializer.Serialize(ev_a)}");
            }
        }
        private void SomHandleFunc()
        {
            while (true)
            {
                _cerr.WriteLine($"SOM: {_host.SomWaitEvent()}");
            }
        }
        private void ErrorHandleFunc()
        {
            while (true)
            {
                _cerr.WriteLine($"Error: {_host.WaitError()}");
            }
        }
        private void MainHandleFunc()
        {
            while (true)
            {
                var message = ReadProtoMessage();
                switch (message.InnerCase)
                {
                    case UIAction.InnerOneofCase.None:
                    case UIAction.InnerOneofCase.Init:
                        throw new Exception("fatal: received invalid UI Action");
                    case UIAction.InnerOneofCase.Kill:
                        return; //graceful shutdown
                    case UIAction.InnerOneofCase.MoveWorld:
                        MoveWorld(message.MoveWorld);
                        break;
                    case UIAction.InnerOneofCase.ShareContent:
                        var content_id = ShareContent(message.ShareContent);
                        break;
                    default:
                        throw new Exception("fatal: received invalid UI Action");
                }
            }
        }
        private void MoveWorld(UIAction.Types.MoveWorld args)
        {
            if (args.WorldUrl.StartsWith("abyss"))
            {
                _cerr.WriteLine("peer join not supported");
                return;
            }
            if(_worlds.TryGetValue("/", out var old_world))
            {
                old_world.Close();
                _host.AndCloseWorld("/");
                _worlds.Remove("/");
            }
            if (_host.AndOpenWorld("/", args.WorldUrl) != 0)
            {
                _cerr.WriteLine("failed to move world: " + args.WorldUrl);
                return;
            }

            //TODO: move this to AndHandleFunc.
            var new_world = new World(_cout, _cerr, new ResourceLoader(_host, "abyst:" + _host.LocalHash + "/", "abyst", _cout), "wtf??", args.WorldUrl);
            new_world.Activate(); //does not block
            _worlds["/"] = new_world;
        }
        private int ShareContent(UIAction.Types.ShareContent args)
        {
            throw new NotImplementedException();
        }

        private readonly BinaryReader _cin;
        private readonly RenderActionWriter _cout;
        private readonly StreamWriter _cerr;
        private AbyssLib.AbyssHost _host = null;

        //local path -> world dictionary. currently it has only one entry("/");
        private readonly Dictionary<string, Content.World> _worlds;
    }
}