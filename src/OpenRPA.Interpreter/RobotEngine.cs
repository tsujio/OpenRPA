using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Interpreter
{
    public class RobotEngine : IDisposable
    {
        private string serverUrl;

        private string robotId;

        private string TempAppDir
        {
            get
            {
                return Path.Combine(Path.GetTempPath(), "OpenRPA");
            }
        }

        private string RobotCacheDir
        {
            get
            {
                return Path.Combine(TempAppDir, "cache");
            }
        }

        public RobotEngine(string serverUrl, string robotId)
        {
            // TODO: validate input
            this.serverUrl = serverUrl;
            this.robotId = robotId;
        }

        public void Execute()
        {
            if (!Directory.Exists(TempAppDir))
            {
                Directory.CreateDirectory(TempAppDir);
            }
            if (!Directory.Exists(RobotCacheDir))
            {
                Directory.CreateDirectory(RobotCacheDir);
            }

            var robotFile = RobotFile.Download(serverUrl, robotId, RobotCacheDir);

            var interp = new WorkflowInterpreter(robotFile);
            interp.Start();
        }

        public void Dispose()
        {
        }
    }
}
