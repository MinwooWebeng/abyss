using AbyssCLI.ABI;
using AbyssCLI.Aml;
using AbyssCLI.Tool;
using System.Runtime;
using static AbyssCLI.AbyssLib;

namespace AbyssCLI.Client
{
    internal class World(AbyssHost host, RenderActionWriter renderActionWriter, StreamWriter cerr, string UUID, AbyssAddress URL)
    {
        public bool TryActivate()
        {
            lock(_lock)
            {
                if (_state != 0) return false;

                _environment.Activate();

                _state = 1; return true;
            }
        }
        public bool TryClose()
        {
            lock(_lock)
            {
                if (_state != 1) return false;

                _environment.Close();
                foreach (var member in _members)
                {
                    foreach (var content in member.Value.Values)
                    {
                        content.Close();
                    }
                }
                _members.Clear();
                foreach (var content in _local_contents.Values)
                {
                    content.Close();
                }
                _local_contents.Clear();

                _state = 2; return true;
            }
        }
        public bool TryAddPeer(string peer_hash)
        {
            lock(_lock)
            {
                if (_state != 1) return false;

                if(!_members.TryAdd(peer_hash, []))
                    return false;

                host.SomInitiateService(peer_hash, UUID);
                var error = host.SomRequestService(peer_hash, UUID);
                if(error != null)
                {
                    cerr.WriteLine("failed to request som service: " + error.ToString());
                }
                foreach (var content in _local_contents.Keys)
                {
                    error = host.SomShareObject(peer_hash, UUID, content);
                    if(error != null)
                    {
                        cerr.WriteLine(error.ToString());
                    }
                }
                return true;
            }
        }
        public bool TryRemovePeer(string peer_hash)
        {
            lock(_lock)
            {
                if (_state != 1) return false;

                if (!_members.TryGetValue(peer_hash, out var contents))
                    return false;

                foreach (var content in contents.Values)
                {
                    content.Close();
                }
                _members.Remove(peer_hash);
                host.SomTerminateService(peer_hash, UUID);
                return true;
            }
        }
        public bool TryAddPeerContent(string peer_hash, string content_uuid, AbyssAddress content_URL, vec3 initial_position)
        {
            lock(_lock)
            {
                if (_state != 1) return false;

                if (!_members.TryGetValue(peer_hash, out var contents))
                    return false;

                var content = new Aml.Content(host, renderActionWriter, cerr, content_uuid, content_URL, initial_position);
                if (!contents.TryAdd(content_uuid, content))
                    return false;

                content.Activate();
                return true;
            }
        }
        public bool TryRemovePeerContent(string peer_hash, string content_uuid)
        {
            lock (_lock)
            {
                if (_state != 1) return false;

                if (!_members.TryGetValue(peer_hash, out var contents))
                    return false;

                if (!contents.TryGetValue(content_uuid, out var content))
                    return false;

                content.Close();
                contents.Remove(content_uuid);
                return true;
            }
        }
        public bool TryRenewPeerContent(string peer_hash, Tuple<AbyssAddress/*url*/, string/*uuid*/, vec3>[] content_infos)
        {
            lock (_lock)
            {
                if (_state != 1) return false;

                if (!_members.TryGetValue(peer_hash, out var contents))
                    return false;

                foreach(var old_content in contents)
                {
                    old_content.Value.Close();
                }
                contents.Clear();

                foreach (var new_content in content_infos)
                {
                    var content = new Aml.Content(host, renderActionWriter, cerr, new_content.Item2, new_content.Item1, new_content.Item3);
                    if (!contents.TryAdd(new_content.Item2, content))
                        continue;

                    content.Activate();
                }
                return true;
            }
        }
        public bool TryAddLocalContent(string content_uuid, AbyssAddress content_URL, string initial_position)
        {
            lock(_lock)
            {
                if (_state != 1) return false;

                var content = new Aml.Content(host, renderActionWriter, cerr, content_uuid, content_URL, AmlValueParser.ParseVec3(initial_position));
                if (!_local_contents.TryAdd(content_uuid, content))
                    return false;

                content.Activate();
                host.SomRegisterObject(content_URL.String, content_uuid, initial_position);
                foreach (var member_hash in _members.Keys)
                {
                    var error = host.SomShareObject(member_hash, UUID, content_uuid);
                    if (error != null)
                    {
                        cerr.WriteLine("SomShareObject: " + error.ToString());
                    }
                }
                return true;
            }
        }
        public bool TryRemoveLocalContent(string content_uuid)
        {
            lock(_lock)
            {
                if (_state != 1) return false;

                if (!_local_contents.TryGetValue(content_uuid, out var content))
                    return false;

                content.Close();
                _local_contents.Remove(content_uuid);
                //TODO: som remove content
                return true;
            }
        }

        public readonly string UUID = UUID;
        private readonly Aml.Content _environment = new(host, renderActionWriter, cerr, UUID, URL, new vec3());
        private readonly Dictionary<string, Dictionary<string, Aml.Content>> _members = [];    //peer hash - [uuid - content]
        private readonly Dictionary<string, Aml.Content> _local_contents = [];    //UUID - content
        private readonly object _lock = new();
        private int _state = 0; //0: not activated, 1: activated, 2: closed
    }
}
