using AbyssCLI.Aml;
using Microsoft.VisualBasic;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

#nullable enable
namespace AbyssCLI
{
    static internal class AbyssLib
    {
        static public string GetVersion()
        {
            unsafe
            {
                [DllImport("abyssnet.dll")]
                static extern int GetVersion(byte* buf, int buflen);

                fixed (byte* pBytes = new byte[16])
                {
                    int len = GetVersion(pBytes, 16);
                    if (len < 0)
                    {
                        return "fail";
                    }
                    return System.Text.Encoding.UTF8.GetString(pBytes, len);
                }
            }
        }
        public class AbyssDllError
        {
            public AbyssDllError(IntPtr error_handle)
            {
                if (error_handle == IntPtr.Zero)
                    throw new ArgumentNullException(nameof(error_handle));
                this.error_handle = error_handle;
            }
            public override string ToString()
            {
                unsafe
                {
                    [DllImport("abyssnet.dll")]
                    static extern int GetErrorBodyLength(IntPtr err_handle);

                    [DllImport("abyssnet.dll")]
                    static extern int GetErrorBody(IntPtr err_handle, byte* buf, int buflen);

                    var msg_len = GetErrorBodyLength(error_handle);
                    var buf = new byte[msg_len];
                    fixed (byte* dBytes = buf)
                    {
                        var len = GetErrorBody(error_handle, dBytes, buf.Length);
                        if (len != buf.Length)
                        {
                            throw new Exception("AbyssDLL: buffer write failure");
                        }
                        return Encoding.UTF8.GetString(buf);
                    }
                }
            }
            ~AbyssDllError()
            {
                [DllImport("abyssnet.dll")]
                static extern void CloseError(IntPtr err_handle);
                CloseError(error_handle);
            }
            private readonly IntPtr error_handle;
        }
        public enum AndEventType
        {
            Error = -1,
            JoinDenied, //0
            JoinExpired,//1
            JoinSuccess,//2
            PeerJoin,   //3
            PeerLeave,  //4
        }
        public class AndWorldInfo
        {
            public required string UUID { get; set; }
            public required string URL { get; set; }
        }
        public class AndEvent
        {
            public AndEvent(AndEventType Type, int Status, string Message, string LocalPath, string PeerHash, string WorldJson)
            {
                this.Type = Type;
                this.Status = Status;
                this.Message = Message;
                this.LocalPath = LocalPath;
                this.PeerHash = PeerHash;
                if (WorldJson != "")
                {
                    var world_info = System.Text.Json.JsonSerializer.Deserialize<AndWorldInfo>(WorldJson)
                        ?? throw new Exception("failed to parse AND world info json");
                    this.UUID = world_info.UUID;
                    this.URL = world_info.URL;
                }
                else
                {
                    this.UUID = "";
                    this.URL = "";
                }
            }
            public AndEventType Type { get; }
            public int Status { get; }
            public string Message { get; }
            public string LocalPath { get; }
            public string PeerHash { get; }
            public string UUID { get; }
            public string URL { get; }
        }
        public enum SomEventType
        {
            Invalid = -1,  //SOM service termiated
            SomReNew,
            SomAppend,
            SomDelete,
            SomDebug
        }
        public class SomEvent(AbyssLib.SomEventType type, string peer_hash, string world_uuid, Tuple<string, string, string>[] objects_info)
        {
            public SomEventType Type { get; } = type;
            public string PeerHash { get; } = peer_hash;
            public string WorldUUID { get; } = world_uuid;
            public Tuple<string /*url(empty on SOD)*/, string /*uuid*/, string /*initial position*/>[] ObjectsInfo { get; } = objects_info;
            public override string ToString()
            {
                return string.Concat(
                    Convert.ChangeType(Type, Enum.GetUnderlyingType(Type.GetType())).ToString(), " ",
                    PeerHash, " ",
                    WorldUUID, " [",
                    Strings.Join(ObjectsInfo.Select(e => e.Item1 + " ~ " + e.Item2 + " @ " + e.Item3).ToArray(), ", "), "]"
                );
            }
        }
        public class AbyssHttpResponse
        {
            internal AbyssHttpResponse(IntPtr handle)
            {
                response_handle = handle;
            }
            ~AbyssHttpResponse()
            {
                [DllImport("abyssnet.dll")]
                static extern void CloseHttpResponse(IntPtr handle);

                CloseHttpResponse(response_handle);
            }

            public int GetStatus()
            {
                [DllImport("abyssnet.dll")]
                static extern int GetReponseStatus(IntPtr handle);

                return GetReponseStatus(response_handle);
            }
            public int GetBodyLength()
            {
                [DllImport("abyssnet.dll")]
                static extern int GetReponseBodyLength(IntPtr handle);

                return GetReponseBodyLength(response_handle);
            }
            public byte[] GetBody()
            {
                unsafe
                {
                    [DllImport("abyssnet.dll")]
                    static extern int GetResponseBody(IntPtr handle, byte* buf, int buflen);

                    var body_len = GetBodyLength();
                    if (body_len == 0)
                    {
                        throw new Exception("Abyst: empty body");
                    }
                    var buf = new byte[body_len];
                    fixed (byte* dBytes = buf)
                    {
                        var len = GetResponseBody(response_handle, dBytes, buf.Length);
                        if (len != buf.Length)
                        {
                            throw new Exception("AbyssDLL: buffer write failure");
                        }
                    }
                    return buf;
                }
            }

            readonly IntPtr response_handle;
        }
        public class AbyssHost
        {
            public AbyssHost(string hash, string backend_root_dir)
            {
                LocalHash = hash;
                unsafe
                {
                    [DllImport("abyssnet.dll")]
                    static extern IntPtr NewAbyssHost(byte* buf, int buflen, byte* backend_root, int backend_root_len);

                    var hash_bytes = Encoding.UTF8.GetBytes(hash);
                    var backend_root_bytes = Encoding.UTF8.GetBytes(backend_root_dir);
                    fixed (byte* pBytes = hash_bytes)
                    {
                        fixed (byte* dBytes = backend_root_bytes)
                        {
                            host_handle = NewAbyssHost(pBytes, hash_bytes.Length, dBytes, backend_root_bytes.Length);
                        }
                    }
                }

                if (host_handle == IntPtr.Zero)
                {
                    throw new Exception("abyss: failed to create host");
                }
            }
            ~AbyssHost()
            {
                unsafe
                {
                    [DllImport("abyssnet.dll")]
                    static extern void CloseAbyssHost(IntPtr handle);

                    CloseAbyssHost(host_handle);
                }
            }
            public AbyssDllError WaitError()
            {
                unsafe
                {
                    [DllImport("abyssnet.dll")]
                    static extern IntPtr GetAhmpError(IntPtr handle);

                    return new AbyssDllError(GetAhmpError(host_handle));
                }
            }
            public string LocalAddr()
            {
                unsafe
                {
                    [DllImport("abyssnet.dll")]
                    static extern int LocalAddr(IntPtr handle, byte* buf, int buflen);

                    fixed (byte* pBytes = new byte[1024])
                    {
                        int len = LocalAddr(host_handle, pBytes, 1024);
                        if (len < 0)
                        {
                            return "";
                        }
                        return Encoding.UTF8.GetString(pBytes, len);
                    }
                }
            }
            public void RequestConnect(string aurl)
            {
                unsafe
                {
                    [DllImport("abyssnet.dll")]
                    static extern void RequestPeerConnect(IntPtr handle, byte* buf, int buflen);

                    var aurl_bytes = Encoding.UTF8.GetBytes(aurl);
                    fixed (byte* pBytes = aurl_bytes)
                    {
                        RequestPeerConnect(host_handle, pBytes, aurl_bytes.Length);
                    }
                }
            }

            public void Disconnect(string hash)
            {
                unsafe
                {
                    [DllImport("abyssnet.dll")]
                    static extern void DisconnectPeer(IntPtr handle, byte* buf, int buflen);

                    var hash_bytes = Encoding.UTF8.GetBytes(hash);
                    fixed (byte* pBytes = hash_bytes)
                    {
                        DisconnectPeer(host_handle, pBytes, hash_bytes.Length);
                    }
                }
            }

            /////////
            // AND //
            /////////
            public AndEvent AndWaitEvent()
            {
                byte[] buffer = new byte[4096];
                unsafe
                {
                    [DllImport("abyssnet.dll")]
                    static extern int WaitANDEvent(IntPtr handle, byte* buf, int buflen);

                    fixed (byte* pBytes = buffer)
                    {
                        var len = WaitANDEvent(host_handle, pBytes, buffer.Length);
                        if (len < 9)
                        {
                            return new AndEvent(AndEventType.Error, 0, "", "", "", "");
                        }

                        return new AndEvent(
                            (AndEventType)pBytes[0],
                            pBytes[1],
                            pBytes[5] != 0 ? System.Text.Encoding.UTF8.GetString(pBytes + 9, pBytes[5]) : "",
                            pBytes[6] != 0 ? System.Text.Encoding.UTF8.GetString(pBytes + 9 + pBytes[5], pBytes[6]) : "",
                            pBytes[7] != 0 ? System.Text.Encoding.UTF8.GetString(pBytes + 9 + pBytes[5] + pBytes[6], pBytes[7]) : "",
                            pBytes[8] != 0 ? System.Text.Encoding.UTF8.GetString(pBytes + 9 + pBytes[5] + pBytes[6] + pBytes[7], pBytes[8]) : ""
                        );
                    }
                }
            }

            public int AndOpenWorld(string local_path, string url)
            {
                unsafe
                {
                    [DllImport("abyssnet.dll")]
                    static extern int OpenWorld(IntPtr handle, byte* path, int pathlen, byte* url, int urllen);

                    var path_bytes = Encoding.UTF8.GetBytes(local_path);
                    fixed (byte* pBytes = path_bytes)
                    {
                        var url_bytes = Encoding.UTF8.GetBytes(url);
                        fixed (byte* uBytes = url_bytes)
                        {
                            return OpenWorld(host_handle, pBytes, path_bytes.Length, uBytes, url_bytes.Length);
                        }
                    }
                }
            }

            public void AndCloseWorld(string local_path)
            {
                unsafe
                {
                    [DllImport("abyssnet.dll")]
                    static extern void CloseWorld(IntPtr handle, byte* path, int pathlen);

                    var path_bytes = Encoding.UTF8.GetBytes(local_path);
                    fixed (byte* pBytes = path_bytes)
                    {
                        CloseWorld(host_handle, pBytes, path_bytes.Length);
                    }
                }
            }

            public void AndJoin(string local_path, string aurl)
            {
                unsafe
                {
                    [DllImport("abyssnet.dll")]
                    static extern void Join(IntPtr handle, byte* path, int pathlen, byte* aurl, int aurllen);

                    var path_bytes = Encoding.UTF8.GetBytes(local_path);
                    fixed (byte* pBytes = path_bytes)
                    {
                        var aurl_bytes = Encoding.UTF8.GetBytes(aurl);
                        fixed (byte* aBytes = aurl_bytes)
                        {
                            Join(host_handle, pBytes, path_bytes.Length, aBytes, aurl_bytes.Length);
                        }
                    }
                }
            }

            /////////
            // SOM //
            /////////
            public AbyssDllError? SomRequestService(string peer_hash, string world_uuid)
            {
                unsafe
                {
                    [DllImport("abyssnet.dll")]
                    static extern IntPtr SOMRequestService(IntPtr host_handle, byte* peer_hash, int peer_hash_len, byte* world_uuid, int world_uuid_len);

                    var peer_hash_bytes = Encoding.UTF8.GetBytes(peer_hash);
                    var world_uuid_bytes = Encoding.UTF8.GetBytes(world_uuid);
                    fixed (byte* phb = peer_hash_bytes)
                    {
                        fixed (byte* wub = world_uuid_bytes)
                        {
                            var result = SOMRequestService(host_handle, phb, peer_hash_bytes.Length, wub, world_uuid_bytes.Length);
                            if (result != IntPtr.Zero)
                            {
                                return new AbyssDllError(result);
                            }
                            return null;
                        }
                    }
                }
            }
            public void SomInitiateService(string peer_hash, string world_uuid)
            {
                return; //this is moved inside abyss_net
                //unsafe
                //{
                //    [DllImport("abyssnet.dll")]
                //    static extern void SOMInitiateService(IntPtr host_handle, byte* peer_hash, int peer_hash_len, byte* world_uuid, int world_uuid_len);

                //    var peer_hash_bytes = Encoding.UTF8.GetBytes(peer_hash);
                //    var world_uuid_bytes = Encoding.UTF8.GetBytes(world_uuid);
                //    fixed (byte* phb = peer_hash_bytes)
                //    {
                //        fixed (byte* wub = world_uuid_bytes)
                //        {
                //            SOMInitiateService(host_handle, phb, peer_hash_bytes.Length, wub, world_uuid_bytes.Length);
                //        }
                //    }
                //}
            }
            public void SomTerminateService(string peer_hash, string world_uuid)
            {
                unsafe
                {
                    [DllImport("abyssnet.dll")]
                    static extern void SOMTerminateService(IntPtr host_handle, byte* peer_hash, int peer_hash_len, byte* world_uuid, int world_uuid_len);

                    var peer_hash_bytes = Encoding.UTF8.GetBytes(peer_hash);
                    var world_uuid_bytes = Encoding.UTF8.GetBytes(world_uuid);
                    fixed (byte* phb = peer_hash_bytes)
                    {
                        fixed (byte* wub = world_uuid_bytes)
                        {
                            SOMTerminateService(host_handle, phb, peer_hash_bytes.Length, wub, world_uuid_bytes.Length);
                        }
                    }
                }
            }
            public void SomRegisterObject(string url, string object_uuid, string initial_position)
            {
                unsafe
                {
                    [DllImport("abyssnet.dll")]
                    static extern void SOMRegisterObject(IntPtr host_handle, byte* url, int url_len, byte* object_uuid, int object_uuid_len, byte* initial_position, int initial_position_len);

                    var url_bytes = Encoding.UTF8.GetBytes(url);
                    var object_uuid_bytes = Encoding.UTF8.GetBytes(object_uuid);
                    var initial_pos_bytes = Encoding.UTF8.GetBytes(initial_position);
                    fixed (byte* urb = url_bytes)
                    {
                        fixed (byte* oub = object_uuid_bytes)
                        {
                            fixed (byte* inp = initial_pos_bytes)
                            {
                                SOMRegisterObject(host_handle, urb, url_bytes.Length, oub, object_uuid_bytes.Length, inp, initial_pos_bytes.Length);
                            }
                        }
                    }
                }
            }
            public AbyssDllError? SomShareObject(string peer_hash, string world_uuid, string object_uuid)
            {
                unsafe
                {
                    [DllImport("abyssnet.dll")]
                    static extern IntPtr SOMShareObject(IntPtr host_handle, byte* peer_hash, int peer_hash_len, byte* world_uuid, int world_uuid_len, byte* objects_uuid, int objects_uuid_len);

                    var peer_hash_bytes = Encoding.UTF8.GetBytes(peer_hash);
                    var world_uuid_bytes = Encoding.UTF8.GetBytes(world_uuid);
                    var object_uuid_bytes = Encoding.UTF8.GetBytes(object_uuid);
                    fixed (byte* phb = peer_hash_bytes)
                    {
                        fixed (byte* wub = world_uuid_bytes)
                        {
                            fixed (byte* oub = object_uuid_bytes)
                            {
                                var result = SOMShareObject(host_handle, phb, peer_hash_bytes.Length, wub, world_uuid_bytes.Length, oub, object_uuid_bytes.Length);
                                if (result != IntPtr.Zero)
                                {
                                    return new AbyssDllError(result);
                                }
                                return null;
                            }
                        }
                    }
                }
            }
            public SomEvent SomWaitEvent()
            {
                unsafe
                {
                    [DllImport("abyssnet.dll")]
                    static extern IntPtr SOMWaitEvent(IntPtr host_handle);

                    [DllImport("abyssnet.dll")]
                    static extern int SOMGetEventBodyLength(IntPtr event_handle);

                    [DllImport("abyssnet.dll")]
                    static extern int SOMGetEventBody(IntPtr host_handle, byte* buf, int buflen);

                    [DllImport("abyssnet.dll")]
                    static extern void SOMCloseEvent(IntPtr event_handle);

                    var event_handle = SOMWaitEvent(host_handle);

                    var body_len = SOMGetEventBodyLength(event_handle);
                    fixed (byte* buf = new byte[body_len])
                    {
                        if (SOMGetEventBody(event_handle, buf, body_len) != body_len)
                        {
                            throw new Exception("AbyssDLL: buffer write failure");
                        }
                        SOMCloseEvent(event_handle);

                        var offset = 0;
                        var type = (SomEventType)buf[offset];
                        offset++;

                        var peer_hash_len = buf[offset];
                        offset++;
                        var peer_hash = Encoding.UTF8.GetString(buf + offset, peer_hash_len);
                        offset += peer_hash_len;

                        var world_uuid_len = buf[offset];
                        offset++;
                        var world_uuid = Encoding.UTF8.GetString(buf + offset, world_uuid_len);
                        offset += world_uuid_len;

                        var object_count = buf[offset];
                        var objects_info = new Tuple<string, string, string>[object_count];
                        offset++;
                        switch (type)
                        {
                            case SomEventType.Invalid:
                                {
                                    break;
                                }
                            case SomEventType.SomReNew or SomEventType.SomAppend:
                                {
                                    for (int i = 0; i < object_count; i++)
                                    {
                                        var url_len = *(UInt16*)(buf + offset);
                                        offset += 2;
                                        var url = Encoding.UTF8.GetString(buf + offset, url_len);
                                        offset += url_len;

                                        var uuid_len = buf[offset];
                                        offset++;
                                        var uuid = Encoding.UTF8.GetString(buf + offset, uuid_len);
                                        offset += uuid_len;

                                        var init_pos_len = buf[offset];
                                        offset++;
                                        var init_pos = Encoding.UTF8.GetString(buf + offset, init_pos_len);
                                        offset += uuid_len;

                                        objects_info[i] = new Tuple<string, string, string>(url, uuid, init_pos);
                                    }
                                    break;
                                }
                            case SomEventType.SomDelete or SomEventType.SomDebug:
                                {
                                    for (int i = 0; i < object_count; i++)
                                    {
                                        var uuid_len = buf[offset];
                                        offset++;
                                        var uuid = Encoding.UTF8.GetString(buf + offset, uuid_len);
                                        offset += uuid_len;

                                        objects_info[i] = new Tuple<string, string, string>("", uuid, "");
                                    }
                                    break;
                                }
                            default:
                                throw new NotSupportedException();
                        }

                        return new SomEvent(type, peer_hash, world_uuid, objects_info);
                    }
                }
            }

            ////////////////////
            // HTTP/3 backend //
            ////////////////////
            public AbyssHttpResponse HttpGet(string aurl)
            {
                unsafe
                {
                    [DllImport("abyssnet.dll")]
                    static extern IntPtr HttpGet(IntPtr handle, byte* aurl, int aurl_len);

                    var url_bytes = Encoding.UTF8.GetBytes(aurl);
                    fixed (byte* pBytes = url_bytes)
                    {
                        return new AbyssHttpResponse(HttpGet(host_handle, pBytes, url_bytes.Length));
                    }
                }
            }

            readonly IntPtr host_handle;

            ////////////////////
            // CLI reflection //
            ////////////////////
            public void ParseAndInvoke(string input)
            {
                try
                {
                    // Split the input string by spaces
                    string[] parts = input.Split(' ');

                    // The first part is the method name
                    string methodName = parts[0];
                    if (methodName == "help")
                    {
                        var methods = this.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public);
                        foreach (var m in methods)
                        {
                            Console.WriteLine($"{m.Name} ({Strings.Join(m.GetParameters().Select(m => m.Name).ToArray(), ", ")})");
                        }
                        return;
                    }

                    // The remaining parts are the method arguments
                    string[] methodArgs = parts[1..];

                    // Find the method by name
                    MethodInfo? method = this.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public);

                    if (method != null)
                    {
                        // Convert string arguments to the appropriate types
                        ParameterInfo[] parameters = method.GetParameters();
                        object[] parsedArgs = new object[parameters.Length];

                        for (int i = 0; i < parameters.Length; i++)
                        {
                            parsedArgs[i] = Convert.ChangeType(methodArgs[i], parameters[i].ParameterType);
                        }

                        // Invoke the method with the parsed arguments
                        var retval = method.Invoke(this, parsedArgs);
                        Console.WriteLine(retval);
                    }
                    else
                    {
                        Console.WriteLine($"Method '{methodName}' not found.");
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Failed to call: {e.Message}");
                }
            }

            public readonly string LocalHash;
        }
    }
}
