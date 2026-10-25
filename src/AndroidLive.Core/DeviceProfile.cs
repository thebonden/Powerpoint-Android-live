using System;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace AndroidLive.Core
{
    public enum VideoOrientation { Automatic, Landscape, Portrait }

    public enum DeviceNameMode { Legacy, Android, Custom }

    public sealed class DeviceProfile
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string Name { get; set; } = "Tablet";
        public string AndroidName { get; set; } = "";
        public DeviceNameMode NameMode { get; set; }
        internal string ListLabel
        {
            get {
                string android=DeviceDiscovery.Clean(AndroidName,60);
                string label=DeviceDiscovery.Clean(Name,60);
                if(android.Length==0)return NameMode==DeviceNameMode.Custom
                    ? "Android-navn ikke hentet — "+label : (label.Length==0?"Tablet":label);
                return label.Length==0 || string.Equals(android,label,StringComparison.Ordinal)
                    ? android : android+" — "+label;
            }
        }
        internal void NormalizeNameMode()
        {
            if(!Enum.IsDefined(typeof(DeviceNameMode),NameMode))NameMode=DeviceNameMode.Legacy;
            if(NameMode!=DeviceNameMode.Legacy)return;
            // Old versions did not record where a name came from. Only known generated
            // placeholders can safely become automatic; preserve all other old names.
            NameMode=string.IsNullOrWhiteSpace(Name) ||
                Regex.IsMatch(Name,@"^Tablet(?: [0-9]+)?$",RegexOptions.CultureInvariant)
                ? DeviceNameMode.Android : DeviceNameMode.Custom;
        }
        internal void ApplyAndroidName(string value)
        {
            NormalizeNameMode();
            string clean=DeviceDiscovery.Clean(value,60);
            if(clean.Length==0)return; // A failed metadata read must not erase a known name.
            AndroidName=clean;
            if(NameMode==DeviceNameMode.Android)Name=clean;
        }
        internal void SetCustomName(string value)
        {
            string clean=DeviceDiscovery.Clean(value,60);
            if(clean.Length==0)throw new ArgumentException("Skriv et navn til enheden.");
            Name=clean;NameMode=DeviceNameMode.Custom;
        }
        internal void UseAndroidName()
        {
            NameMode=DeviceNameMode.Android;
            if(!string.IsNullOrEmpty(AndroidName))Name=AndroidName;
        }
        public string Endpoint { get; set; } = "";
        public string HardwareSerial { get; set; } = "";
        public bool ReconnectOnStartup { get; set; } = true;
        public bool Audio { get; set; }
        public bool ShowTouches { get; set; }
        public VideoOrientation Orientation { get; set; }
    }

    internal static class CaptureOrientation
    {
        internal static string FromSize(string output, VideoOrientation orientation)
        {
            if (orientation == VideoOrientation.Automatic) return null;
            if (orientation != VideoOrientation.Landscape && orientation != VideoOrientation.Portrait)
                throw new ArgumentOutOfRangeException(nameof(orientation));
            var matches = Regex.Matches(output ?? "", @"(?m)^(Physical|Override) size:\s*(\d{1,5})x(\d{1,5})\s*$");
            if (matches.Count == 0) throw new InvalidOperationException("Tablettens skærmstørrelse kunne ikke læses. Vælg Automatisk og prøv igen.");
            // Android's override, if present, is the effective natural display size.
            Match size = matches[0];
            foreach (Match candidate in matches) if (candidate.Groups[1].Value == "Override") size = candidate;
            int width = int.Parse(size.Groups[2].Value), height = int.Parse(size.Groups[3].Value);
            if (width <= 0 || height <= 0 || width == height)
                throw new InvalidOperationException("Skærmen har ingen entydig stående/liggende retning. Vælg Automatisk.");
            bool naturalLandscape = width > height;
            return naturalLandscape == (orientation == VideoOrientation.Landscape) ? "@0" : "@90";
        }

        internal static async Task<string> ResolveAsync(string adb, string endpoint, VideoOrientation orientation, CancellationToken cancel)
        {
            cancel.ThrowIfCancellationRequested();
            if (orientation == VideoOrientation.Automatic) return null;
            if (!WifiEndpoint.IsValid(endpoint)) throw new ArgumentException("Ugyldig tabletadresse.");
            var result = await LocalCommand.RunAsync(adb, "-H localhost -P 5037 -s " + endpoint + " shell wm size",
                cancel, TimeSpan.FromSeconds(8), adb).ConfigureAwait(false);
            if (result.ExitCode != 0) throw new InvalidOperationException("Tablettens orientering kunne ikke læses. Kontroller forbindelsen.");
            return FromSize(result.Output, orientation);
        }
    }
}