using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Windows
{
    public class WindowModel
    {
        private Process process;

        private IntPtr HWnd
        {
            get
            {
                return this.process.MainWindowHandle;
            }
        }

        public string WindowTitle
        {
            get
            {
                return this.process.MainWindowTitle;
            }
        }

        public static WindowModel FindByPosition(int x, int y)
        {
            IntPtr hWnd = Win32.WindowFromPoint(new Point(x, y));
            if (hWnd == IntPtr.Zero)
            {
                throw new Exception($"Window at ({x}, {y}) not found");
            }

            Win32.GetWindowThreadProcessId(hWnd, out uint pid);
            var proc = Process.GetProcessById((int)pid);

            return new WindowModel(proc);
        }

        private WindowModel(Process process)
        {
            this.process = process;
        }

        internal void TryBringToForeground()
        {
            // Reference: https://dobon.net/vb/dotnet/process/appactivate.html

            // Show window if minimized
            if (Win32.IsIconic(this.HWnd))
            {
                Win32.ShowWindowAsync(this.HWnd, Win32.SW_RESTORE);
            }

            IntPtr hWnd = Win32.GetForegroundWindow();
            if (hWnd == this.HWnd)
            {
                return;
            }

            uint foreThread = Win32.GetWindowThreadProcessId(hWnd, IntPtr.Zero);
            uint thisThread = Win32.GetCurrentThreadId();
            uint timeout = 200000;
            if (foreThread != thisThread)
            {
                // Remember current value
                Win32.SystemParametersInfoGet(Win32.SPI_GETFOREGROUNDLOCKTIMEOUT, 0, ref timeout, 0);

                // Set ForegroundLockTimeout to 0
                Win32.SystemParametersInfoSet(Win32.SPI_SETFOREGROUNDLOCKTIMEOUT, 0, 0, 0);
            }

            // Try bring window to foreground
            Win32.SetForegroundWindow(this.HWnd);
            Win32.SetWindowPos(this.HWnd, Win32.HWND_TOP, 0, 0, 0, 0,
                Win32.SWP_NOMOVE | Win32.SWP_NOSIZE | Win32.SWP_SHOWWINDOW | Win32.SWP_ASYNCWINDOWPOS);
            Win32.BringWindowToTop(this.HWnd);
            Win32.ShowWindowAsync(this.HWnd, Win32.SW_SHOW);
            Win32.SetFocus(this.HWnd);

            // Restore ForegroundLockTimeout
            if (foreThread != thisThread)
            {
                Win32.SystemParametersInfoSet(Win32.SPI_SETFOREGROUNDLOCKTIMEOUT, 0, timeout, 0);
            }
        }

        public Rectangle GetRectangle()
        {
            Win32.RECT r;
            if (!Win32.GetWindowRect(this.HWnd, out r))
            {
                throw new Exception("Failed to get window rect");
            }
            return new Rectangle(r.left, r.top, r.right - r.left, r.bottom - r.top);
        }

        public Bitmap CaptureWindow()
        {
            TryBringToForeground();

            System.Threading.Thread.Sleep(500);

            Rectangle rect = GetRectangle();

            var bmp = new Bitmap(rect.Width, rect.Height, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.CopyFromScreen(rect.X, rect.Y, 0, 0, rect.Size, CopyPixelOperation.SourceCopy);
            }

            return bmp;
        }
    }
}
