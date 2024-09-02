using AbyssCLI.Tool;
using System.IO.MemoryMappedFiles;
using System.Xml;

namespace AbyssCLI.Aml
{
    internal class MeshImpl : AmlNode
    {
        internal MeshImpl(AmlNode parent_node, int render_parent, XmlNode xml_node)
            :base(parent_node)
        {
            Id = xml_node.Attributes["id"]?.Value;
            if (Id != null)
            {
                ElementDictionary[Id] = this;
            }
            Source = xml_node.Attributes["src"]?.Value;
            if (Source == null) { throw new Exception(
                "src attribute is null in <mesh" + (Id == null ? "" : (":" + Id)) + ">"); }
            MimeType = xml_node.Attributes["type"]?.Value;
            if (MimeType == null) { throw new Exception(
                "type attribute is null in <mesh" + (Id == null ? "" : (":" + Id)) + ">"); }

            MeshWaiter = new();
            _render_parent = render_parent;
            foreach (XmlNode child in xml_node?.ChildNodes)
            {
                Children.Add(child.Name switch
                {
                    "material" => new MaterialImpl(this, child),
                    _ => throw new Exception("Invalid tag in <mesh" + (Id == null ? "" : (":" + Id)) + ">"),
                });
            }
        }
        protected override async Task ActivateSelfCallback(CancellationToken token)
        {
            switch (MimeType)
            {
                case "model/obj":
                    var component_id = await ResourceLoader.GetFileAsync(Source, MIME.ModelObj);
                    RenderActionWriter.ElemAttachStaticMesh(_render_parent, component_id);
                    MeshWaiter.SetValue(component_id);
                    return;
                default:
                    MeshWaiter.SetValue(-1);
                    throw new Exception("unsupported type in <mesh" + (Id == null ? "" : (":" + Id)) + ">");
            }
        }
        protected override void DeceaseSelfCallback()
        {
            if(MeshWaiter.IsFirstAccess())
            {
                MeshWaiter.SetValue(-1);
            }
        }
        public static string Tag => "mesh";
        public string Id { get; }
        public string Source { get; }
        public string MimeType { get; }
        public Waiter<int> MeshWaiter;

        private readonly int _render_parent;
    }
}
