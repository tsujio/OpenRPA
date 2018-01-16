using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Configuration.Install;
using System.Linq;
using System.Threading.Tasks;

namespace OpenRPA.Client
{
    [RunInstaller(true)]
    public partial class AppConfigInstaller : System.Configuration.Install.Installer
    {
        public AppConfigInstaller()
        {
            InitializeComponent();
        }

        public override void Commit(IDictionary savedState)
        {
            base.Commit(savedState);

            string serverUrl = this.Context.Parameters["serverUrl"];
            string assemblyPath = Context.Parameters["assemblypath"];

            System.IO.File.WriteAllText(@"c:\users\tsujio\desktop\test.txt", assemblyPath);

            Configuration config = ConfigurationManager.OpenExeConfiguration(assemblyPath);

            config.AppSettings.Settings["serverUrl"].Value = serverUrl;

            config.Save();
        }
    }
}
