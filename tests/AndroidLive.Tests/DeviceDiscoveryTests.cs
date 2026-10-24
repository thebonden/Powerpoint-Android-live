using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.IO;
using AndroidLive;
using AndroidLive.Core;
internal static class DeviceDiscoveryTests
{
    internal static void Run(Action<string,Action> test)
    {
        test("explicit QR update keeps slide identity, name and settings",()=>{
            var settings=new UserSettings();
            var profile=settings.AcceptConnection(null,new DeviceConnection{Endpoint="10.0.0.8:4444",Name="Tablet001",Serial="one"});
            string id=profile.Id;profile.Name="Mit navn";profile.Audio=true;
            var updated=settings.AcceptConnection(profile,new DeviceConnection{Endpoint="10.0.0.9:5555",Name="Android name",Serial="one"});
            Check(updated.Id==id&&updated.Name=="Mit navn"&&updated.Audio&&settings.Devices.Count==1);
            Check(settings.AcceptConnection(null,new DeviceConnection{Endpoint="10.0.0.10:6666",Name="Android name",Serial="one"})==profile);
        });
        test("new tablet at reused IP does not steal another tablet's saved slides",()=>{
            var settings=new UserSettings();
            var first=settings.AcceptConnection(null,new DeviceConnection{Endpoint="10.0.0.8:4444",Name="First",Serial="one"});
            var second=settings.AcceptConnection(null,new DeviceConnection{Endpoint="10.0.0.8:4444",Name="Second",Serial="two"});
            Check(first.Id!=second.Id&&first.Name=="First"&&first.HardwareSerial=="one"&&settings.Devices.Count==2);
            bool refused=false;
            try{settings.AcceptConnection(first,new DeviceConnection{Endpoint="10.0.0.9:5555",Serial="two"});}
            catch(InvalidOperationException){refused=true;}Check(refused&&first.HardwareSerial=="one");
        });
        test("device name prefers Android friendly name, with model fallback",()=>{
            var named=Fake(a=>a.EndsWith("device_name")?"Tablet001\n":a.EndsWith("ro.serialno")?"serial-one":"MODEL");
            var found=named.InspectAsync("10.0.0.8:4444",CancellationToken.None).GetAwaiter().GetResult();
            Check(found.Name=="Tablet001"&&found.Serial=="serial-one");
            var fallback=Fake(a=>a.EndsWith("device_name")?"null":a.EndsWith("ro.product.model")?"SM-X356B":"");
            Check(fallback.InspectAsync("10.0.0.8:4444",CancellationToken.None).GetAwaiter().GetResult().Name=="SM-X356B");
        });
        test("metadata rejects control lines and truncates without splitting surrogate",()=>{
            foreach(var value in new[]{"null"," UNKNOWN ","a\nb","a\tb",""})Check(DeviceDiscovery.Clean(value,60)=="");
            Check(DeviceDiscovery.Clean(new string('a',59)+"😀",60)==new string('a',59));
            Check(DeviceDiscovery.Clean(" Tablet001\r\n",60)=="Tablet001");
        });
        test("missing metadata does not fail explicit connection inspection",()=>{
            var discovery=new DeviceDiscovery((a,c)=>{throw new TimeoutException();});
            var found=discovery.InspectAsync("10.0.0.8:4444",CancellationToken.None).GetAwaiter().GetResult();
            Check(found.Name==""&&found.Serial=="");
        });
        test("discovery parses only unique validated TLS connection advertisements",()=>{
            string services="List of discovered mdns services\nphone _adb-tls-connect._tcp. 10.0.0.9:5555\n"+
                "phone _adb-tls-connect._tcp 10.0.0.9:5555\npair _adb-tls-pairing._tcp. 10.0.0.7:1111\n"+
                "bad _adb-tls-connect._tcp. 10.0.0.1:22 --evil\nbad _adb-tls-connect._tcp. 127.0.0.1:5555";
            Check(DeviceDiscovery.ParseServices(services).SequenceEqual(new[]{"10.0.0.9:5555"}));
        });
        test("new endpoint matches hardware identity, preserving custom name and slide key",()=>{
            var discovery=Fake(a=>{
                if(a=="mdns services")return "wrong _adb-tls-connect._tcp. 10.0.0.9:5555\nright _adb-tls-connect._tcp. 10.0.0.10:6666";
                if(a.EndsWith("get-state"))return a.Contains("10.0.0.8:4444")?"offline":"device";
                if(a.EndsWith("device_name"))return "Tablet001";
                if(a.EndsWith("ro.serialno"))return a.Contains("10.0.0.10:6666")?"serial-one":"serial-other";
                return "";
            });
            var profile=new DeviceProfile{Id="stable",Name="Mit navn",HardwareSerial="serial-one",Endpoint="10.0.0.8:4444"};
            var found=discovery.ReconnectAsync(profile,CancellationToken.None).GetAwaiter().GetResult();
            Check(found.Endpoint=="10.0.0.10:6666"&&profile.Name=="Mit navn"&&profile.Id=="stable");
        });
        test("reused saved address cannot show a different known tablet",()=>{
            var discovery=Fake(a=>a.EndsWith("get-state")?"device":a.EndsWith("ro.serialno")?"wrong":"");
            bool refused=false;
            try{discovery.ReconnectAsync(new DeviceProfile{Endpoint="10.0.0.8:4444",HardwareSerial="right"},CancellationToken.None).GetAwaiter().GetResult();}
            catch(InvalidOperationException){refused=true;}Check(refused);
        });
        test("legacy profile learns identity on saved endpoint, never guesses another endpoint",()=>{
            var discovery=Fake(a=>a.EndsWith("get-state")?"device":a.EndsWith("ro.serialno")?"learned":"");
            Check(discovery.ReconnectAsync(new DeviceProfile{Endpoint="10.0.0.8:4444"},CancellationToken.None).GetAwaiter().GetResult().Serial=="learned");
            var offline=Fake(a=>{Check(a!="mdns services");return "";});
            bool refused=false;
            try{offline.ReconnectAsync(new DeviceProfile{Endpoint="10.0.0.8:4444"},CancellationToken.None).GetAwaiter().GetResult();}
            catch(InvalidOperationException){refused=true;}Check(refused);
        });
        test("cancelled discovery never runs a command",()=>{
            var discovery=Fake(a=>{throw new Exception("Command must not run");});
            using(var cancel=new CancellationTokenSource()){
                cancel.Cancel();bool cancelled=false;
                try{discovery.InspectAsync("10.0.0.8:4444",cancel.Token).GetAwaiter().GetResult();}
                catch(OperationCanceledException){cancelled=true;}Check(cancelled);
            }
        });
        test("custom name, identity and slide key survive XML round trip",()=>{
            var original=new UserSettings();
            original.Devices.Add(new DeviceProfile{Id="slide-key",Name="Taktik tablet",HardwareSerial="serial-one",Endpoint="10.0.0.8:4444"});
            original.SelectedDeviceId="slide-key";
            var xml=new XmlSerializer(typeof(UserSettings));var text=new StringWriter();xml.Serialize(text,original);
            var loaded=(UserSettings)xml.Deserialize(new StringReader(text.ToString()));loaded.Normalize();
            Check(loaded.Selected.Name=="Taktik tablet"&&loaded.Selected.Id=="slide-key"&&loaded.Selected.HardwareSerial=="serial-one");
        });
    }
    private static DeviceDiscovery Fake(Func<string,string> output)
    {return new DeviceDiscovery((a,c)=>{c.ThrowIfCancellationRequested();return Task.FromResult(new CommandResult{ExitCode=0,Output=output(a),Error=""});});}
    private static void Check(bool ok){if(!ok)throw new Exception("Assertion failed");}
}