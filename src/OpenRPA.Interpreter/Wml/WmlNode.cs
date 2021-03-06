﻿using Newtonsoft.Json.Linq;
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

                case KeyboardInputNode.TYPE:
                    return new KeyboardInputNode(node);

                case OpenExplorerNode.TYPE:
                    return new OpenExplorerNode(node);

                case FileReadNode.TYPE:
                    return new FileReadNode(node);

                case UserInputNode.TYPE:
                    return new UserInputNode(node);

                case VariableNode.TYPE:
                    return new VariableNode(node);

                case WaitNode.TYPE:
                    return new WaitNode(node);

                case WhileNode.TYPE:
                    return new WhileNode(node);

                case WhileEndNode.TYPE:
                    return new WhileEndNode(node);

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

        internal abstract void Evaluate(Context context);
    }
}
