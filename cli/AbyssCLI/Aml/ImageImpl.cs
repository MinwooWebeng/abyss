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
        protected override async Task ActivateSelfCallback(CancellationToken token)
        {
            int component_id = MimeType switch
            {
                "image/png" => await ResourceLoader.GetFileAsync(Source, MIME.ImagePng),
                "image/jpg" or "image/jpeg" => await ResourceLoader.GetFileAsync(Source, MIME.ImageJpeg),
                _ => throw new Exception("unsupported type in <image" + (Id == null ? "" : (":" + Id)) + ">"),
            };
            token.ThrowIfCancellationRequested();
            if(component_id == -1) //failed to load resource.
            {
                return;
            }
            var material_id = _parent_material.MaterialWaiter.GetValue();
            token.ThrowIfCancellationRequested();
            RenderActionWriter.MaterialSetParamC(material_id, Role, component_id);
        }
        //no cleanup - resourceLoader manages resource lifecycle.
        public static string Tag => "img";
        public string Id { get; }
        public string Source { get; }
        public string MimeType { get; }
        public string Role { get; }

        private readonly MaterialImpl _parent_material;
    }
}
