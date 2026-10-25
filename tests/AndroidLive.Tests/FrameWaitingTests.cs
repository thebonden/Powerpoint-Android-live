using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using AndroidLive;
using AndroidLive.Core;
using NativeWindow = AndroidLive.NativeWindow;

internal static class FrameWaitingTests
{
    internal static void Run(Action<string,Action> test)
    {
        test("waiting frame hides BOTH text and icon until exactly 15 seconds",()=>{
            var state=new FrameWaitingState();
            Equal(FrameDisplay.Blank,state.Update("slide1","a",false,At(20)));
            Equal(FrameDisplay.Blank,state.Update("slide1","a",false,At(34.999)));
            Equal(FrameDisplay.Waiting,state.Update("slide1","a",false,At(35)));
        });
        test("live video is immediate and a later outage gets a fresh delay",()=>{
            var state=new FrameWaitingState();
            Equal(FrameDisplay.Blank,state.Update("slide1","a",false,At(0)));
            Equal(FrameDisplay.Live,state.Update("slide1","a",true,At(2)));
            Equal(FrameDisplay.Live,state.Update("slide1","a",true,At(60)));
            Equal(FrameDisplay.Blank,state.Update("slide1","a",false,At(61)));
            Equal(FrameDisplay.Blank,state.Update("slide1","a",false,At(75)));
            Equal(FrameDisplay.Waiting,state.Update("slide1","a",false,At(76)));
            Equal(FrameDisplay.Live,state.Update("slide1","a",true,At(76.1)));
        });
        test("each device waits independently and repeated retries do not postpone text",()=>{
            var state=new FrameWaitingState();
            state.Update("slide1","a",false,At(0));
            Equal(FrameDisplay.Live,state.Update("slide1","b",true,At(0)));
            Equal(FrameDisplay.Blank,state.Update("slide1","b",false,At(10)));
            for(int t=1;t<=14;t++)Equal(FrameDisplay.Blank,state.Update("slide1","a",false,At(t)));
            Equal(FrameDisplay.Waiting,state.Update("slide1","a",false,At(15)));
            Equal(FrameDisplay.Blank,state.Update("slide1","b",false,At(15)));
            Equal(FrameDisplay.Waiting,state.Update("slide1","b",false,At(25)));
        });
        test("forward, backward and new slideshow reset the waiting interval",()=>{
            var state=new FrameWaitingState();
            state.Update("show1:slide1","a",false,At(0));
            Equal(FrameDisplay.Waiting,state.Update("show1:slide1","a",false,At(16)));
            Equal(FrameDisplay.Blank,state.Update("show1:slide2","a",false,At(17)));
            Equal(FrameDisplay.Blank,state.Update("show1:slide1","a",false,At(18)));
            Equal(FrameDisplay.Waiting,state.Update("show1:slide1","a",false,At(33)));
            state.Reset();
            Equal(FrameDisplay.Blank,state.Update("show1:slide1","a",false,At(50)));
            Equal(FrameDisplay.Blank,state.Update("show2:slide1","a",false,At(100)));
        });
        test("waiting surface is completely blank before delay and paints names after delay",()=>{
            using(var cover=new WaitingWindow()) {
                cover.ClientSize=new Size(600,360);
                cover.SetContent(false,"TABLET001 — Instruktør");
                using(var blank=Render(cover))Check(IsBlank(blank));
                cover.SetContent(true,"TABLET001 — Instruktør");
                Check(cover.WaitingText.Contains("TABLET001 — Instruktør"));
                using(var waiting=Render(cover))Check(!IsBlank(waiting));
                cover.SetContent(false,"TABLET001 — Instruktør");
                using(var reset=Render(cover))Check(IsBlank(reset));
            }
        });
        test("waiting window places, hides, detaches and disposes without taking focus",()=>{
            IntPtr before=GetForegroundWindow(), handle;
            using(var host=new WaitingWindow())
            using(var cover=new WaitingWindow()) {
                Check(host.Place(IntPtr.Zero,new Rectangle(-32000,-32000,600,400),false,"host"));
                handle=cover.Handle;
                Check(cover.Place(host.Handle,new Rectangle(-32000,-32000,300,200),false,"Tablet"));
                Check(NativeWindow.IsWindowVisible(handle));
                Equal(before,GetForegroundWindow());
                cover.Conceal();Check(!NativeWindow.IsWindowVisible(handle));
                Check(cover.Place(host.Handle,new Rectangle(-32000,-32000,200,300),true,"Tablet"));
                Equal(before,GetForegroundWindow());
            }
            Check(!NativeWindow.IsWindow(handle));
        });
    }
    internal static int RenderSamples(string directory)
    {
        Directory.CreateDirectory(directory);
        using(var window=new WaitingWindow()) {
            foreach(var size in new[]{new Size(600,360),new Size(240,150),new Size(720,240)}) {
                window.ClientSize=size;
                foreach(bool waiting in new[]{false,true}) {
                    window.SetContent(waiting,"TABLET001 — Instruktør");
                    using(var bitmap=Render(window))
                        bitmap.Save(Path.Combine(directory,(waiting?"waiting-":"blank-")+size.Width+".png"));
                }
            }
        }
        return 0;
    }
    private static Bitmap Render(WaitingWindow window)
    {
        var bitmap=new Bitmap(window.ClientSize.Width,window.ClientSize.Height);
        window.DrawToBitmap(bitmap,new Rectangle(Point.Empty,window.ClientSize));return bitmap;
    }
    private static bool IsBlank(Bitmap bitmap)
    {
        for(int y=0;y<bitmap.Height;y++)
            for(int x=0;x<bitmap.Width;x++)
                if(bitmap.GetPixel(x,y).ToArgb()!=WaitingWindow.SurfaceColor.ToArgb())return false;
        return true;
    }
    private static TimeSpan At(double seconds)=>TimeSpan.FromSeconds(seconds);
    private static void Check(bool condition){if(!condition)throw new Exception("Assertion failed");}
    private static void Equal<T>(T expected,T actual){if(!Equals(expected,actual))throw new Exception("Expected "+expected+", got "+actual);}
    [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
}