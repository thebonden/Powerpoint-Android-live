using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using HjvTak.LiveAndroid.Core;
using Office = Microsoft.Office.Core;

namespace HjvTak.LiveAndroid
{
    [ComVisible(true)]
    public sealed class AndroidRibbon : Office.IRibbonExtensibility
    {
        private readonly ThisAddIn addIn;
        private Office.IRibbonUI ribbon;
        private UserSettings settings = UserSettings.Load();
        private bool busy;
        internal AndroidRibbon(ThisAddIn addIn){this.addIn=addIn;}
        public string GetCustomUI(string ribbonId)
        {
            using(var stream=typeof(AndroidRibbon).Assembly.GetManifestResourceStream("HjvTak.LiveAndroid.AndroidRibbon.xml"))
            using(var reader=new StreamReader(stream))return reader.ReadToEnd();
        }
        public void OnLoad(Office.IRibbonUI ui){ribbon=ui;}
        internal void Refresh(){try{ribbon?.Invalidate();}catch(COMException){}}
        public bool GetEnabled(Office.IRibbonControl control)
        {
            var controller=addIn.Controller;if(controller==null)return false;
            if(control.Id=="StopAll")return controller.IsActive;
            if(control.Id=="StopAndroid")return controller.IsDeviceActive(settings.SelectedDeviceId);
            if(busy)return false;
            if(control.Id=="AddTablet"||control.Id=="Settings"||control.Id=="Status"||control.Id=="ShowOverlay")return true;
            if(control.Id=="StartAll")return settings.Devices.Count>0;
            return settings.Selected!=null;
        }
        public bool GetPressed(Office.IRibbonControl control)
        {
            if(control.Id=="Audio")return settings.Selected?.Audio??false;
            if(control.Id=="Touches")return settings.Selected?.ShowTouches??false;
            if(control.Id=="Preview")return addIn.Controller?.PreviewEnabled??false;
            return addIn.Controller?.OverlayEnabled??false;
        }
        public int GetDeviceCount(Office.IRibbonControl control)=>Math.Max(1,settings.Devices.Count);
        public string GetDeviceLabel(Office.IRibbonControl control,int index)=>settings.Devices.Count==0?"Tilføj en tablet":settings.Devices[index].Name;
        public string GetDeviceId(Office.IRibbonControl control,int index)=>settings.Devices.Count==0?"NoDevice":settings.Devices[index].Id;
        public int GetDeviceIndex(Office.IRibbonControl control)=>Math.Max(0,settings.Devices.FindIndex(d=>d.Id==settings.SelectedDeviceId));
        public void OnDeviceSelected(Office.IRibbonControl control,string id,int index)
        {
            Run(()=>{if(index<0||index>=settings.Devices.Count)return;
                settings.SelectedDeviceId=settings.Devices[index].Id;settings.Save();addIn.Controller.SelectDevice(settings.Selected);});
        }
        public int GetOrientation(Office.IRibbonControl control)=>(int)(settings.Selected?.Orientation??VideoOrientation.Automatic);
        public void OnOrientation(Office.IRibbonControl control,string id,int index)
        { if(index<0||index>2)return;ChangeOptions(d=>d.Orientation=(VideoOrientation)index); }
        public void OnAudio(Office.IRibbonControl control,bool pressed){ChangeOptions(d=>d.Audio=pressed);}
        public void OnTouches(Office.IRibbonControl control,bool pressed){ChangeOptions(d=>d.ShowTouches=pressed);}
        private void ChangeOptions(Action<DeviceProfile> change)
        {
            RunAsync(async()=>{
                var device=settings.Select…8048 tokens truncated…       transition=slide.SlideShowTransition;
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