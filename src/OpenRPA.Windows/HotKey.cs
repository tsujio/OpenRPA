using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace OpenRPA.Windows
{
    public class HotKey : IDisposable
    {
        // Reference: https://anis774.net/codevault/hotkey.html

        public enum MOD_KEY : int
        {
            ALT = 0x0001,
            CONTROL = 0x0002,
            SHIFT = 0x0004,
        }

        HotKeyForm form;

        public event EventHandler HotKeyPush;

        public HotKey(MOD_KEY modKey, Keys key)
        {
            form = new HotKeyForm(modKey, key, raiseHotKeyPush);
        }

        private void raiseHotKeyPush()
        {
            if (HotKeyPush != null)
            {
                HotKeyPush(this, EventArgs.Empty);
            }
        }

        public void Dispose()
        {
            form.Dispose();
        }

        private class HotKeyForm : Form
        {
            const int WM_HOTKEY = 0x0312;

            int id;

            ThreadStart proc;

            public HotKeyForm(MOD_KEY modKey, Keys key, ThreadStart proc)
            {
                this.proc = proc;
                for (int i = 0x0000; i <= 0xbfff; i++)
                {
                    if (Win32.RegisterHotKey(this.Handle, i, modKey, key) != 0)
                    {
                        id = i;
                        break;
                    }
                }
            }

            protected override void WndProc(ref Message m)
            {
                base.WndProc(ref m);

                if (m.Msg == WM_HOTKEY)
                {
                    if ((int)m.WParam == id)
                    {
                        proc();
                    }
                }
            }

            protected override void Dispose(bool disposing)
            {
                Win32.UnregisterHotKey(this.Handle, id);
                base.Dispose(disposing);
            }
        }
    }
}
