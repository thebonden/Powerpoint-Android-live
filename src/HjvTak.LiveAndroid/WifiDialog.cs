using System;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using HjvTak.LiveAndroid.Core;

namespace HjvTak.LiveAndroid
{
    internal sealed class WifiDialog : Form
    {
        private readonly WifiConnector connector;
        private readonly CancellationTokenSource cancellation = new CancellationTokenSource();
        private readonly TextBox address;
        private readonly Label status;
        private readonly Button qr, connect;
        private bool busy, disposalRequested;
        internal string Endpoint { get; private set; }

        internal WifiDialog(string scrcpyPath, string savedEndpoint, bool reconnect)
        {
            string adb = Path.Combine(Path.GetDirectoryName(scrcpyPath), "adb.exe");
            if (!File.Exists(adb)) throw new FileNotFoundException("adb.exe mangler ved siden af scrcpy.exe. Udpak hele scrcpy-pakken, og vælg den under Settings.");
            string helper = DeploymentFiles.QrHelper(typeof(WifiDialog).Assembly);
            connector = new WifiConnector(adb, helper);
            Text = "Forbind tablet via Wi-Fi · PowerPoint"; ClientSize = new Size(610, 300);
            AutoScaleMode = AutoScaleMode.Dpi; FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterScreen; MaximizeBox = false; MinimizeBox = false;
            qr = new Button { Left=20, Top=90, Width=260, Height=38, Text="Forbind med QR-kode" };
            address = new TextBox { Left=20, Top=170, Width=400, Text=WifiEndpoint.IsValid(savedEndpoint) ? savedEndpoint : "" };
            connect = new Button { Left=435, Top=167, Width=155, Height=30, Text="Forbind til adressen" };
            status = new Label { Left=20, Top=215, Width=570, Height=65, Text="Ingen USB-forbindelse eller OBS Studio er nødvendig." };
            var cancel = new Button { Left=470, Top=90, Width=120, Height=38, Text="Annuller", DialogResult=DialogResult.Cancel };
            Controls.AddRange(new Control[] {
                new Label { Left=20, Top=15, Width=570, Height=65, Text="PC og tablet skal være på samme lokale Wi-Fi. På Android 11+: åbn Udviklerindstillinger → Trådløs fejlfinding → Par med QR-kode.\nBrug scanneren dér, ikke den almindelige kamera-app." },
                qr, cancel, new Label { Left=20, Top=145, Width=570, Text="Allerede parret? Brug IP:port fra hovedsiden Trådløs fejlfinding:" }, address, connect, status
            });
            qr.Click += async (s,e) => await Connect(true);
            connect.Click += async (s,e) => await Connect(false);
            CancelButton = cancel;
            Shown += async (s,e) => await Connect(!reconnect || !WifiEndpoint.IsValid(address.Text));
            FormClosing += (s,e) => cancellation.Cancel();
        }
        private async Task Connect(bool pairing)
        {
            if (busy || cancellation.IsCancellationRequested) return;
            busy=true; qr.Enabled=false; connect.Enabled=false; address.Enabled=false;
            status.Text=pairing ? "QR-vinduet åbnes. Scan koden på tabletten…" : "Kontrollerer Wi-Fi-forbindelsen…";
            try
            {
                string endpoint = pairing ? await connector.PairAsync(cancellation.Token) : await connector.ConnectAsync(address.Text.Trim(), cancellation.Token);
                if (IsDisposed || cancellation.IsCancellationRequested) return;
                if (endpoint == null) { status.Text="Parringen blev annulleret. Du kan prøve igen."; return; }
                Endpoint=endpoint; DialogResult=DialogResult.OK; Close();
            }
            catch (OperationCanceledException) { }
            catch (Exception ex) { if (!IsDisposed && !cancellation.IsCancellationRequested) status.Text=ex.Message; }
            finally
            {
                busy=false;
                if (disposalRequested) cancellation.Dispose();
                if (!IsDisposed && !cancellation.IsCancellationRequested) { qr.Enabled=true; connect.Enabled=true; address.Enabled=true; }
            }
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing && !disposalRequested) { disposalRequested=true; cancellation.Cancel(); if (!busy) cancellation.Dispose(); }
            base.Dispose(disposing);
        }
    }
}
