using System;
using System.Text;

namespace HjvTak.LiveAndroid.Core
{
    public static class ScrcpyArguments
    {
        public static string Build(string title, string serial)
        {
            if (string.IsNullOrWhiteSpace(title)) throw new ArgumentException("Window title is required.");
            // Launch out of view until the STA timer finds the owned video window.
            string args = "--window-title=" + Quote(title) +
                " --window-borderless --window-x=-32000 --window-y=-32000 --window-width=320 --window-height=240" +
                " --no-audio --no-control";
            if (!string.IsNullOrWhiteSpace(serial)) args += " --serial=" + Quote(serial.Trim());
            return args;
        }

        // CommandLineToArgvW/CRT-compatible quoting, including trailing backslashes.
        public static string Quote(string value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            if (value.IndexOf('\0') >= 0 || value.IndexOf('\r') >= 0 || value.IndexOf('\n') >= 0)
                throw new ArgumentException("Arguments must be single-line text.");
            var result = new StringBuilder("\"");
            int slashes = 0;
            foreach (char c in value)
            {
                if (c == '\\') { slashes++; continue; }
                if (c == '"') result.Append('\\', slashes * 2 + 1);
                else result.Append('\\', slashes);
                result.Append(c);
                slashes = 0;
            }
            return result.Append('\\', slashes * 2).Append('"').ToString();
        }
    }
}
