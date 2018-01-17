using Newtonsoft.Json.Linq;
using OpenRPA.Interpreter.Wml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Interpreter
{
    internal class WorkflowInterpreter
    {
        private RobotFile robotFile;

        internal WorkflowInterpreter(RobotFile robotFile)
        {
            this.robotFile = robotFile;
        }

        internal void Start()
        {
            var programPath = robotFile.GetAbsolutePath(robotFile.Meta.Program);

            var context = new Context();
            context.Helper.GetFullUrl = (path) => robotFile.ServerUrl + path;

            JArray workflow;
            using (var f = File.OpenRead(programPath))
            using (var r = new StreamReader(f))
            {
                workflow = JArray.Parse(r.ReadToEnd());
            }

            var flowInLoop = new List<WmlNode>();
            foreach (var node in workflow.Select(jToken => WmlNode.Parse(jToken)))
            {
                // Temprary loop implementation
                if (node.Type == WhileNode.TYPE || flowInLoop.Count > 0)
                {
                    flowInLoop.Add(node);

                    if (node.Type != WhileEndNode.TYPE)
                    {
                        continue;
                    }

                    var whileNode = flowInLoop.First() as WhileNode;
                    flowInLoop.RemoveAt(0);

                    while (whileNode.EvaluateCondition(context))
                    {
                        foreach (var n in flowInLoop)
                        {
                            n.Evaluate(context);
                        }
                    }

                    flowInLoop.Clear();
                }

                node.Evaluate(context);
            }
        }
    }
}
