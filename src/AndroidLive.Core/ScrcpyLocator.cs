using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using System.Threading;

namespace AndroidLive.Core
{
    internal static class ScrcpyLocator
    {
        // Search only explicit local locations. Never execute a candidate to discover it.
        internal static bool IsComplete(string executable)
        {
            try {
                if (!LocalAbsolute(executable) || !string.Equals(Path.GetFileName(executable), "scrcpy.exe", StringComparison.OrdinalIgnoreCase)) return false;
                string folder = Path.GetDirectoryName(executable);
                return File.Exists(executable) && File.Exists(Path.Combine(folder, "adb.exe")) &&
                    File.Exists(Path.Combine(folder, "scrcpy-server")) &&
                    Directory.EnumerateFiles(folder, "SDL*.dll").Any() &&
                    Directory.EnumerateFiles(folder, "avcodec*.dll").Any() &&
                    Directory.EnumerateFiles(folder, "avformat*.dll").Any() &&
                    Directory.EnumerateFiles(folder, "avutil*.dll").Any();
            }
            catch (Exception ex) when (IsFileError(ex)) { return false; }
        }
        internal static string Find(string preferred, IEnumerable<string> folders, IEnumerable<string> roots, CancellationToken cancel)
        {
            cancel.ThrowIfCancellationRequested();
            if (IsComplete(preferred)) return Path.GetFullPath(preferred);
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string folder in folders.Concat(roots.SelectMany(root => CandidateFolders(root, cancel)))) {
                cancel.ThrowIfCancellationRequested();
                if (string.IsNullOrWhiteSpace(folder)) continue;
                try {
                    string candidate = Path.Combine(folder.Trim().Trim('"'), "scrcpy.exe");
                    if (seen.Add(candidate) && IsComplete(candidate)) return Path.GetFullPath(candidate);
                } catch (Exception ex) when (IsFileError(ex)) { }
            }
            return null;
        }
        private static IEnumerable<string> CandidateFolders(string root, CancellationToken cancel)
        {
            // WinGet: Packages/Genymobile.scrcpy_.../scrcpy-win64-vX; ZIP: Downloads/scrcpy-win64-vX.
            yield return root;
            foreach (string child in Children(root)) {
                cancel.ThrowIfCancellationRequested();
                if (!PackageName(child)) continue;
                yield return child;
                foreach (string nested in Children(child)) {
                    cancel.ThrowIfCancellationRequested();
                    if (PackageName(nested)) yield return nested;
                }
            }
        }
        private static bool PackageName(string folder)
        {
            string name = Path.GetFileName(folder);
            return name.Equals("scrcpy", StringComparison.OrdinalIgnoreCase) ||
                name.StartsWith("scrcpy-win", StringComparison.OrdinalIgnoreCase) ||
                name.StartsWith("Genymobile.scrcpy", StringComparison.OrdinalIgnoreCase);
        }
        private static string[] Children(string root)
        {
            try {
                if (!LocalAbsolute(root) || !Directory.Exists(root)) return new string[0];
                return Directory.EnumerateDirectories(root).Take(512)
                    .Where(p => (File.GetAttributes(p) & FileAttributes.ReparsePoint) == 0)
                    .OrderBy(p => p, StringComparer.OrdinalIgnoreCase).ToArray();
            } catch (Exception ex) when (IsFileError(ex)) { return new string[0]; }
        }
        private static bool LocalAbsolute(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !Path.IsPathRooted(path)) return false;
            string root = Path.GetPathRoot(path);
            return root.Length == 3 && root[1] == ':' && new DriveInfo(root).DriveType == DriveType.Fixed;
        }
        private static bool IsFileError(Exception ex) => ex is IOException || ex is UnauthorizedAccessException ||
            ex is ArgumentException || ex is NotSupportedException || ex is SecurityException;
    }
}
