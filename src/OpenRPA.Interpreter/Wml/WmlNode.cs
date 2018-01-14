using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Interpreter.Wml
{
    internal abstract class WmlNode
    {
        internal string Id { get; private set; }

        internal string Type { get; private set; }

        internal string DisplayType { get; private set; }

        internal string Name { get; private set; }

        internal static WmlNode Parse(JToken jToken)
        {
            var node = jToken.ToObject<dynamic>();

            string type = node.type;
            switch (type)
            {
                case StartNode.TYPE:
                    return new StartNode(node);

                case EndNode.TYPE:
                    return new EndNode(node);

                case ImageMatchingNode.TYPE:
                    return new ImageMatchingNode(node);

                default:
                    throw new Exception($"Unknown node type: {type}");
            }
        }

        protected WmlNode(JToken jToken)
        {
            var node = jToken.ToObject<dynamic>();

            Id = node.id;
            Type = node.type;
            DisplayType = node.displayType;
            Name = node.name;
        }

        internal abstract void Evaluate();
    }
}
