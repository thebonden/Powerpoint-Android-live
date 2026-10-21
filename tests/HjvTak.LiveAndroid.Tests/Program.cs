using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using HjvTak.LiveAndroid;
using NativeWindow = HjvTak.LiveAndroid.NativeWindow;
using HjvTak.LiveAndroid.Core;

internal static class Program
{
    private static int passed, failed;
    [STAThread]
    private static int Main(string[] args)
    {
        // This executable doubles as a fake scrcpy, so integration tests exercise real child-process cleanup.
        string title = args.FirstOrDefault(a => a.StartsWith("--window-title="));
        if (title != null) return FakeScrcpy(title.Substring("--window-title=".Length), args);
        Test("16:9 slide on 4:3 screen is letterboxed", () => Map(new Rectangle(0, 0, 1600, 1200), 960, 540, 0, 0, 960, 540, new Rectangle(0, 150, 1600, 900)));
        Test("4:3 slide on wide screen is pillarboxed", () => Map(new Rectangle(0, 0, 1920, 1080), 720, 540, 0, 0, 720, 540, new Rectangle(240, 0, 1440, 1080)));
        Test("negative monitor origin and scaled marker", () => Map(new Rectangle(-1920, -200, 1920, 1080), 960, 540, 96, 54, 480, 270, new Rectangle(-1728, -92, 960, 540)));
        Test("portrait slide and moved window", () => Map(new Rectangle(100, 50, 800, 600), 300, 600, 0, 0, 300, 600, new Rectangle(350, 50, 300, 600)));
        Test("invalid/partly off-slide markers fail closed", () => {
            Rectangle result;
            foreach (double bad in new[] { -1d, 0d, double.NaN, double.PositiveInfinity })
                Check(!OverlayGeometry.TryMap(new Rectangle(0,0,1920,1080), bad, 540, 0,0,100,100,out result));
            Check(!OverlayGeometry.TryMap(new Rectangle(0,0,0,1080),960,540,0,0,100,100,out result));
            Check(!OverlayGeometry.TryMap(new Rectangle(0,0,1920,1080),960,540,900,0,100,100,out result));
            Check(!OverlayGeometry.TryMap(new Rectangle(0,0,1920,1080),960,540,-1,0,100,100,out result));
        });
        Test("every visibility guard hides independently", () => {
            for (int mask = 0; mask < 128; mask++)
                Equal(mask == 127, OverlayPolicy.ShouldShow((mask&1)!=0,(mask&2)!=0,(mask&4)!=0,(mask&8)!=0,(mask&16)!=0,(mask&32)!=0,(mask&64)!=0));
        });
        Test("Windows argument round-trip cannot inject options", () => {
            foreach (var value in new[] { "", "phone serial", "abc\\", "abc\\\\", "a\"b", "a\\\"b\\", "--serial=x --always-on-top", "telefonæøå" })
            {
                int count;
                IntPtr list = CommandLineToArgvW("scrcpy.exe " + ScrcpyArguments.Quote(value), out count);
                try { Equal(2,count); Equal(value,Marshal.PtrToStringUni(Marshal.ReadIntPtr(list, IntPtr.Size))); }
                finally { LocalFree(list); }
            }
            Throws(() => ScrcpyArguments.Quote("x\ny"));
        });
        Test("arguments are video-only and omit blank serial", () => {
            var value = ScrcpyArguments.Build("test", " ");
            Check(value.Contains("--no-audio") && value.Contains("--no-control") && value.Contains("--window-borderless"));
            Check(!value.Contains("--serial="));
        });
        Test("startup timeout is bounded and remains faulted", () => {
            var state = new SessionState(); var now = DateTime.UtcNow;
            state.Start(now); Check(!state.Poll(now.AddSeconds(19),false,0,false));
            Check(state.Poll(now.AddSeconds(20),false,0,false)); Equal(DeviceState.Faulted,state.State);
            Check(!state.Poll(now.AddSeconds(21),false,0,true)); Equal(DeviceState.Faulted,state.State);
        });
        Test("exit, disconnect, stop and reconnect transitions", () => {
            var state = new SessionState(); var now = DateTime.UtcNow;
            state.Start(now); Check(state.Poll(now,true,1,false)); Equal(DeviceState.Faulted,state.State);
            state.Start(now); Check(!state.Poll(now,false,0,true)); Equal(DeviceState.Running,state.State);
            Check(state.Poll(now,false,0,false)); Equal(DeviceState.Faulted,state.State);
            state.Stop(); state.Stop(); Equal(DeviceState.Stopped,state.State);
            state.Start(now); state.Poll(now,false,0,true); Check(state.Poll(now,true,2,false));
        });
        Test("real process: start, exact PID window, hide, stop, restart", () => {
            using (var session = new ScrcpySession())
            {
                string exe = typeof(Program).Assembly.Location;
                session.Start(exe, "test"); Await(() => { session.Poll(); return session.Status.State == DeviceState.Running; });
                IntPtr hwnd = session.Window;
                Check(hwnd != IntPtr.Zero); Check(!NativeWindow.IsOwnedBy(hwnd, Process.GetCurrentProcess().Id));
                session.Hide(); Check(!NativeWindow.IsWindowVisible(hwnd));
                session.Stop(); session.Stop(); Await(() => !NativeWindow.IsWindow(hwnd));
                session.Start(exe,"test"); Await(() => { session.Poll(); return session.Status.State == DeviceState.Running; });
                session.Stop(); Equal(DeviceState.Stopped,session.Status.State);
            }
        });
        Test("real process: failing start retains diagnostics", () => {
            using (var session = new ScrcpySession())
            {
                session.Start(typeof(Program).Assembly.Location,"fail");
                Await(() => { session.Poll(); return session.Status.State == DeviceState.Faulted; });
                Check(session.Status.Detail.Contains("code 2"));
                Check(session.Diagnostics.Contains("simulated device disconnect"));
            }
        });
        Test("missing executable fails before launching", () => {
            using (var session = new ScrcpySession()) { Throws(() => session.Start("missing.exe",null)); Equal(IntPtr.Zero,session.Window); }
        });
        Console.WriteLine("Tests: " + passed + " passed, " + failed + " failed.");
        return failed == 0 ? 0 : 1;
    }
    private static int FakeScrcpy(string title, string[] args)
    {
        if (args.Contains("--serial=fail")) { Console.Error.WriteLine("simulated device disconnect"); return 2; }
        using (var window = new Form { Text=title, StartPosition=FormStartPosition.Manual, Location=new Point(-32000,-32000), Size=new Size(320,240), ShowInTaskbar=false })
        {
            // Dead-man timer avoids an orphan helper if the test runner is interrupted.
            var timer = new System.Windows.Forms.Timer { Interval=15000 };
            timer.Tick += (s,e) => window.Close(); timer.Start();
            Application.Run(window); timer.Dispose();
        }
        return 0;
    }
    private static void Test(string name, Action test)
    {
        try { test(); passed++; Console.WriteLine("PASS " + name); }
        catch (Exception ex) { failed++; Console.WriteLine("FAIL " + name + ": " + ex.Message); }
    }
    private static void Await(Func<bool> ready)
    {
        var clock = Stopwatch.StartNew();
        while (!ready()) { if (clock.ElapsedMilliseconds > 5000) throw new Exception("Timed out"); Thread.Sleep(30); }
    }
    private static void Map(Rectangle client,double sw,double sh,double x,double y,double w,double h,Rectangle expected)
    { Rectangle result; Check(OverlayGeometry.TryMap(client,sw,sh,x,y,w,h,out result)); Equal(expected,result); }
    private static void Check(bool value) { if (!value) throw new Exception("Assertion failed"); }
    private static void Equal<T>(T expected,T actual) { if (!EqualityComparer<T>.Default.Equals(expected,actual)) throw new Exception("Expected " + expected + ", got " + actual); }
    private static void Throws(Action action) { try { action(); } catch (ArgumentException) { return; } catch (InvalidOperationException) { return; } throw new Exception("Expected exception"); }
    [DllImport("shell32.dll", CharSet=CharSet.Unicode)] private static extern IntPtr CommandLineToArgvW(string command,out int count);
    [DllImport("kernel32.dll")] private static extern IntPtr LocalFree(IntPtr value);
}
