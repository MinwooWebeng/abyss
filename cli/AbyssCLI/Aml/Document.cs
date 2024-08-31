using AbyssCLI.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#pragma warning disable IDE1006 //naming convention
namespace AbyssCLI.Aml
{
    internal interface Document
    {
        public Head head { get; }
        public Body body { get; }
    }

    internal interface Head : Parent { }
    internal interface Script : Selector { }
    internal interface Body : Parent { }
}
#pragma warning restore IDE1006