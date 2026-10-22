using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace HjvTak.LiveAndroid.Core
{
    internal sealed class WifiConnector
    {
        private readonly string adb, helper;
        internal WifiConnector(string adbPath, string helperPath) { adb = adbPath; helper = helperPath; }
        internal async Task<string> PairAsync(CancellationToken cancel)
        {
            if (!File.Exists(helper)) throw new FileNotFoundException("The Wi-Fi QR helper is missing. Rebuild/reinstall the complete PowerPoint add-in.");
            var result = await LocalCommand.RunAsync(helper, "--powerpoint --adb " + ScrcpyArguments.Quote(adb),
                cancel, TimeSpan.FromMinutes(4), adb).ConfigureAwait(false);
            if (result.ExitCode != 0) throw new InvalidOperationException("The QR window could not complete pairing. Check the selected scrcpy folder and try again.");
            string endpoint = WifiEndpoint.ParseHelperResult(result.Output);
            if (endpoint == null) return null;
            // Revalidate after helper exit before saving settings or starting a feed.
            return await ConnectAsync(endpoint, cancel).ConfigureAwait(false);
        }
        internal async Task<string> ConnectAsync(string endpoint, CancellationToken cancel)
        {
            if (!WifiEndpoint.IsValid(endpoint)) throw new ArgumentException("Enter the IPv4 address and CONNECTION port from Wireless debugging, for example 192.168.1.50:37123.");
            var connected = await RunAdb("connect " + endpoint, cancel).ConfigureAwait(false);
            // ADB connect can return exit code zero with a failure message: get-state is authoritative.
            var state = await RunAdb("-s " + endpoint + " get-state", cancel).ConfigureAwait(false);
            if (connected.ExitCode != 0 || state.ExitCode != 0 || state.Output.Trim() != "device")
                throw new InvalidOperationException("The tablet is not connected. Use QR pairing, or check the connection port on the tablet's Wireless debugging main screen.");
            return endpoint;
        }
        private Task<CommandResult> RunAdb(string args, CancellationToken cancel)
        {
            if (!File.Exists(adb)) throw new FileNotFoundException("adb.exe must be beside the selected scrcpy.exe. Extract the complete official scrcpy distribution.");
            return LocalCommand.RunAsync(adb, "-H localhost -P 5037 " + args, cancel, TimeSpan.FromSeconds(15), adb);
        }
    }
}
