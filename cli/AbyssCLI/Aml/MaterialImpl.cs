using AbyssCLI.Tool;
using System.Xml;

namespace AbyssCLI.Aml
{
    internal class MaterialImpl : AmlNode
    {
        public MaterialImpl(MeshImpl parent_node, XmlNode xml_node)
            : base(parent_node)
        {
            Id = xml_node.Attributes["id"]?.Value;
            if (Id != null)
            {
                ElementDictionary[Id] = this;
            }
            if (int.TryParse(xml_node.Attributes["pos"]?.Value, out var pos))
                Pos = pos;
            Shader = xml_node.Attributes["shader"]?.Value;
            if (Shader == null) { throw new Exception("shader attribute is null in <Material" + (Id == null ? "" : (":" + Id)) + ">"); }

            MaterialWaiter = new();
            _parent_node = parent_node;
            foreach (XmlNode child in xml_node.ChildNodes)
            {
                Children.Add(child.Name switch
                {
                    "img" => new ImageImpl(this, child),
                    _ => throw new Exception("Invalid tag in <material" + (Id == null ? "" : (":" + Id)) + ">"),
                });
            }
        }
        protected override Task ActivateSelfCallback(CancellationToken token)
        {
            var component_id = Content.RenderID.ComponentId;
            RenderActionWriter.CreateMaterialV(component_id, Shader);
            MaterialWaiter.SetValue(component_id);

            var mesh_id = _parent_node.MeshWaiter.GetValue();
            token.ThrowIfCancellationRequested();

            RenderActionWriter.StaticMeshSetMaterial(mesh_id, Pos, component_id);
            return Task.CompletedTask;
        }
        protected override void DeceaseSelfCallback()
        {
            if (MaterialWaiter.IsFirstAccess())
            {
                MaterialWaiter.SetValue(-1);
            }
        }
        protected override void CleanupSelfCallback()
        {
            var material_comp = MaterialWaiter.GetValue();
            if (material_comp != -1)
            {
                RenderActionWriter.DeleteMaterial(material_comp);
            }
        }
        public static string Tag => "material";
        public string Id { get; }
        public int Pos { get; }
        public string Shader { get; }
        //TODO: src and mime for custom shader support.
        public Waiter<int> MaterialWaiter;

        private readonly MeshImpl _parent_node;
    }
}
