using System;
using System.Text;

namespace AndroidLive.Core
{
    public static class ScrcpyArguments
    {
        public static string Build(string title, string serial, bool audio = false, bool showTouches = false, string captureOrientation = null)
        {
            if (string.IsNullOrWhiteSpace(title)) throw new ArgumentException("Window title is required.");
            // Fit the complete device image inside any slide/preview rectangle. Disable
            // the window aspect lock, not the video aspect ratio; letterbox adds padding.
            // Launch off-screen until scrcpy shows its initialized video window (4.1+).
            string args = "--window-title=" + Quote(title) +
                " --render-fit=letterbox --no-window-aspect-ratio-lock --background-color=000000 --window-borderless --window-x=-32000 --window-y=-32000 --window-width=320 --window-height=240";
            if (!audio) args += " --no-audio";
            // scrcpy rejects --show-touches with --no-control. Disable keyboard, mouse, gamepad and automatic clipboard synchronization.
            args += showTouches ? " --show-touches --keyboard=disabled --mouse=disabled --gamepad=disabled --no-clipboard-autosync" : " --no-control";
            if (captureOrientation != null)
            {
                if (captureOrientation != "@0" && captureOrientation != "@90") throw new ArgumentException("Unsupported capture orientation.");
                args += " --capture-orientation=" + captureOrientation;
            }
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
