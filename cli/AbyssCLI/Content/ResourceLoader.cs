using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace AbyssCLI.Content
{
    //each content must have one MediaLoader
    //TODO: add CORS protection before adding cookie.
    internal class ResourceLoader(AbyssCLI.AbyssLib.AbyssHost host)
    {
        public async Task<byte[]> GetHttpFileAsync(string url)
        {
            var response = await httpClient.GetAsync(url);
            return response.IsSuccessStatusCode ? await response.Content.ReadAsByteArrayAsync() : null;
        }
        public async Task<byte[]> GetAbystFileAsync(string url)
        {
            return await Task.Run(() => {
                var response = host.HttpGet(url);
                return response.GetStatus() == 200 ? response.GetBody() : throw new Exception(Encoding.UTF8.GetString(response.GetBody()));
            });
        }
        public async Task<byte[]> GetLocalFileAsync(string url)
        {
            return await GetAbystFileAsync("abyst:" + host.LocalHash + "/" + url.Trim().TrimStart('/'));
        }
        //public async Task<Stream> GetStreamAsync(string url)
        //{
        //    throw new NotImplementedException();
        //}
        private readonly AbyssLib.AbyssHost host = host;
        private readonly HttpClient httpClient = new();
        private readonly ConcurrentDictionary<string, int> media_cache; //registered when resource is requested.
    }
}
