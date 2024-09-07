//using AbyssCLI.ABI;
//using static AbyssCLI.AbyssLib;

//namespace AbyssCLI.Aml
//{
//    internal class World
//    {
//        public World(AbyssHost host, RenderActionWriter renderActionWriter, StreamWriter cerr,
//            string UUID, string URL)
//        {
//            this.UUID = UUID;

//            var base_addr = URL.Split('/', 2)[0]; //TODO: clerarify address parser.
//            _documentImpl = new(
//                new Tool.Contexted(),
//                renderActionWriter,
//                cerr,
//                new ResourceLoader(host, base_addr + "/", renderActionWriter),
//                URL);
//        }
//        public void Activate() => _documentImpl.Activate();
//        public void Close() => _documentImpl.Close();
//        public Task CloseAsync() => _documentImpl.CloseAsync();
//        public readonly string UUID;
//        private readonly DocumentImpl _documentImpl;
//    }
//}
