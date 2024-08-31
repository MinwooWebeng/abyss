using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace AbyssCLI.Aml
{
    internal class BodyImpl : DocumentContext, Body
    {
        //TODO: root element.
        public BodyImpl(DocumentContext context)
            : base(context)
        {}
        public BodyImpl(DocumentContext context, XmlNode xml_node)
            : base(context)
        {
            id = xml_node.Attributes["id"]?.Value;
            //TODO: add children
        }
        public void appendChild(Selector child)
        {
            throw new NotImplementedException();
        }
        public string tag => "body";
        public string id { get; }

        private List<Selector> _children = [];

    }
}
