using AbyssCLI.ABI;
using AbyssCLI.Tool;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO.MemoryMappedFiles;
using System.Reflection.Metadata.Ecma335;
using System.Text;

namespace AbyssCLI.Content
{
    //each content must have one MediaLoader
    //TODO: add CORS protection before adding cookie.
    internal class ResourceLoader(
        AbyssCLI.AbyssLib.AbyssHost host, 
        string abyst_prefix, string mmf_prefix, 
        RenderActionWriter renderActionWriter)
    {
        public class FileResource
        {
            public int ComponentId = -1; //primary id
            public MemoryMappedFile MMF = null; //TODO: remove componenet from renderer and close this.
            public AbyssCLI.ABI.File ABIFileInfo = null;
        }
        public bool TryGetFileOrWaiter(string Source, MIME MimeType, out FileResource resource, out Waiter<FileResource> waiter)
        {
            WaiterGroup<FileResource> waiting_group;
            lock(_media_cache)
            {
                if (!_media_cache.TryGetValue(Source, out waiting_group)) {
                    waiting_group = new();
                    _ = Loadresource(Source, MimeType, waiting_group); //do not wait.
                    _media_cache.Add(Source, waiting_group);
                }
            }
            
            return waiting_group.TryGetValueOrWaiter(out resource, out waiter);
        }
        public async Task<byte[]> GetHttpFileAsync(string url)
        {
            var response = await _http_client.GetAsync(url);
            return response.IsSuccessStatusCode ? await response.Content.ReadAsByteArrayAsync() : null;
        }
        public async Task<byte[]> GetAbystFileAsync(string url)
        {
            return await Task.Run(() => {
                var response = _host.HttpGet(url);
                return response.GetStatus() == 200 ? response.GetBody() : throw new Exception(url + " : " + Encoding.UTF8.GetString(response.GetBody()));
            });
        }
        public async Task<byte[]> GetLocalFileAsync(string url)
        {
            return await GetAbystFileAsync(_abyst_prefix + url.Trim().TrimStart('/'));
        }

        private async Task Loadresource(string Source, MIME MimeType, WaiterGroup<FileResource> dest)
        {
            byte[] fileBytes;
            try
            {
                if (Source.StartsWith("http"))
                {
                    fileBytes = await GetHttpFileAsync(Source);
                }
                else if (Source.StartsWith("abyst"))
                {
                    fileBytes = await GetAbystFileAsync(Source); //this may should be blocked.
                }
                else
                {
                    fileBytes = await GetLocalFileAsync(Source);
                }
            }
            catch
            {
                //TODO: log error.
                dest.FinalizeValue(new FileResource()
                {
                    ComponentId = -1
                });
                return;
            }

            if (dest.IsFinalized)
                return;

            //should never throw from here.
            var component_id = RenderID.ComponentId;
            var mmf_path = _mmf_path_prefix + component_id.ToString();
            var mmf = MemoryMappedFile.CreateNew(mmf_path, fileBytes.Length);
            var accessor = mmf.CreateViewAccessor();
            accessor.WriteArray(0, fileBytes, 0, fileBytes.Length);
            accessor.Flush();
            accessor.Dispose();
            var abi_fileinfo = new AbyssCLI.ABI.File()
            {
                Mime = MimeType,
                MmapName = mmf_path,
                Off = 0,
                Len = (uint)fileBytes.Length,
            };

            switch (MimeType)
            {
                case MIME.ModelObj:
                    _render_action_writer.CreateStaticMesh(component_id, abi_fileinfo);
                    break;
                case MIME.ImagePng or MIME.ImageJpeg:
                    _render_action_writer.CreateImage(component_id, abi_fileinfo);
                    break;
                default:
                    //TODO: log error.
                    mmf.Dispose();
                    dest.FinalizeValue(new FileResource()
                    {
                        ComponentId = -1
                    });
                    return;
            };

            dest.FinalizeValue(new FileResource
            {
                ComponentId = component_id,
                MMF = mmf,
                ABIFileInfo = abi_fileinfo,
            });
        }

        private readonly AbyssLib.AbyssHost _host = host;
        private readonly string _abyst_prefix = abyst_prefix;
        private readonly string _mmf_path_prefix = mmf_prefix;
        private readonly RenderActionWriter _render_action_writer = renderActionWriter;
        private readonly HttpClient _http_client = new();
        private readonly Dictionary<string, WaiterGroup<FileResource>> _media_cache = []; //registered when resource is requested.
    }
}
