using AbyssCLI.ABI;
using System.Collections.Concurrent;
using System.IO.MemoryMappedFiles;
using System.Reflection.Metadata.Ecma335;
using System.Text;

namespace AbyssCLI.Content
{
    //each content must have one MediaLoader
    //TODO: add CORS protection before adding cookie.
    internal class ResourceLoader(AbyssCLI.AbyssLib.AbyssHost host, string abyst_prefix, string mmf_prefix, RenderActionWriter renderActionWriter)
    {
        public class FileWaiter
        {
            public int component_id;
            public Semaphore semaphore = new(0, 1);
            public int state = 0; //0: init, 1: loading, 2: loaded (no need to check sema)
            public MemoryMappedFile mmf; //TODO: remove componenet from renderer and close this.
        }
        public async Task<int> GetFileAsync(string Source, MIME MimeType)
        {
            var file_waiter = _media_cache.GetOrAdd(Source, (string _) => new FileWaiter());
            if (Interlocked.CompareExchange(ref file_waiter.state, 1, 0) == 0)
            {
                AbyssCLI.ABI.File file;
                try
                {
                    file_waiter.component_id = RenderID.ComponentId;
                    byte[] fileBytes;
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

                    var mmf_path = _mmf_path_prefix + file_waiter.component_id.ToString();
                    file_waiter.mmf = MemoryMappedFile.CreateNew(mmf_path, fileBytes.Length);
                    var accessor = file_waiter.mmf.CreateViewAccessor();
                    accessor.WriteArray(0, fileBytes, 0, fileBytes.Length);
                    accessor.Flush();
                    accessor.Dispose();
                    file = new AbyssCLI.ABI.File()
                    {
                        Mime = MimeType,
                        MmapName = mmf_path,
                        Off = 0,
                        Len = (uint)fileBytes.Length,
                    };
                }
                catch
                {
                    file_waiter.component_id = -1; //invaild
                    throw;
                }
                finally
                {
                    file_waiter.state = 2;
                    file_waiter.semaphore.Release();
                }
                switch (MimeType)
                {
                    case MIME.ModelObj:
                        _render_action_writer.CreateStaticMesh(file_waiter.component_id, file);
                        break;
                    case MIME.ImagePng or MIME.ImageJpeg:
                        _render_action_writer.CreateImage(file_waiter.component_id, file);
                        break;
                    default:
                        throw new Exception("unsupported MIME:" +  MimeType.ToString());
                };
                return file_waiter.component_id;
            }
            else if(file_waiter.state == 1)
            {
                file_waiter.semaphore.WaitOne();
                file_waiter.semaphore.Release();
                return file_waiter.component_id;
            }
            return file_waiter.component_id;
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

        private readonly AbyssLib.AbyssHost _host = host;
        private readonly string _abyst_prefix = abyst_prefix;
        private readonly string _mmf_path_prefix = mmf_prefix;
        private readonly RenderActionWriter _render_action_writer = renderActionWriter;
        private readonly HttpClient _http_client = new();
        private readonly ConcurrentDictionary<string, FileWaiter> _media_cache = new(); //registered when resource is requested.
    }
}
