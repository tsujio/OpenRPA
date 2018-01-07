using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using OpenRPA.Capture;

namespace OpenRPA.Client
{
    public partial class ClientForm : Form
    {
        public ClientForm()
        {
            InitializeComponent();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            ParseArgs(out string schema, out string command, out string option);

            Dispatch(schema, command, option);
        }

        private void ParseArgs(out string schema, out string command, out string option)
        {
            string[] args = Environment.GetCommandLineArgs();

            var r = new System.Text.RegularExpressions.Regex(@"^(\w+):(\w+)(/.*)?$");

            var m = r.Match(args.Length > 1 ? args[1] : "");
            if (!m.Success)
            {
                throw new ArgumentException($"Usage: {args[0]} schema:command/option");
            }

            schema = m.Groups[1].Value;
            command = m.Groups[2].Value;
            option = m.Groups[3].Value.TrimStart('/');
        }

        private void Dispatch(string schema, string command, string option)
        {
            switch (command)
            {
                case "capture":
                    var w = new WindowCapturer();
                    w.CaptureAndSend();
                    break;

                default:
                    throw new NotSupportedException($"Command '{command}' not supported");
            }
        }
    }
}
