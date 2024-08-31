using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbyssCLI.Aml
{
    public class JSConsole(StreamWriter target_stream)
    {
#pragma warning disable IDE1006 //naming convention
        public void log(string message)
        {
            target_stream.WriteLine(message);
        }
#pragma warning restore IDE1006

        private readonly StreamWriter target_stream = target_stream;
    }
}
