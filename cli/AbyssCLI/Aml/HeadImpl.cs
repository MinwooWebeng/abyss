using System.Net.Http.Headers;
using System.Xml;

namespace AbyssCLI.Aml
{
    internal class HeadImpl : DocumentContext, Head
    {
        public HeadImpl(DocumentContext context, Document document)
            : base(context)
        {
            _document = document;
            _children = [];
        }
        public HeadImpl(DocumentContext context, XmlNode head_node, Document document)
            : base(context)
        {
            id = head_node.Attributes["id"]?.Value;

            _children = [];
            foreach (XmlNode child in head_node.ChildNodes)
            {
                switch (child.Name)
                {
                    case "script":
                        _children.Add(new ScriptImpl(this, child, document));
                        break;
                    default:
                        throw new Exception("Invalid tag in <head>");
                }
            }
        }
        protected override Task ActivateCallback(CancellationToken token)
        {
            lock (_children)
            {
                foreach (Selector child in _children)
                {
                    if (child is ScriptImpl)
                    {
                        var script = child as ScriptImpl;
                        script.Activate();
                    }
                }
            }
            return Task.CompletedTask;
        }

        public void appendChild(Selector child)
        {
            if (child is ScriptImpl)
            {
                lock (_children)
                {
                    var script = new ScriptImpl(this, child as ScriptImpl, _document);
                    _children.Add(script);
                    script.Activate();
                }
            }
            else
            {
                ErrorStream.WriteLine("failed to add child to <head>: tag not allowed");
            }
        }
        public string tag => "head";
        public string id { get; }

        private readonly Document _document;
        private readonly List<Selector> _children = [];
    }
}
