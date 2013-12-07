using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;

namespace Vestbi
{
    public class Script : IScriptClass
    {
        /// Script entry point
        public string Transform(string value)
        {
            // Put your code here
            return string.Join("", value.OrderBy(x => Guid.NewGuid()));
        }
    }
}