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

        private string sendOption;

        public WindowCapturer(string sendOption)
        {
            this.sendOption = sendOption;
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

            bmp.Save(System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                "OpenRPA-Capture.bmp"
            ));

            // Send capture image to server
            using (var stream = new MemoryStream())
            {
                bmp.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                HttpContent content = new StreamContent(stream);

                using (var client = new HttpClient())
                using (var formData = new MultipartFormDataContent())
                {
                    formData.Add(content);

                    // TODO: Security
                    var url = "http://localhost:" + this.sendOption;

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
