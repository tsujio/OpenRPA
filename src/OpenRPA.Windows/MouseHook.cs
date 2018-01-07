using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Windows
{
    public class MouseHook
    {
        public enum HookType
        {
            LeftClick,
        }

        public delegate void MouseEventHandler(MouseHook sender, int x, int y);

        public event MouseEventHandler MouseEvent;

        private HookType hookType;

        private IntPtr hHook;

        public MouseHook(HookType hookType)
        {
            this.hookType = hookType;
        }

        public void Start()
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                IntPtr hMod = Win32.GetModuleHandle(curModule.ModuleName);
                hHook = Win32.SetWindowsHookEx(Win32.WH_MOUSE_LL, OnMouseEventOccureed, hMod, 0);
            }

            if (hHook == IntPtr.Zero)
            {
                throw new Exception("Failed to set mouse hook");
            }
        }

        private IntPtr OnMouseEventOccureed(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && Win32.MouseMessages.WM_LBUTTONDOWN == (Win32.MouseMessages)wParam)
            {
                var hookStruct = (Win32.MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam,
                    typeof(Win32.MSLLHOOKSTRUCT));

                MouseEvent(this, hookStruct.pt.x, hookStruct.pt.y);
            }

            return Win32.CallNextHookEx(hHook, nCode, wParam, lParam);
        }

        public void Stop()
        {
            Win32.UnhookWindowsHookEx(hHook);
        }
    }
}
