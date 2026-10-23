using System;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using HjvTak.Setup;

internal static class InstallerTests
{
    private static int passed;
    private static int Main()
    {
        string root=Path.Combine(Path.GetTempPath(),"HjvTak-setup-tests-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try {
            Test("valid embedded payload expands without a user ZIP folder",()=>{
                using(var zip=Zip("HjvTak.LiveAndroid.vsto","prerequisites.exe","tools/licenses/license.txt")){
                    string target=Path.Combine(root,"valid");PackageFiles.Extract(zip,target,Hash(zip));
                    Check(File.Exists(Path.Combine(target,"tools/licenses/license.txt")));
                }
            });
            Test("modified payload fails before extraction",()=>{
                using(var zip=Zip("HjvTak.LiveAndroid.vsto","prerequisites.exe"))
                    Reject(()=>PackageFiles.Extract(zip,Path.Combine(root,"badhash"),"WRONG"));
                Check(!Directory.Exists(Path.Combine(root,"badhash")));
            });
            Test("zip traversal is rejected before creating any files",()=>{
                using(var zip=Zip("HjvTak.LiveAndroid.vsto","prerequisites.exe","../outside.txt"))
                    Reject(()=>PackageFiles.Extract(zip,Path.Combine(root,"traversal"),Hash(zip)));
                Check(!File.Exists(Path.Combine(root,"outside.txt"))&&!Directory.Exists(Path.Combine(root,"traversal")));
            });
            Test("duplicate case-insensitive Windows paths are rejected",()=>{
                using(var zip=Zip("HjvTak.LiveAndroid.vsto","prerequisites.exe","A.txt","a.txt"))
                    Reject(()=>PackageFiles.Extract(zip,Path.Combine(root,"duplicate"),Hash(zip)));
            });
            Test("incomplete payload is rejected",()=>{
                using(var zip=Zip("HjvTak.LiveAndroid.vsto"))
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
                Check(Installation.LocalManifest("file:///C:/Old%20Folder/HjvTak.LiveAndroid.vsto|vstolocal")==@"C:\Old Folder\HjvTak.LiveAndroid.vsto");
                Check(Installation.LocalManifest("")==null);
                foreach(string value in new[]{"https://example.org/HjvTak.LiveAndroid.vsto","file://server/share/HjvTak.LiveAndroid.vsto","file:///C:/Other.vsto"})
                    Reject(()=>Installation.LocalManifest(value));
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
    private static void Reject(Action action){try{action();}catch(InvalidDataException){return;}catch(IOException){return;}catch(InvalidOperationException){return;}throw new Exception("Expected rejection");}
    private static void Test(string name,Action action){action();passed++;Console.WriteLine("PASS "+name);}
}