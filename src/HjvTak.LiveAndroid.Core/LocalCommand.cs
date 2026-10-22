using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HjvTak.LiveAndroid.Core
{
    internal sealed class CommandResult
    {
        internal int ExitCode;
        internal string Output;
        internal string Error;
    }

    internal static class LocalCommand
    {
        internal static void SetAdbEnvironment(ProcessStartInfo start, string adbPath)
        {
            start.EnvironmentVariables["ADB"] = adbPath;
            start.EnvironmentVariables["ADB_SERVER_SOCKET"] = "tcp:localhost:5037";
            start.EnvironmentVariables["ANDROID_ADB_SERVER_ADDRESS"] = "localhost";
            start.EnvironmentVariables["ANDROID_ADB_SERVER_PORT"] = "5037";
        }

        // Only waits for/kills the process created here. Never kills the shared ADB server.
        internal static async Task<CommandResult> RunAsync(string executable, string arguments,
            CancellationToken cancellation, TimeSpan timeout, string adbPath = null)
        {
            cancellation.ThrowIfCancellationRequested();
            var start = new ProcessStartInfo(executable, arguments) {
                UseShellExecute = false, CreateNoWindow = true, WorkingDirectory = Path.GetDirectoryName(executable),
                RedirectStandardOutput = true, RedirectStandardError = true
            };
            if (adbPath != null) SetAdbEnvironment(start, adbPath);
            using (var process = new Process { StartInfo = start })
            {
                bool started = false;
                try
                {
                    if (!process.Start()) throw new InvalidOperationException("Could not start the connection tool.");
                    started = true;
                    Task<string> output = ReadBounded(process.StandardOutput);
                    Task<string> error = ReadBounded(process.StandardError);
                    var clock = Stopwatch.StartNew();
                    while (!process.HasExited)
                    {
                        cancellation.ThrowIfCancellationRequested();
                        if (output.IsFaulted) await output.ConfigureAwait(false);
                        if (error.IsFaulted) await error.ConfigureAwait(false);
                        if (clock.Elapsed >= timeout) throw new TimeoutException("The Wi-Fi connection timed out. Check Wireless debugging and try again.");
                        await Task.Delay(50, cancellation).ConfigureAwait(false);
                    }
                    var readers = Task.WhenAll(output, error);
                    if (await Task.WhenAny(readers, Task.Delay(1500, cancellation)).ConfigureAwait(false) != readers)
                    { cancellation.ThrowIfCancellationRequested(); throw new IOException("The connection tool did not close its output pipe."); }
                    await readers.ConfigureAwait(false);
                    cancellation.ThrowIfCancellationRequested();
                    return new CommandResult { ExitCode = process.ExitCode, Output = output.Result, Error = error.Result };
                }
                finally
                {
                    if (started && !process.HasExited)
                    {
                        await Task.Run(() => {
                            try {
                                process.CloseMainWindow();
                                if (!process.WaitForExit(1500)) { process.Kill(); process.WaitForExit(1000); }
                            }
                            catch (InvalidOperationException) { }
                            catch (Win32Exception) { }
                        }).ConfigureAwait(false);
                    }
                }
            }
        }
        private static async Task<string> ReadBounded(StreamReader reader)
        {
            var text = new StringBuilder();
            var buffer = new char[1024];
            int count;
            while ((count = await reader.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false)) != 0)
            {
                if (text.Length + count > 32768) throw new IOException("The connection tool produced too much output.");
                text.Append(buffer, 0, count);
            }
            return text.ToString();
        }
    }
}
