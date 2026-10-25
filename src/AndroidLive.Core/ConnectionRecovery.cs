using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AndroidLive.Core
{
    // Owned by the UI thread. All callbacks/continuations stay on its synchronization context.
    // No QR pairing, shared ADB restart or Office UI is performed by this coordinator.
    internal sealed class ConnectionRecovery : IDisposable
    {
        private sealed class Entry
        {
            internal string Id, Detail, LastFailure;
            internal Func<DeviceState> State;
            internal Func<CancellationToken,Task> Connect;
            internal Func<CancellationToken,Task<bool>> Probe;
            internal Action Lost;
            internal readonly CancellationTokenSource Cancel=new CancellationTokenSource();
            internal bool Busy, Waiting;
            internal int Attempts, Misses;
            internal DateTime Due, ProbeAt;
            internal DateTime? StableSince;
        }
        private readonly Dictionary<string,Entry> entries=new Dictionary<string,Entry>();
        private readonly Func<DateTime> clock;
        internal ConnectionRecovery(Func<DateTime> now=null){clock=now??(()=>DateTime.UtcNow);}
        internal bool Any => entries.Count!=0;
        internal bool Contains(string id)=>id!=null&&entries.ContainsKey(id);
        internal string Status(string id)=>id!=null&&entries.TryGetValue(id,out var e)?e.Detail:null;
        internal string LastFailure(string id)=>id!=null&&entries.TryGetValue(id,out var e)?e.LastFailure:null;
        internal void Track(string id,Func<DeviceState> state,Func<CancellationToken,Task> connect,
            Func<CancellationToken,Task<bool>> probe,Action lost,bool connectNow)
        {
            Stop(id);
            entries.Add(id,new Entry{Id=id,State=state,Connect=connect,Probe=probe,Lost=lost,
                Waiting=connectNow,Due=clock(),ProbeAt=clock().AddSeconds(15),
                Detail=connectNow?"Forbinder til gemt tablet…":null});
        }
        private bool Current(Entry e)=>entries.TryGetValue(e.Id,out var current)&&ReferenceEquals(e,current);
        internal void Stop(string id)
        {
            if(id==null||!entries.TryGetValue(id,out var e))return;
            entries.Remove(id);e.Cancel.Cancel();
            if(!e.Busy)e.Cancel.Dispose();
        }
        internal Task TickAsync()
        {
            var tasks=new List<Task>();
            foreach(var e in entries.Values.ToArray())
            {
                if(e.Busy)continue;
                var now=clock();
                if(e.Waiting){if(now>=e.Due)tasks.Add(ConnectAsync(e));continue;}
                var state=e.State();
                if(state==DeviceState.Faulted){Schedule(e);continue;}
                if(state!=DeviceState.Running){e.StableSince=null;continue;}
                if(e.StableSince==null)e.StableSince=now;
                if(now-e.StableSince.Value>=TimeSpan.FromSeconds(60))e.Attempts=0;
                if(now>=e.ProbeAt)tasks.Add(ProbeAsync(e));
            }
            return Task.WhenAll(tasks);
        }
        private void Schedule(Entry e)
        {
            e.Waiting=true;e.StableSince=null;e.Misses=0;
            int[] seconds={2,5,10,30};
            int delay=seconds[Math.Min(e.Attempts,seconds.Length-1)];
            e.Attempts=Math.Min(e.Attempts+1,seconds.Length-1);
            e.Due=clock().AddSeconds(delay);
            e.Detail="Forbindelse afbrudt. Prøver automatisk igen om "+delay+" sek. Kontroller Wi-Fi og Trådløs fejlfinding.";
        }
        private async Task ConnectAsync(Entry e)
        {
            e.Busy=true;e.Detail="Genforbinder til tabletten…";
            try
            {
                await e.Connect(e.Cancel.Token);
                if(!Current(e))return;
                e.Waiting=false;e.Misses=0;e.StableSince=null;
                e.ProbeAt=clock().AddSeconds(15);e.Detail=null;
            }
            catch(OperationCanceledException){if(Current(e))Schedule(e);}
            catch(Exception ex){if(Current(e)){e.LastFailure=ex.Message;Schedule(e);}}
            finally{e.Busy=false;if(!Current(e))e.Cancel.Dispose();}
        }
        private async Task ProbeAsync(Entry e)
        {
            e.Busy=true;
            try
            {
                bool healthy=await e.Probe(e.Cancel.Token);
                if(!Current(e))return;
                e.Misses=healthy?0:e.Misses+1;
                if(e.Misses>=2)Lost(e);
                else e.Detail=healthy?null:"Kontrollerer forbindelsen til tabletten…";
            }
            catch(OperationCanceledException){ }
            catch(Exception ex)
            {
                if(Current(e)){e.LastFailure=ex.Message;e.Misses++;if(e.Misses>=2)Lost(e);}
            }
            finally
            {
                e.ProbeAt=clock().AddSeconds(15);
                e.Busy=false;if(!Current(e))e.Cancel.Dispose();
            }
        }
        private void Lost(Entry e)
        {
            try{e.Lost();}
            catch(Exception ex){e.LastFailure=ex.Message;}
            Schedule(e);
        }
        public void Dispose(){foreach(string id in entries.Keys.ToArray())Stop(id);}
    }
}