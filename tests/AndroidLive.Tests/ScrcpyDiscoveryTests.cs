using System;
using System.IO;
using System.Threading;
using AndroidLive.Core;

internal static class ScrcpyDiscoveryTests
{
    internal static void Run(Action<string,Action> test)
    {
        test("scrcpy: saved complete package takes precedence", () => Fixture(root => {
            string saved=Package(Path.Combine(root,"chosen"));
            string other=Package(Path.Combine(root,"scrcpy-win64-v9"));
            Assert(ScrcpyLocator.Find(saved,new[]{Path.GetDirectoryName(other)},new string[0],CancellationToken.None)==saved);
        }));
        test("scrcpy: incomplete saved package falls back to WinGet layout", () => Fixture(root => {
            string broken=Package(Path.Combine(root,"broken"));File.Delete(Path.Combine(Path.GetDirectoryName(broken),"adb.exe"));
            string complete=Package(Path.Combine(root,"Genymobile.scrcpy_test","scrcpy-win64-v4.1"));
            Assert(ScrcpyLocator.Find(broken,new[]{Path.GetDirectoryName(broken)},new[]{root},CancellationToken.None)==complete);
        }));
        test("scrcpy: missing support files cannot be accepted", () => Fixture(root => {
            foreach(string file in new[]{"scrcpy.exe","adb.exe","scrcpy-server","SDL3.dll","avcodec-test.dll","avformat-test.dll","avutil-test.dll"}) {
                string exe=Package(Path.Combine(root,Guid.NewGuid().ToString("N")));
                File.Delete(Path.Combine(Path.GetDirectoryName(exe),file));Assert(!ScrcpyLocator.IsComplete(exe));
            }
            Assert(!ScrcpyLocator.IsComplete("scrcpy.exe"));Assert(!ScrcpyLocator.IsComplete("C:relative\\scrcpy.exe"));
            Assert(!ScrcpyLocator.IsComplete("\\\\server\\share\\scrcpy.exe"));Assert(!ScrcpyLocator.IsComplete("bad\0path"));
        }));
        test("scrcpy: search ignores OBS and unrelated/deep trees", () => Fixture(root => {
            Package(Path.Combine(root,"scrcpy-obs","scrcpy-win64-v4"));
            Package(Path.Combine(root,"unrelated","scrcpy-win64-v4"));
            Package(Path.Combine(root,"scrcpy-win64-v4","scrcpy-win64-v4","scrcpy-win64-v4"));
            Assert(ScrcpyLocator.Find(null,new string[0],new[]{root},CancellationToken.None)==null);
        }));
        test("scrcpy: retry discovers a newly installed package without restarting", () => Fixture(root => {
            Assert(ScrcpyLocator.Find(null,new string[0],new[]{root},CancellationToken.None)==null);
            string installed=Package(Path.Combine(root,"scrcpy-win64-v4"));
            Assert(ScrcpyLocator.Find(null,new string[0],new[]{root},CancellationToken.None)==installed);
        }));
        test("scrcpy: cancellation prevents selection", () => Fixture(root => {
            string exe=Package(root);bool cancelled=false;
            try { ScrcpyLocator.Find(exe,new string[0],new string[0],new CancellationToken(true)); }
            catch(OperationCanceledException){cancelled=true;}Assert(cancelled);
        }));
    }
    private static string Package(string folder)
    {
        Directory.CreateDirectory(folder);
        foreach(string file in new[]{"scrcpy.exe","adb.exe","scrcpy-server","SDL3.dll","avcodec-test.dll","avformat-test.dll","avutil-test.dll"})
            File.WriteAllText(Path.Combine(folder,file),"fixture only; never executed");
        return Path.Combine(folder,"scrcpy.exe");
    }
    private static void Fixture(Action<string> action)
    {
        string root=Path.GetFullPath(Path.Combine(Path.GetTempPath(),"ppt-scrcpy-test-"+Guid.NewGuid().ToString("N")));
        Directory.CreateDirectory(root);
        try {action(root);} finally {Directory.Delete(root,true);}
    }
    private static void Assert(bool value){if(!value)throw new InvalidOperationException("Discovery assertion failed");}
}
