using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
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
        private bool hasBeenCommandExecuted = false;

        public ClientForm()
        {
            InitializeComponent();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            // Command is executed only once
            if (hasBeenCommandExecuted)
            {
                return;
            }
            hasBeenCommandExecuted = true;

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
                    DoCaptureCommand(schema, command, option);
                    break;

                default:
                    throw new NotSupportedException($"Command '{command}' not supported");
            }
        }

        private Action commandAction;

        private void DoCaptureCommand(string schema, string command, string option)
        {
            string captureImageUploadUrl = ConfigurationManager.AppSettings["captureImageUploadUrl"];

            var w = new WindowCapturer(captureImageUploadUrl, option);

            // Wrap object by function not to garbage collected
            commandAction = () =>
            {
                w.Finish += () =>
                {
                    this.Close();
                };

                w.CaptureAndSend();
            };

            commandAction();
        }
    }
}
