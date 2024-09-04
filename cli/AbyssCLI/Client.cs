using AbyssCLI.ABI;
using AbyssCLI.Content;
using System.Runtime.CompilerServices;
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
                _ = Task.Run(()=>
                {
                    while (true)
                    {
                        AndHandleFunc(_host.AndWaitEvent());
                    }
                });
                _ = Task.Run(()=>
                {
                    while (true)
                    {
                        var som_event = _host.SomWaitEvent();
                        if (som_event.Type == AbyssLib.SomEventType.Invalid)
                            return;
                        SomHandleFunc(som_event);
                    }
                });
                _ = Task.Run(ErrorHandleFunc);

                _cout.LocalInfo(_host.LocalAddr());

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
        private void AndHandleFunc(AbyssLib.AndEvent and_event)
        {
            _cerr.WriteLine($"AND: {JsonSerializer.Serialize(and_event)}");
            //{ "Type":2,
            //"Status":200,"Message":"Open",
            //"LocalPath":"/","PeerHash":"mallang",
            //"WorldJson":"{\u0022UUID\u0022:\u0022b84bf1dd-f36a-4e4f-bd44-5567ea1553ac\u0022,
            //\u0022URL\u0022:\u0022static/empty.aml\u0022}"}
            switch (and_event.Type)
            {
                case AbyssLib.AndEventType.JoinDenied:
                case AbyssLib.AndEventType.JoinExpired:
                    //do nothing?
                    break;
                case AbyssLib.AndEventType.JoinSuccess:
                    var new_world = new World(
                        _cout, _cerr, 
                        new ResourceLoader(_host, "abyst:" + _host.LocalHash + "/", "abyst", _cout), 
                        and_event.UUID, 
                        and_event.URL);

                    lock (_worlds)
                    {
                        if(_worlds.TryGetValue(and_event.LocalPath, out _))
                        {
                            _cerr.WriteLine("fatal: world collision at " + and_event.LocalPath);
                            return;
                        }
                        _worlds.Add(and_event.LocalPath, new_world);
                    }
                    new_world.Activate(); //does not block

                    break;
                case AbyssLib.AndEventType.PeerJoin:
                    break;
                case AbyssLib.AndEventType.PeerLeave:
                    break;
                default:
                    throw new Exception("terminating and handler");
            }
        }
        private void SomHandleFunc(AbyssLib.SomEvent som_event)
        {
            _cerr.WriteLine($"SOM: {som_event}");
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
                    case UIAction.InnerOneofCase.ConnectPeer:
                        _host.RequestConnect(message.ConnectPeer.Aurl);
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
                _cerr.WriteLine("peer join not supported yet");
                return;
            }
            lock(_worlds)
            {
                if (_worlds.TryGetValue("/", out var old_world))
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
            }
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