using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Xml.Serialization;
using AndroidLive;
using AndroidLive.Core;

internal static class ConnectionRecoveryTests
{
    internal static void Run(Action<string,Action> test)
    {
        test("recovery: saved startup connection retries offline then returns without QR",()=>{
            var now=DateTime.UtcNow;int attempts=0;var state=DeviceState.Stopped;
            using(var recovery=new ConnectionRecovery(()=>now)){
                recovery.Track("a",()=>state,c=>{
                    c.ThrowIfCancellationRequested();attempts++;
                    if(attempts==1)throw new IOException("offline");
                    state=DeviceState.Running;return Task.CompletedTask;
                },c=>Task.FromResult(true),()=>{},true);
                Tick(recovery);Check(attempts==1&&recovery.Status("a")!=null);
                now=now.AddSeconds(1);Tick(recovery);Check(attempts==1);
                now=now.AddSeconds(1);Tick(recovery);Check(attempts==2&&state==DeviceState.Running&&recovery.Status("a")==null);
            }
        });
        test("recovery: bounded backoff and reset only after stable video",()=>{
            var now=DateTime.UtcNow;int attempts=0;var state=DeviceState.Running;
            using(var recovery=new ConnectionRecovery(()=>now)){
                recovery.Track("a",()=>state,c=>{attempts++;state=DeviceState.Running;return Task.CompletedTask;},
                    c=>Task.FromResult(true),()=>{},false);
                foreach(int delay in new[]{2,5,10,30,30}){
                    state=DeviceState.Faulted;Tick(recovery);
                    now=now.AddSeconds(delay-1);Tick(recovery);int before=attempts;
                    now=now.AddSeconds(1);Tick(recovery);Check(attempts==before+1);
                }
                Tick(recovery);now=now.AddSeconds(61);Tick(recovery);
                state=DeviceState.Faulted;Tick(recovery);int previous=attempts;
                now=now.AddSeconds(2);Tick(recovery);Check(attempts==previous+1);
            }
        });
        test("recovery: two failed liveness checks restart only the affected stream",()=>{
            var now=DateTime.UtcNow;int lostA=0,lostB=0,startsA=0,startsB=0,probes=0;
            using(var recovery=new ConnectionRecovery(()=>now)){
                recovery.Track("a",()=>DeviceState.Running,c=>{startsA++;return Task.CompletedTask;},
                    c=>{probes++;return Task.FromResult(false);},()=>lostA++,false);
                recovery.Track("b",()=>DeviceState.Running,c=>{startsB++;return Task.CompletedTask;},
                    c=>Task.FromResult(true),()=>lostB++,false);
                now=now.AddSeconds(15);Tick(recovery);Check(probes==1&&lostA==0);
                now=now.AddSeconds(15);Tick(recovery);Check(lostA==1&&lostB==0);
                now=now.AddSeconds(2);Tick(recovery);Check(startsA==1&&startsB==0);
            }
        });
        test("recovery: healthy probe clears one miss without restarting",()=>{
            var now=DateTime.UtcNow;bool healthy=false;int lost=0;
            using(var recovery=new ConnectionRecovery(()=>now)){
                recovery.Track("a",()=>DeviceState.Running,c=>Task.CompletedTask,
                    c=>Task.FromResult(healthy),()=>lost++,false);
                now=now.AddSeconds(15);Tick(recovery);
                healthy=true;now=now.AddSeconds(15);Tick(recovery);Check(recovery.Status("a")==null);
                healthy=false;now=now.AddSeconds(15);Tick(recovery);Check(lost==0);
            }
        });
        test("recovery: stop cancels pending work and stale completion cannot revive tracking",()=>{
            var now=DateTime.UtcNow;var gate=new TaskCompletionSource<bool>();CancellationToken pending=default(CancellationToken);int attempts=0;
            using(var recovery=new ConnectionRecovery(()=>now)){
                recovery.Track("a",()=>DeviceState.Stopped,c=>{pending=c;attempts++;return gate.Task;},c=>Task.FromResult(true),()=>{},true);
                var active=recovery.TickAsync();Tick(recovery);Check(attempts==1);
                recovery.Stop("a");Check(pending.IsCancellationRequested&&!recovery.Any);
                recovery.Track("a",()=>DeviceState.Running,c=>{attempts++;return Task.CompletedTask;},c=>Task.FromResult(true),()=>{},false);
                gate.SetResult(true);active.GetAwaiter().GetResult();
                Check(recovery.Contains("a")&&recovery.Status("a")==null);
                recovery.Dispose();now=now.AddMinutes(1);Tick(recovery);Check(attempts==1&&!recovery.Any);
            }
        });
        test("recovery: a cancelled probe cannot stop a replacement session",()=>{
            var now=DateTime.UtcNow;int lost=0;var gate=new TaskCompletionSource<bool>();
            using(var recovery=new ConnectionRecovery(()=>now)){
                recovery.Track("a",()=>DeviceState.Running,c=>Task.CompletedTask,c=>gate.Task,()=>lost++,false);
                now=now.AddSeconds(15);var active=recovery.TickAsync();
                recovery.Stop("a");gate.SetResult(false);active.GetAwaiter().GetResult();
                Check(lost==0&&!recovery.Any);
            }
        });
        test("recovery: nine simulated hours retain monitoring through idle periods and eight outages",()=>{
            var started=DateTime.UtcNow;var now=started;var unavailableUntil=started;
            var state=DeviceState.Running;int attempts=0,recovered=0,probes=0,outages=0;
            using(var recovery=new ConnectionRecovery(()=>now)){
                recovery.Track("a",()=>state,c=>{
                    attempts++;
                    if(now<unavailableUntil)throw new IOException("temporary network loss");
                    state=DeviceState.Running;recovered++;return Task.CompletedTask;
                },c=>{probes++;return Task.FromResult(true);},()=>{},false);
                for(int seconds=0;seconds<=9*60*60;seconds+=5){
                    now=started.AddSeconds(seconds);
                    if(seconds>0&&seconds<9*60*60&&seconds%3600==0){
                        outages++;state=DeviceState.Faulted;unavailableUntil=now.AddSeconds(60);
                    }
                    Tick(recovery);
                }
                Check(outages==8&&recovered==8&&attempts<60&&probes>1500);
                Check(state==DeviceState.Running&&recovery.Contains("a"));
                recovery.Stop("a");now=now.AddHours(9);Tick(recovery);Check(!recovery.Any);
            }
        });
        test("startup preference: legacy defaults resume and explicit Stop survives reload",()=>{
            var settings=new UserSettings();var running=new DeviceProfile{Id="a"};var stopped=new DeviceProfile{Id="b",ReconnectOnStartup=false};
            settings.Devices.Add(running);settings.Devices.Add(stopped);
            var xml=new XmlSerializer(typeof(UserSettings));var text=new StringWriter();xml.Serialize(text,settings);
            var copy=(UserSettings)xml.Deserialize(new StringReader(text.ToString()));copy.Normalize();
            Check(copy.Devices[0].ReconnectOnStartup&&!copy.Devices[1].ReconnectOnStartup);
            var legacy=(UserSettings)xml.Deserialize(new StringReader("<UserSettings><Serial>10.0.0.8:4444</Serial></UserSettings>"));
            legacy.Normalize();Check(legacy.Selected.ReconnectOnStartup);
        });
        test("liveness probe checks a specific device identity without pairing or disconnecting",()=>{
            int calls=0;bool correct=true;
            var discovery=new DeviceDiscovery((a,c)=>{
                calls++;Check(a=="-s 10.0.0.8:4444 shell getprop ro.serialno");
                return Task.FromResult(new CommandResult{ExitCode=0,Output=correct?"known":"wrong",Error=""});
            });
            var profile=new DeviceProfile{Endpoint="10.0.0.8:4444",HardwareSerial="known"};
            Check(discovery.ProbeAsync(profile,CancellationToken.None).GetAwaiter().GetResult());
            correct=false;Check(!discovery.ProbeAsync(profile,CancellationToken.None).GetAwaiter().GetResult());
            profile.HardwareSerial="";Check(!discovery.ProbeAsync(profile,CancellationToken.None).GetAwaiter().GetResult()&&calls==2);
        });
    }
    private static void Tick(ConnectionRecovery recovery){recovery.TickAsync().GetAwaiter().GetResult();}
    private static void Check(bool ok){if(!ok)throw new Exception("Assertion failed");}
}