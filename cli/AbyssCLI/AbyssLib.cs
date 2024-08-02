using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace AbyssCLI
{
    static public class AbyssLib
    {
        static public string GetVersion()
        {
            unsafe
            {
                [DllImport("../../../abyssnet.dll")]
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
        public enum ANDEventType
        {
            Error = -1,
            JoinDenied,
            JoinExpired,
            JoinSuccess,
            PeerJoin,
            PeerLeave,
        }
        public class ANDEvent
        {
            public ANDEvent(ANDEventType Type, int Status, string Message, string LocalPath, string PeerHash, string WorldJson)
            {
                this.Type = Type;
                this.Status = Status;
                this.Message = Message;
                this.LocalPath = LocalPath;
                this.PeerHash = PeerHash;
                this.WorldJson = WorldJson;
            }
            public ANDEventType Type { get; }
            public int Status { get; }
            public string Message { get; }
            public string LocalPath { get; }
            public string PeerHash { get; }
            public string WorldJson { get; }
        }
        public class AbyssHost
        {
            public AbyssHost(string hash) {
                unsafe
                {
                    [DllImport("../../../abyssnet.dll")]
                    static extern IntPtr NewAbyssHost(byte* buf, int buflen);

                    var hash_bytes = Encoding.UTF8.GetBytes(hash);
                    fixed (byte* pBytes = hash_bytes)
                    {
                        host_handle = NewAbyssHost(pBytes, hash_bytes.Length);
                    }
                }
            }
            public void Close()
            {
                unsafe
                {
                    [DllImport("../../../abyssnet.dll")]
                    static extern void CloseAbyssHost(IntPtr handle);

                    CloseAbyssHost(host_handle);
                }
            }
            public string LocalAddr()
            {
                unsafe
                {
                    [DllImport("../../../abyssnet.dll")]
                    static extern int LocalAddr(IntPtr handle, byte* buf, int buflen);

                    fixed (byte* pBytes = new byte[1024])
                    {
                        int len = LocalAddr(host_handle, pBytes, 1024);
                        if (len < 0)
                        {
                            return "";
                        }
                        return System.Text.Encoding.UTF8.GetString(pBytes, len);
                    }
                }
            }
            public void RequestConnect(string aurl)
            {
                unsafe
                {
                    [DllImport("../../../abyssnet.dll")]
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
                    [DllImport("../../../abyssnet.dll")]
                    static extern void DisconnectPeer(IntPtr handle, byte* buf, int buflen);

                    var hash_bytes = Encoding.UTF8.GetBytes(hash);
                    fixed (byte* pBytes = hash_bytes)
                    {
                        DisconnectPeer(host_handle, pBytes, hash_bytes.Length);
                    }
                }
            }

            readonly byte[] buffer = new byte[4096];
            public ANDEvent WaitANDEvent()
            {
                unsafe
                {
                    [DllImport("../../../abyssnet.dll")]
                    static extern int WaitANDEvent(IntPtr handle, byte* buf, int buflen);

                    fixed (byte* pBytes = buffer)
                    {
                        var len = WaitANDEvent(host_handle, pBytes, buffer.Length);
                        if (len < 9) {
                            return new ANDEvent(ANDEventType.Error, 0, "", "", "", "");
                        }

                        return new ANDEvent(
                            (ANDEventType)pBytes[0],
                            pBytes[1],
                            pBytes[5] != 0 ? System.Text.Encoding.UTF8.GetString(pBytes + 9, pBytes[5]) : "",
                            pBytes[6] != 0 ? System.Text.Encoding.UTF8.GetString(pBytes + 9 + pBytes[5], pBytes[6]) : "",
                            pBytes[7] != 0 ? System.Text.Encoding.UTF8.GetString(pBytes + 9 + pBytes[5] + pBytes[6], pBytes[7]) : "",
                            pBytes[8] != 0 ? System.Text.Encoding.UTF8.GetString(pBytes + 9 + pBytes[5] + pBytes[6] + pBytes[7], pBytes[8]) : ""
                        );
                    }
                }
            }

            public int OpenWorld(string local_path, string url)
            {
                unsafe
                {
                    [DllImport("../../../abyssnet.dll")]
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

            public void CloseWorld(string local_path)
            {
                unsafe
                {
                    [DllImport("../../../abyssnet.dll")]
                    static extern void CloseWorld(IntPtr handle, byte* path, int pathlen);

                    var path_bytes = Encoding.UTF8.GetBytes(local_path);
                    fixed (byte* pBytes = path_bytes)
                    {
                        CloseWorld(host_handle, pBytes, path_bytes.Length);
                    }
                }
            }

            public void Join(string local_path, string aurl)
            {
                unsafe
                {
                    [DllImport("../../../abyssnet.dll")]
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

            readonly IntPtr host_handle;
        }
    }
}
