using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using AndroidLive;
using AndroidLive.Core;

internal static class OfficeUiContextTests
{
    internal static void Run(Action<string,Action> test)
    {
        test("Office recovery: missing or replaced callback context resumes on the original STA",()=>{
            var previous=SynchronizationContext.Current;
            try{
                using(var ui=new OfficeUiContext())
                foreach(var foreign in new SynchronizationContext[]{null,new SynchronizationContext()}){
                    SynchronizationContext.SetSynchronizationContext(foreign);
                    int owner=Thread.CurrentThread.ManagedThreadId,after=0;var state=DeviceState.Stopped;
                    using(var recovery=new ConnectionRecovery()){
                        recovery.Track("a",()=>state,async c=>{
                            await Task.Delay(30,c);
                            after=Thread.CurrentThread.ManagedThreadId;
                            if(after!=owner)throw new InvalidOperationException("Call the controller on PowerPoint's UI thread.");
                            state=DeviceState.Running;
                        },async c=>{await Task.Delay(10,c);return true;},()=>{},true);
                        var pending=ui.RunAsync(recovery.TickAsync);
                        Check(ReferenceEquals(SynchronizationContext.Current,foreign));
                        Pump(pending);
                        Check(after==owner&&state==DeviceState.Running&&recovery.Status("a")==null);
                    }
                }
            }finally{SynchronizationContext.SetSynchronizationContext(previous);}
        });
        test("Office shutdown: pending continuation completes before dispatch window is released",()=>{
            var previous=SynchronizationContext.Current;
            try{
                using(var ui=new OfficeUiContext()){
                    SynchronizationContext.SetSynchronizationContext(null);
                    int owner=Thread.CurrentThread.ManagedThreadId;bool finished=false;
                    var pending=ui.RunAsync(async()=>{await Task.Delay(30);Check(Thread.CurrentThread.ManagedThreadId==owner);finished=true;});
                    ui.Dispose();
                    bool refused=false;try{ui.RunAsync(()=>Task.CompletedTask);}catch(ObjectDisposedException){refused=true;}
                    Check(refused);Pump(pending);Check(finished);
                }
            }finally{SynchronizationContext.SetSynchronizationContext(previous);}
        });
        test("Office callbacks: nested awaits and cancellation stay on the owner thread",()=>{
            var previous=SynchronizationContext.Current;
            try{
                using(var ui=new OfficeUiContext())
                using(var cancel=new CancellationTokenSource()){
                    SynchronizationContext.SetSynchronizationContext(null);
                    int owner=Thread.CurrentThread.ManagedThreadId;bool finalized=false;
                    var pending=ui.RunAsync(async()=>{
                        try{
                            await ui.RunAsync(async()=>{await Task.Delay(10);Check(Thread.CurrentThread.ManagedThreadId==owner);});
                            await Task.Delay(10000,cancel.Token);
                        }catch(OperationCanceledException){Check(Thread.CurrentThread.ManagedThreadId==owner);}
                        finally{Check(Thread.CurrentThread.ManagedThreadId==owner);finalized=true;}
                    });
                    cancel.Cancel();Pump(pending);Check(finalized);
                }
            }finally{SynchronizationContext.SetSynchronizationContext(previous);}
        });
    }
    private static void Pump(Task task)
    {
        var watch=Stopwatch.StartNew();
        while(!task.IsCompleted){if(watch.ElapsedMilliseconds>5000)throw new Exception("STA continuation timed out");Application.DoEvents();Thread.Sleep(5);}
        task.GetAwaiter().GetResult();
    }
    private static void Check(bool ok){if(!ok)throw new Exception("Assertion failed");}
}