using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Interpreter
{
    public class RobotInterpreter
    {
        private string serverUrl;

        private string robotId;

        public RobotInterpreter(string serverUrl, string robotId)
        {
            this.serverUrl = serverUrl;
            this.robotId = robotId;
        }

        public void Execute()
        {

        }
    }
}
