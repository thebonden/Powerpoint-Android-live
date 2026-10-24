using System;
using System.IO;

namespace AndroidLive.Core
{
    // Read-only identifiers from released development builds. Never use these for new branding.
    internal static class LegacyCompatibility
    {
        internal const string ProductId="HjvTak.LiveAndroid";
        internal const string Manifest="HjvTak.LiveAndroid.vsto";
        internal const string SlideTag="HJVTAK_ANDROID_DEVICE";
        internal const string SlideName="HJVTAK_ANDROID_LIVE";
        internal static string SettingsPath(string localAppData) =>
            Path.Combine(localAppData,"HjvTak","AndroidLive","settings.xml");
    }
    internal static class SlideIdentity
    {
        internal const string Name="ANDROID_LIVE_FRAME";
        internal const string Tag="ANDROID_LIVE_DEVICE";
        internal static string Resolve(string name,string currentTag,string legacyTag)
        {
            string id=!string.IsNullOrEmpty(currentTag)?currentTag:legacyTag;
            if(!string.IsNullOrEmpty(id))return id.ToLowerInvariant();
            return name==Name||name==LegacyCompatibility.SlideName?"legacy":null;
        }
    }
}