using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Interpreter.Wml
{
    internal class EndNode : WmlNode
    {
        internal const string TYPE = "End";

        internal EndNode(dynamic node) : base(node as JToken)
        {
        }

        internal override void Evaluate(Context context)
        {
        }
    }
}
