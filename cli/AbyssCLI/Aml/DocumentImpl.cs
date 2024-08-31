using AbyssCLI.ABI;
using AbyssCLI.Content;
using System.Xml;

namespace AbyssCLI.Aml
{
    internal class DocumentImpl(Contexted root, RenderActionWriter renderActionWriter, StreamWriter cerr, ResourceLoader resourceLoader, string raw_document) 
        : DocumentContext(root, renderActionWriter, cerr, resourceLoader), Document
    {
        protected override Task ActivateCallback(CancellationToken token)
        {
            XmlDocument doc = new();
            doc.LoadXml(raw_document);

            // Check for the DOCTYPE
            if (doc.DocumentType == null || doc.DocumentType.Name != "AML")
                throw new Exception("DOCTYPE mismatch");

            XmlNode aml_node = doc.SelectSingleNode("/aml");
            if (aml_node == null || aml_node.ParentNode != doc)
                throw new Exception("<aml> not found");

            XmlNode head_node = aml_node.SelectSingleNode("head");
            if (head_node != null)
            {
                _head = new(this, head_node, this);
            }
            else
            {
                _head = new(this, this);
            }
            _head.Activate();


            var body_node = aml_node.SelectSingleNode("body");
            if (body_node != null)
            {
                _body = new(this);
            }
            else
            {
                _body = new(this, body_node);
            }
            _body.Activate();

            return Task.CompletedTask;
        }
        protected override void DeceaseCallback()
        {
            _head.Close();
            _body.Close();
        }
        protected override async Task CleanupAsyncCallback()
        {
            //no cleanup required for document node
            //await for head and body cleanup
            await _head.CloseAsync();
            await _body.CloseAsync();
        }

        private readonly string raw_document = raw_document;
        public Head head { get { return _head; } }
        private HeadImpl _head;
        public Body body { get { return _body; } }
        private BodyImpl _body;
    }
}
