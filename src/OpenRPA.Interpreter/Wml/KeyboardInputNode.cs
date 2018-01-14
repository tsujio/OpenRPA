using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace OpenRPA.Interpreter.Wml
{
    internal class KeyboardInputNode : WmlNode
    {
        internal const string TYPE = "KeyboardInput";

        internal string Keys { get; private set; }

        internal KeyboardInputNode(dynamic node) : base(node as JToken)
        {
            var prop = node.prop.ToObject<dynamic>();

            Keys = prop.keys;
        }

        internal override void Evaluate(Context context)
        {
            SendKeys.SendWait(Keys);
        }
    }
}
