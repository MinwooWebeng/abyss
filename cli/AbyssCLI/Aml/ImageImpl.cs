using AbyssCLI.Tool;
using System.Xml;

namespace AbyssCLI.Aml
{
    internal class ImageImpl : AmlNode
    {
        public ImageImpl(MaterialImpl parent_material, XmlNode xml_node)
            :base(parent_material)
        {
            Id = xml_node.Attributes["id"]?.Value;
            if (Id != null)
            {
                ElementDictionary[Id] = this;
            }
            Source = xml_node.Attributes["src"]?.Value;
            if (Source == null) { throw new Exception(
                "src attribute is null in <image" + (Id == null ? "" : (":" + Id)) + ">"); }
            MimeType = xml_node.Attributes["type"]?.Value;
            if (MimeType == null) { throw new Exception(
                "type attribute is null in <image" + (Id == null ? "" : (":" + Id)) + ">"); }
            Role = xml_node.Attributes["role"]?.Value;
            if (Role == null) { throw new Exception(
                "role attribute is null in <image" + (Id == null ? "" : (":" + Id)) + ">"); }

            _parent_material = parent_material;
        }
        protected override Task ActivateSelfCallback(CancellationToken token)
        {
            Content.ResourceLoader.FileResource resource;
            switch (MimeType)
            {
                case "image/png":
                    if (!ResourceLoader.TryGetFileOrWaiter(Source, MIME.ImagePng, out resource, out _resource_waiter))
                    {
                        //resource not ready - wait for value;
                        resource = _resource_waiter.GetValue();
                    }
                    break;
                case "image/jpg" or "image/jpeg":
                    if (!ResourceLoader.TryGetFileOrWaiter(Source, MIME.ImageJpeg, out resource, out _resource_waiter))
                    {
                        //resource not ready - wait for value;
                        resource = _resource_waiter.GetValue();
                    }
                    break;
                default:
                    return Task.CompletedTask;
            }

            if (resource.ComponentId == -1 || token.IsCancellationRequested)
            {
                return Task.CompletedTask;
            }

            if (!_parent_material.MaterialWaiterGroup.TryGetValueOrWaiter(out var material_id, out var material_waiter))
            {
                material_id = material_waiter.GetValue();
            }
            if (material_id == -1 || token.IsCancellationRequested)
            {
                return Task.CompletedTask;
            }

            //side effect on renderer - do we need cleanup?
            RenderActionWriter.MaterialSetParamC(material_id, Role, resource.ComponentId);
            return Task.CompletedTask;
        }
        protected override void DeceaseSelfCallback()
        {
            _resource_waiter?.CancelWithValue(default);
        }
        public static string Tag => "img";
        public string Id { get; }
        public string Source { get; }
        public string MimeType { get; }
        public string Role { get; }

        private readonly MaterialImpl _parent_material;
        private Waiter<Content.ResourceLoader.FileResource> _resource_waiter;
    }
}
