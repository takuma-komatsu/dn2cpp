using System;
using System.IO;

namespace FileIoPathValidationSubset;

internal static class Program
{
    internal static void Run(string dir)
    {
        string f = Path.Combine(dir, "arguments.bin");
        File.WriteAllBytes(f, new byte[] { 1, 2, 3 });
        // The path checks of the lowered File, Path, Directory and Environment members:
        // .NET's type, ParamName and sentence, in .NET's order. A name with a NUL never
        // reaches the file it prefixes, except through the CurrentDirectory setter,
        // which .NET truncates there. A refusal whose sentence names the path prints its
        // type only.
        Console.WriteLine("-- System.IO path arguments --");
        string nul = f + "\0x";
        Console.WriteLine($"existsNul={File.Exists(nul)} dirExistsNul={Directory.Exists(dir + "\0x")}");
        Console.WriteLine($"existsEmpty={File.Exists("")} dirExistsEmpty={Directory.Exists("")}");
        // Unix .NET counts anything that is not a directory as a file, a device included.
        Console.WriteLine($"existsDevice={File.Exists("/dev/null")}");
        ProbePath("readTextEmpty", () => File.ReadAllText(""));
        ProbePath("readTextNul", () => File.ReadAllText(nul));
        ProbePath("readBytesEmpty", () => File.ReadAllBytes(""));
        ProbePath("readBytesNul", () => File.ReadAllBytes(nul));
        ProbePath("writeTextEmpty", () => File.WriteAllText("", "x"));
        ProbePath("writeTextNul", () => File.WriteAllText(nul, "x"));
        ProbePath("writeBytesEmpty", () => File.WriteAllBytes("", new byte[1]));
        ProbePath("writeBytesNul", () => File.WriteAllBytes(nul, new byte[1]));
        string nullBytes = Path.Combine(dir, "nb.bin");
        ProbePath("writeBytesNullBytes", () => File.WriteAllBytes(nullBytes, (byte[])null));
        ProbePath("writeBytesNullBoth", () => File.WriteAllBytes(null, (byte[])null));
        ProbePath("writeBytesNullPath", () => File.WriteAllBytes(null, new byte[1]));
        Console.WriteLine($"nullBytesCreated={File.Exists(nullBytes)}");
        ProbePath("deleteEmpty", () => File.Delete(""));
        ProbePath("deleteNul", () => File.Delete(nul));
        ProbePath("deleteNull", () => File.Delete(null));
        Console.WriteLine($"nulTarget={(File.Exists(f) ? File.ReadAllBytes(f).Length : -1)}");
        ProbePath("readTextDir", () => File.ReadAllText(dir));
        ProbePath("readBytesDir", () => File.ReadAllBytes(dir));
        ProbePath("writeTextDir", () => File.WriteAllText(dir, "x"));
        ProbePath("writeBytesDir", () => File.WriteAllBytes(dir, new byte[1]));
        ProbePath("deleteDir", () => File.Delete(dir));
        ProbePath("fullPathEmpty", () => Path.GetFullPath(""));
        ProbePath("fullPathNul", () => Path.GetFullPath("a\0b"));

        string cwd = Directory.GetCurrentDirectory();
        File.WriteAllText(Path.Combine(dir, "cwd.txt"), "c");
        ProbePath("cwdEmpty", () => Environment.CurrentDirectory = "");
        ProbePath("cwdNull", () => Environment.CurrentDirectory = null);
        ProbePath("setCwdEmpty", () => Directory.SetCurrentDirectory(""));
        ProbePath("setCwdNull", () => Directory.SetCurrentDirectory(null));
        ProbePath("setCwdNul", () => Directory.SetCurrentDirectory(dir + "\0x"));
        Console.WriteLine($"cwdMoved={File.Exists("cwd.txt")}");
        Environment.CurrentDirectory = dir + "\0x";
        Console.WriteLine($"cwdNulTruncated={File.Exists("cwd.txt")}");
        Directory.SetCurrentDirectory(cwd);
        Console.WriteLine($"cwdRestored={Directory.GetCurrentDirectory() == cwd}");
        ProbeSetterFailure("relative", "missing-io-cwd-argument");
        ProbeSetterFailure("nul", "missing-io-cwd-argument\0tail");
        Console.WriteLine("-- System.IO path arguments end --");
    }

    private static void ProbeSetterFailure(string label, string value)
    {
        try
        {
            Environment.CurrentDirectory = value;
            Console.WriteLine("cwd failure " + label + "=no exception");
        }
        catch (Exception e)
        {
            string expected = "Could not find a part of the path '" + value + "'.";
            Console.WriteLine("cwd failure " + label + "=" + e.GetType().Name
                + " original=" + (e.Message == expected));
        }
    }

    internal static void RunLexicalPathNormalization(string dir)
    {
        Console.WriteLine("-- managed UTF16 full paths --");
        string cwd = Directory.GetCurrentDirectory();
        string root = Path.GetFullPath(dir);
        Directory.SetCurrentDirectory(root);
        root = Directory.GetCurrentDirectory();
        try
        {
            string[] names = { "missing\ud800", "missing\udfff", "caf\u00e9-\ud83d\ude00",
                "a\ud800/./b/../leaf\udfff/", "a/../missing\ud800", "./a//b/../leaf" };
            for (int i = 0; i < names.Length; i++)
            {
                string full = Path.GetFullPath(names[i]);
                Console.WriteLine("lexical " + i + " units=" + CodeUnits(full.Substring(root.Length + 1))
                    + " absoluteSame=" + (full == Path.GetFullPath(Path.Combine(root, names[i]))));
            }
            Console.WriteLine("lexical dot-root=" + (Path.GetFullPath(".") == root)
                + " parent-root=" + (Path.GetFullPath("one/..") == root));
            ProbeMissingUnits("high", "missing\ud800", root);
            ProbeMissingUnits("low", "missing\udfff", root);
        }
        finally
        {
            Directory.SetCurrentDirectory(cwd);
        }
        Console.WriteLine("-- managed UTF16 full paths end --");
    }

    private static string CodeUnits(string value)
    {
        string units = "";
        foreach (char ch in value)
            units += ((int)ch).ToString("X4") + " ";
        return units.TrimEnd();
    }

    internal static void RunFileDotComponents(string dir)
    {
        Console.WriteLine("-- lexical file operation paths --");
        string cwd = Directory.GetCurrentDirectory();
        Directory.SetCurrentDirectory(Path.GetFullPath(dir));
        string root = Directory.GetCurrentDirectory();
        try
        {
            File.WriteAllText("keep.txt", "keep");
            string folded = "absent/../keep.txt";
            Console.WriteLine("dot exists=" + File.Exists(folded) + " direct=" + File.Exists("keep.txt")
                + " trailing=" + File.Exists(folded + "/"));
            ProbePath("dot read text", () => Console.WriteLine("dot text=" + File.ReadAllText(folded)));
            ProbePath("dot read bytes", () =>
            {
                byte[] bytes = File.ReadAllBytes(folded);
                Console.WriteLine("dot bytes=" + bytes.Length + "/" + bytes[0]);
            });
            ProbePath("dot write text", () => File.WriteAllText("absent/../write.txt", "written"));
            Console.WriteLine("dot written=" + (File.Exists("write.txt") ? File.ReadAllText("write.txt") : "missing"));
            ProbePath("dot write bytes", () => File.WriteAllBytes("absent/../write.bin", new byte[] { 9, 8 }));
            Console.WriteLine("dot bytes written=" + (File.Exists("write.bin") ? File.ReadAllBytes("write.bin").Length : -1));
            ProbePath("dot absolute read", () => Console.WriteLine("dot absolute="
                + File.ReadAllText(Path.Combine(root, "absent", "..", "write.txt"))));
            ProbePath("dot read trailing", () => File.ReadAllText("write.txt/"));
            ProbePath("dot delete", () => File.Delete(folded));
            Console.WriteLine("dot deleted=" + !File.Exists("keep.txt"));
            ProbePath("dot delete missing", () => File.Delete("absent/../missing.txt"));
            Directory.CreateDirectory("kept-dir");
            Console.WriteLine("dot directory exists=" + Directory.Exists("absent/../kept-dir"));
            ProbePath("dot file info", () => Console.WriteLine("dot info=" + new FileInfo("absent/../write.txt").Length));
            ProbePath("dot file stream", () =>
            {
                using var stream = File.OpenRead("absent/../write.txt");
                Console.WriteLine("dot stream=" + stream.ReadByte());
            });
            if (Path.DirectorySeparatorChar == '/')
            {
                string vanished = Path.Combine(root, "vanished-cwd");
                Directory.CreateDirectory(vanished);
                Directory.SetCurrentDirectory(vanished);
                Directory.Delete(vanished);
                Console.WriteLine("deleted cwd file exists=" + File.Exists("missing")
                    + " directory exists=" + Directory.Exists("missing"));
                Directory.SetCurrentDirectory(root);
            }
        }
        finally
        {
            Directory.SetCurrentDirectory(cwd);
        }
        Console.WriteLine("-- lexical file operation paths end --");
    }

    private static void ProbeMissingUnits(string label, string path, string root)
    {
        try
        {
            File.ReadAllText(path);
            Console.WriteLine("missing " + label + "=no exception");
        }
        catch (Exception error)
        {
            Console.WriteLine("missing " + label + "=" + error.GetType().Name
                + " units=" + CodeUnits(error.Message.Replace(root, "<root>")));
        }
    }

    private static void ProbePath(string label, Action action)
    {
        try
        {
            action();
            Console.WriteLine($"{label}=noexc");
        }
        catch (ArgumentException e)
        {
            Console.WriteLine($"{label}={e.GetType().Name} param={e.ParamName} message={e.Message}");
        }
        catch (Exception e)
        {
            Console.WriteLine($"{label}={e.GetType().Name}");
        }
    }
}
