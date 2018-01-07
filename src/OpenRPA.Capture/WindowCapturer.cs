using OpenRPA.Windows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Capture
{
    public class WindowCapturer
    {
        public event Action Finish;

        public void CaptureAndSend()
        {
            var hook = new MouseHook(MouseHook.HookType.LeftClick);

            hook.MouseEvent += OnMouseLeftClickEvent;

            hook.Start();
        }

        private void OnMouseLeftClickEvent(MouseHook sender, int x, int y)
        {
            sender.Stop();

            System.Diagnostics.Debug.WriteLine($"{x}, {y}");

            Finish();
        }
    }
}
