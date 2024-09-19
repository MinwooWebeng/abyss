using AbyssCLI.ABI;
using AbyssCLI.Tool;
using System.Text;
using System.Xml;

namespace AbyssCLI.Aml
{
    internal sealed class DocumentImpl(Contexted root,
        RenderActionWriter renderActionWriter, StreamWriter cerr,
        ResourceLoader resourceLoader, AbyssAddress URL, vec3 body_position)
        : AmlNode(root, renderActionWriter, cerr, resourceLoader)
    {
        protected override async Task ActivateSelfCallback(CancellationToken token)
        {
            byte[] aml_file = URL.Scheme switch
            {
                AbyssAddress.EScheme.WWW => await ResourceLoader.GetHttpFileAsync(URL.WebAddress),
                AbyssAddress.EScheme.Abyst => await ResourceLoader.GetAbystFileAsync(URL.String),
                _ => throw new Exception("invalid address scheme"),
            };
            token.ThrowIfCancellationRequested();
            if (aml_file == null)
                throw new Exception("failed to load aml");

            //AML parsing
            XmlDocument doc = new();
            doc.LoadXml(Encoding.UTF8.GetString(aml_file));
            // Check for the DOCTYPE
            if (doc.DocumentType == null || doc.DocumentType.Name != "AML")
                throw new Exception("DOCTYPE mismatch");

            XmlNode aml_node = doc.SelectSingleNode("/aml");
            if (aml_node == null || aml_node.ParentNode != doc)
                throw new Exception("<aml> not found");

            XmlNode head_node = aml_node.SelectSingleNode("head");
            Children.Add(new HeadImpl(this, head_node, this));

            var body_node = aml_node.SelectSingleNode("body");
            Children.Add(new BodyImpl(this, body_node, body_position));
        }
        private readonly AbyssAddress URL = URL;

        //valid only after Activation
        public HeadImpl Head => Children[0] as HeadImpl;
        public BodyImpl Body => Children[1] as BodyImpl;
    }
}
