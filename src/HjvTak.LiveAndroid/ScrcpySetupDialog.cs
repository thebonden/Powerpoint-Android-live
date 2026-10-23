using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using HjvTak.LiveAndroid.Core;

namespace HjvTak.LiveAndroid
{
    internal sealed class ScrcpySetupDialog : Form
    {
        private const string InstallUrl = "https://github.com/Genymobile/scrcpy/blob/master/doc/windows.md";
        private readonly UserSettings settings;
        private readonly Label status;
        private readonly Button retry, install, browse;
        private readonly CancellationTokenSource cancellation = new CancellationTokenSource();
        private bool busy, disposed;
        internal ScrcpySetupDialog(UserSettings settings)
        {
            this.settings = settings;
            Text = "Klargør tabletforbindelsen"; ClientSize = new Size(610, 270);
            AutoScaleMode = AutoScaleMode.Dpi; FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterScreen; MinimizeBox = false; MaximizeBox = false;
            status = new Label { Left=20, Top=20, Width=565, Height=140, Text="Søger efter scrcpy på din pc…" };
            install = new Button { Left=20, Top=170, Width=200, Height=36, Text="Installér scrcpy…", Enabled=false };
            retry = new Button { Left=235, Top=170, Width=165, Height=36, Text="Søg igen", Enabled=false };
            browse = new Button { Left=415, Top=170, Width=170, Height=36, Text="Vælg mappe…", Enabled=false };
            var cancel = new Button { Left=465, Top=225, Width=120, Text="Annuller", DialogResult=DialogResult.Cancel };
            Controls.AddRange(new Control[] {status,install,retry,browse,cancel}); CancelButton=cancel;
            install.Click += (s,e) => {
                try {
                    Process.Start(new ProcessStartInfo(InstallUrl) { UseShellExecute=true });
                    status.Text="Installér scrcpy via den officielle vejledning, eller hent og udpak hele Windows-pakken.\n\nVend derefter tilbage hertil og klik Søg igen. Hvis du har udpakket et andet sted, kan du vælge mappen.";
                } catch (Exception ex) { status.Text="Vejledningen kunne ikke åbnes: " + ex.Message + "\n" + InstallUrl; }
            };
            retry.Click += async (s,e) => await Search();
            browse.Click += (s,e) => {
                using (var picker = new OpenFileDialog { Filter="scrcpy|scrcpy.exe", CheckFileExists=true })
                    if(picker.ShowDialog(this)==DialogResult.OK) Accept(picker.FileName);
            };
            Shown += async (s,e) => await Search();
            FormClosing += (s,e) => cancellation.Cancel();
        }
        private async Task Search()
        {
            if(busy || cancellation.IsCancellationRequested) return;
            busy=true; retry.Enabled=install.Enabled=browse.Enabled=false; status.Text="Søger efter scrcpy på din pc…";
            var token=cancellation.Token;
            try {
                string found=await Task.Run(() => ScrcpyLocator.Find(settings.ScrcpyPath, PathFolders(), SearchRoots(), token),token);
                if(IsDisposed || token.IsCancellationRequested) return;
                if(found!=null) { Accept(found); return; }
                status.Text="scrcpy skal installeres, før tabletten kan forbindes. Det er programmet, der viser tabletbilledet.\n\nKlik Installér scrcpy for den officielle vejledning. Når installationen er færdig, klik Søg igen. En ufuldstændig pakke skal udpakkes igen.";
            } catch(OperationCanceledException) { }
            catch(Exception ex) { if(!IsDisposed && !token.IsCancellationRequested) status.Text="Søgningen kunne ikke gennemføres: "+ex.Message; }
            finally { busy=false; if(!IsDisposed && !token.IsCancellationRequested) retry.Enabled=install.Enabled=browse.Enabled=true; }
        }
        private void Accept(string path)
        {
            if(!ScrcpyLocator.IsComplete(path)) { status.Text="Mappen mangler dele af scrcpy. Udpak hele den officielle Windows-pakke, inklusive adb.exe, scrcpy-server og DLL-filerne."; return; }
            settings.ScrcpyPath=path;
            DialogResult=DialogResult.OK; Close();
        }
        private static IEnumerable<string> PathFolders()
        {
            foreach(var target in new[] {EnvironmentVariableTarget.User,EnvironmentVariableTarget.Machine,EnvironmentVariableTarget.Process})
                foreach(string entry in (Environment.GetEnvironmentVariable("PATH",target) ?? "").Split(';')) yield return entry;
        }
        private static IEnumerable<string> SearchRoots()
        {
            string local=Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string profile=Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            yield return Path.Combine(local,"Microsoft","WinGet","Packages");
            yield return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),"WinGet","Packages");
            yield return Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            yield return Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            yield return Path.Combine(local,"Programs");
            yield return Path.Combine(profile,"Downloads");
            yield return Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            yield return Path.Combine(profile,"scoop","apps","scrcpy");
            yield return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),"chocolatey","lib","scrcpy","tools");
        }
        protected override void Dispose(bool disposing)
        {
            if(disposing && !disposed) { disposed=true; cancellation.Cancel(); cancellation.Dispose(); }
            base.Dispose(disposing);
        }
    }
}
