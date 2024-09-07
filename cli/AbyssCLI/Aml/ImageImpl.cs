using AbyssCLI.Tool;
using System.Xml;

namespace AbyssCLI.Aml
{
    internal class ImageImpl : AmlNode
    {
        public ImageImpl(MaterialImpl parent_material, XmlNode xml_node)
            : base(parent_material)
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
                "src attribute is null in <image" + (Id == null ? "" : (":" + Id)) + ">");
            }
            MimeType = xml_node.Attributes["type"]?.Value;
            if (MimeType == null)
            {
                throw new Exception(
                "type attribute is null in <image" + (Id == null ? "" : (":" + Id)) + ">");
            }
            Role = xml_node.Attributes["role"]?.Value;
            if (Role == null)
            {
                throw new Exception(
                "role attribute is null in <image" + (Id == null ? "" : (":" + Id)) + ">");
            }

            _parent_material = parent_material;
        }
        protected override Task ActivateSelfCallback(CancellationToken token)
        {
            switch (MimeType)
            {
                case "image/png":
                    if (!ResourceLoader.TryGetFileOrWaiter(Source, MIME.ImagePng, out _resource, out _resource_waiter))
                    {
                        //resource not ready - wait for value;
                        _resource = _resource_waiter.GetValue();
                    }
                    break;
                case "image/jpg" or "image/jpeg":
                    if (!ResourceLoader.TryGetFileOrWaiter(Source, MIME.ImageJpeg, out _resource, out _resource_waiter))
                    {
                        //resource not ready - wait for value;
                        _resource = _resource_waiter.GetValue();
                    }
                    break;
                default:
                    return Task.CompletedTask;
            }
            token.ThrowIfCancellationRequested();
            if (!_resource.IsValid)
                throw new Exception("failed to load " + Source + " in <image" + (Id == null ? "" : (":" + Id)) + ">");


            if (!_parent_material.MaterialWaiterGroup.TryGetValueOrWaiter(out var material_id, out var material_waiter))
            {
                material_id = material_waiter.GetValue();
            }
            token.ThrowIfCancellationRequested();
            if (material_id == 0)
                return Task.CompletedTask;

            _component_id = RenderID.ComponentId;
            RenderActionWriter.CreateImage(_component_id, _resource.ABIFileInfo);
            RenderActionWriter.MaterialSetParamC(material_id, Role, _component_id);
            return Task.CompletedTask;
        }
        protected override void DeceaseSelfCallback()
        {
            _resource_waiter?.CancelWithValue(default);
        }
        protected override void CleanupSelfCallback()
        {
            if (_component_id != 0)
                RenderActionWriter.DeleteImage(_component_id);
        }
        public static string Tag => "img";
        public string Id { get; }
        public string Source { get; }
        public string MimeType { get; }
        public string Role { get; }

        private readonly MaterialImpl _parent_material;
        private Waiter<ResourceLoader.FileResource> _resource_waiter;
        ResourceLoader.FileResource _resource;
        private int _component_id;
    }
}
