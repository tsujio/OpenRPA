using OpenRPA.Windows;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace OpenRPA.Capture
{
    public class WindowCapturer : IDisposable
    {
        public event Action Finish;

        private HotKey hotKey;

        private System.Timers.Timer timer;

        private string serverUrl;

        private string uploadToken;

        private string CaptureUploadUrl
        {
            get
            {
                return this.serverUrl + "/capture?token=" + this.uploadToken;
            }
        }

        public WindowCapturer(string serverUrl, string uploadToken)
        {
            if (String.IsNullOrWhiteSpace(serverUrl))
            {
                throw new ArgumentException("serverUrl not specified");
            }
            if (!IsValidUploadToken(uploadToken))
            {
                throw new ArgumentException("Invalid uploadToken");
            }

            this.serverUrl = serverUrl;
            this.uploadToken = uploadToken;
        }

        private bool IsValidUploadToken(string uploadToken)
        {
            if (String.IsNullOrEmpty(uploadToken))
            {
                return false;
            }

            var r = new System.Text.RegularExpressions.Regex(@"^[\w-]+$");
            var m = r.Match(uploadToken);
            return m.Success;
        }

        public void CaptureAndSend()
        {
            // Set up hot key
            hotKey = new HotKey(HotKey.MOD_KEY.CONTROL | HotKey.MOD_KEY.ALT, Keys.F5);
            hotKey.HotKeyPush += OnHotKeyPush;

            timer = new System.Timers.Timer();
            timer.Elapsed += new System.Timers.ElapsedEventHandler(OnTimeElapsed);
            timer.Interval = 100;
            timer.AutoReset = true;
            timer.Enabled = true;
        }

        private void OnHotKeyPush(object sender, EventArgs e)
        {
            var mouse = new MouseModel();
            int x = mouse.Position.X;
            int y = mouse.Position.Y;

            // Capture window where cursor points
            WindowModel w = WindowModel.FindByPosition(x, y);
            Bitmap bmp = w.CaptureWindow();

            // Send capture image to server
            using (var stream = new MemoryStream())
            {
                // Make http content object from capture image
                bmp.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                stream.Position = 0;
                HttpContent content = new StreamContent(stream);

                using (var client = new HttpClient())
                using (var formData = new MultipartFormDataContent())
                {
                    formData.Add(content, "capture", "capture.png");
                    formData.Add(new StringContent(w.WindowTitle), "title");

                    var response = client.PostAsync(this.CaptureUploadUrl, formData).Result;
                    if (!response.IsSuccessStatusCode)
                    {
                        throw new Exception("Sending captured image failed");
                    }
                }
            }

            Finish();
        }

        private WindowModel prevWindow;

        private void OnTimeElapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            var mouse = new MouseModel();
            var window = WindowModel.FindByPositionOrNull(mouse.Position.X, mouse.Position.Y);

            if (window != null && !window.Equals(prevWindow))
            {
                var rect = window.GetRectangle();
                WindowModel.DrawRect(rect.X, rect.Y, rect.Width, rect.Height);

                prevWindow = window;
            }
        }

        public void Dispose()
        {
            WindowModel.ClearRect();
            hotKey.Dispose();
            timer.Enabled = false;
        }
    }
}
