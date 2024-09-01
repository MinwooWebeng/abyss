using System.Net.Http.Headers;
using System.Xml;

namespace AbyssCLI.Aml
{
    internal sealed class HeadImpl : AmlNode
    {
        public HeadImpl(AmlNode context, XmlNode head_node, DocumentImpl document)
            : base(context)
        {
            Id = head_node.Attributes["id"]?.Value;

            foreach (XmlNode child in head_node?.ChildNodes)
            {
                switch (child.Name)
                {
                    case "script":
                        Children.Add(new ScriptImpl(this, child, document));
                        break;
                    default:
                        throw new Exception("Invalid tag in <head>");
                }
            }
        }
        public static string Tag => "head";
        public string Id { get; }

        //private readonly DocumentImpl _document; //use when dynamically loading scripts.
    }
}
