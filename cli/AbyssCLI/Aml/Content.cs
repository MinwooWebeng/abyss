using AbyssCLI.ABI;
using AbyssCLI.Tool;
using static AbyssCLI.AbyssLib;

namespace AbyssCLI.Aml
{
    internal class Content(AbyssLib.AbyssHost host, RenderActionWriter renderActionWriter, StreamWriter cerr,
        string UUID, AbyssAddress URL, vec3 initial_position)
    {
        public void Activate() => _documentImpl.Activate();
        public void Close() => _documentImpl.Close();
        public Task CloseAsync() => _documentImpl.CloseAsync();
        public readonly string UUID = UUID;
        private readonly DocumentImpl _documentImpl = new(
                new Tool.Contexted(),
                renderActionWriter,
                cerr,
                new ResourceLoader(host, cerr, URL),
                URL,
                initial_position);
    }
}
