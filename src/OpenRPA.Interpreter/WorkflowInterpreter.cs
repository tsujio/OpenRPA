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

            JArray workflow;
            using (var f = File.OpenRead(programPath))
            using (var r = new StreamReader(f))
            {
                workflow = JArray.Parse(r.ReadToEnd());
            }

            foreach (var node in workflow.Select(jToken => WmlNode.Parse(jToken)))
            {
                node.Evaluate();
            }
        }
    }
}
