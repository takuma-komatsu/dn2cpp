using System;
using System.IO;

namespace IoErrorSubset;

internal static class Program
{
    internal static void Run(string dir)
    {
        Console.WriteLine("== io missing paths ==");
        string root = Path.GetFullPath(dir);
        string file = Path.Combine(dir, "present.txt");
        File.WriteAllText(file, "x");
        string missingLeaf = Path.Combine(dir, "absent.txt");
        string missingParent = Path.Combine(dir, "missing", "..", "missing", "child.txt");
        string unicodeParent = Path.Combine(dir, "日本語");
        Directory.CreateDirectory(unicodeParent);

        void Probe(string label, Action action)
        {
            try
            {
                action();
                Console.WriteLine(label + ": no exception");
            }
            catch (Exception e)
            {
                Console.WriteLine(label + ": " + e.GetType().Name + " | "
                    + e.Message.Replace(root, "<scratch>").Replace("\0", "<nul>"));
            }
        }

        Console.WriteLine("normalize missing leaf=" + Path.GetFullPath(missingLeaf).Replace(root, "<scratch>"));
        Console.WriteLine("normalize missing parent=" + Path.GetFullPath(missingParent).Replace(root, "<scratch>"));
        Console.WriteLine("normalize unicode missing="
            + Path.GetFullPath(Path.Combine(unicodeParent, "absent.txt")).Replace(root, "<scratch>"));
        if (OperatingSystem.IsWindows())
        {
            string extended = @"\\?\C:\MISSING~1\..\child";
            string ntExtended = @"\??\C:\MISSING~1\..\child";
            Console.WriteLine("extended path unchanged=" + (Path.GetFullPath(extended) == extended));
            Console.WriteLine("nt extended path unchanged=" + (Path.GetFullPath(ntExtended) == ntExtended));
        }
        Probe("read leaf", () => File.ReadAllText(missingLeaf));
        Probe("read unicode leaf", () => File.ReadAllText(Path.Combine(unicodeParent, "absent.txt")));
        Probe("read parent", () => File.ReadAllText(missingParent));
        Probe("open parent", () => File.OpenRead(missingParent).Dispose());
        Probe("read file parent", () => File.ReadAllText(Path.Combine(file, "child")));
        Probe("write parent", () => File.WriteAllText(missingParent, "x"));
        if (Path.DirectorySeparatorChar == '/')
            Probe("write dangling link", () => File.WriteAllText(Path.Combine(dir, "dangling-io-error"), "x"));
        Probe("delete leaf", () => File.Delete(missingLeaf));
        Probe("delete parent", () => File.Delete(missingParent));
        Probe("read directory", () => File.ReadAllText(dir));
        Probe("write directory", () => File.WriteAllText(dir, "x"));
        Probe("cwd missing", () => Directory.SetCurrentDirectory(Path.Combine(dir, "missing")));
        Probe("cwd file", () => Directory.SetCurrentDirectory(file));
        if (OperatingSystem.IsWindows())
        {
            string previous = Environment.CurrentDirectory;
            try
            {
                Environment.CurrentDirectory = root;
                Probe("env cwd relative file", () => Environment.CurrentDirectory = "present.txt");
                Probe("directory cwd relative file", () => Directory.SetCurrentDirectory("present.txt"));
                Probe("env cwd nul missing", () => Environment.CurrentDirectory = Path.Combine(root, "missing") + "\0tail");
            }
            finally
            {
                Environment.CurrentDirectory = previous;
            }
        }
        Console.WriteLine("io missing paths complete");
    }
}
