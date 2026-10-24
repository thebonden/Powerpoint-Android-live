using System;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using AndroidLive;
using AndroidLive.Core;
using VideoWindow = AndroidLive.NativeWindow;

// Opt-in only. Uses an already paired tablet; does not access Office, pair, record,
// save settings or touch other scrcpy/ADB processes.
internal static class LiveStartupProbe
{
    internal static int Run(string[] args)
    {
        if(args.Length!=3 || !WifiEndpoint.IsValid(args[2]))return 2;
        try {
            using(var host=new Form{Text="Android Live · Test af billedopstart",
                StartPosition=FormStartPosition.Manual,Location=new Point(80,80),ClientSize=new Size(640,400)})
            using(var session=new ScrcpySession()) {
                host.Show();
                for(int attempt=1;attempt<=3;attempt++) {
                    session.Start(args[1],args[2]);
                    var clock=Stopwatch.StartNew();
                    bool texture=false,placed=false,hidden=false,resumed=false;
                    while(clock.ElapsedMilliseconds<8000) {
                        Application.DoEvents();session.Poll();
                        if(session.Status.State==DeviceState.Faulted)
                            throw new Exception("Attempt "+attempt+": "+session.Status.Detail);
                        if(session.Status.State==DeviceState.Running) {
                            texture|=session.Diagnostics.Contains("Texture:");
                            if(clock.ElapsedMilliseconds>=4000 && clock.ElapsedMilliseconds<4500) {
                                session.Hide();hidden=true;
                            } else {
                                var bounds=host.RectangleToScreen(host.ClientRectangle);
                                if(clock.ElapsedMilliseconds>=5000)bounds.Width-=80;
                                if(!VideoWindow.Place(session.Window,host.Handle,bounds))throw new Exception("Window placement failed");
                                placed=true;if(hidden)resumed=true;
                            }
                        }
                        Thread.Sleep(100);
                    }
                    if(!texture||!placed||!hidden||!resumed)throw new Exception("Incomplete video/resize/hide/resume evidence");
                    session.Stop();
                    Console.WriteLine("PASS live startup "+attempt+": texture, placement, resize, hide/resume and stop");
                }
            }
            return 0;
        }catch(Exception ex){Console.Error.WriteLine(ex.Message);return 1;}
    }
}