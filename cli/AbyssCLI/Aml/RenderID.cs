namespace AbyssCLI.Aml
{
    internal static class RenderID
    {
        public static int ElementId { get { return Interlocked.Increment(ref _element_id); } }
        private static int _element_id = 0;

        public static int ComponentId { get { return Interlocked.Increment(ref _component_id); } }
        private static int _component_id = 0;
    }
}
