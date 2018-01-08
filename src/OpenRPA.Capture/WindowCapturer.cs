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

        private string captureImageUploadUrl;

        private string uploadToken;

        public WindowCapturer(string captureImageUploadUrl, string uploadToken)
        {
            if (String.IsNullOrWhiteSpace(captureImageUploadUrl))
            {
                throw new ArgumentException("captureImageUploadUrl not specified");
            }
            if (!IsValidUploadToken(uploadToken))
            {
                throw new ArgumentException("Invalid uploadToken");
            }

            this.captureImageUploadUrl = captureImageUploadUrl;
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
            var hook = new MouseHook(MouseHook.HookType.LeftClick);

            hook.MouseEvent += OnMouseLeftClickEvent;

            hook.Start();
        }

        private void OnMouseLeftClickEvent(MouseHook sender, int x, int y)
        {
            sender.Stop();

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

                    var url = this.captureImageUploadUrl + "?token=" + this.uploadToken;

                    var response = client.PostAsync(url, formData).Result;
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
