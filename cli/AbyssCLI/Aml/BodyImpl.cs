using System.Xml;

namespace AbyssCLI.Aml
{
    internal sealed class BodyImpl : AmlNode
    {
        public BodyImpl(AmlNode context, XmlNode xml_node, vec3 body_position)
            : base(context)
        {
            _root_elem = RenderID.ElementId;

            _body_position = xml_node.Attributes["pos"]!= null ? Aml.AmlValueParser.ParseVec3(xml_node.Attributes["pos"].Value) : body_position;
            foreach (XmlNode child in xml_node?.ChildNodes)
            {
                Children.Add(child.Name switch
                {
                    "o" => new GroupImpl(this, _root_elem, child),
                    "mesh" => new MeshImpl(this, _root_elem, child),
                    _ => throw new Exception("Invalid tag in <body>"),
                });
            }
        }
        protected override Task ActivateSelfCallback(CancellationToken token)
        {
            RenderActionWriter.CreateElement(0, _root_elem);
            RenderActionWriter.ElemSetPos(_root_elem, new ABI.Vec3
            {
                X = _body_position.x,
                Y = _body_position.y,
                Z = _body_position.z
            });
            return Task.CompletedTask;
        }
        protected override void DeceaseSelfCallback()
        {
            RenderActionWriter.MoveElement(_root_elem, -1);
        }
        protected override void CleanupSelfCallback()
        {
            RenderActionWriter.DeleteElement(_root_elem);
        }
        public static string Tag => "body";
        public vec3 Pos => _body_position;

        private readonly int _root_elem;
        private readonly vec3 _body_position;
    }
}
