using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Office = Microsoft.Office.Core;

namespace HjvTak.LiveAndroid
{
    [ComVisible(true)]
    public sealed class AndroidRibbon : Office.IRibbonExtensibility
    {
        private readonly ThisAddIn addIn;
        private Office.IRibbonUI ribbon;
        internal AndroidRibbon(ThisAddIn addIn) { this.addIn = addIn; }
        public string GetCustomUI(string ribbonId)
        {
            using (var stream = typeof(AndroidRibbon).Assembly.GetManifestResourceStream("HjvTak.LiveAndroid.AndroidRibbon.xml"))
            using (var reader = new StreamReader(stream)) return reader.ReadToEnd();
        }
        public void OnLoad(Office.IRibbonUI ui) { ribbon = ui; }
        internal void Refresh() { try { ribbon?.Invalidate(); } catch (COMException) { } }
        public bool GetEnabled(Office.IRibbonControl control)
        {
            var controller = addIn.Controller;
            if (controller == null) return false;
            if (control.Id == "StartAndroid" || control.Id == "ConnectWifi" || control.Id == "Settings") return !controller.IsActive;
            if (control.Id == "StopAndroid") return controller.IsActive;
            return true;
        }
        public bool GetPressed(Office.IRibbonControl control) => addIn.Controller?.OverlayEnabled ?? false;
        public void OnAddMarker(Office.IRibbonControl control) { Run(() => addIn.Controller.AddAnchorToCurrentSlide()); }
        public void OnConnectWifi(Office.IRibbonControl control) { OpenWifi(false); }
        public void OnStart(Office.IRibbonControl control) { OpenWifi(true); }
        private void OpenWifi(bool reconnect)
        {
            Run(() => {
                var settings = UserSettings.Load();
                if (!Core.ScrcpyLocator.IsComplete(settings.ScrcpyPath))
                {
                    using (var dialog = new ScrcpySetupDialog(settings)) if (dialog.ShowDialog() != DialogResult.OK) return;
                    settings.Save();
                }
                using (var dialog = new WifiDialog(settings.ScrcpyPath, settings.Serial, reconnect))
                {
                    if (dialog.ShowDialog() != DialogResult.OK) return;
                    settings.Serial = dialog.Endpoint;
                }
                settings.Save();
                addIn.Controller.StartDevice(settings.ScrcpyPath, settings.Serial);
            });
        }
        public void OnStop(Office.IRibbonControl control) { Run(() => addIn.Controller.StopDevice()); }
        public void OnToggle(Office.IRibbonControl control, bool pressed) { Run(() => addIn.Controller.ToggleOverlay()); }
        public void OnSettings(Office.IRibbonControl control)
        { Run(() => { using (var dialog = new SettingsDialog(UserSettings.Load())) dialog.ShowDialog(); }); }
        public void OnStatus(Office.IRibbonControl control)
        {
            Run(() => MessageBox.Show(addIn.Controller.Status + "\n\n" + addIn.Controller.Diagnostics +
                "\n\n0.0.005 development build. This overlay is not saved as video in the PPTX or included in PowerPoint exports.",
                "Android Live status", MessageBoxButtons.OK, MessageBoxIcon.Information));
        }
        private void Run(Action action)
        {
            try { if (addIn.Controller != null) action(); }
            catch (Exception ex) { MessageBox.Show(ex.Message, "Android Live", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
            finally { Refresh(); }
        }
    }
}
