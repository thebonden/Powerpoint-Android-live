using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using AndroidLive;
using NativeWindow = AndroidLive.NativeWindow;
using AndroidLive.Core;

internal static class Program
{
    private static int passed, failed;
    [STAThread]
    private static int Main(string[] args)
    {
        if (args.Length == 2 && args[0] == "--render-brand-help") return BrandHelpTests.RenderSamples(args[1]);
        if (args.Length == 2 && args[0] == "--render-waiting") return FrameWaitingTests.RenderSamples(args[1]);
        if (args.Length > 0 && args[0] == "--probe-live-startup") return LiveStartupProbe.Run(args);
        if (args.Length > 0 && (args[0] == "--fake-mode" || args[0] == "--powerpoint" || args[0] == "-H")) return FakeWifi(args);
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
        Test("hidden startup HWND is not published or resized before scrcpy is ready", () => {
            string gateName="Local\\AndroidLive-TestReady-"+Guid.NewGuid().ToString("N");
            using(var gate=new EventWaitHandle(false,EventResetMode.ManualReset,gateName))
            using(var session=new ScrcpySession()) {
                session.Start(typeof(Program).Assembly.Location,"delayed:"+gateName);
                Await(()=>session.Diagnostics.Contains("startup window created"));
                session.Poll();
                Equal(DeviceState.Starting,session.Status.State);
                Equal(IntPtr.Zero,session.Window);
                session.Hide(); // Must not hide the producer's first-show handshake.
                gate.Set();
                Await(()=>{session.Poll();return session.Status.State==DeviceState.Running;});
                IntPtr ready=session.Window;
                Check(ready!=IntPtr.Zero);
                session.Hide(); session.Poll();
                Equal(ready,session.Window); // Subsequent hiding must not revoke readiness.
                Equal(DeviceState.Running,session.Status.State);
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
        Test("invalid executable launch fails cleanly and can restart", () => {
            string badExe = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "android-live-test-" + Guid.NewGuid().ToString("N") + ".exe");
            System.IO.File.WriteAllText(badExe,"not a Windows executable");
            try
            {
                using (var session = new ScrcpySession())
                {
                    bool failedToStart = false;
                    try { session.Start(badExe,null); } catch (System.ComponentModel.Win32Exception) { failedToStart = true; }
                    Check(failedToStart); Equal(DeviceState.Faulted,session.Status.State); Equal(IntPtr.Zero,session.Window);
                    session.Stop();
                    session.Start(typeof(Program).Assembly.Location,"test");
                    Await(() => { session.Poll(); return session.Status.State == DeviceState.Running; });
                }
            }
            finally { System.IO.File.Delete(badExe); }
        });
        Test("Wi-Fi address accepts explicit IPv4 connection ports", () => {
            foreach (var address in new[] { "192.168.1.50:37123", "10.0.0.8:1", "172.16.0.1:65535" }) Check(WifiEndpoint.IsValid(address));
        });
        Test("Wi-Fi address rejects ambiguous or injected targets", () => {
            foreach (var address in new[] { "", "127.0.0.1:55", "0.0.0.0:55", "224.0.0.1:55", "10.0.0.255:55", "10.0.0.1:0", "10.0.0.1:65536", "010.0.0.1:55", "10.0.0.1:055", "tablet:55", "[::1]:55", "10:0.0.1.55", "10.0.0.1:55 --foo", "10.0.0.1:55\n", "10.0.0.1:55;bad" }) Check(!WifiEndpoint.IsValid(address));
        });
        Test("private helper protocol rejects unrelated and duplicate output", () => {
            Equal("10.0.0.8:4444", WifiEndpoint.ParseHelperResult("ANDROID_PPT_QR_V1 10.0.0.8:4444\r\n"));
            Equal(null, WifiEndpoint.ParseHelperResult(""));
            foreach (var value in new[] { "10.0.0.8:4444", "ANDROID_OBS_QR_V1 10.0.0.8:4444", "ANDROID_PPT_QR_V1 127.0.0.1:55", "ANDROID_PPT_QR_V1 10.0.0.8:4444 extra", "ANDROID_PPT_QR_V1 10.0.0.8:4444\nANDROID_PPT_QR_V1 10.0.0.9:5555" }) Throws(() => WifiEndpoint.ParseHelperResult(value));
        });
        Test("Wi-Fi QR success revalidates connection using local ADB", () => {
            string exe=typeof(Program).Assembly.Location;
            Equal("10.0.0.8:4444", new WifiConnector(exe,exe).PairAsync(System.Threading.CancellationToken.None).GetAwaiter().GetResult());
        });
        Test("closing QR returns no endpoint", () => WithWifiCase("cancel", () => {
            string exe=typeof(Program).Assembly.Location;
            Equal(null,new WifiConnector(exe,exe).PairAsync(System.Threading.CancellationToken.None).GetAwaiter().GetResult());
        }));
        Test("QR malformed output cannot launch a connection", () => WithWifiCase("malformed", () => {
            string exe=typeof(Program).Assembly.Location;
            Throws(() => new WifiConnector(exe,exe).PairAsync(System.Threading.CancellationToken.None).GetAwaiter().GetResult());
        }));
        Test("QR helper nonzero exit cannot be accepted", () => WithWifiCase("failure", () => {
            string exe=typeof(Program).Assembly.Location;
            Throws(() => new WifiConnector(exe,exe).PairAsync(System.Threading.CancellationToken.None).GetAwaiter().GetResult());
        }));
        Test("Wi-Fi reconnect rejects offline device even with exit zero", () => WithWifiCase("offline", () => {
            string exe=typeof(Program).Assembly.Location;
            Throws(() => new WifiConnector(exe,exe).ConnectAsync("10.0.0.8:4444",System.Threading.CancellationToken.None).GetAwaiter().GetResult());
        }));
        Test("invalid Wi-Fi target is rejected before a process starts", () => {
            Throws(() => new WifiConnector("missing.exe","missing.exe").ConnectAsync("10.0.0.8:4444 --evil",System.Threading.CancellationToken.None).GetAwaiter().GetResult());
        });
        Test("command cancellation terminates only its own child", () => CheckCommandCancellation(false));
        Test("command timeout terminates a stalled child", () => CheckCommandCancellation(true));
        Test("connection output is bounded", () => {
            bool rejected=false;
            try { LocalCommand.RunAsync(typeof(Program).Assembly.Location,"--fake-mode flood",System.Threading.CancellationToken.None,TimeSpan.FromSeconds(3)).GetAwaiter().GetResult(); }
            catch(System.IO.IOException) { rejected=true; }
            Check(rejected);
        });
        Test("pre-cancelled connection never launches", () => {
            using(var cancel=new CancellationTokenSource()) {
                cancel.Cancel(); bool cancelled=false;
                try { LocalCommand.RunAsync("missing.exe","",cancel.Token,TimeSpan.FromSeconds(3)).GetAwaiter().GetResult(); }
                catch(OperationCanceledException){cancelled=true;} Check(cancelled);
            }
        });
        ScrcpyDiscoveryTests.Run(Test);
        MultiDeviceTests.Run(Test);
        CompatibilityTests.Run(Test);
        DeviceDiscoveryTests.Run(Test);
        ConnectionRecoveryTests.Run(Test);
        OfficeUiContextTests.Run(Test);
        FrameWaitingTests.Run(Test);
        BrandHelpTests.Run(Test);
        Test("QR helper resolves outside the actual CLR shadow-copy folder", ShadowCopyProbe.Test);
        Console.WriteLine("Tests: " + passed + " passed, " + failed + " failed.");
        return failed == 0 ? 0 : 1;
    }
    private static void WithWifiCase(string value,Action test)
    {
        string previous=Environment.GetEnvironmentVariable("PPT_WIFI_TEST_CASE");
        try { Environment.SetEnvironmentVariable("PPT_WIFI_TEST_CASE",value); test(); }
        finally { Environment.SetEnvironmentVariable("PPT_WIFI_TEST_CASE",previous); }
    }
    private static int FakeWifi(string[] args)
    {
        if(args[0]=="--fake-mode") {
            if(args[1]=="flood") { Console.Write(new string('x',40000)); return 0; }
            System.IO.File.WriteAllText(args[2],Process.GetCurrentProcess().Id.ToString());
            Thread.Sleep(30000); return 0;
        }
        string scenario=Environment.GetEnvironmentVariable("PPT_WIFI_TEST_CASE");
        if(args[0]=="--powerpoint") {
            if(args.Length!=3 || args[1]!="--adb" || args[2]!=typeof(Program).Assembly.Location)return 3;
            if(scenario=="failure")return 2;
            if(scenario!="cancel")Console.WriteLine(scenario=="malformed" ? "untrusted output" : "ANDROID_PPT_QR_V1 10.0.0.8:4444");
            return 0;
        }
        if(!args.Take(4).SequenceEqual(new[]{"-H","localhost","-P","5037"}) ||
           Environment.GetEnvironmentVariable("ADB_SERVER_SOCKET")!="tcp:localhost:5037" ||
           Environment.GetEnvironmentVariable("ADB")!=typeof(Program).Assembly.Location)return 3;
        if(args.Skip(4).SequenceEqual(new[]{"-s","10.0.0.8:4444","shell","wm","size"})) { Console.WriteLine("Physical size: 1200x1920"); return 0; }
        if(args.Skip(4).SequenceEqual(new[]{"connect","10.0.0.8:4444"})) { Console.WriteLine("connected"); return 0; }
        if(args.Skip(4).SequenceEqual(new[]{"-s","10.0.0.8:4444","get-state"})) { Console.WriteLine(scenario=="offline"?"offline":"device"); return 0; }
        return 4;
    }
    private static void CheckCommandCancellation(bool timeout)
    {
        string path=System.IO.Path.Combine(System.IO.Path.GetTempPath(),"ppt-wifi-pid-"+Guid.NewGuid().ToString("N"));
        try {
            using(var cancel=new CancellationTokenSource()) {
                var task=LocalCommand.RunAsync(typeof(Program).Assembly.Location,"--fake-mode wait "+ScrcpyArguments.Quote(path),cancel.Token,TimeSpan.FromSeconds(timeout?1:10));
                Await(()=>System.IO.File.Exists(path) && new System.IO.FileInfo(path).Length>0);
                int pid=int.Parse(System.IO.File.ReadAllText(path));
                if(!timeout)cancel.Cancel();
                bool rejected=false;
                try { task.GetAwaiter().GetResult(); }
                catch(OperationCanceledException){rejected=!timeout;}
                catch(TimeoutException){rejected=timeout;}
                Check(rejected);
                try { using(var process=Process.GetProcessById(pid))Check(process.HasExited); }
                catch(ArgumentException) { }
            }
        } finally { if(System.IO.File.Exists(path))System.IO.File.Delete(path); }
    }
    private static int FakeScrcpy(string title, string[] args)
    {
        if (args.Contains("--serial=fail")) { Console.Error.WriteLine("simulated device disconnect"); return 2; }
        using (var window = new Form { Text=title, StartPosition=FormStartPosition.Manual, Location=new Point(-32000,-32000), Size=new Size(320,240), ShowInTaskbar=false })
        {
            // Dead-man timer avoids an orphan helper if the test runner is interrupted.
            var timer = new System.Windows.Forms.Timer { Interval=15000 };
            timer.Tick += (s,e) => window.Close(); timer.Start();
            string delayed=args.FirstOrDefault(a=>a.StartsWith("--serial=delayed:"));
            if(delayed!=null) {
                // SDL creates a hidden HWND before video dimensions exist. Simulate that
                // interval separately from Application.Run(form), which shows immediately.
                using(var context=new ApplicationContext())
                using(var gate=EventWaitHandle.OpenExisting(delayed.Substring("--serial=delayed:".Length)))
                using(var ready=new System.Windows.Forms.Timer{Interval=25}) {
                    IntPtr initial=window.Handle;
                    Console.Error.WriteLine("startup window created");
                    ready.Tick+=(s,e)=>{if(gate.WaitOne(0)){ready.Stop();window.Show();}};
                    window.FormClosed+=(s,e)=>context.ExitThread();
                    ready.Start();Application.Run(context);
                }
            } else Application.Run(window);
            timer.Dispose();
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
