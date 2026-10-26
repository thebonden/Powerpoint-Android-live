using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AndroidLive.Core
{
    internal sealed class DeviceConnection
    {
        internal string Endpoint, Name, Serial;
    }

    // Metadata is optional. Connection identity is required before selecting a rediscovered endpoint.
    internal sealed class DeviceDiscovery
    {
        private readonly Func<string,CancellationToken,Task<CommandResult>> run;
        internal DeviceDiscovery(string adb) : this((args,cancel) =>
            LocalCommand.RunAsync(adb,"-H localhost -P 5037 "+args,cancel,TimeSpan.FromSeconds(4),adb)) {}
        internal DeviceDiscovery(Func<string,CancellationToken,Task<CommandResult>> command) { run=command; }

        internal static string Clean(string value, int limit)
        {
            value=(value??"").Trim();
            if(value.Length==0 || value.Equals("null",StringComparison.OrdinalIgnoreCase) ||
                value.Equals("unknown",StringComparison.OrdinalIgnoreCase) || value.Any(char.IsControl))return "";
            if(value.Length>limit) {
                int length=limit;
                if(char.IsHighSurrogate(value[length-1]))length--;
                value=value.Substring(0,length);
            }
            return value;
        }
        private async Task<string> Read(string args,CancellationToken cancel)
        {
            cancel.ThrowIfCancellationRequested();
            try {
                var result=await run(args,cancel).ConfigureAwait(false);
                return result.ExitCode==0?result.Output:"";
            }
            catch(TimeoutException){return "";}
            catch(IOException){return "";}
            catch(System.ComponentModel.Win32Exception){return "";}
        }
        internal async Task<DeviceConnection> InspectAsync(string endpoint,CancellationToken cancel)
        {
            if(!WifiEndpoint.IsValid(endpoint))throw new ArgumentException("Ugyldig tabletadresse.");
            string prefix="-s "+endpoint+" shell ";
            string name=Clean(await Read(prefix+"settings get global device_name",cancel).ConfigureAwait(false),60);
            if(name.Length==0)name=Clean(await Read(prefix+"getprop ro.product.model",cancel).ConfigureAwait(false),60);
            string serial=Clean(await Read(prefix+"getprop ro.serialno",cancel).ConfigureAwait(false),128);
            cancel.ThrowIfCancellationRequested();
            return new DeviceConnection{Endpoint=endpoint,Name=name,Serial=serial};
        }
        internal async Task<bool> ProbeAsync(DeviceProfile device,CancellationToken cancel)
        {
            if(!WifiEndpoint.IsValid(device.Endpoint)||string.IsNullOrEmpty(device.HardwareSerial))return false;
            string serial=Clean(await Read("-s "+device.Endpoint+" shell getprop ro.serialno",cancel).ConfigureAwait(false),128);
            cancel.ThrowIfCancellationRequested();
            return serial==device.HardwareSerial;
        }
        internal static IEnumerable<string> ParseServices(string output)
        {
            return (output??"").Split('\n').Select(line=>line.Trim().Split((char[])null,StringSplitOptions.RemoveEmptyEntries))
                .Where(parts=>parts.Length==3 && (parts[1]=="_adb-tls-connect._tcp." || parts[1]=="_adb-tls-connect._tcp") &&
                    WifiEndpoint.IsValid(parts[2])).Select(parts=>parts[2]).Distinct().Take(12);
        }
        private async Task<DeviceConnection> TryConnect(string endpoint,string serial,CancellationToken cancel)
        {
            if(!WifiEndpoint.IsValid(endpoint))return null;
            await Read("connect "+endpoint,cancel).ConfigureAwait(false);
            if((await Read("-s "+endpoint+" get-state",cancel).ConfigureAwait(false)).Trim()!="device")return null;
            var found=await InspectAsync(endpoint,cancel).ConfigureAwait(false);
            if(!string.IsNullOrEmpty(serial) && found.Serial!=serial)return null;
            return found;
        }
        internal async Task<DeviceConnection> ReconnectAsync(DeviceProfile device,CancellationToken cancel)
        {
            // Bound the complete search, not just each individual process.
            using(var deadline=CancellationTokenSource.CreateLinkedTokenSource(cancel)) {
                deadline.CancelAfter(TimeSpan.FromSeconds(35));
                try {
                    var found=await TryConnect(device.Endpoint,device.HardwareSerial,deadline.Token).ConfigureAwait(false);
                    if(found!=null)return found;
                    if(!string.IsNullOrEmpty(device.HardwareSerial)) {
                        string services=await Read("mdns services",deadline.Token).ConfigureAwait(false);
                        foreach(string endpoint in ParseServices(services).Where(e=>e!=device.Endpoint)) {
                            found=await TryConnect(endpoint,device.HardwareSerial,deadline.Token).ConfigureAwait(false);
                            if(found!=null)return found;
                        }
                    }
                }
                catch(OperationCanceledException) when(!cancel.IsCancellationRequested) { }
            }
            cancel.ThrowIfCancellationRequested();
            throw new InvalidOperationException("Tabletten blev ikke fundet. Tænd Trådløs fejlfinding og brug samme Wi-Fi. Prøv Forbind valgt igen. Brug kun Opdater valgt / QR, hvis parringen mangler, eller adressen skal opdateres manuelt. Dine slides bevares.");
        }
    }
}