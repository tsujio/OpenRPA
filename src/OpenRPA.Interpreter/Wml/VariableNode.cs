using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json.Linq;

namespace OpenRPA.Interpreter.Wml
{
    class VariableNode : WmlNode
    {
        internal const string TYPE = "Variable";

        internal string Action { get; private set; }

        internal string VariableName { get; private set; }

        internal VariableNode(dynamic node) : base(node as JToken)
        {
            var prop = node.prop.ToObject<dynamic>();

            Action = prop.action;
            VariableName = prop.variableName;
        }

        internal override void Evaluate(Context context)
        {
            if (String.IsNullOrEmpty(VariableName))
            {
                throw new ArgumentException("Variable name not specified");
            }

            switch (Action)
            {
                case "Read":
                    if (!context.Variables.ContainsKey(VariableName))
                    {
                        throw new ArgumentException($"Variable '{VariableName}' not found");
                    }

                    Clipboard.SetText(context.Variables[VariableName]);
                    break;

                case "Write":
                    context.Variables[VariableName] = Clipboard.GetText();
                    break;
            }
        }
    }
}
