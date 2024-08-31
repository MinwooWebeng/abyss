using AbyssCLI.ABI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbyssCLI.Content
{
    internal class World(RenderActionWriter renderActionWriter, StreamWriter cerr, ResourceLoader resourceLoader, string UUID, string url)
        : Content(renderActionWriter, cerr, resourceLoader, url)
    {
        public readonly string UUID = UUID;
    }
}
