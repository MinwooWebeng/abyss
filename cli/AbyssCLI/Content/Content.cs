using AbyssCLI.ABI;
using AbyssCLI.Aml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AbyssCLI.Content
{
    internal class Content(RenderActionWriter renderActionWriter, StreamWriter cerr, ResourceLoader resourceLoader, string url)
        :Contexted()
    {
        protected override async Task ActivateCallback(CancellationToken token)
        {
            byte[] aml_file;
            if (URL.StartsWith("http"))
            {   //web content
                aml_file = await _resourceLoader.GetHttpFileAsync(URL);
            }
            else if (URL.StartsWith("abyst"))
            {   //peer content
                aml_file = await _resourceLoader.GetAbystFileAsync(URL);
            }
            else
            {   //local content-not allowed
                throw new Exception("local address not allowed as world URL");
            }

            if (aml_file == null)
            {
                throw new Exception("failed to load aml");
            }
            var aml_text = Encoding.UTF8.GetString(aml_file);
            token.ThrowIfCancellationRequested();

            //TODO: load
            _document = new DocumentImpl(this, _renderActionWriter, _cerr, _resourceLoader, aml_text);
            _ = _document.Activate();
        }
        protected override void ErrorCallback(Exception e)
        {
            if (e is not TaskCanceledException and not OperationCanceledException)
                _cerr.WriteLine(e.Message + ": " + e.StackTrace);
        }
        protected override void DeceaseCallback()
        {
            _document.Close();
        }
        //no cleanup required.

        public readonly string URL = url;

        private readonly RenderActionWriter _renderActionWriter = renderActionWriter;
        private readonly StreamWriter _cerr = cerr;
        private readonly ResourceLoader _resourceLoader = resourceLoader;
        private DocumentImpl _document;
    }
}