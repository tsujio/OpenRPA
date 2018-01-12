using OpenRPA.Windows;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Capture
{
    public class WindowCapturer
    {
        public event Action Finish;

        private MouseHook mouseHook;

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
            // Store MouseHook object to instance field not to be garbage collected.
            // This instance is referenced by native code (SetWindowsHookEx).
            // If native code refers GCed object, application will crash.
            mouseHook = new MouseHook(MouseHook.HookType.LeftClick);

            mouseHook.MouseEvent += OnMouseLeftClickEvent;

            mouseHook.Start();
        }

        private void OnMouseLeftClickEvent(int x, int y)
        {
            mouseHook.Stop();

            // Capture window at clicked point
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
    }
}
