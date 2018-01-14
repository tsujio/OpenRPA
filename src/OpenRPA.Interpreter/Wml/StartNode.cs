using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Interpreter.Wml
{
    internal class StartNode : WmlNode
    {
        internal const string TYPE = "Start";

        internal StartNode(dynamic node) : base(node as JToken)
        {
        }

        internal override void Evaluate(Context context)
        {
        }
    }
}
