using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using HjvTak.LiveAndroid.Core;

namespace HjvTak.LiveAndroid
{
    internal sealed class ScrcpySession : IDisposable
    {
        private Process process;
        private string title;
        private IntPtr window;
        private readonly object logLock = new object();
        private string log = "";
        internal SessionState Status { get; } = new SessionState();
        internal string Diagnostics { get { lock (logLock) return log; } }
        internal IntPtr Window => process != null && NativeWindow.IsOwnedBy(window, process.Id) ? window : IntPtr.Zero;

        internal void Start(string executable, string serial)
        {
            Stop();
            if (!Path.IsPathRooted(executable ?? "") || !File.Exists(executable) ||
                !string.Equals(Path.GetExtension(executable), ".exe", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Choose the full path to the official scrcpy.exe in Settings.");
            title = "HJVTAK-LIVE-" + Guid.NewGuid().ToString("N");
            lock (logLock) log = "";
            var start = new ProcessStartInfo(executable, ScrcpyArguments.Build(title, serial))
            {
                WorkingDirectory = Path.GetDirectoryName(executable), UseShellExecute = false,
                CreateNoWindow = true, RedirectStandardError = true, RedirectStandardOutput = true
            };
            process = new Process { StartInfo = start };
            process.ErrorDataReceived += Capture;
            process.OutputDataReceived += Capture;
            try
            {
                Status.Start(DateTime.UtcNow);
                if (!process.Start()) throw new InvalidOperationException("Could not start scrcpy.");
                process.BeginErrorReadLine();
                process.BeginOutputReadLine();
            }
            catch (Exception ex) { Release(); Status.Fail(ex.Message); throw; }
        }
        // Called only by the UI-thread timer: no COM access from process callbacks.
        internal void Poll()
        {
            if (process == null) return;
            bool exited = process.HasExited;
            if (!exited && Window == IntPtr.Zero) window = NativeWindow.FindVideo(process.Id, title);
            if (Status.Poll(DateTime.UtcNow, exited, exited ? process.ExitCode : 0, Window != IntPtr.Zero)) Release();
        }
        private void Capture(object sender, DataReceivedEventArgs e)
        {
            if (!ReferenceEquals(sender, process) || string.IsNullOrEmpty(e.Data)) return;
            lock (logLock)
            {
                log += e.Data + Environment.NewLine;
                if (log.Length > 8192) log = log.Substring(log.Length - 8192);
            }
        }
        internal void Hide() { NativeWindow.Hide(Window); }
        internal void Stop() { Release(); Status.Stop(); }
        private void Release()
        {
            if (process == null) return;
            var owned = process;
            Hide();
            NativeWindow.Close(Window);
            try
            {
                // Bounded wait only on explicit stop/shutdown/failure, never on each slide change.
                if (!owned.HasExited && !owned.WaitForExit(250)) owned.Kill();
            }
            catch (InvalidOperationException) { }
            catch (Win32Exception ex) { lock (logLock) log += "Stop: " + ex.Message; }
            finally
            {
                owned.ErrorDataReceived -= Capture;
                owned.OutputDataReceived -= Capture;
                owned.Dispose();
                process = null; window = IntPtr.Zero; title = null;
            }
        }
        public void Dispose() { Stop(); }
    }
}
