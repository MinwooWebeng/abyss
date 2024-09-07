#pragma warning disable IDE1006 //naming convension
namespace AbyssCLI.Aml
{
    public class Document
    {
        internal Document(DocumentImpl impl) { _impl = impl; }
        private readonly DocumentImpl _impl;

        public Head head => new(_impl.Head);
        public Body body => new(_impl.Body);

        public object getElementById(string id)
        {
            _impl.ElementDictionary.TryGetValue(id, out var element);
            return element switch
            {
                GroupImpl => new Group(element as GroupImpl),
                _ => null
            };
        }
    }
    public class Head
    {
        internal Head(HeadImpl impl) { _impl = impl; }
        private readonly HeadImpl _impl;
        public static string tag => HeadImpl.Tag;
    }
    public class Script
    {
        internal Script(ScriptImpl impl) { _impl = impl; }
        private readonly ScriptImpl _impl;
        public static string tag => ScriptImpl.Tag;
    }
    public class Body
    {
        internal Body(BodyImpl impl) { _impl = impl; }
        private readonly BodyImpl _impl;
        public static string tag => BodyImpl.Tag;
    }
    public class Group
    {
        internal Group(GroupImpl impl) { _impl = impl; }
        private readonly GroupImpl _impl;
        public string id => _impl.Id;
        public static string tag => GroupImpl.Tag;
    }
    public class Mesh
    {
        internal Mesh(MeshImpl impl) { _impl = impl; }
        private readonly MeshImpl _impl;
        public string id => _impl.Id;
        public static string tag => MeshImpl.Tag;
        public string src => _impl.Source;
        public string type => _impl.MimeType;
    }
}
#pragma warning restore IDE1006 //naming convension
