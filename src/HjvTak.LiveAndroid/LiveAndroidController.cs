using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using HjvTak.LiveAndroid.Core;
using Office = Microsoft.Office.Core;
using PowerPoint = Microsoft.Office.Interop.PowerPoint;

namespace HjvTak.LiveAndroid
{
    /// <summary>Independent external windows per device; all Office access stays on the STA.</summary>
    public sealed class LiveAndroidController : IDisposable
    {
        public const string Version = "0.0.100";
        public const string AnchorName = "HJVTAK_ANDROID_LIVE";
        private const string DeviceTag = "HJVTAK_ANDROID_DEVICE";
        private readonly PowerPoint.Application app;
        private readonly DeviceStreams streams = new DeviceStreams();
        private readonly Dictionary<string, CancellationTokenSource> pending = new Dictionary<string, CancellationTokenSource>();
        private readonly Dictionary<string,string> names = new Dictionary<string,string>();
        private readonly System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer { Interval = 100 };
        private readonly PreviewWindow preview = new PreviewWindow();
        private readonly int uiThread = Thread.CurrentThread.ManagedThreadId;
        private bool enabled = true, disposed, previewRequested;
        private string selectedId = "legacy";
        private DateTime resumeAfter;
        private bool transitionPending;
        private string alignment = "Forbind en tablet, og indsæt dens ramme på en slide.";
        public event EventHandler StatusChanged;
        public string Status => string.Join("\n", streams.All.Select(s => Name(s.Key) + ": " + s.Value.Status.Detail)) + "\n" + alignment;
        public string Diagnostics => string.Join("\n\n", streams.All.Select(s => Name(s.Key) + ":\n" + s.Value.Diagnostics));
        public bool IsActive => pending.Count != 0 || streams.All.Any(s => Active(s.Value));
        public bool OverlayEnabled => enabled;
        public bool PreviewEnabled => previewRequested;
        internal bool IsDeviceActive(string id) => id != null && (pending.ContainsKey(id) || Active(streams.Get(id)));
        private static bool Active(ScrcpySession session) => session != null &&
            (session.Status.State == DeviceState.Starting || session.Status.State == DeviceState.Running);
        private string Name(string id) => names.TryGetValue(id,out var name) ? name : id;

        public LiveAndroidController(PowerPoint.Application application)
        {
            app = application ?? throw new ArgumentNullException(nameof(application));
            app.SlideShowBegin += OnShowChanged;
            app.SlideShowNextSlide += OnShowChanged;
            app.SlideShowEnd += OnShowEnd;
            app.PresentationClose += OnPresentationClose;
            preview.Dismissed += (s,e) => { previewRequested=false; streams.HideAll(); Changed(); };
            timer.Tick += OnTick; timer.Start();
        }
        // Compatibility entry for the optional Office harness, with no editing preview.
        public void StartDevice(string executable, string serial)
        {
            EnsureThread(); streams.Start("legacy",executable,serial,false,false,null); enabled=true; Changed();
        }
        internal IntPtr VideoWindow(string id) => streams.Get(id)?.Window ?? IntPtr.Zero;
        internal void SelectDevice(DeviceProfile profile)
        {
            EnsureThread(); if(profile==null)return;
            selectedId=profile.Id; names[profile.Id]=profile.Name; Changed();
        }
        internal async Task StartDeviceAsync(string executable, DeviceProfile profile, bool showPreview = true)
        {
            EnsureThread();
            if (!WifiEndpoint.IsValid(profile.Endpoint)) throw new ArgumentException("Forbind tabletten med QR først.");
            StopDevice(profile.Id);
            var cancel = new CancellationTokenSource();
            pending[profile.Id]=cancel;
            string id=profile.Id, endpoint=profile.Endpoint;
            bool audio=profile.Audio, touches=profile.ShowTouches;
            var orientation=profile.Orientation;
            names[id]=profile.Name; Changed();
            try
            {
                var connection = await new DeviceDiscovery(Path.Combine(Path.GetDirectoryName(executable),"adb.exe")).ReconnectAsync(profile,cancel.Token);
                endpoint=connection.Endpoint;
                profile.Endpoint=endpoint; profile.HardwareSerial=connection.Serial;
                string capture = await CaptureOrientation.ResolveAsync(Path.Combine(Path.GetDirectoryName(executable),"adb.exe"),
                    endpoint,orientation,cancel.Token);
                cancel.Token.ThrowIfCancellationRequested();
                EnsureThread();
                streams.Start(id,executable,endpoint,audio,touches,capture);
                selectedId=id; if(showPreview){enabled=true;previewRequested=true;}
            }
            finally
            {
                if(pending.TryGetValue(id,out var current) && ReferenceEquals(current,cancel))pending.Remove(id);
                cancel.Dispose();
                if(!disposed)Changed();
            }
        }
        internal void StopDevice(string id)
        {
            EnsureThread();
            if(id==null)return;
            if(pending.TryGetValue(id,out var cancel)){pending.Remove(id);cancel.Cancel();}
            streams.Stop(id);
            if(selectedId==id)preview.Hide();
            Changed();
        }
        public void StopDevice()
        {
            EnsureThread();
            foreach(var cancel in pending.Values.ToArray())cancel.Cancel();
            pending.Clear(); streams.Dispose(); preview.Hide(); previewRequested=false; Changed();
        }
        public void ToggleOverlay()
        { EnsureThread(); enabled=!enabled; if(!enabled){streams.HideAll();preview.Hide();} Changed(); }
        internal void TogglePreview()
        { EnsureThread(); previewRequested=!previewRequested; if(!previewRequested){preview.Hide();streams.HideAll();} Changed(); }
        public void AddAnchorToCurrentSlide() => AddAnchorToCurrentSlide(new DeviceProfile{Id="legacy",Name="Tablet"});
        internal void AddAnchorToCurrentSlide(DeviceProfile device)
        {
            EnsureThread();
            PowerPoint.DocumentWindow document=null; PowerPoint.View view=null; PowerPoint.Slide slide=null;
            PowerPoint.Presentation presentation=null; PowerPoint.PageSetup page=null; PowerPoint.Shapes shapes=null;
            PowerPoint.Shape shape=null; PowerPoint.TextFrame text=null; PowerPoint.TextRange range=null; PowerPoint.Tags tags=null;
            try
            {
                document=app.ActiveWindow;
                if(document==null || (document.ViewType!=PowerPoint.PpViewType.ppViewNormal && document.ViewType!=PowerPoint.PpViewType.ppViewSlide))
                    throw new InvalidOperationException("Åbn sliden i normal redigeringsvisning først.");
                view=document.View; slide=view.Slide as PowerPoint.Slide;
                if(slide==null)throw new InvalidOperationException("Vælg en slide først.");
                shapes=slide.Shapes;
                int count=0;
                for(int i=1;i<=shapes.Count;i++)
                {
                    var candidate=shapes[i];
                    string id=MarkerDevice(candidate);
                    if(id==device.Id){shape=candidate;shape.Select();return;}
                    if(id!=null)count++;
                    Release(candidate);
                }
                presentation=document.Presentation; page=presentation.PageSetup;
                float left=page.SlideWidth*(count%2==0 ? .06f : .53f);
                float top=page.SlideHeight*(count%4<2 ? .10f : .55f);
                shape=shapes.AddShape(Office.MsoAutoShapeType.msoShapeRectangle,left,top,page.SlideWidth*.40f,page.SlideHeight*.35f);
                shape.Name=device.Id=="legacy"?AnchorName:AnchorName+"_"+device.Id;
                tags=shape.Tags; tags.Add(DeviceTag,device.Id);
                shape.AlternativeText="Live tablet: "+device.Name+". Eksternt vindue; medtages ikke i PowerPoint-eksport.";
                text=shape.TextFrame; range=text.TextRange;
                range.Text=device.Name+"\nLivebillede under slideshow\n"+Version;
                shape.Select();
            }
            finally { Release(tags);Release(range);Release(text);Release(shape);Release(shapes);Release(page);Release(presentation);Release(slide);Release(view);Release(document); }
        }
        private static string MarkerDevice(PowerPoint.Shape shape)
        {
            PowerPoint.Tags tags=null;
            try {
                tags=shape.Tags; string id=tags[DeviceTag];
                if(!string.IsNullOrEmpty(id))return id.ToLowerInvariant(); // PowerPoint normalizes tag values to uppercase.
                return shape.Name==AnchorName?"legacy":null;
            } finally {Release(tags);}
        }
        private void OnShowChanged(PowerPoint.SlideShowWindow show)
        { streams.HideAll();preview.Hide();resumeAfter=DateTime.UtcNow.AddMilliseconds(250);transitionPending=true; }
        private void OnShowEnd(PowerPoint.Presentation presentation)
        { HideAndDetach();previewRequested=false;alignment="Slideshow afsluttet; billeder skjult."; }
        private void OnPresentationClose(PowerPoint.Presentation presentation)
        { HideAndDetach();previewRequested=false; }
        private void HideAndDetach()
        { preview.Hide();foreach(var item in streams.All){item.Value.Hide();NativeWindow.DetachOwner(item.Value.Window);} }
        private void OnTick(object sender,EventArgs args)
        {
            if(disposed)return;
            string previous=Status;
            try {foreach(var item in streams.All)item.Value.Poll();Align();}
            catch(COMException){HideAndDetach();alignment="PowerPoint er optaget; billeder skjult.";}
            catch(InvalidComObjectException){HideAndDetach();alignment="PowerPoint-vinduet er lukket.";}
            catch(Exception ex){HideAndDetach();alignment="Billeder skjult: "+ex.Message;}
            if(Status!=previous)Changed();
        }
        private void AlignPreview()
        {
            var session=streams.Get(selectedId);
            foreach(var item in streams.All)if(item.Key!=selectedId)item.Value.Hide();
            var host=new IntPtr(app.HWND);
            bool show=previewRequested && session?.Status.State==DeviceState.Running &&
                NativeWindow.IsWindowVisible(host) && !NativeWindow.IsIconic(host) && NativeWindow.IsHostForeground(host);
            if(!show){preview.Hide();session?.Hide();alignment="Start slideshow med tabletrammer, eller vælg Forhåndsvisning.";return;}
            preview.Text=Name(selectedId)+" · Forhåndsvisning";
            if(!preview.Visible)preview.Show(new WindowOwner(host));
            if(preview.WindowState==FormWindowState.Minimized){session.Hide();return;}
            using(new NativeWindow.DpiScope())
                if(!NativeWindow.Place(session.Window,preview.Handle,preview.VideoBounds))session.Hide();
            alignment="Forhåndsvisning. Under slideshow vises hver tablet i sin ramme.";
        }
        private void Align()
        {
            if(!enabled || DateTime.UtcNow<resumeAfter){streams.HideAll();preview.Hide();return;}
            PowerPoint.SlideShowWindows shows=null;PowerPoint.SlideShowWindow show=null;PowerPoint.SlideShowView view=null;
            PowerPoint.Slide slide=null;PowerPoint.Shapes shapes=null;PowerPoint.Presentation presentation=null;PowerPoint.PageSetup page=null;
            var markers=new Dictionary<string,PowerPoint.Shape>();
            var duplicate=new HashSet<string>();
            try
            {
                shows=app.SlideShowWindows;
                if(shows.Count==0){AlignPreview();return;}
                preview.Hide();
                if(shows.Count!=1){streams.HideAll();alignment="Flere slideshows åbne; kør ét ad gangen.";return;}
                show=shows[1];var hwnd=new IntPtr(show.HWND);view=show.View;
                bool normal=view.State==PowerPoint.PpSlideShowState.ppSlideShowRunning;
                bool visible=NativeWindow.IsWindowVisible(hwnd)&&!NativeWindow.IsIconic(hwnd);
                bool foreground=NativeWindow.IsHostForeground(hwnd);
                if(!normal||!visible||!foreground){streams.HideAll();alignment="Billeder skjult: slideshow blankt, pauset eller i baggrunden.";return;}
                slide=view.Slide;
                if(transitionPending)
                {
                    PowerPoint.SlideShowTransition transition=null;
                    try {
                        transition=slide.SlideShowTransition;
                        double duration=transition.EntryEffect==PowerPoint.PpEntryEffect.ppEffectNone?0:transition.Duration;
                        resumeAfter=DateTime.UtcNow.AddSeconds(Math.Max(0,Math.Min(60,duration)));
                        transitionPending=false;streams.HideAll();return;
                    } finally {Release(transition);}
                }
                shapes=slide.Shapes;
                for(int i=1;i<=shapes.Count;i++)
                {
                    var shape=shapes[i];string id=MarkerDevice(shape);
                    if(id==null){Release(shape);continue;}
                    if(markers.ContainsKey(id)){duplicate.Add(id);Release(shape);}else markers.Add(id,shape);
                }
                presentation=show.Presentation;page=presentation.PageSetup;
                int shown=0;
                using(new NativeWindow.DpiScope())
                {
                    Rectangle client;
                    if(!NativeWindow.TryGetClient(hwnd,out client)){streams.HideAll();return;}
                    foreach(var item in streams.All)
                    {
                        var session=item.Value;PowerPoint.Shape anchor;
                        bool valid=markers.TryGetValue(item.Key,out anchor)&&!duplicate.Contains(item.Key)&&
                            anchor.Visible==Office.MsoTriState.msoTrue && Math.Abs(anchor.Rotation)<.01 &&
                            anchor.Type==Office.MsoShapeType.msoAutoShape && anchor.AutoShapeType==Office.MsoAutoShapeType.msoShapeRectangle;
                        Rectangle bounds;
                        if(!OverlayPolicy.ShouldShow(session.Status.State==DeviceState.Running,enabled,true,normal,visible,foreground,valid) ||
                            !OverlayGeometry.TryMap(client,page.SlideWidth,page.SlideHeight,anchor.Left,anchor.Top,anchor.Width,anchor.Height,out bounds) ||
                            !NativeWindow.Place(session.Window,hwnd,bounds))
                        {session.Hide();continue;}
                        shown++;
                    }
                }
                alignment=shown>0?"Overlay visible: "+shown+" livebillede(r). Eksterne vinduer; ikke indlejret video.":
                    "Ingen aktiv tablet med én gyldig ramme på denne slide. Forbind tabletten og indsæt dens ramme.";
                if(duplicate.Count>0)alignment+=" Dublerede rammer for samme tablet skjules.";
            }
            finally {
                foreach(var shape in markers.Values)Release(shape);
                Release(page);Release(presentation);Release(shapes);Release(slide);Release(view);Release(show);Release(shows);
            }
        }
        private sealed class WindowOwner : IWin32Window
        { public IntPtr Handle {get;} internal WindowOwner(IntPtr handle){Handle=handle;} }
        private static void Release(object value){if(value!=null&&Marshal.IsComObject(value))Marshal.ReleaseComObject(value);}
        private void Changed(){StatusChanged?.Invoke(this,EventArgs.Empty);}
        private void EnsureThread()
        {
            if(disposed)throw new ObjectDisposedException(nameof(LiveAndroidController));
            if(Thread.CurrentThread.ManagedThreadId!=uiThread)throw new InvalidOperationException("Call the controller on PowerPoint's UI thread.");
        }
        public void Dispose()
        {
            if(disposed)return;EnsureThread();StopDevice();disposed=true;
            timer.Stop();timer.Tick-=OnTick;timer.Dispose();preview.Dispose();
            app.SlideShowBegin-=OnShowChanged;app.SlideShowNextSlide-=OnShowChanged;
            app.SlideShowEnd-=OnShowEnd;app.PresentationClose-=OnPresentationClose;
        }
    }
}