using Microsoft.ClearScript.V8;
using System.Xml;

namespace AbyssCLI.Aml
{
    internal sealed class ScriptImpl : AmlNode
    {
        public ScriptImpl(AmlNode context, XmlNode xml_node, DocumentImpl document)
            : base(context)
        {
            _document = document;
            _script = xml_node.InnerText;
        }
        protected override Task ActivateSelfCallback(CancellationToken token)
        {
            _engine = new(
                new V8RuntimeConstraints()
                {
                    MaxOldSpaceSize = 32 * 1024 * 1024
                }
            );
            _engine.AddHostObject("document", new Document(_document));
            _engine.AddHostObject("console", new JSConsole(ErrorStream));
            token.ThrowIfCancellationRequested();

            _engine.Execute(_script);
            return Task.CompletedTask;
        }
        protected override void DeceaseSelfCallback()
        {
            _engine.Interrupt();
        }
        protected override void CleanupSelfCallback()
        {
            _engine.Dispose();
        }
        //TODO: find out a safer way of killing V8 after decease.
        public static string Tag => "script";

        private readonly DocumentImpl _document;
        private V8ScriptEngine _engine;
        private readonly string _script;
    }
}