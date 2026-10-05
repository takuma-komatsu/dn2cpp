using System;
using System.Reflection;

namespace ReflectAssemblyErrorSubset;

internal static class Program
{
    internal static void Run()
    {
        Console.WriteLine("== assembly load invalid name ==");
        void Probe(string label, string name)
        {
            try
            {
                Assembly.Load(name);
                Console.WriteLine(label + ": no exception");
            }
            catch (Exception e)
            {
                Console.WriteLine(label + ": " + e.GetType().Name + " | " + e.Message);
            }
        }
        Probe("blank", " ");
        Probe("tab", "\t");
        Probe("empty", "");
        Console.WriteLine("assembly load invalid name complete");
    }

    internal static void RunAssemblyNameValidation()
    {
        Console.WriteLine("== assembly name load validation ==");
        ProbeLoad("null object", () => Assembly.Load((AssemblyName)null!));
        ProbeLoad("null name", () => Assembly.Load(new AssemblyName()));
        ProbeLoad("empty name", () => Assembly.Load(new AssemblyName { Name = "" }));
        ProbeLoad("blank name", () => Assembly.Load(new AssemblyName { Name = " " }));
        ProbeLoad("blank version", () => Assembly.Load(new AssemblyName { Name = " ", Version = new Version(1, 2, 3, 4) }));
        ProbeLoad("blank culture", () => Assembly.Load(new AssemblyName { Name = " ", CultureName = "en-US" }));
        var tokenName = new AssemblyName { Name = " " };
        tokenName.SetPublicKeyToken(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 });
        ProbeLoad("blank token", () => Assembly.Load(tokenName));
        var completeName = new AssemblyName { Name = " ", Version = new Version(1, 2, 3, 4), CultureName = "en-US" };
        completeName.SetPublicKeyToken(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 });
        ProbeLoad("blank complete", () => Assembly.Load(completeName));
        ProbeLoad("string null", () => Assembly.Load((string)null!));
        ProbeLoad("string empty", () => Assembly.Load(""));
        ProbeLoad("string blank", () => Assembly.Load(" "));
        Assembly own = typeof(Program).Assembly;
        Console.WriteLine("load object identity=" + (Assembly.Load(own.GetName()) == own));
        Console.WriteLine("load string identity=" + (Assembly.Load(own.GetName().Name!) == own));
        Console.WriteLine("assembly name load validation end");
    }

    private static void ProbeLoad(string label, Func<Assembly> load)
    {
        try
        {
            load();
            Console.WriteLine("load " + label + ": returned");
        }
        catch (Exception e)
        {
            Console.WriteLine("load " + label + ": " + e.GetType().FullName + "|" + e.HResult.ToString("X8")
                + "|" + e.Message.Replace("\r", "\\r").Replace("\n", "\\n"));
            if (e is ArgumentException argument)
                Console.WriteLine("load " + label + " param=" + (argument.ParamName ?? "<null>"));
            if (e is System.IO.FileNotFoundException file)
                Console.WriteLine("load " + label + " file=" + (file.FileName is null ? "<null>" : "[" + file.FileName + "]"));
        }
    }
}
