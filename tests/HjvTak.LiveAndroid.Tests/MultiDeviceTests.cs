using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Xml.Serialization;
using HjvTak.LiveAndroid;
using HjvTak.LiveAndroid.Core;

internal static class MultiDeviceTests
{
    internal static void Run(Action<string,Action> test)
    {
        test("stream options: independent audio/touch/orientation combinations",()=>{
            foreach(bool audio in new[]{false,true})
            foreach(bool touches in new[]{false,true})
            foreach(string angle in new[]{null,"@0","@90"}){
                string args=ScrcpyArguments.Build("test","10.0.0.8:4444",audio,touches,angle);
                Check(args.Contains("--no-audio")==!audio);
                Check(args.Contains("--show-touches")==touches);
                Check(args.Contains("--no-control")==!touches);
                if(touches)Check(args.Contains("--keyboard=disabled")&&args.Contains("--mouse=disabled")&&args.Contains("--gamepad=disabled")&&args.Contains("--no-clipboard-autosync"));
                Check(!args.Contains("--audio-source=mic"));
                Check(args.Contains("--capture-orientation=")==(angle!=null));
            }
            bool rejected=false;try{ScrcpyArguments.Build("test","x",true,true,"@0 --record=x");}catch(ArgumentException){rejected=true;}Check(rejected);
        });
        test("orientation respects natural portrait and landscape dimensions",()=>{
            Check(CaptureOrientation.FromSize("Physical size: 1200x1920\n",VideoOrientation.Portrait)=="@0");
            Check(CaptureOrientation.FromSize("Physical size: 1200x1920\n",VideoOrientation.Landscape)=="@90");
            Check(CaptureOrientation.FromSize("Physical size: 1920x1200\r\n",VideoOrientation.Landscape)=="@0");
            Check(CaptureOrientation.FromSize("Physical size: 1920x1200\n",VideoOrientation.Portrait)=="@90");
            Check(CaptureOrientation.FromSize("Physical size: 1200x1920\nOverride size: 1920x1200\n",VideoOrientation.Landscape)=="@0");
            Check(CaptureOrientation.FromSize("anything",VideoOrientation.Automatic)==null);
            foreach(string output in new[]{"","wm: permission denied","Physical size: 0x1920","Physical size: 100x100","Physical size: 1200x1920 extra"}){
                bool rejected=false;try{CaptureOrientation.FromSize(output,VideoOrientation.Portrait);}catch(InvalidOperationException){rejected=true;}Check(rejected);
            }
        });
        test("orientation ADB query is scoped, validated and cancellable",()=>{
            string exe=typeof(MultiDeviceTests).Assembly.Location;
            Check(CaptureOrientation.ResolveAsync(exe,"10.0.0.8:4444",VideoOrientation.Landscape,CancellationToken.None).GetAwaiter().GetResult()=="@90");
            Check(CaptureOrientation.ResolveAsync("missing.exe","",VideoOrientation.Automatic,CancellationToken.None).GetAwaiter().GetResult()==null);
            using(var cts=new CancellationTokenSource()){
                cts.Cancel();bool rejected=false;
                try{CaptureOrientation.ResolveAsync("missing.exe","10.0.0.8:4444",VideoOrientation.Portrait,cts.Token).GetAwaiter().GetResult();}
                catch(OperationCanceledException){rejected=true;}Check(rejected);
            }
        });
        test("old settings migrate to a stable device without changing the paired endpoint",()=>{
            var settings=new UserSettings{Serial="10.0.0.8:4444"};settings.Normalize();
            Check(settings.Devices.Count==1&&settings.Selected.Id=="legacy"&&settings.Selected.Endpoint==settings.Serial);
            Check(!settings.Selected.Audio&&!settings.Selected.ShowTouches&&settings.Selected.Orientation==VideoOrientation.Automatic);
            settings.Normalize();Check(settings.Devices.Count==1);
        });
        test("multiple device preferences survive XML round trip independently",()=>{
            var settings=new UserSettings();
            settings.Devices.Add(new DeviceProfile{Id="a",Name="Tablet Æ",Endpoint="10.0.0.8:4444",Audio=true,Orientation=VideoOrientation.Landscape});
            settings.Devices.Add(new DeviceProfile{Id="b",Name="Tablet 2",Endpoint="10.0.0.9:5555",ShowTouches=true,Orientation=VideoOrientation.Portrait});
            settings.SelectedDeviceId="b";
            var serializer=new XmlSerializer(typeof(UserSettings));
            var writer=new StringWriter();serializer.Serialize(writer,settings);
            var copy=(UserSettings)serializer.Deserialize(new StringReader(writer.ToString()));copy.Normalize();
            Check(copy.Devices.Count==2&&copy.Selected.Id=="b"&&!copy.Selected.Audio&&copy.Selected.ShowTouches);
            Check(copy.Devices[0].Audio&&copy.Devices[0].Name=="Tablet Æ"&&copy.Devices[0].Orientation==VideoOrientation.Landscape);
        });
        test("two live processes remain independent across stop, restart and failure",()=>{
            string exe=typeof(MultiDeviceTests).Assembly.Location;
            using(var streams=new DeviceStreams()){
                var first=streams.Start("a",exe,"test-a",false,true,"@0");
                var second=streams.Start("b",exe,"test-b",true,false,"@90");
                Await(()=>{first.Poll();second.Poll();return first.Status.State==DeviceState.Running&&second.Status.State==DeviceState.Running;});
                IntPtr firstWindow=first.Window,secondWindow=second.Window;Check(firstWindow!=secondWindow);
                streams.Stop("a");Await(()=>!NativeWindow.IsWindow(firstWindow));second.Poll();
                Check(second.Window==secondWindow&&second.Status.State==DeviceState.Running);
                streams.Start("a",exe,"fail",false,false,null);
                Await(()=>{first.Poll();return first.Status.State==DeviceState.Faulted;});
                second.Poll();Check(second.Window==secondWindow&&second.Status.State==DeviceState.Running);
                streams.Start("a",exe,"test-a",false,false,null);
                Await(()=>{first.Poll();return first.Status.State==DeviceState.Running;});
                Check(first.Window!=secondWindow);streams.HideAll();
                Check(!NativeWindow.IsWindowVisible(first.Window)&&!NativeWindow.IsWindowVisible(second.Window));
            }
        });
    }
    private static void Check(bool ok){if(!ok)throw new Exception("Assertion failed");}
    private static void Await(Func<bool> condition){
        var clock=Stopwatch.StartNew();
        while(!condition()){if(clock.ElapsedMilliseconds>5000)throw new Exception("Timed out");Thread.Sleep(25);}
    }
}