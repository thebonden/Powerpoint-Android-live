using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;

namespace HjvTak.LiveAndroid
{
    internal static class NativeWindow
    {
        private const int GwlExStyle = -20, GwlOwner = -8;
        private const long NoActivate = 0x08000000L, ToolWindow = 0x80L;
        internal static IntPtr FindVideo(int processId, string title)
        {
            IntPtr found = IntPtr.Zero;
            EnumWindows((hwnd, _) => {
                uint pid;
                GetWindowThreadProcessId(hwnd, out pid);
                if (pid != processId) return true;
                var text = new StringBuilder(512);
                GetWindowText(hwnd, text, text.Capacity);
                if (text.ToString() != title) return true;
                found = hwnd;
                return false;
            }, IntPtr.Zero);
            return found;
        }
        internal static bool IsOwnedBy(IntPtr hwnd, int processId)
        {
            uint pid;
            return hwnd != IntPtr.Zero && IsWindow(hwnd) &&
                GetWindowThreadProcessId(hwnd, out pid) != 0 && pid == processId;
        }
        internal static bool IsHostForeground(IntPtr host)
        {
            uint processId;
            return GetWindowThreadProcessId(host, out processId) != 0 && IsOwnedBy(GetForegroundWindow(), (int)processId);
        }

        internal static bool TryGetClient(IntPtr hwnd, out Rectangle result)
        {
            result = Rectangle.Empty;
            Rect rect;
            Point point = new Point();
            if (!GetClientRect(hwnd, out rect) || !ClientToScreen(hwnd, ref point)) return false;
            result = new Rectangle(point.X, point.Y, rect.Right - rect.Left, rect.Bottom - rect.Top);
            return result.Width > 0 && result.Height > 0;
        }
        internal static bool Place(IntPtr video, IntPtr show, Rectangle rect)
        {
            // An owned popup stays above its slideshow without becoming globally always-on-top.
            // Never SetParent: this remains an external window, not slide-contained media.
            SetLong(video, GwlOwner, show);
            if (GetLong(video, GwlOwner) != show) return false;
            long style = GetLong(video, GwlExStyle).ToInt64();
            SetLong(video, GwlExStyle, new IntPtr(style | NoActivate | ToolWindow));
            return SetWindowPos(video, IntPtr.Zero, rect.X, rect.Y, rect.Width, rect.Height,
                0x0010 | 0x0040); // NOACTIVATE | SHOWWINDOW, HWND_TOP
        }
        internal static void Hide(IntPtr hwnd) { if (hwnd != IntPtr.Zero) ShowWindow(hwnd, 0); }
        internal static void DetachOwner(IntPtr hwnd) { if (hwnd != IntPtr.Zero) SetLong(hwnd, GwlOwner, IntPtr.Zero); }
        internal static void Close(IntPtr hwnd) { if (hwnd != IntPtr.Zero) PostMessage(hwnd, 0x0010, IntPtr.Zero, IntPtr.Zero); }
        private static IntPtr GetLong(IntPtr hwnd, int index) => IntPtr.Size == 8 ? GetWindowLongPtr(hwnd, index) : new IntPtr(GetWindowLong(hwnd, index));
        private static void SetLong(IntPtr hwnd, int index, IntPtr value)
        { if (IntPtr.Size == 8) SetWindowLongPtr(hwnd, index, value); else SetWindowLong(hwnd, index, value.ToInt32()); }
        internal sealed class DpiScope : IDisposable
        {
            private IntPtr previous;
            internal DpiScope()
            {
                // Keep coordinates in physical pixels without changing Office's process-wide DPI mode.
                try { previous = SetThreadDpiAwarenessContext(new IntPtr(-4)); }
                catch (EntryPointNotFoundException) { }
            }
            public void Dispose() { if (previous != IntPtr.Zero) SetThreadDpiAwarenessContext(previous); }
        }
        private delegate bool EnumWindowsProc(IntPtr hwnd, IntPtr param);
        [DllImport("user32.dll")] private static extern bool EnumWindows(EnumWindowsProc callback, IntPtr param);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetWindowText(IntPtr hwnd, StringBuilder text, int count);
        [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr hwnd, out uint processId);
        [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")] internal static extern bool IsWindow(IntPtr hwnd);
        [DllImport("user32.dll")] internal static extern bool IsWindowVisible(IntPtr hwnd);
        [DllImport("user32.dll")] internal static extern bool IsIconic(IntPtr hwnd);
        [DllImport("user32.dll")] private static extern bool GetClientRect(IntPtr hwnd, out Rect rect);
        [DllImport("user32.dll")] private static extern bool ClientToScreen(IntPtr hwnd, ref Point point);
        [DllImport("user32.dll")] private static extern bool SetWindowPos(IntPtr hwnd, IntPtr after, int x, int y, int w, int h, uint flags);
        [DllImport("user32.dll")] private static extern bool ShowWindow(IntPtr hwnd, int command);
        [DllImport("user32.dll")] private static extern bool PostMessage(IntPtr hwnd, uint message, IntPtr wparam, IntPtr lparam);
        [DllImport("user32.dll", EntryPoint = "GetWindowLongW")] private static extern int GetWindowLong(IntPtr hwnd, int index);
        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")] private static extern IntPtr GetWindowLongPtr(IntPtr hwnd, int index);
        [DllImport("user32.dll", EntryPoint = "SetWindowLongW")] private static extern int SetWindowLong(IntPtr hwnd, int index, int value);
        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")] private static extern IntPtr SetWindowLongPtr(IntPtr hwnd, int index, IntPtr value);
        [DllImport("user32.dll")] private static extern IntPtr SetThreadDpiAwarenessContext(IntPtr context);
        [StructLayout(LayoutKind.Sequential)] private struct Rect { public int Left, Top, Right, Bottom; }
        [StructLayout(LayoutKind.Sequential)] private struct Point { public int X, Y; }
    }
}
