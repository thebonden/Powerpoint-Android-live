using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;

namespace HjvTak.Setup
{
    internal static class PackageFiles
    {
        internal const string Manifest="HjvTak.LiveAndroid.vsto";
        internal static string Child(string root,string relative)
        {
            if(string.IsNullOrWhiteSpace(relative)||Path.IsPathRooted(relative)||relative.Contains(":"))
                throw new InvalidDataException("Ugyldig filsti i installationspakken.");
            string path=Path.GetFullPath(Path.Combine(root,relative));
            string prefix=Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar)+Path.DirectorySeparatorChar;
            if(!path.StartsWith(prefix,StringComparison.OrdinalIgnoreCase) || path.Length>240)
                throw new InvalidDataException("Installationsstien er ugyldig eller for lang.");
            return path;
        }
        internal static void Extract(Stream content,string target,string expectedHash)
        {
            using(var buffer=new MemoryStream()) {
                content.CopyTo(buffer);buffer.Position=0;
                using(var hash=SHA256.Create())
                    if(BitConverter.ToString(hash.ComputeHash(buffer)).Replace("-","")!=expectedHash)
                        throw new InvalidDataException("Installationsfilen er beskadiget. Hent den igen.");
                buffer.Position=0;
                using(var zip=new ZipArchive(buffer,ZipArchiveMode.Read)) {
                    var seen=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    var files=new List<Tuple<ZipArchiveEntry,string>>();
                    long total=0;
                    foreach(var entry in zip.Entries) {
                        if(string.IsNullOrEmpty(entry.Name))continue;
                        string relative=entry.FullName.Replace('/',Path.DirectorySeparatorChar);
                        string path=Child(target,relative);
                        if(!seen.Add(path))throw new InvalidDataException("Dobbelt fil i pakken.");
                        total+=entry.Length;
                        if(total>64*1024*1024)throw new InvalidDataException("Installationspakken er for stor.");
                        files.Add(Tuple.Create(entry,path));
                    }
                    if(!seen.Contains(Child(target,Manifest))||!seen.Contains(Child(target,"prerequisites.exe")))
                        throw new InvalidDataException("Installationspakken er ufuldstændig.");
                    // Validate every path before creating any file.
                    foreach(var file in files) {
                        Directory.CreateDirectory(Path.GetDirectoryName(file.Item2));
                        using(var input=file.Item1.Open())
                        using(var output=new FileStream(file.Item2,FileMode.CreateNew,FileAccess.Write))
                            input.CopyTo(output);
                    }
                }
            }
        }
        internal static void MoveIntoPlace(string stage,string current,string backup)
        {
            if(Directory.Exists(current))Directory.Move(current,backup);
            try{Directory.Move(stage,current);}
            catch{if(Directory.Exists(backup))Directory.Move(backup,current);throw;}
        }
        internal static void Restore(string current,string backup,string failed)
        {
            if(Directory.Exists(current))Directory.Move(current,failed);
            if(Directory.Exists(backup))Directory.Move(backup,current);
        }
    }
}