using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using AndroidLive.Core;
using Office = Microsoft.Office.Core;

namespace AndroidLive
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
            using(var stream=typeof(AndroidRibbon).Assembly.GetManifestResourceStream("AndroidLive.AndroidRibbon.xml"))
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
        public string GetDeviceName(Office.IRibbonControl control)=>settings.Selected?.Name??"";
        public void OnDeviceName(Office.IRibbonControl control,string value)
        {
            Run(()=>{
                var device=settings.Selected;if(device==null)return;
                string previous=device.Name;var previousMode=device.NameMode;
                try{device.SetCustomName(value);settings.Save();}
                catch{device.Name=previous;device.NameMode=previousMode;throw;}
                addIn.Controller.SelectDevice(device);
            });
        }
        public int GetDeviceCount(Office.IRibbonControl control)=>Math.Max(1,settings.Devices.Count);
        public string GetDeviceLabel(Office.IRibbonControl control,int index)=>settings.Devices.Count==0?"Tilføj en tablet":settings.Devices[index].ListLabel;
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
                var device=settings.Selected;if(device==null)return;
                bool restart=addIn.Controller.IsDeviceActive(device.Id);
                change(device);settings.Save();
                if(restart)await addIn.Controller.StartDeviceAsync(settings.ScrcpyPath,device,false);
                settings.Save();
            });
        }
        public void OnAddMarker(Office.IRibbonControl control)
        {Run(()=>{if(settings.Selected!=null)addIn.Controller.AddAnchorToCurrentSlide(settings.Selected);});}
        public void OnAddTablet(Office.IRibbonControl control){OpenWifi(false,true);}
        public void OnConnectWifi(Office.IRibbonControl control){OpenWifi(false,false);}
        public void OnStart(Office.IRibbonControl control){OpenWifi(true,false);}
        private bool EnsureScrcpy()
        {
            if(ScrcpyLocator.IsComplete(settings.ScrcpyPath))return true;
            using(var dialog=new ScrcpySetupDialog(settings))if(dialog.ShowDialog()!=DialogResult.OK)return false;
            settings.Save();return true;
        }
        private void OpenWifi(bool reconnect,bool newDevice)
        {
            RunAsync(async()=>{
                if(!EnsureScrcpy())return;
                var device=newDevice?null:settings.Selected;
                DeviceConnection connection;
                using(var dialog=new WifiDialog(settings.ScrcpyPath,device?.Endpoint??"",reconnect,device)){
                    if(dialog.ShowDialog()!=DialogResult.OK)return;connection=dialog.Connection;
                }
                device=settings.AcceptConnection(device,connection);settings.Save();
                await addIn.Controller.StartDeviceAsync(settings.ScrcpyPath,device);settings.Save();
            });
        }
        public void OnStartAll(Office.IRibbonControl control)
        {
            RunAsync(async()=>{
                if(!EnsureScrcpy())return;
                var errors=new System.Collections.Generic.List<string>();
                foreach(var device in settings.Devices.ToArray()){
                    if(addIn.Controller.IsDeviceActive(device.Id))continue;
                    try{await addIn.Controller.StartDeviceAsync(settings.ScrcpyPath,device);settings.Save();}
                    catch(OperationCanceledException){break;}
                    catch(Exception ex){errors.Add(device.Name+": "+ex.Message);}
                }
                addIn.Controller.SelectDevice(settings.Selected);
                if(errors.Count>0)MessageBox.Show(string.Join("\n\n",errors),"Tabletter der ikke kunne forbindes");
            });
        }
        public void OnStop(Office.IRibbonControl control){Run(()=>addIn.Controller.StopDevice(settings.SelectedDeviceId));}
        public void OnStopAll(Office.IRibbonControl control){Run(()=>addIn.Controller.StopDevice());}
        public void OnToggle(Office.IRibbonControl control,bool pressed){Run(()=>addIn.Controller.ToggleOverlay());}
        public void OnPreview(Office.IRibbonControl control,bool pressed)
        {Run(()=>{addIn.Controller.SelectDevice(settings.Selected);addIn.Controller.TogglePreview();});}
        public void OnRename(Office.IRibbonControl control)
        {Run(()=>{if(settings.Selected==null)return;using(var dialog=new RenameDialog(settings))dialog.ShowDialog();addIn.Controller.SelectDevice(settings.Selected);});}
        public void OnRemoveDevice(Office.IRibbonControl control)
        {
            Run(()=>{
                var device=settings.Selected;if(device==null)return;
                if(MessageBox.Show("Fjern "+device.ListLabel+" fra listen?\n\n"+
                    "Enhedens livebillede stoppes. Dine slides og rammer bevares, men rammer til denne enhed viser ikke længere livebillede.",
                    "Fjern enhed",MessageBoxButtons.YesNo,MessageBoxIcon.Question,
                    MessageBoxDefaultButton.Button2)!=DialogResult.Yes)return;
                var previousDevices=settings.Devices.ToList();
                string previousSelected=settings.SelectedDeviceId,previousSerial=settings.Serial;
                try{settings.RemoveDevice(device.Id);settings.Save();}
                catch{settings.Devices=previousDevices;settings.SelectedDeviceId=previousSelected;settings.Serial=previousSerial;throw;}
                addIn.Controller.RemoveDevice(device.Id);
                addIn.Controller.SelectDevice(settings.Selected);
            });
        }
        public void OnSettings(Office.IRibbonControl control)
        {Run(()=>{using(var dialog=new SettingsDialog(settings))dialog.ShowDialog();addIn.Controller.SelectDevice(settings.Selected);});}
        public void OnStatus(Office.IRibbonControl control)
        {Run(()=>MessageBox.Show(addIn.Controller.Status+"\n\n"+addIn.Controller.Diagnostics+
            "\n\n"+LiveAndroidController.Version+" · Udviklingsudgave. Livebilleder er eksterne vinduer og medtages ikke i PowerPoint-eksport.",
            "Android Live status",MessageBoxButtons.OK,MessageBoxIcon.Information));}
        private void Run(Action action)
        {
            try{if(addIn.Controller!=null)action();}
            catch(Exception ex){MessageBox.Show(ex.Message,"Android Live",MessageBoxButtons.OK,MessageBoxIcon.Warning);}
            finally{Refresh();}
        }
        private async void RunAsync(Func<Task> action)
        {
            if(busy||addIn.Controller==null)return;
            // Office ribbon callbacks do not guarantee a synchronization context.
            if(SynchronizationContext.Current==null)SynchronizationContext.SetSynchronizationContext(new WindowsFormsSynchronizationContext());
            busy=true;Refresh();
            try{await action();}
            catch(OperationCanceledException){}
            catch(ObjectDisposedException) when(addIn.Controller==null){}
            catch(Exception ex){MessageBox.Show(ex.Message,"Android Live",MessageBoxButtons.OK,MessageBoxIcon.Warning);}
            finally{busy=false;Refresh();}
        }
    }
}