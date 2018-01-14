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
            Win32.SetCursorPos(x, y);

            switch (action)
            {
                case MouseActionType.LeftClick:
                    LeftClick();
                    break;
            }
        }

        public static void LeftClick()
        {
            Win32.mouse_event(Win32.MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
            Win32.mouse_event(Win32.MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
        }
    }
}
