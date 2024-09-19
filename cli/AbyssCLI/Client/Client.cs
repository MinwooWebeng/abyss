using AbyssCLI.ABI;
using AbyssCLI.Aml;
using AbyssCLI.Tool;
using System.Text.Json;
using static AbyssCLI.AbyssLib;

namespace AbyssCLI.Client
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
        }
        public async Task RunAsync()
        {
            UIAction init_msg;
            try
            {
                //Host Initialization
                init_msg = ReadProtoMessage();
                if (init_msg.InnerCase != UIAction.InnerOneofCase.Init)
                {
                    throw new Exception("fatal: host not initialized");
                }
                _host = new AbyssHost(init_msg.Init.LocalHash, init_msg.Init.Http3RootDir);
            }
            catch (Exception ex)
            {
                _cerr.WriteLine(ex.ToString());
                return;
            }

            _cout.LocalInfo(_host.LocalAddr());

            await Task.WhenAll([
                Task.Run(()=>{
                    _cerr.WriteLine("AND fatal:" + TryLoop(AndHandleFunc));
                }),
                Task.Run(()=>{
                    _cerr.WriteLine("SOM fatal:" + TryLoop(SomHandleFunc));
                }),
                Task.Run(()=>{
                    _cerr.WriteLine("Cerr fatal:" + TryLoop(ErrorHandleFunc));
                }),
                Task.Run(()=>{
                    _cerr.WriteLine("Main fatal:" + TryLoop(MainHandleFunc));
                }),
            ]);
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
        private static Exception TryLoop(Action action)
        {
            try
            {
                while (true)
                {
                    action();
                }
            }
            catch (Exception ex)
            {
                return ex;
            }
        }
        private void AndHandleFunc()
        {
            var and_event = _host.AndWaitEvent();
            _cerr.WriteLine($"AND: {JsonSerializer.Serialize(and_event)}"); //debug
            switch (and_event.Type)
            {
                case AndEventType.JoinDenied:
                case AndEventType.JoinExpired: /* do nothing? */ return;
                case AndEventType.JoinSuccess: OnAndJoinSuccess(and_event); return;
                case AndEventType.PeerJoin: OnAndPeerJoin(and_event); return;
                case AndEventType.PeerLeave: OnAndPeerLeave(and_event); return;
                case AndEventType.Error:
                default: throw new Exception("terminating and handler");
            }
        }
        private void SomHandleFunc()
        {
            var som_event = _host.SomWaitEvent();
            _cerr.WriteLine($"SOM: {som_event}"); //debug
            switch (som_event.Type)
            {
                case SomEventType.SomReNew: OnSomRenew(som_event); return;
                case SomEventType.SomAppend: OnSomAppend(som_event); return;
                case SomEventType.SomDelete: OnSomDelete(som_event); return;
                case SomEventType.SomDebug: _cerr.WriteLine(som_event.PeerHash); return;
                default: throw new Exception("terminating som handler");
            }
        }
        private void ErrorHandleFunc()
        {
            _cerr.WriteLine($"Error: {_host.WaitError()}");
        }
        private void MainHandleFunc()
        {
            var message = ReadProtoMessage();
            switch (message.InnerCase)
            {
                case UIAction.InnerOneofCase.MoveWorld: OnMoveWorld(message.MoveWorld); return;
                case UIAction.InnerOneofCase.ShareContent: OnShareContent(message.ShareContent); return;
                case UIAction.InnerOneofCase.ConnectPeer: _host.RequestConnect(message.ConnectPeer.Aurl); return;
                case UIAction.InnerOneofCase.Kill:
                case UIAction.InnerOneofCase.None:
                case UIAction.InnerOneofCase.Init:
                default: throw new Exception("fatal: received invalid UI Action");
            }
        }

        //Actual handler functions
        //peer join
        // 1) update world members.
        // 2) request SOM service to peer.
        // 3) initiate SOM service for join target.
        // 4) if there is my shared object for world, share
        // 5) on peer leave, terminate SOM service for peer.
        private void OnAndJoinSuccess(AndEvent and_event)
        {
            AbyssAddress world_url;
            try
            {
                world_url = new AbyssAddress(and_event.URL);
            }
            catch
            {
                _cerr.WriteLine("failed to parse world address: " + and_event.URL);
                return;
            }
            if (!(world_url.Scheme == AbyssAddress.EScheme.Abyst ||
                world_url.Scheme == AbyssAddress.EScheme.WWW))
            {
                _cerr.WriteLine("invalid world address scheme: " + and_event.URL);
                return;
            }

            var new_world = new World(_host, _cout, _cerr, and_event.UUID, world_url);
            if (!_world_map.TryAddMapping(and_event.LocalPath, and_event.UUID, new_world))
            {
                _cerr.WriteLine("world already exist for local path: " + and_event.LocalPath);
                return;
            }
            if(!new_world.TryActivate())
            {
                _cerr.WriteLine("failed to activate world: " + and_event.UUID);
                return;
            }
        }
        private void OnAndPeerJoin(AndEvent and_event)
        {
            if(!_world_map.TryGetWorld(and_event.UUID, out World world))
            {
                _cerr.WriteLine("peer joined in non-existing world: " + and_event.UUID);
                return;
            }
            if(!world.TryAddPeer(and_event.PeerHash))
            {
                _cerr.WriteLine("failed to add peer to world: " + and_event.UUID);
                return;
            }
        }
        private void OnAndPeerLeave(AndEvent and_event)
        {
            if (!_world_map.TryGetWorld(and_event.UUID, out World world))
            {
                _cerr.WriteLine("peer leaving non-existing world: " + and_event.UUID);
                return;
            }
            if (!world.TryRemovePeer(and_event.PeerHash))
            {
                _cerr.WriteLine("failed to remove peer from world: " + and_event.UUID);
                return;
            }
        }
        private void OnSomRenew(SomEvent somEvent)
        {
            if (!_world_map.TryGetWorld(somEvent.WorldUUID, out World world))
            {
                _cerr.WriteLine("som for non-existing world: " + somEvent.WorldUUID);
                return;
            }
            Tuple<AbyssAddress, string, vec3>[] parsed_content_info;
            try
            {
                parsed_content_info = somEvent.ObjectsInfo.Select(x => new Tuple<AbyssAddress, string, vec3>(new AbyssAddress(x.Item1), x.Item2, AmlValueParser.ParseVec3(x.Item3))).ToArray();
            }
            catch (Exception e)
            {
                _cerr.WriteLine("invalid address in som renew peer contents: " + somEvent.PeerHash + e.Message);
                return;
            }
            if (!world.TryRenewPeerContent(somEvent.PeerHash, parsed_content_info))
            {
                _cerr.WriteLine("failed to renew peer contents: " + somEvent.PeerHash);
                return;
            }
        }
        private void OnSomAppend(SomEvent somEvent)
        {
            if (!_world_map.TryGetWorld(somEvent.WorldUUID, out World world))
            {
                _cerr.WriteLine("som for non-existing world: " + somEvent.WorldUUID);
                return;
            }
            if (!world.TryAddPeerContent(somEvent.PeerHash, somEvent.ObjectsInfo[0].Item2, new AbyssAddress(somEvent.ObjectsInfo[0].Item1), AmlValueParser.ParseVec3(somEvent.ObjectsInfo[0].Item3)))
            {
                _cerr.WriteLine("failed to add peer contents: " + somEvent.PeerHash + somEvent.ObjectsInfo);
                return;
            }
        }
        private void OnSomDelete(SomEvent somEvent)
        {
            if (!_world_map.TryGetWorld(somEvent.WorldUUID, out World world))
            {
                _cerr.WriteLine("som for non-existing world: " + somEvent.WorldUUID);
                return;
            }
            if (!world.TryRemovePeerContent(somEvent.PeerHash, somEvent.ObjectsInfo[0].Item2))
            {
                _cerr.WriteLine("failed to add peer contents: " + somEvent.PeerHash + somEvent.ObjectsInfo);
                return;
            }
        }
        private void OnMoveWorld(UIAction.Types.MoveWorld args)
        {
            if (!TryParseMaybeLocalAddress(args.WorldUrl, out var world_url))
            {
                _cerr.WriteLine("failed to parse address: " + args.WorldUrl);
                return;
            }

            if(_world_map.TryPopMapping("/", out var old_world))
            {
                _host.AndCloseWorld("/");
                if (!old_world.TryClose())
                {
                    _cerr.WriteLine("failed to close world: " + old_world.UUID);
                    return;
                }
            }
            switch (world_url.Scheme)
            {
                //open
                case AbyssAddress.EScheme.WWW:
                case AbyssAddress.EScheme.Abyst:
                    if (_host.AndOpenWorld("/", world_url.String) != 0)
                    {
                        _cerr.WriteLine("failed to open world");
                    }
                    break;
                //join
                case AbyssAddress.EScheme.Abyss:
                    _host.AndJoin("/", world_url.String);
                    break;
            }
        }
        private void OnShareContent(UIAction.Types.ShareContent args)
        {
            if (!TryParseMaybeLocalAddress(args.Url, out var content_url))
            {
                _cerr.WriteLine("OnShareContent: failed to parse address: " + args.Url);
                return;
            }

            if (!_world_map.TryGetUUID("/", out var old_world_uuid) || !_world_map.TryGetWorld(old_world_uuid, out var old_world))
            {
                _cerr.WriteLine("OnShareContent: failed to find world");
                return;
            }
            var content_uuid = Guid.NewGuid().ToString();
            if (!old_world.TryAddLocalContent(content_uuid, content_url, args.InitialPosition))
            {
                _cerr.WriteLine("failed to share local content:" + content_url);
                return;
            }
        }
        private bool TryParseMaybeLocalAddress(string address, out AbyssAddress result)
        {
            result = null;
            try
            {
                result = new AbyssAddress(address);
                return true;
            }
            catch
            {
                if (address.Contains(':'))
                    return false;

                //consider as local address.
                result = new AbyssAddress(new AbyssAddress("abyst:" + _host.LocalHash).GetRelativeAddress(address));
                return true;
            }
        }

        private readonly BinaryReader _cin;
        private readonly RenderActionWriter _cout;
        private readonly StreamWriter _cerr;
        private AbyssHost _host = null;

        private readonly WorldPathMapper _world_map = new();
    }
}