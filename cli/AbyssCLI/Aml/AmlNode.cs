using AbyssCLI.ABI;
using AbyssCLI.Tool;
using System.Collections.Concurrent;

namespace AbyssCLI.Aml
{
    internal class AmlNode : Contexted
    {
        protected AmlNode(Contexted root, RenderActionWriter renderActionWriter, StreamWriter cerr, ResourceLoader resourceLoader)
            : base(root)
        {
            RenderActionWriter = renderActionWriter;
            ErrorStream = cerr;
            ResourceLoader = resourceLoader;

            Children = [];
            ElementDictionary = [];
        }
        protected AmlNode(AmlNode base_context)
            : base(base_context)
        {
            RenderActionWriter = base_context.RenderActionWriter;
            ErrorStream = base_context.ErrorStream;
            ResourceLoader = base_context.ResourceLoader;

            Parent = base_context;
            Children = [];
            ElementDictionary = base_context.ElementDictionary;
        }
        protected sealed override async Task ActivateCallback(CancellationToken token)
        {
            await ActivateSelfCallback(token);
            lock (Children)
            {
                foreach (AmlNode child in Children)
                {
                    child.Activate();
                }
            }
        }
        protected sealed override void ErrorCallback(Exception e)
        {
            if (e is not OperationCanceledException)
                ErrorStream.WriteLine(e.Message + ": " + e.StackTrace);
        }
        protected sealed override void DeceaseCallback()
        {
            DeceaseSelfCallback();
            lock (Children)
            {
                foreach (AmlNode child in Children)
                {
                    child.Close();
                }
            }
        }
        protected sealed override void CleanupCallback()
        {
            lock (Children)
            {
                foreach (AmlNode child in Children)
                {
                    child.Close();
                }
            }
            CleanupSelfCallback();
        }

        //***Construct all children on constructor
        //***DO NOT CALL Activate() from derived class
        protected virtual Task ActivateSelfCallback(CancellationToken token) { return Task.CompletedTask; }
        protected virtual void DeceaseSelfCallback() { return; }
        protected virtual void CleanupSelfCallback() { return; }

        //TODO: 
        public readonly ResourceLoader ResourceLoader;
        public readonly StreamWriter ErrorStream;
        public readonly RenderActionWriter RenderActionWriter;
        public readonly ConcurrentDictionary<string, AmlNode> ElementDictionary;

        protected readonly AmlNode Parent;
        protected readonly List<AmlNode> Children;
    }
}
