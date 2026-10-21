using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using PowerPoint = Microsoft.Office.Interop.PowerPoint;

namespace HjvTak.LiveAndroid
{
    /// <summary>
    /// Early prototype. Places a scrcpy native video window over a named PowerPoint
    /// rectangle. This is NOT a video object embedded into a PPTX.
    /// Must be invoked from the PowerPoint main STA thread.
    /// </summary>
    public sealed class LiveAndroidController : IDisposable
    {
        public const string Version = "0.0.001";
        public const string AnchorName = "HJVTAK_ANDROID_LIVE";
        private readonly PowerPoint.Application app;
        private readonly string scrcpyPath;
        private Process scrcpy;
        private int sequence;

        public LiveAndroidController(PowerPoint.Application application, string scrcpyExecutablePath)
        {
            app = application ?? throw new ArgumentNullException(nameof(application));
            scrcpyPath = scrcpyExecutablePath ?? throw new ArgumentNullException(nameof(scrcpyExecutablePath));
        }

        public void AddAnchorToCurrentSlide()
        {
            var slide = app.ActiveWindow.View.Slide as PowerPoint.Slide;
            if (slide == null) throw new InvalidOperationException("Open a slide in normal editing view.");
            float w = (float)app.ActivePresentation.PageSetup.SlideWidth;
            float h = (float)app.ActivePresentation.PageSetup.SlideHeight;
            var shape = slide.Shapes.AddShape(Microsoft.Office.Core.MsoAutoShapeType.msoShapeRectangle,
                w * .62f, h * .12f, w * .29f, h * .72f);
            shape.Name = AnchorName;
            shape.TextFrame.TextRange.Text = "Android live – " + Version + "\n(visible in slideshow)";
        }

        public void StartDevice(string adbSerial = null)
        {
            StopDevice();
            var title = "HJVTAK-LIVE-" + Process.GetCurrentProcess().Id + "-" +
                        Interlocked.Increment(ref sequence);
            WindowTitle = title;
            var args = "--window-title=" + Quote(title);
            if (!string.IsNullOrWhiteSpace(adbSerial)) args += " --serial=" + Quote(adbSerial);
            scrcpy = Process.Start(new ProcessStartInfo(scrcpyPath, args)
            {
                UseShellExecute = false,
                CreateNoWindow = true
            });
        }

        public string WindowTitle { get; private set; }

        public void AlignToSlideShow()
        {
            var show = app.SlideShowWindows.Count > 0 ? app.SlideShowWindows[1] : null;
            if (show == null) { HideVideo(); return; }
            PowerPoint.Slide slide = show.View.Slide;
            PowerPoint.Shape anchor = null;
            foreach (PowerPoint.Shape s in slide.Shapes)
            {
                if (s.Name == AnchorName) { anchor = s; break; }
            }
            if (anchor == null) { HideVideo(); return; }

            var videoHwnd = FindWindow(null, WindowTitle);
            if (videoHwnd == IntPtr.Zero) return;
            IntPtr showHwnd = new IntPtr(show.HWND);
            if (!GetClientRect(showHwnd, out RECT client)) return;
            POINT p = new POINT { X = 0, Y = 0 };
            if (!ClientToScreen(showHwnd, ref p)) return;

            // Fit the authored slide inside slideshow client area, including letterboxing.
            float sw = (float)app.ActivePresentation.PageSetup.SlideWidth;
            float sh = (float)app.ActivePresentation.PageSetup.SlideHeight;
            double clientW = client.Right - client.Left;
            double clientH = client.Bottom - client.Top;
            double scale = Math.Min(clientW / sw, clientH / sh);
            double offX = (clientW - sw * scale) / 2;
            double offY = (clientH - sh * scale) / 2;
            int x = (int)Math.Round(p.X + offX + anchor.Left * scale);
            int y = (int)Math.Round(p.Y + offY + anchor.Top * scale);
            int w = Math.Max(1, (int)Math.Round(anchor.Width * scale));
            int h = Math.Max(1, (int)Math.Round(anchor.Height * scale));

            // Place a borderless-looking native window over the slide anchor.
            // Do not set TOPMOST: keep PowerPoint and the video in user-controlled z-order.
            SetWindowPos(videoHwnd, IntPtr.Zero, x, y, w, h,
                SWP_NOZORDER | SWP_SHOWWINDOW);
        }

        public void HideVideo()
        {
            if (WindowTitle == null) return;
            var hwnd = FindWindow(null, WindowTitle);
            if (hwnd != IntPtr.Zero) ShowWindow(hwnd, 0);
        }

        public void StopDevice()
        {
            if (scrcpy != null)
            {
                try { if (!scrcpy.HasExited) { scrcpy.CloseMainWindow(); if (!scrcpy.WaitForExit(800)) scrcpy.Kill(); } }
                catch (InvalidOperationException) { }
                finally { scrcpy.Dispose(); scrcpy = null; WindowTitle = null; }
            }
        }

        public void Dispose() { StopDevice(); }

        private static string Quote(string s) => "\"" + s.Replace("\"", "\\\"") + "\"";
        private const uint SWP_NOZORDER = 0x0004;
        private const uint SWP_SHOWWINDOW = 0x0040;
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr FindWindow(string className, string title);
        [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetClientRect(IntPtr hwnd, out RECT rect);
        [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool ClientToScreen(IntPtr hwnd, ref POINT point);
        [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetWindowPos(IntPtr hwnd, IntPtr insertAfter, int x, int y,
            int cx, int cy, uint flags);
        [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool ShowWindow(IntPtr hwnd, int command);
        [StructLayout(LayoutKind.Sequential)]
        private struct RECT { public int Left, Top, Right, Bottom; }
        [StructLayout(LayoutKind.Sequential)]
        private struct POINT { public int X, Y; }
    }
}
