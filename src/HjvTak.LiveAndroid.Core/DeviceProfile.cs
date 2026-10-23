using System;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace HjvTak.LiveAndroid.Core
{
    public enum VideoOrientation { Automatic, Landscape, Portrait }

    public sealed class DeviceProfile
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string Name { get; set; } = "Tablet";
        public string Endpoint { get; set; } = "";
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