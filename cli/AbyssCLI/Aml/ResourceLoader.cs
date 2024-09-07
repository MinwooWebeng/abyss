using AbyssCLI.ABI;
using AbyssCLI.Tool;
using System.IO.MemoryMappedFiles;
using System.Text;

namespace AbyssCLI.Aml
{
    //each content must have one MediaLoader
    //TODO: add CORS protection before adding cookie.
    internal class ResourceLoader(
        AbyssLib.AbyssHost host,
        StreamWriter cerr,
        AbyssAddress origin)
    {
        public class FileResource
        {
            public MemoryMappedFile MMF = null; //TODO: remove componenet from renderer and close this.
            public ABI.File ABIFileInfo = null;
            public bool IsValid = false; //must be only set from ResourceLoader
        }
        public bool TryGetFileOrWaiter(string URL, MIME MimeType, out FileResource resource, out Waiter<FileResource> waiter)
        {
            if (!_origin.TryParseMaybeRelativeAddress(URL, out var Source))
                throw new Exception("invalid URL");

            WaiterGroup<FileResource> waiting_group;
            lock (_media_cache)
            {
                if (!_media_cache.TryGetValue(Source.String, out waiting_group))
                {
                    waiting_group = new();
                    _ = Loadresource(Source, MimeType, waiting_group); //do not wait.
                    _media_cache.Add(Source.String, waiting_group);
                }
            }

            return waiting_group.TryGetValueOrWaiter(out resource, out waiter);
        }
        public async Task<byte[]> GetHttpFileAsync(Uri url)
        {
            var response = await _http_client.GetAsync(url);
            return response.IsSuccessStatusCode ? await response.Content.ReadAsByteArrayAsync() : null;
        }
        public async Task<byte[]> GetAbystFileAsync(string url)
        {
            return await Task.Run(() =>
            {
                var response = _host.HttpGet(url);
                return response.GetStatus() == 200 ? response.GetBody() : throw new Exception(url + " : " + Encoding.UTF8.GetString(response.GetBody()));
            });
        }

        private async Task Loadresource(AbyssAddress Source, MIME MimeType, WaiterGroup<FileResource> dest)
        {
            byte[] fileBytes;
            try
            {
                fileBytes = Source.Scheme switch
                {
                    AbyssAddress.EScheme.WWW => await GetHttpFileAsync(Source.WebAddress),
                    AbyssAddress.EScheme.Abyst => await GetAbystFileAsync(Source.String),
                    _ => throw new Exception("invalid address scheme"),
                };
            }
            catch (Exception e)
            {
                //TODO: log error.
                _cerr.WriteLine("invalid address for resource: " + Source.String);
                _cerr.WriteLine(e.Message);
                _cerr.WriteLine(e.StackTrace);
                dest.TryFinalizeValue(new FileResource());
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
            var abi_fileinfo = new ABI.File()
            {
                Mime = MimeType,
                MmapName = mmf_path,
                Off = 0,
                Len = (uint)fileBytes.Length,
            };

            dest.TryFinalizeValue(new FileResource
            {
                IsValid = true,
                MMF = mmf,
                ABIFileInfo = abi_fileinfo,
            });
        }

        private readonly AbyssLib.AbyssHost _host = host;
        private readonly StreamWriter _cerr = cerr;
        private readonly AbyssAddress _origin = origin;
        private readonly string _mmf_path_prefix = "abyst" + RanStr.RandomString(10);
        private readonly HttpClient _http_client = new();
        private readonly Dictionary<string, WaiterGroup<FileResource>> _media_cache = []; //registered when resource is requested.
    }
}
