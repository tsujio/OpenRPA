using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Interpreter
{
    internal class RobotFile
    {
        // Set public to be used in json library
        public class MetaFile
        {
            internal const string FILENAME = "Robotfile";

            public string Version { get; set; }

            public string Id { get; set; }

            public string Name { get; set; }

            public string Program { get; set; }
        }

        internal MetaFile Meta { get; private set; }

        private string path;

        private string dest;

        internal static RobotFile Load(string path)
        {
            string dest = DecompressFile(path);

            MetaFile meta;
            using (var f = File.OpenRead(Path.Combine(dest, MetaFile.FILENAME)))
            using (var r = new StreamReader(f))
            {
                var settings = new JsonSerializerSettings
                {
                    ContractResolver = new CamelCasePropertyNamesContractResolver(),
                };
                meta = JsonConvert.DeserializeObject<MetaFile>(r.ReadToEnd());
            }

            return new RobotFile(path, dest, meta);
        }

        private static string DecompressFile(string path)
        {
            var dir = Path.GetDirectoryName(path);
            var filename = Path.GetFileNameWithoutExtension(path);
            var dest = Path.Combine(dir, filename);

            if (Directory.Exists(dest))
            {
                Directory.Delete(dest, true);
            }

            ZipFile.ExtractToDirectory(path, dest);

            return dest;
        }

        internal RobotFile(string path, string dest, MetaFile meta)
        {
            this.path = path;
            this.dest = dest;
            this.Meta = meta;
        }

        internal string GetAbsolutePath(string relativePath)
        {
            return Path.Combine(this.dest, relativePath);
        }
    }
}
