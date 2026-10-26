using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AndroidLive
{
    // Own a real STA dispatch window: Office may clear/replace SynchronizationContext
    // between callbacks, and the shared WinForms context may outlive its marshaler.
    internal sealed class OfficeUiContext : IDisposable
    {
        private sealed class DispatchContext : SynchronizationContext
        {
            private readonly Control owner;
            internal DispatchContext(Control control){owner=control;}
            public override SynchronizationContext CreateCopy()=>this;
            public override void Post(SendOrPostCallback callback,object state)
            {
                owner.BeginInvoke(new Action(()=>{
                    var previous=Current;SetSynchronizationContext(this);
                    try{callback(state);}finally{SetSynchronizationContext(previous);}
                }));
            }
        }
        private readonly int thread=Thread.CurrentThread.ManagedThreadId;
        private readonly SynchronizationContext context;
        private readonly Control lifetime;
        private int active;
        private bool disposeRequested;
        internal OfficeUiContext()
        {
            var previous=SynchronizationContext.Current;
            try{
                lifetime=new Control();
                var handle=lifetime.Handle; // Force creation on the owning STA.
                context=new DispatchContext(lifetime);
            }finally{SynchronizationContext.SetSynchronizationContext(previous);}
        }
        internal Task RunAsync(Func<Task> action)
        {
            if(disposeRequested)throw new ObjectDisposedException(nameof(OfficeUiContext));
            EnsureThread();
            var previous=SynchronizationContext.Current;
            SynchronizationContext.SetSynchronizationContext(context);
            try{return ExecuteAsync(action);}
            finally{SynchronizationContext.SetSynchronizationContext(previous);}
        }
        private async Task ExecuteAsync(Func<Task> action)
        {
            active++;
            try{await action();}
            finally{active--;if(active==0&&disposeRequested)lifetime.Dispose();}
        }
        private void EnsureThread()
        {
            if(Thread.CurrentThread.ManagedThreadId!=thread)throw new InvalidOperationException("Async Office work must start on its UI thread.");
        }
        public void Dispose()
        {
            EnsureThread();
            if(disposeRequested)return;
            disposeRequested=true;
            // Cancellation callbacks must still reach the STA before its dispatch HWND is released.
            if(active==0)lifetime.Dispose();
        }
    }
}