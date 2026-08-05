using System;
using System.Collections.Generic;
using System.IO;
using FileSystemEnumCore;

namespace DirectoryInfoEnumerateSubset
{
    // DirectoryInfo / FileSystemInfo instance surface: the info-returning
    // enumerators (EnumerateFiles, GetFileSystemInfos) — verifying the real
    // DirectoryInfo/FileInfo IL transpiles and runs, not just the string
    // enumeration overloads.
    internal static class Program
    {
        internal static void __GateEntry(string root)
        {
            Console.WriteLine("-- DirectoryInfo enumeration --");
            var di = new DirectoryInfo(root);
            Console.WriteLine("exists=" + di.Exists);

            var txtNames = new List<string>();
            foreach (FileInfo f in di.EnumerateFiles("*.txt"))
                txtNames.Add(f.Name);
            EnumSort.SortOrdinal(txtNames);
            EnumSort.Print("files(*.txt)", txtNames);

            int allCount = 0;
            foreach (FileInfo f in di.EnumerateFiles("*", SearchOption.AllDirectories))
                allCount++;
            Console.WriteLine("files(*,all).count=" + allCount);

            var entries = new List<string>();
            foreach (FileSystemInfo fsi in di.GetFileSystemInfos())
                entries.Add((fsi is DirectoryInfo ? "d:" : "f:") + fsi.Name);
            EnumSort.SortOrdinal(entries);
            EnumSort.Print("fsinfos(top)", entries);

            var sub = new DirectoryInfo(Path.Combine(root, "sub1"));
            Console.WriteLine("sub1.name=" + sub.Name + " sub1.exists=" + sub.Exists);
            var ghost = new DirectoryInfo(Path.Combine(root, "no-such-dir"));
            Console.WriteLine("ghost.exists=" + ghost.Exists);
        }
    }
}
