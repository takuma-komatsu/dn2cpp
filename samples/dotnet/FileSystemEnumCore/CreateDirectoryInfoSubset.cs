using System;
using System.IO;

namespace CreateDirectoryInfoSubset
{
    // Directory.CreateDirectory's DirectoryInfo return value, actually USED:
    // Name / Exists / FullName (the FullName is dereferenced to build a child
    // path and write through it). Regression gate for the former transpiler
    // behavior of pushing a null DirectoryInfo placeholder — under it every
    // line here NREs at runtime while no transpile-time measurement can tell.
    internal static class Program
    {
        internal static void __GateEntry(string root)
        {
            Console.WriteLine("-- CreateDirectory returns a real DirectoryInfo --");
            string target = Path.Combine(root, "made", "deep");
            DirectoryInfo created = Directory.CreateDirectory(target);
            Console.WriteLine("name=" + created.Name);
            Console.WriteLine("exists=" + created.Exists);
            Console.WriteLine("fullNameMatches=" + (created.FullName == Path.GetFullPath(target)));

            // Deref FullName for real: build a child path from it and write a file.
            string inside = Path.Combine(created.FullName, "inside.txt");
            File.WriteAllText(inside, "made");
            Console.WriteLine("fileInCreated=" + File.Exists(Path.Combine(target, "inside.txt")));

            DirectoryInfo again = Directory.CreateDirectory(target); // idempotent
            Console.WriteLine("againName=" + again.Name + " againExists=" + again.Exists);
        }
    }
}
