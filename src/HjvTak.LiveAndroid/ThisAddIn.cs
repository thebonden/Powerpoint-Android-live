using System;
using Office = Microsoft.Office.Core;

namespace HjvTak.LiveAndroid
{
    public partial class ThisAddIn
    {
        internal LiveAndroidController Controller { get; private set; }
        private AndroidRibbon ribbon;
        protected override Office.IRibbonExtensibility CreateRibbonExtensibilityObject()
        { return ribbon ?? (ribbon = new AndroidRibbon(this)); }
        private void ThisAddIn_Startup(object sender, EventArgs e)
        {
            Controller = new LiveAndroidController(Application);
            Controller.StatusChanged += OnStatusChanged;
            ribbon?.Refresh();
        }
        private void OnStatusChanged(object sender, EventArgs e) { ribbon?.Refresh(); }
        private void ThisAddIn_Shutdown(object sender, EventArgs e)
        {
            if (Controller == null) return;
            Controller.StatusChanged -= OnStatusChanged;
            Controller.Dispose(); Controller = null;
        }
        private void InternalStartup()
        {
            Startup += ThisAddIn_Startup;
            Shutdown += ThisAddIn_Shutdown;
        }
    }
}
