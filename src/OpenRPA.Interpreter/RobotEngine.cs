using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Interpreter
{
    public class RobotEngine
    {
        private string serverUrl;

        private string robotId;

        private string RobotDownloadUrl
        {
            get
            {
                return this.serverUrl + "/workflow/" + this.robotId;
            }
        }

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

        private string RobotFileName
        {
            get
            {
                return robotId + ".rpa";
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

            string path = DownloadRobot();

            var robotFile = RobotFile.Load(path);
            string programPath = robotFile.GetAbsolutePath(robotFile.Meta.Program);
        }

        private string DownloadRobot()
        {
            var path = Path.Combine(RobotCacheDir, RobotFileName);

            if (File.Exists(path))
            {
                return path;
            }

            using (var client = new HttpClient())
            {
                var response = client.GetAsync(RobotDownloadUrl).Result;
                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception("Failed to download robot");
                }

                var stream = response.Content.ReadAsStreamAsync().Result;

                using (var f = File.Create(path))
                {
                    stream.CopyTo(f);
                }

                return path;
            }
        }
    }
}
