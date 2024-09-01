using AbyssCLI.ABI;
using AbyssCLI.Content;
using System.Xml;

namespace AbyssCLI.Aml
{
    internal sealed class DocumentImpl : AmlNode
    {
        public DocumentImpl(Contexted root, RenderActionWriter renderActionWriter, StreamWriter cerr, ResourceLoader resourceLoader, string raw_document) 
            : base(root, renderActionWriter, cerr, resourceLoader)
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
            Children.Add(new HeadImpl(this, head_node, this));

            var body_node = aml_node.SelectSingleNode("body");
            Children.Add(new BodyImpl(this, body_node));
        }
        
        public HeadImpl Head => Children[0] as HeadImpl;
        public BodyImpl Body => Children[1] as BodyImpl;
    }
}
