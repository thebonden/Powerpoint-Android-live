using System;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using AndroidLive.Setup;
using AndroidLive.Core;

internal static class InstallerTests
{
    private static int passed;
    private static int Main()
    {
        string root=Path.Combine(Path.GetTempPath(),"AndroidLive-setup-tests-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try {
            Test("valid embedded payload expands without a user ZIP folder",()=>{
                using(var zip=Zip("AndroidLive.vsto","prerequisites.exe","tools/licenses/license.txt")){
                    string target=Path.Combine(root,"valid");PackageFiles.Extract(zip,target,Hash(zip));
                    Check(File.Exists(Path.Combine(target,"tools/licenses/license.txt")));
                }
            });
            Test("modified payload fails before extraction",()=>{
                using(var zip=Zip("AndroidLive.vsto","prerequisites.exe"))
                    Reject(()=>PackageFiles.Extract(zip,Path.Combine(root,"badhash"),"WRONG"));
                Check(!Directory.Exists(Path.Combine(root,"badhash")));
            });
            Test("zip traversal is rejected before creating any files",()=>{
                using(var zip=Zip("AndroidLive.vsto","prerequisites.exe","../outside.txt"))
                    Reject(()=>PackageFiles.Extract(zip,Path.Combine(root,"traversal"),Hash(zip)));
                Check(!File.Exists(Path.Combine(root,"outside.txt"))&&!Directory.Exists(Path.Combine(root,"traversal")));
            });
            Test("duplicate case-insensitive Windows paths are rejected",()=>{
                using(var zip=Zip("AndroidLive.vsto","prerequisites.exe","A.txt","a.txt"))
                    Reject(()=>PackageFiles.Extract(zip,Path.Combine(root,"duplicate"),Hash(zip)));
            });
            Test("incomplete payload is rejected",()=>{
                using(var zip=Zip("AndroidLive.vsto"))
                    Reject(()=>PackageFiles.Extract(zip,Path.Combine(root,"incomplete"),Hash(zip)));
            });
            Test("successful file swap keeps settings outside application directory",()=>{
                string home=Path.Combine(root,"upgrade");Directory.CreateDirectory(home);
                string current=Path.Combine(home,"App"),stage=Path.Combine(home,"stage"),backup=Path.Combine(home,"backup");
                Directory.CreateDirectory(current);Directory.CreateDirectory(stage);
                File.WriteAllText(Path.Combine(current,"version"),"old");File.WriteAllText(Path.Combine(stage,"version"),"new");
                File.WriteAllText(Path.Combine(home,"settings.xml"),"tablets");
                PackageFiles.MoveIntoPlace(stage,current,backup);
                Check(File.ReadAllText(Path.Combine(current,"version"))=="new"&&File.ReadAllText(Path.Combine(backup,"version"))=="old");
                PackageFiles.Restore(current,backup,Path.Combine(home,"failed"));
                Check(File.ReadAllText(Path.Combine(current,"version"))=="old"&&File.ReadAllText(Path.Combine(home,"settings.xml"))=="tablets");
            });
            Test("failed file swap restores original directory",()=>{
                string home=Path.Combine(root,"failed-swap");string current=Path.Combine(home,"App");Directory.CreateDirectory(current);
                File.WriteAllText(Path.Combine(current,"version"),"old");
                Reject(()=>PackageFiles.MoveIntoPlace(Path.Combine(home,"missing"),current,Path.Combine(home,"backup")));
                Check(File.ReadAllText(Path.Combine(current,"version"))=="old");
            });
            Test("migration accepts only this product's local manifest",()=>{
                Check(Installation.LocalManifest("file:///C:/Old%20Folder/AndroidLive.vsto|vstolocal")==@"C:\Old Folder\AndroidLive.vsto");
                Check(Installation.LocalManifest("")==null);
                foreach(string value in new[]{"https://example.org/AndroidLive.vsto","file://server/share/AndroidLive.vsto","file:///C:/Other.vsto"})
                    Reject(()=>Installation.LocalManifest(value));
            });
            Test("legacy manifest is accepted only for migration",()=>{
                string old=Path.Combine(@"C:\Old",LegacyCompatibility.Manifest);
                Check(Installation.LocalManifest(new Uri(old).AbsoluteUri)==old);
                Check(PackageFiles.Manifest=="AndroidLive.vsto");
            });
            Test("managed CLI requires explicit silent install or uninstall",()=>{
                var install=CommandLine.Parse(new[]{"--install","--silent"});
                Check(install.Action==SetupAction.Install&&install.Silent);
                var uninstall=CommandLine.Parse(new[]{"--silent","--uninstall","--log",Path.Combine(root,"it.log")});
                Check(uninstall.Action==SetupAction.Uninstall&&uninstall.LogPath.EndsWith("it.log"));
                Check(CommandLine.Parse(new[]{"--detect"}).Action==SetupAction.Detect);
                Check(CommandLine.Parse(new string[0]).Action==SetupAction.Interactive);
            });
            Test("invalid or ambiguous CLI never falls back to interactive install",()=>{
                foreach(var args in new[]{new[]{"--silent"},new[]{"--install"},new[]{"--install","--uninstall","--silent"},
                    new[]{"--install","--silent","--silent"},new[]{"--detect","--install"},new[]{"--install","--silent","--log"},new[]{"--unknown"}})
                    Reject(()=>CommandLine.Parse(args));
            });
            Test("deployment version detection requires a valid minimum version",()=>{
                string manifest=Path.Combine(root,"version.vsto");
                File.WriteAllText(manifest,"<assembly><assemblyIdentity version='0.0.102.0'/></assembly>");
                Check(DeploymentPolicy.MeetsVersion(manifest,"0.0.102.0"));
                Check(!DeploymentPolicy.MeetsVersion(manifest,"0.0.103.0"));
                File.WriteAllText(manifest,"<assembly><assemblyIdentity version='invalid'/></assembly>");
                Check(!DeploymentPolicy.MeetsVersion(manifest,"0.0.102.0"));
            });
            Test("unsigned manifests fail managed-trust preflight",()=>{
                string manifest=Path.Combine(root,"unsigned.vsto");
                File.WriteAllText(manifest,"<assembly><assemblyIdentity version='0.0.102.0'/></assembly>");
                bool refused=false;try{DeploymentPolicy.RequireManagedTrust(manifest);}catch(SetupFailure ex){refused=ex.ExitCode==1625;}
                Check(refused);
            });
            Test("manifest parser refuses external DTDs",()=>{
                string manifest=Path.Combine(root,"dtd.vsto");
                File.WriteAllText(manifest,"<!DOCTYPE assembly SYSTEM 'file:///C:/must-not-read'><assembly/>");
                Check(!DeploymentPolicy.MeetsVersion(manifest,"0.0.102.0"));
            });
            Console.WriteLine("Installer tests: "+passed+" passed.");return 0;
        }catch(Exception ex){Console.WriteLine("FAIL "+ex);return 1;}
        // Retain this unique, isolated test directory for diagnostics; never delete broad user paths.
    }
    private static MemoryStream Zip(params string[] names)
    {
        var stream=new MemoryStream();
        using(var zip=new ZipArchive(stream,ZipArchiveMode.Create,true))
            foreach(var name in names)using(var writer=new StreamWriter(zip.CreateEntry(name).Open()))writer.Write("test");
        stream.Position=0;return stream;
    }
    private static string Hash(MemoryStream stream)
    {
        stream.Position=0;string value;
        using(var hash=SHA256.Create())value=BitConverter.ToString(hash.ComputeHash(stream)).Replace("-","");
        stream.Position=0;return value;
    }
    private static void Check(bool value){if(!value)throw new Exception("Assertion failed");}
    private static void Reject(Action action){try{action();}catch(InvalidDataException){return;}catch(IOException){return;}catch(InvalidOperationException){return;}catch(ArgumentException){return;}throw new Exception("Expected rejection");}
    private static void Test(string name,Action action){action();passed++;Console.WriteLine("PASS "+name);}
}