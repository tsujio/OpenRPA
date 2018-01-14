using Newtonsoft.Json.Linq;
using OpenRPA.Windows;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Interpreter.Wml
{
    internal class ImageMatchingNode : WmlNode
    {
        internal string ImageUrlPath { get; private set; }

        internal IList<int> StartPos { get; private set; }

        internal IList<int> EndPos { get; private set; }

        internal string WindowTitle { get; private set; }

        internal const string TYPE = "ImageMatching";

        internal ImageMatchingNode(dynamic node) : base(node as JToken)
        {
            var prop = node.prop.ToObject<dynamic>();

            ImageUrlPath = prop.imageUrlPath;
            StartPos = (prop.startPos as JArray).ToObject<IList<int>>();
            EndPos = (prop.endPos as JArray).ToObject<IList<int>>();
            WindowTitle = prop.windowTitle;
        }

        internal override void Evaluate()
        {
            var window = WindowModel.FindByTitle(WindowTitle);
            Bitmap bmp = window.CaptureWindow();
        }
    }
}
