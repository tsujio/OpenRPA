using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Interpreter
{
    public class RobotInterpreter
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

        public RobotInterpreter(string serverUrl, string robotId)
        {
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

            string path = DownloadRobot(RobotDownloadUrl);
        }

        private string DownloadRobot(string url)
        {
            using (var client = new HttpClient())
            {
                var response = client.GetAsync(url).Result;
                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception("Failed to download robot");
                }

                var stream = response.Content.ReadAsStreamAsync().Result;

                var path = Path.Combine(RobotCacheDir, RobotFileName);
                using (var f = File.Create(path))
                {
                    stream.CopyTo(f);
                }

                return path;
            }
        }
    }
}
