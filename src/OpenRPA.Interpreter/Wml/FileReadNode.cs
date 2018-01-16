using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

namespace OpenRPA.Interpreter.Wml
{
    internal class FileReadNode : WmlNode
    {
        internal const string TYPE = "FileRead";

        internal string Path { get; private set; }

        internal FileReadNode(dynamic node) : base(node as JToken)
        {
            var prop = node.prop.ToObject<dynamic>();

            Path = prop.path;
        }

        internal override void Evaluate(Context context)
        {
            var text = File.ReadAllText(Path);

            Clipboard.SetText(text);
        }
    }
}
