using System.Xml;

namespace AbyssCLI.Aml
{
    internal class GroupImpl : AmlNode
    {
        public GroupImpl(AmlNode parent_node, int render_parent, XmlNode xml_this_node)
            : base(parent_node)
        {
            Id = xml_this_node.Attributes["id"]?.Value;
            if (Id != null)
            {
                ElementDictionary[Id] = this;
            }
            Pos = Aml.AmlValueParser.ParseVec3(xml_this_node.Attributes["pos"]?.Value);
            Rot = Aml.AmlValueParser.ParseVec4(xml_this_node.Attributes["rot"]?.Value);
            _render_parent = render_parent;
            _render_elem = RenderID.ElementId;
            foreach (XmlNode child in xml_this_node.ChildNodes)
            {
                Children.Add(child.Name switch
                {
                    "o" => new GroupImpl(this, _render_elem, child),
                    "mesh" => new MeshImpl(this, _render_elem, child),
                    _ => throw new Exception("Invalid tag in <o" + (Id == null ? "" : (":" + Id)) + ">"),
                });
            }
        }
        protected override Task ActivateSelfCallback(CancellationToken token)
        {
            RenderActionWriter.CreateElement(_render_parent, _render_elem);
            RenderActionWriter.ElemSetPos(_render_elem, new ABI.Vec3() { X = Pos.x, Y = Pos.y, Z = Pos.z });
            return Task.CompletedTask;
        }
        protected override void DeceaseSelfCallback()
        {
            RenderActionWriter.MoveElement(_render_elem, -1);
            if (Id != null)
            {
                ElementDictionary.Remove(Id, out _);
            }
        }
        protected override void CleanupSelfCallback()
        {
            RenderActionWriter.DeleteElement(_render_elem);
        }

        public static string Tag => "o";
        public string Id { get; }
        public vec3 Pos;
        public vec4 Rot;

        private readonly int _render_parent;
        private readonly int _render_elem;
    }
}
