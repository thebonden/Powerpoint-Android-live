using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using HjvTak.LiveAndroid.Core;

namespace HjvTak.LiveAndroid
{
    internal sealed class SettingsDialog : Form
    {
        internal SettingsDialog(UserSettings settings)
        {
            Text = "Android Live settings"; ClientSize = new Size(600, 245);
            AutoScaleMode = AutoScaleMode.Dpi; FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterScreen; MinimizeBox = false; MaximizeBox = false;
            var path = new TextBox { Left = 16, Top = 40, Width = 470, Text = settings.ScrcpyPath };
            var serial = new TextBox { Left = 16, Top = 105, Width = 470, Text = settings.Serial };
            var browse = new Button { Left = 495, Top = 38, Width = 90, Text = "Browse..." };
            browse.Click += (s, e) => {
                using (var picker = new OpenFileDialog { Filter = "scrcpy executable|scrcpy.exe", CheckFileExists = true })
                    if (picker.ShowDialog(this) == DialogResult.OK) path.Text = picker.FileName;
            };
            var find = new Button { Left = 16, Top = 200, Width = 180, Text = "Find automatisk" };
            find.Click += (s, e) => {
                var candidate = new UserSettings { ScrcpyPath = path.Text, Serial = serial.Text };
                using (var dialog = new ScrcpySetupDialog(candidate))
                    if (dialog.ShowDialog(this) == DialogResult.OK) path.Text = candidate.ScrcpyPath;
            };
            var save = new Button { Left = 395, Top = 200, Width = 90, Text = "Save" };
            save.Click += (s, e) => {
                try
                {
                    string executable = path.Text.Trim();
                    if (!ScrcpyLocator.IsComplete(executable))
                        throw new InvalidOperationException("Vælg scrcpy.exe fra hele den udpakkede Windows-pakke, inklusive adb.exe, scrcpy-server og DLL-filer.");
                    if (!string.IsNullOrWhiteSpace(serial.Text) && !WifiEndpoint.IsValid(serial.Text.Trim()))
                        throw new InvalidOperationException("Enter a Wi-Fi IPv4:port connection address, or leave blank and use QR pairing.");
                    settings.ScrcpyPath = executable; settings.Serial = serial.Text.Trim(); settings.Save();
                    DialogResult = DialogResult.OK; Close();
                }
                catch (Exception ex) { MessageBox.Show(this, ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning); }
            };
            var cancel = new Button { Left = 495, Top = 200, Width = 90, Text = "Cancel", DialogResult = DialogResult.Cancel };
            Controls.AddRange(new Control[] {
                new Label { Left = 16, Top = 16, Width = 550, Text = "Official scrcpy.exe (keep the distribution's adb and support files beside it)" }, path, browse, find,
                new Label { Left = 16, Top = 81, Width = 550, Text = "Wi-Fi connection address (optional; QR pairing fills this automatically)" }, serial,
                new Label { Left = 16, Top = 145, Width = 565, Height = 46, Text = "Video-only overlay. Audio and phone control are disabled.\nUse Connect via Wi-Fi / QR; no USB or OBS Studio is needed." }, save, cancel });
            AcceptButton = save; CancelButton = cancel;
        }
    }
}
