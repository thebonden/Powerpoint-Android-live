using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using AndroidLive;
using PowerPoint = Microsoft.Office.Interop.PowerPoint;
using Office = Microsoft.Office.Core;

internal static class Program
{
    private static int assertions;
    [STAThread]
    private static int Main(string[] args)
    {
        if (Process.GetProcessesByName("POWERPNT").Length != 0)
        { Console.Error.WriteLine("Close PowerPoint before this opt-in smoke test; existing sessions are never used."); return 2; }
        if (args.Length != 1 || !File.Exists(args[0])) { Console.Error.WriteLine("Pass the full path to AndroidLive.Tests.exe (fake scrcpy)."); return 2; }
        PowerPoint.Application app = null;
        PowerPoint.Presentation deck = null;
        LiveAndroidController controller = null;
        try
        {
            app = new PowerPoint.Application(); app.Visible = Office.MsoTriState.msoTrue;
            deck = app.Presentations.Add(Office.MsoTriState.msoTrue);
            deck.PageSetup.SlideWidth = 960; deck.PageSetup.SlideHeight = 540;
            deck.Slides.Add(1, PowerPoint.PpSlideLayout.ppLayoutBlank);
            deck.Slides.Add(2, PowerPoint.PpSlideLayout.ppLayoutBlank);
            app.ActiveWindow.ViewType = PowerPoint.PpViewType.ppViewNormal;
            app.ActiveWindow.View.GotoSlide(1);
            controller = new LiveAndroidController(app);
            controller.AddAnchorToCurrentSlide(); controller.AddAnchorToCurrentSlide();
            Check(deck.Slides[1].Shapes.Count == 1,"marker insertion is idempotent");
            controller.StartDevice(Path.GetFullPath(args[0]),"test");
            Wait(() => VideoWindow(controller) != IntPtr.Zero);
            Pump(200); Check(!IsWindowVisible(VideoWindow(controller)),"video stays hidden in editing view");
            deck.SlideShowSettings.ShowType = PowerPoint.PpSlideShowType.ppShowTypeWindow;
            var show = deck.SlideShowSettings.Run();
            app.Activate(); show.Activate();
            SetForegroundWindow(new IntPtr(show.HWND));
            uint hostPid, foregroundPid;
            GetWindowThreadProcessId(new IntPtr(show.HWND), out hostPid);
            GetWindowThreadProcessId(GetForegroundWindow(), out foregroundPid);
            if (hostPid != foregroundPid)
            {
                Console.Error.WriteLine("INCOMPLETE: Windows denied foreground activation. Run this opt-in test from an interactive desktop. No visible-overlay assertions have passed.");
                return 2;
            }
            Wait(() => IsWindowVisible(VideoWindow(controller)));
            Check(controller.Status.Contains("Overlay visible"),"overlay appears on marked slide");
            show.View.GotoSlide(2);
            Wait(() => !IsWindowVisible(VideoWindow(controller)));
            Check(!IsWindowVisible(VideoWindow(controller)),"overlay hides on unmarked slide");
            show.View.GotoSlide(1);
            Wait(() => IsWindowVisible(VideoWindow(controller)));
            show.View.State = PowerPoint.PpSlideShowState.ppSlideShowBlackScreen;
            Wait(() => !IsWindowVisible(VideoWindow(controller)));
            Check(!IsWindowVisible(VideoWindow(controller)),"black screen hides overlay");
            show.View.State = PowerPoint.PpSlideShowState.ppSlideShowWhiteScreen;
            Pump(150); Check(!IsWindowVisible(VideoWindow(controller)),"white screen hides overlay");
            show.View.State = PowerPoint.PpSlideShowState.ppSlideShowRunning;
            Wait(() => IsWindowVisible(VideoWindow(controller)));
            controller.ToggleOverlay(); Pump(150);
            Check(!IsWindowVisible(VideoWindow(controller)),"manual hide persists");
            controller.ToggleOverlay(); Wait(() => IsWindowVisible(VideoWindow(controller)));
            show.View.Exit(); Pump(250);
            Check(!IsWindowVisible(VideoWindow(controller)),"show end hides video");
            controller.StopDevice(); Check(!controller.IsActive,"stop releases device session");
            Console.WriteLine("Office smoke: " + assertions + " assertions passed. PowerPoint " + app.Version);
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); Console.Error.WriteLine(controller?.Status); Console.Error.WriteLine(controller?.Diagnostics); return 1; }
        finally
        {
            controller?.Dispose();
            if (deck != null) { deck.Saved = Office.MsoTriState.msoTrue; deck.Close(); Marshal.ReleaseComObject(deck); }
            if (app != null) { app.Quit(); Marshal.ReleaseComObject(app); }
        }
    }
    private static IntPtr VideoWindow(LiveAndroidController controller)
    {
        return (IntPtr)typeof(LiveAndroidController).GetMethod("VideoWindow",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(controller,new object[]{"legacy"});
    }
    private static void Wait(Func<bool> ready)
    {
        var clock = Stopwatch.StartNew();
        while (!ready()) { if (clock.ElapsedMilliseconds > 5000) throw new Exception("Timed out waiting for Office/overlay state"); Pump(30); }
    }
    private static void Pump(int milliseconds)
    {
        var clock = Stopwatch.StartNew();
        while (clock.ElapsedMilliseconds < milliseconds) { System.Windows.Forms.Application.DoEvents(); Thread.Sleep(10); }
    }
    private static void Check(bool value,string name) { if (!value) throw new Exception(name); assertions++; Console.WriteLine("PASS " + name); }
    [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr hwnd, out uint pid);
    [DllImport("user32.dll")] private static extern bool IsWindowVisible(IntPtr hwnd);
    [DllImport("user32.dll")] private static extern bool SetForegroundWindow(IntPtr hwnd);
}
