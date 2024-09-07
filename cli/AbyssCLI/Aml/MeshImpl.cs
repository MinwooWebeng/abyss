using AbyssCLI.Tool;
using System.Xml;

namespace AbyssCLI.Aml
{
    internal class MeshImpl : AmlNode
    {
        internal MeshImpl(AmlNode parent_node, int render_parent, XmlNode xml_node)
            : base(parent_node)
        {
            Id = xml_node.Attributes["id"]?.Value;
            if (Id != null)
            {
                ElementDictionary[Id] = this;
            }
            Source = xml_node.Attributes["src"]?.Value;
            if (Source == null)
            {
                throw new Exception(
                "src attribute is null in <mesh" + (Id == null ? "" : (":" + Id)) + ">");
            }
            MimeType = xml_node.Attributes["type"]?.Value;
            if (MimeType == null)
            {
                throw new Exception(
                "type attribute is null in <mesh" + (Id == null ? "" : (":" + Id)) + ">");
            }

            MeshWaiterGroup = new();
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
        protected override Task ActivateSelfCallback(CancellationToken token)
        {
            switch (MimeType)
            {
                case "model/obj":
                    if (!ResourceLoader.TryGetFileOrWaiter(Source, MIME.ModelObj, out var resource, out _resource_waiter))
                    {
                        //resource not ready - wait for value;
                        resource = _resource_waiter.GetValue();
                    }
                    token.ThrowIfCancellationRequested();
                    if (!resource.IsValid)
                        throw new Exception("failed to load " + Source + " in <mesh" + (Id == null ? "" : (":" + Id)) + ">");

                    var component_id = RenderID.ComponentId;
                    RenderActionWriter.CreateStaticMesh(component_id, resource.ABIFileInfo);
                    if (!MeshWaiterGroup.TryFinalizeValue(component_id))
                    { //decease called
                        RenderActionWriter.DeleteStaticMesh(component_id);
                        return Task.CompletedTask;
                    }
                    RenderActionWriter.ElemAttachStaticMesh(_render_parent, component_id);
                    return Task.CompletedTask;
                default:
                    throw new Exception("unsupported type in <mesh" + (Id == null ? "" : (":" + Id)) + ">");
            }
        }
        protected override void DeceaseSelfCallback()
        {
            _resource_waiter?.CancelWithValue(new ResourceLoader.FileResource());
            MeshWaiterGroup.TryFinalizeValue(0);
        }
        protected override void CleanupSelfCallback()
        {
            var component_id = MeshWaiterGroup.GetValue();
            if (component_id != 0)
                RenderActionWriter.DeleteStaticMesh(component_id);
        }
        public static string Tag => "mesh";
        public string Id { get; }
        public string Source { get; }
        public string MimeType { get; }
        public WaiterGroup<int> MeshWaiterGroup;

        private readonly int _render_parent;
        private Waiter<ResourceLoader.FileResource> _resource_waiter;
    }
}
