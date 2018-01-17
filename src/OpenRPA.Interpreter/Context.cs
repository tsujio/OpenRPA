using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Interpreter
{
    internal class Context
    {
        internal class HelperClass
        {
            internal Func<String, String> GetFullUrl;
        }

        internal HelperClass Helper { get; }

        internal IDictionary<String, String> Variables { get; private set; }

        internal Context()
        {
            Helper = new HelperClass();
            Variables = new Dictionary<String, String>();
        }
    }
}
