using Microsoft.ClearScript.V8;
using System.Xml;

#pragma warning disable IDE1006 //naming convention
namespace AbyssCLI.Aml
{
    internal class ScriptImpl : DocumentContext, Script
    {
        public ScriptImpl(DocumentContext context, XmlNode xml_node, Document document)
            : base(context)
        {
            id = xml_node.Attributes["id"]?.Value;
            _document = document;
            _script = xml_node.InnerText;
        }
        public ScriptImpl(DocumentContext context, ScriptImpl parsed_script, Document document)
            : base(context)
        {
            id = parsed_script.id;
            _document = document;
            _script = parsed_script._script;
        }
        protected override Task ActivateCallback(CancellationToken token)
        {
            _engine = new(
                new V8RuntimeConstraints()
                {
                    MaxOldSpaceSize = 32 * 1024 * 1024
                }
            );
            _engine.AddHostObject("document", _document);
            _engine.AddHostObject("console", new JSConsole(ErrorStream));
            token.ThrowIfCancellationRequested();

            _engine.Execute(_script);
            return Task.CompletedTask;
        }
        protected override void DeceaseCallback()
        {
            _engine.Interrupt();
        }
        protected override Task CleanupAsyncCallback()
        {
            _engine.Dispose();
            return Task.CompletedTask;
        }
        //TODO: find out a safer way of killing V8 after decease.

        public string tag => "head";
        public string id { get; }

        private readonly Document _document;
        private V8ScriptEngine _engine;
        private readonly string _script;
    }
}
#pragma warning restore IDE1006