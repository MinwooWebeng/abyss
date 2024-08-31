using AbyssCLI.ABI;
using AbyssCLI.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbyssCLI.Aml
{
    internal class DocumentContext : Contexted
    {
        protected DocumentContext(Contexted root, RenderActionWriter renderActionWriter, StreamWriter cerr, ResourceLoader resourceLoader)
            : base(root)
        {
            RenderActionWriter = renderActionWriter;
            ErrorStream = cerr;
            ResourceLoader = resourceLoader;
        }
        protected DocumentContext(DocumentContext base_context)
            : base(base_context)
        {
            RenderActionWriter = base_context.RenderActionWriter;
            ErrorStream = base_context.ErrorStream;
            ResourceLoader = base_context.ResourceLoader;
        }
        protected sealed override void ErrorCallback(Exception e) =>
            ErrorStream.WriteLine(e.Message + ": " + e.StackTrace);
        //TODO: 
        public readonly ResourceLoader ResourceLoader;
        public readonly StreamWriter ErrorStream;
        public readonly RenderActionWriter RenderActionWriter;
    }
}
