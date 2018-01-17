using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;

namespace OpenRPA.Interpreter.Wml
{
    internal class OpenExplorerNode : WmlNode
    {
        internal const string TYPE = "OpenExplorer";

        internal string Path { get; private set; }

        internal OpenExplorerNode(dynamic node) : base(node as JToken)
        {
            var prop = node.prop.ToObject<dynamic>();

            Path = prop.path;
        }

        internal override void Evaluate(Context context)
        {
            Process.Start("explorer.exe", Path);
        }
    }
}
