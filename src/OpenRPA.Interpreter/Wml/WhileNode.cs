using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json.Linq;

namespace OpenRPA.Interpreter.Wml
{
    class WhileNode : WmlNode
    {
        internal const string TYPE = "While";

        internal bool InverseCondition { get; private set; }

        internal string VariableName { get; private set; }

        internal WhileNode(dynamic node) : base(node as JToken)
        {
            var prop = node.prop.ToObject<dynamic>();

            InverseCondition = prop.inverseCondition;
            VariableName = prop.variableName;
        }

        internal override void Evaluate(Context context)
        {
        }

        internal bool EvaluateCondition(Context context)
        {
            if (!context.Variables.ContainsKey(VariableName))
            {
                throw new ArgumentException($"Variable '{VariableName}' not found");
            }

            var result = context.Variables[VariableName] == Clipboard.GetText();
            return InverseCondition ? !result : result;
        }
    }

    class WhileEndNode : WmlNode
    {
        internal const string TYPE = "WhileEnd";

        internal WhileEndNode(dynamic node) : base(node as JToken)
        {
        }

        internal override void Evaluate(Context context)
        {
        }
    }
}
