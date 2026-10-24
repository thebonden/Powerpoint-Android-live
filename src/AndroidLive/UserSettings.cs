using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using AndroidLive.Core;

namespace AndroidLive
{
    public sealed class UserSettings
    {
        public string ScrcpyPath { get; set; } = "";
        public string Serial { get; set; } = ""; // Older installations: migrated without losing the paired endpoint.
        public string SelectedDeviceId { get; set; } = "";
        public List<DeviceProfile> Devices { get; set; } = new List<DeviceProfile>();
        [XmlIgnore] public DeviceProfile Selected => Devices.FirstOrDefault(d => d.Id == SelectedDeviceId);
        private static string SettingsPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AndroidLive", "settings.xml");
        internal DeviceProfile AcceptConnection(DeviceProfile device, DeviceConnection connection)
        {
            if(connection==null || !WifiEndpoint.IsValid(connection.Endpoint))throw new ArgumentException("Ugyldig forbindelse.");
            var existing=Devices.FirstOrDefault(d=>d!=device &&
                ((!string.IsNullOrEmpty(connection.Serial) && d.HardwareSerial==connection.Serial) ||
                 (d.Endpoint==connection.Endpoint && (string.IsNullOrEmpty(connection.Serial)||string.IsNullOrEmpty(d.HardwareSerial)))));
            if(existing!=null) {
                if(device!=null)throw new InvalidOperationException("Denne tablet er allerede gemt som "+existing.Name+". Vælg den i listen. Den valgte tablets slides er ikke ændret.");
                device=existing;
            }
            if(device==null) {
                device=new DeviceProfile{Name="Tablet "+(Devices.Count+1),NameMode=DeviceNameMode.Android};
                Devices.Add(device);
            }
            device.ApplyAndroidName(connection.Name);
            device.Endpoint=connection.Endpoint;device.HardwareSerial=connection.Serial;
            SelectedDeviceId=device.Id;
            return device;
        }
        internal bool RemoveDevice(string id)
        {
            int index=Devices.FindIndex(d=>d.Id==id);
            if(index<0)return false;
            Devices.RemoveAt(index);
            // Retired single-device migration data must not recreate a removed profile.
            Serial="";
            if(SelectedDeviceId==id)
                SelectedDeviceId=Devices.Count==0?"":Devices[Math.Min(index,Devices.Count-1)].Id;
            return true;
        }
        public void Normalize()
        {
            if (Devices == null) Devices = new List<DeviceProfile>();
            Devices = Devices.Where(d => d != null && !string.IsNullOrWhiteSpace(d.Id)).GroupBy(d=>d.Id).Select(g=>g.First()).ToList();
            if (Devices.Count == 0 && WifiEndpoint.IsValid(Serial))
                Devices.Add(new DeviceProfile { Id = "legacy", Name = "Tablet 1", Endpoint = Serial });
            if (Selected == null) SelectedDeviceId = Devices.FirstOrDefault()?.Id ?? "";
            foreach (var device in Devices) {
                device.NormalizeNameMode();
                if (!Enum.IsDefined(typeof(VideoOrientation),device.Orientation)) device.Orientation = VideoOrientation.Automatic;
            }
        }
        public static UserSettings Load() => LoadFrom(SettingsPath,
            LegacyCompatibility.SettingsPath(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)));
        internal static UserSettings LoadFrom(string currentPath,string legacyPath)
        {
            UserSettings settings;
            // Only use legacy data if the neutral settings file has never been created.
            // A corrupt current file must not resurrect an old device list.
            string path=File.Exists(currentPath)?currentPath:legacyPath;
            try {
                using (var reader = File.OpenRead(path)) settings = (UserSettings)new XmlSerializer(typeof(UserSettings)).Deserialize(reader);
            }
            catch (InvalidOperationException) { settings = new UserSettings(); }
            catch (IOException) { settings = new UserSettings(); }
            catch (UnauthorizedAccessException) { settings = new UserSettings(); }
            settings.Normalize(); return settings;
        }
        public void Save() => SaveTo(SettingsPath);
        internal void SaveTo(string path)
        {
            Normalize();
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            string temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try {
                using (var writer = File.Create(temporary)) new XmlSerializer(typeof(UserSettings)).Serialize(writer, this);
                if (File.Exists(path)) File.Replace(temporary, path, null);
                else File.Move(temporary, path);
            }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
        }
    }
}