using System;
using System.IO;
using System.Xml.Serialization;

namespace HjvTak.LiveAndroid
{
    public sealed class UserSettings
    {
        public string ScrcpyPath { get; set; } = "";
        public string Serial { get; set; } = "";
        private static string SettingsPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HjvTak", "AndroidLive", "settings.xml");
        public static UserSettings Load()
        {
            if (!File.Exists(SettingsPath)) return new UserSettings();
            try { using (var reader = File.OpenRead(SettingsPath)) return (UserSettings)new XmlSerializer(typeof(UserSettings)).Deserialize(reader); }
            catch (InvalidOperationException) { return new UserSettings(); }
            catch (IOException) { return new UserSettings(); }
            catch (UnauthorizedAccessException) { return new UserSettings(); }
        }
        public void Save()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath));
            string temporary = SettingsPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                using (var writer = File.Create(temporary)) new XmlSerializer(typeof(UserSettings)).Serialize(writer, this);
                if (File.Exists(SettingsPath)) File.Replace(temporary, SettingsPath, null);
                else File.Move(temporary, SettingsPath);
            }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
        }
    }
}
