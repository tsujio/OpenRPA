using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace OpenRPA.Interpreter.Wml
{
    class WaitNode : WmlNode
    {
        internal const string TYPE = "Wait";

        internal int Timeout { get; private set; }

        internal WaitNode(dynamic node) : base(node as JToken)
        {
            var prop = node.prop.ToObject<dynamic>();

            Timeout = prop.timeout;
        }

        internal override void Evaluate(Context context)
        {
            System.Threading.Thread.Sleep(Timeout * 1000);
        }
    }
}
