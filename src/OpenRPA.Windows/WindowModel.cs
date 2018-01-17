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
    public class WindowNotFoundException : Exception
    {
        public WindowNotFoundException(string message) : base(message)
        {
        }
    }

    public class WindowModel
    {
        private IntPtr hWnd;

        private string windowTitle;

        public string WindowTitle { get => windowTitle; }

        public static WindowModel FindByPositionOrNull(int x, int y)
        {
            IntPtr hWnd = Win32.WindowFromPoint(new Point(x, y));
            if (hWnd == IntPtr.Zero)
            {
                return null;
            }

            return new WindowModel(hWnd);
        }

        public static WindowModel FindByPosition(int x, int y)
        {
            var window = FindByPositionOrNull(x, y);
            if (window == null)
            {
                throw new WindowNotFoundException($"Window at ({x}, {y}) not found");
            }

            return window;
        }

        public static WindowModel FindByTitle(string title)
        {
            var windows = new List<WindowModel>();

            Win32.EnumWindows((IntPtr hWnd, IntPtr lParam) =>
            {
                var w = new WindowModel(hWnd);
                if (w.WindowTitle == title)
                {
                    windows.Add(w);
                }

                // Next iteration
                return true;
            }, IntPtr.Zero);

            if (windows.Count == 0)
            {
                throw new WindowNotFoundException($"Window '{title}' not found");
            }

            return windows.First();
        }

        public static void DrawRect(int x, int y, int width, int height, bool clearBeforeDraw)
        {
            IntPtr desktopPtr = Win32.GetDC(IntPtr.Zero);

            using (Graphics g = Graphics.FromHdc(desktopPtr))
            using (var p = new Pen(Color.LimeGreen, 6))
            {
                if (clearBeforeDraw)
                {
                    ClearRect();
                }

                // Draw rect
                g.DrawRectangle(p, new Rectangle(x, y, width, height));
            }
            Win32.ReleaseDC(IntPtr.Zero, desktopPtr);
        }

        public static void ClearRect()
        {
            Win32.InvalidateRect(IntPtr.Zero, IntPtr.Zero, false);
        }

        private WindowModel(IntPtr hWnd)
        {
            this.hWnd = hWnd;
            this.windowTitle = "";

            int size = Win32.GetWindowTextLength(hWnd);
            if (size > 0)
            {
                var sb = new StringBuilder(size + 1);
                if (Win32.GetWindowText(hWnd, sb, sb.Capacity) > 0)
                {
                    this.windowTitle = sb.ToString();
                }
            }
        }

        internal void TryBringToForeground()
        {
            // Reference: https://dobon.net/vb/dotnet/process/appactivate.html

            // Show window if minimized
            if (Win32.IsIconic(this.hWnd))
            {
                Win32.ShowWindowAsync(this.hWnd, Win32.SW_RESTORE);
            }

            IntPtr hForeWnd = Win32.GetForegroundWindow();
            if (hForeWnd == this.hWnd)
            {
                return;
            }

            uint foreThread = Win32.GetWindowThreadProcessId(hForeWnd, IntPtr.Zero);
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
            Win32.SetForegroundWindow(this.hWnd);
            Win32.SetWindowPos(this.hWnd, Win32.HWND_TOP, 0, 0, 0, 0,
                Win32.SWP_NOMOVE | Win32.SWP_NOSIZE | Win32.SWP_SHOWWINDOW | Win32.SWP_ASYNCWINDOWPOS);
            Win32.BringWindowToTop(this.hWnd);
            Win32.ShowWindowAsync(this.hWnd, Win32.SW_SHOW);
            Win32.SetFocus(this.hWnd);

            // Restore ForegroundLockTimeout
            if (foreThread != thisThread)
            {
                Win32.SystemParametersInfoSet(Win32.SPI_SETFOREGROUNDLOCKTIMEOUT, 0, timeout, 0);
            }
        }

        public Rectangle GetRectangle()
        {
            Win32.RECT r;
            if (!Win32.GetWindowRect(this.hWnd, out r))
            {
                throw new Exception($"Failed to get window rect: {Win32.GetLastErrorMessage()}");
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

        public override bool Equals(object obj)
        {
            var other = obj as WindowModel;

            if (other == null)
            {
                return false;
            }

            return this.hWnd.Equals(other.hWnd);
        }
    }
}
