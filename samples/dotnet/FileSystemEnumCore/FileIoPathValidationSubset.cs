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
