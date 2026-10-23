using System;
using System.IO;
using HjvTak.LiveAndroid.Core;

public sealed class ShadowCopyProbe : MarshalByRefObject
{
    public string[] Resolve()
    {
        var assembly=typeof(ShadowCopyProbe).Assembly;
        return new[]{assembly.Location, new Uri(assembly.CodeBase).LocalPath, DeploymentFiles.QrHelper(assembly)};
    }
    internal static void Test()
    {
        string root=Path.Combine(Path.GetTempPath(),"ppt-shadow-"+Guid.NewGuid().ToString("N"));
        string original=Path.Combine(root,"deployment with space æ");
        string cache=Path.Combine(root,"shadow");
        Directory.CreateDirectory(Path.Combine(original,"tools"));Directory.CreateDirectory(cache);
        string exe=Path.Combine(original,"probe.exe");
        File.Copy(typeof(ShadowCopyProbe).Assembly.Location,exe);
        string helper=Path.Combine(original,"tools","Android-WiFi-QR.exe");File.WriteAllText(helper,"fixture only");
        AppDomain domain=null;
        try {
            domain=AppDomain.CreateDomain("shadow-copy-test",null,new AppDomainSetup {
                ApplicationBase=original,ShadowCopyFiles="true",CachePath=cache,ApplicationName="ppt-shadow-test"
            });
            var probe=(ShadowCopyProbe)domain.CreateInstanceFromAndUnwrap(exe,typeof(ShadowCopyProbe).FullName);
            string[] paths=probe.Resolve();
            if(paths[0]==paths[1])throw new InvalidOperationException("Test assembly was not shadow copied");
            if(paths[2]!=helper || !File.Exists(paths[2]))throw new InvalidOperationException("Helper was not resolved in the original deployment");
        } finally {if(domain!=null)AppDomain.Unload(domain);Directory.Delete(root,true);}
    }
}
