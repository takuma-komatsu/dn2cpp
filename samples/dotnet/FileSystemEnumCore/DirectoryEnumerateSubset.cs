using System;
using System.IO;
using FileSystemEnumCore;

namespace DirectoryEnumerateSubset
{
    // Static System.IO.Directory enumeration surface: EnumerateFiles (bare,
    // pattern, recursive SearchOption), GetFiles, EnumerateDirectories,
    // GetDirectories, EnumerateFileSystemEntries — the CoreLib
    // FileSystemEnumerable/FileSystemEnumerator machinery over the
    // SystemNative_OpenDir/ReadDir/CloseDir PAL.
    internal static class Program
    {
        internal static void __GateEntry(string root)
        {
            Console.WriteLine("-- Directory enumeration --");
            EnumSort.Print("files(top)",
                EnumSort.Relative(root, Directory.EnumerateFiles(root)));
            EnumSort.Print("files(*.txt)",
                EnumSort.Relative(root, Directory.EnumerateFiles(root, "*.txt")));
            EnumSort.Print("files(*.txt,all)",
                EnumSort.Relative(root, Directory.EnumerateFiles(root, "*.txt", SearchOption.AllDirectories)));
            string[] all = Directory.GetFiles(root, "*", SearchOption.AllDirectories);
            EnumSort.Print("getfiles(*,all)", EnumSort.Relative(root, all));
            EnumSort.Print("dirs(top)",
                EnumSort.Relative(root, Directory.EnumerateDirectories(root)));
            string[] allDirs = Directory.GetDirectories(root, "*", SearchOption.AllDirectories);
            EnumSort.Print("getdirs(*,all)", EnumSort.Relative(root, allDirs));
            EnumSort.Print("entries(sub1)",
                EnumSort.Relative(root, Directory.EnumerateFileSystemEntries(Path.Combine(root, "sub1"))));
            EnumSort.Print("files(emptydir)",
                EnumSort.Relative(root, Directory.EnumerateFiles(Path.Combine(root, "emptydir"))));
        }
    }
}
