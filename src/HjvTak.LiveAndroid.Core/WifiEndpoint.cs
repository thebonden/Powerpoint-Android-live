using System;
using System.Globalization;

namespace HjvTak.LiveAndroid.Core
{
    public static class WifiEndpoint
    {
        // Same explicit IPv4 connection-address contract as the reused QR helper.
        public static bool IsValid(string endpoint)
        {
            if (string.IsNullOrEmpty(endpoint) || endpoint.Length > 21) return false;
            var parts = endpoint.Split('.', ':');
            if (parts.Length != 5 || endpoint.LastIndexOf(':') != endpoint.LastIndexOfAny(new[] { '.', ':' })) return false;
            for (int i = 0; i < parts.Length; i++)
            {
                string part = parts[i];
                if (part.Length == 0 || (part.Length > 1 && part[0] == '0')) return false;
                foreach (char c in part) if (c < '0' || c > '9') return false;
                int value;
                if (!int.TryParse(part, NumberStyles.None, CultureInfo.InvariantCulture, out value) || value > (i == 4 ? 65535 : 255)) return false;
                if (i == 0 && (value == 0 || value == 127 || value >= 224)) return false;
                if (i == 3 && value == 255) return false;
                if (i == 4 && value == 0) return false;
            }
            // Enforce exact separators, not merely five numeric components.
            return endpoint == string.Join(".", parts[0], parts[1], parts[2], parts[3]) + ":" + parts[4];
        }
        public static string ParseHelperResult(string output)
        {
            if (string.IsNullOrWhiteSpace(output)) return null; // The user closed the helper.
            const string prefix = "ANDROID_PPT_QR_V1 ";
            string value = output.Trim();
            if (!value.StartsWith(prefix, StringComparison.Ordinal) || !IsValid(value.Substring(prefix.Length)))
                throw new InvalidOperationException("QR helper returned an invalid connection address. Try pairing again.");
            return value.Substring(prefix.Length);
        }
    }
}
