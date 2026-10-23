using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using HjvTak.LiveAndroid.Core;

namespace HjvTak.LiveAndroid
{
    public sealed class UserSettings
    {
        public string ScrcpyPath { get; set; } = "";
        public string Serial { get; set; } = ""; // Older installations: migrated without losing the paired endpoint.
        public string SelectedDeviceId { get; set; } = "";
        public List<DeviceProfile> Devices { get; set; } = new List<DeviceProfile>();
        [XmlIgnore] public DeviceProfile Selected => Devices.FirstOrDefault(d => d.Id == SelectedDeviceId);
        private static string SettingsPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HjvTak", "AndroidLive", "settings.xml");
        public void Normalize()
        {
            if (Devices == null) Devices = new List<DeviceProfile>();
            Devices = Devices.Where(d => d != null && !string.IsNullOrWhiteSpace(d.Id)).GroupBy(d=>d.Id).Select(g=>g.First()).ToList();
            if (Devices.Count == 0 && WifiEndpoint.IsValid(Serial))
                Devices.Add(new DeviceProfile { Id = "legacy", Name = "Tablet 1", Endpoint = Serial });
            if (Selected == null) SelectedDeviceId = Devices.FirstOrDefault()?.Id ?? "";
            foreach (var device in Devices)
                if (!Enum.IsDefined(typeof(VideoOrientation),device.Orientation)) device.Orientation = VideoOrientation.Automatic;
        }
        public static UserSettings Load()
        {
            UserSettings settings;
            try {
                using (var reader = File.OpenRead(SettingsPath)) settings = (UserSettings)new XmlSerializer(typeof(UserSettings)).Deserialize(reader);
            }
            catch (InvalidOperationException) { settings = new UserSettings(); }
            catch (IOException) { settings = new UserSettings(); }
            catch (UnauthorizedAccessException) { settings = new UserSettings(); }
            settings.Normalize(); return settings;
        }
        public void Save()
        {
            Normalize();
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath));
            string temporary = SettingsPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try {
                using (var writer = File.Create(temporary)) new XmlSerializer(typeof(UserSettings)).Serialize(writer, this);
                if (File.Exists(SettingsPath)) File.Replace(temporary, SettingsPath, null);
                else File.Move(temporary, SettingsPath);
            }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
        }
    }
}