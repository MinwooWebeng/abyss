using System.Xml;

namespace AbyssCLI.Aml
{
    internal sealed class BodyImpl : AmlNode
    {
        public BodyImpl(AmlNode context, XmlNode xml_node)
            : base(context)
        {
            _root_elem = Content.RenderID.ElementId;
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

        private readonly int _root_elem;

    }
}
