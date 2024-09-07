using Microsoft.ClearScript.V8;

namespace AbyssCLI.Aml
{
    internal class JSWorker
    {
        public JSWorker(string script, Document document, StreamWriter console)
        {
            engine = new(
                new V8RuntimeConstraints()
                {
                    MaxOldSpaceSize = 32 * 1024 * 1024
                }
            );
            this.script = script;

            engine.AddHostObject("document", document);
            engine.AddHostObject("console", new JSConsole(console));
            //TODO: window
        }
        public void Run()
        {
            engine.Execute(script);
        }
        private readonly V8ScriptEngine engine;
        private readonly string script;
    }
}
