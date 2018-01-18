using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace OpenRPA.Windows
{
    public class MouseModel
    {
        public enum MouseActionType
        {
            None,
            LeftClick,
            RightClick,
            DoubleLeftClick,
            Drag,
        }

        public class PositionClass
        {
            public int X { get => Cursor.Position.X; }

            public int Y { get => Cursor.Position.Y; }
        }

        private PositionClass position = new PositionClass();

        public PositionClass Position { get => position; }

        public MouseModel()
        {
        }

        public void Move(int x, int y, MouseActionType action = MouseActionType.None)
        {
            if (action == MouseActionType.Drag)
            {
                StartDrag();
            }

            Win32.SetCursorPos(x, y);

            switch (action)
            {
                case MouseActionType.LeftClick:
                    LeftClick();
                    break;

                case MouseActionType.RightClick:
                    RightClick();
                    break;

                case MouseActionType.DoubleLeftClick:
                    DoubleLeftClick();
                    break;

                case MouseActionType.Drag:
                    StopDrag();
                    break;
            }
        }

        public void LeftClick()
        {
            Win32.mouse_event(Win32.MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
            Win32.mouse_event(Win32.MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
        }

        public void RightClick()
        {
            Win32.mouse_event(Win32.MOUSEEVENTF_RIGHTDOWN, 0, 0, 0, 0);
            Win32.mouse_event(Win32.MOUSEEVENTF_RIGHTUP, 0, 0, 0, 0);
        }

        public void DoubleLeftClick()
        {
            LeftClick();
            LeftClick();
        }

        private void StartDrag()
        {
            Win32.mouse_event(Win32.MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
        }

        private void StopDrag()
        {
            Win32.mouse_event(Win32.MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
        }
    }
}
