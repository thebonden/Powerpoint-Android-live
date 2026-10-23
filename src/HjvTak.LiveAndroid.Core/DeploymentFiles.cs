using System;
using System.IO;
using System.Reflection;

namespace HjvTak.LiveAndroid.Core
{
    internal static class DeploymentFiles
    {
        internal static string QrHelper(Assembly assembly)
        {
            // VSTO shadow-copies managed DLLs without their content files.
            // CodeBase retains the original signed deployment/cache location.
            var source = new Uri(assembly.CodeBase, UriKind.Absolute);
            if (!source.IsFile) throw new InvalidOperationException("The installed add-in must have a local deployment location.");
            return Path.Combine(Path.GetDirectoryName(source.LocalPath), "tools", "Android-WiFi-QR.exe");
        }
    }
}
