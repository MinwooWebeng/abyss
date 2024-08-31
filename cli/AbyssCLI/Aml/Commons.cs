using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#pragma warning disable IDE1006 //naming convention
namespace AbyssCLI.Aml
{
    internal interface Selector //base of every AML elements
    {
        public string tag { get; }
        public string id { get; }
    }
    internal interface Locator : Selector
    {
        public string pos { get; set; }
        public string rot { get; set; }
        //TODO: motion
    }
    internal interface Parent : Selector
    {
        public void appendChild(Selector child);
    }
}
#pragma warning restore IDE1006
