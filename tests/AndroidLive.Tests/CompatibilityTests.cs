using System;
using System.IO;
using AndroidLive;
using AndroidLive.Core;

internal static class CompatibilityTests
{
    internal static void Run(Action<string,Action> test)
    {
        test("legacy settings load and save neutrally without changing device names or slide keys",()=>{
            string root=Temporary();
            string old=LegacyCompatibility.SettingsPath(root),current=Path.Combine(root,"AndroidLive","settings.xml");
            var settings=new UserSettings();
            settings.Devices.Add(new DeviceProfile{Id="slide-id",Name="My teaching tablet",Endpoint="10.0.0.8:4444",HardwareSerial="known",Audio=true});
            settings.SelectedDeviceId="slide-id";settings.SaveTo(old);
            string before=File.ReadAllText(old);
            var loaded=UserSettings.LoadFrom(current,old);loaded.SaveTo(current);
            Check(loaded.Selected.Id=="slide-id"&&loaded.Selected.Name=="My teaching tablet"&&loaded.Selected.Audio);
            Check(File.ReadAllText(old)==before&&File.Exists(current));
        });
        test("neutral settings take precedence over the legacy profile",()=>{
            string root=Temporary(),current=Path.Combine(root,"current.xml"),old=Path.Combine(root,"old.xml");
            var first=new UserSettings();first.Devices.Add(new DeviceProfile{Name="New"});first.SaveTo(current);
            var second=new UserSettings();second.Devices.Add(new DeviceProfile{Name="Old"});second.SaveTo(old);
            Check(UserSettings.LoadFrom(current,old).Selected.Name=="New");
        });
        test("corrupt current settings do not resurrect an obsolete device list",()=>{
            string root=Temporary(),current=Path.Combine(root,"current.xml"),old=Path.Combine(root,"old.xml");
            File.WriteAllText(current,"not XML");
            var settings=new UserSettings();settings.Devices.Add(new DeviceProfile{Name="Old"});settings.SaveTo(old);
            Check(UserSettings.LoadFrom(current,old).Devices.Count==0);
        });
        test("old and new slide frames retain routing while new identifiers take precedence",()=>{
            Check(SlideIdentity.Resolve("any","","ABCDEF")=="abcdef");
            Check(SlideIdentity.Resolve("any","NEW","OLD")=="new");
            Check(SlideIdentity.Resolve(LegacyCompatibility.SlideName,"","")=="legacy");
            Check(SlideIdentity.Resolve(SlideIdentity.Name,"","")=="legacy");
            Check(SlideIdentity.Resolve("ordinary rectangle","","")==null);
        });
    }
    private static string Temporary(){string path=Path.Combine(Path.GetTempPath(),"AndroidLive-migration-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(path);return path;}
    private static void Check(bool ok){if(!ok)throw new Exception("Assertion failed");}
}