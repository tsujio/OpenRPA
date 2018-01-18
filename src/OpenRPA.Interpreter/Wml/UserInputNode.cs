using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace OpenRPA.Interpreter.Wml
{
    internal class UserInputNode : WmlNode
    {
        internal const string TYPE = "UserInput";

        internal string VariableName { get; private set; }

        internal string PromptText { get; private set; }

        internal UserInputNode(dynamic node) : base(node as JToken)
        {
            var prop = node.prop.ToObject<dynamic>();

            VariableName = prop.variableName;
            PromptText = prop.promptText;
        }

        internal override void Evaluate(Context context)
        {
            if (String.IsNullOrEmpty(VariableName))
            {
                throw new ArgumentException("Variable name not specified");
            }

            Form prompt = new Form()
            {
                Width = 300,
                Height = 200,
                Text = "OpenRPA",
            };

            prompt.Controls.Add(new Label()
            {
                Left = 50,
                Top = 40,
                Width = 200,
                Text = PromptText,
            });

            var textbox = new TextBox()
            {
                Left = 50,
                Top = 80,
                Width = 200,
            };
            prompt.Controls.Add(textbox);

            var button = new Button()
            {
                Left = 50,
                Top = 120,
                Text = "OK",
            };
            button.Click += (sender, e) => prompt.Close();
            prompt.Controls.Add(button);

            prompt.ShowDialog();

            context.Variables[VariableName] = textbox.Text;
        }
    }
}
