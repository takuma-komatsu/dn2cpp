using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Reflection;
using System.Resources;

namespace ResourceTypeGuard;
internal static class Program
{
    private delegate bool ReaderQuery(string? typeName);
    private static bool Validate(string? typeName) => throw new Exception("rewrite");
    private static ReaderQuery ReflectedValidate() => (ReaderQuery)typeof(ResourceReader).GetMethod("ValidateReaderType", BindingFlags.Static | BindingFlags.NonPublic)!.CreateDelegate(typeof(ReaderQuery));
    private delegate bool Query(string? typeName, string? defaultName);
    private static bool Direct(string? typeName, string? defaultName) => throw new Exception("rewrite");
    private static Query Address() => throw new Exception("rewrite");
    private static Query Reflected()
    {
        var owner = typeof(ResourceManager);
        var method = owner.GetMethod("IsDefaultType", BindingFlags.Static | BindingFlags.NonPublic)!;
        if (method.DeclaringType != owner || method.Name != "IsDefaultType"
            || owner.GetMethod("IsDefaultType", BindingFlags.Static | BindingFlags.Public) is not null
            || owner.GetMethod("IsDefaultType", BindingFlags.Instance | BindingFlags.NonPublic) is not null)
            throw new Exception("resource guard metadata identity or visibility");
        var query = (Query)method.CreateDelegate(typeof(Query));
        if (query.Method.DeclaringType != owner || query.Method.Name != method.Name)
            throw new Exception("resource guard delegate identity");
        return query;
    }
    private static bool Invoke(string? typeName, string? defaultName) => (bool)typeof(ResourceManager).GetMethod("IsDefaultType", BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, new object?[] { typeName, defaultName })!;
    private static int effects;
    private static string? Argument(int expected, string? value)
    {
        if (effects != expected) throw new Exception("argument order");
        effects++;
        return value;
    }
    private static void Test(Query query, bool wrapped = false)
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        const string reader = "System.Resources.ResourceReader";
        string?[] names = { null, "", reader, "system.resources.ResourceReader", reader + "Extra", " " + reader,
            reader + " ", reader + ",mscorlib", reader + ", MSCORLIB ", reader + ",\tmscorlib\r\n",
            reader + ",\u00a0mscorlib\u00a0", reader + ",\u2003mscorlib\u2003,ignored", reader + ",mscorlib, Version=0.0.0.0, Culture=bad, PublicKeyToken=bad",
            reader + ",mscorlib,", reader + ",mscorlib,,garbage", reader + ",System.Private.CoreLib",
            reader + ", mscorlibExtra", reader + ",", reader + ", ", reader + ",,mscorlib",
            reader + "\0,mscorlib", reader + ",mscorlib\0", ",mscorlib", "X,Y,Z", "Å,mscorlib" };
        string?[] defaults = { reader, "", null, "X", "Å", reader + ",mscorlib" };
        for (int i = 0; i < names.Length; i++)
            for (int j = 0; j < defaults.Length; j++)
            {
                effects = 0;
                try { Console.WriteLine("case:" + i + ":" + j + ":" + query(Argument(0, names[i]), Argument(1, defaults[j]))); }
                catch (TargetInvocationException error) { if (!wrapped) throw new Exception("unexpected reflection wrapper"); Console.WriteLine("case:" + i + ":" + j + ":" + error.InnerException!.GetType().FullName); }
                catch (Exception error) { if (wrapped) throw new Exception("missing reflection wrapper"); Console.WriteLine("case:" + i + ":" + j + ":" + error.GetType().FullName); }
                if (effects != 2) throw new Exception("arguments not evaluated");
            }
        Console.WriteLine("resource-type-guard:end");
    }
    private static void TestValidate(ReaderQuery query)
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        string?[] names = { null, "", "System.Resources.ResourceReader", "System.Resources.ResourceReader, mscorlib",
            "System.Resources.ResourceReader, MSCORLIB,ignored", "System.Resources.ResourceReader, Wrong",
            "System.Resources.ResourceReaderExtra", "System.Resources.ResourceReader,", "System.Resources.ResourceReader ,mscorlib" };
        for (int i = 0; i < names.Length; i++)
        {
            try { Console.WriteLine("validate:" + i + ":" + query(names[i])); }
            catch (Exception error) { Console.WriteLine("validate:" + i + ":" + error.GetType().FullName); }
        }
        Console.WriteLine("resource-type-validate:end");
    }
    private static void RunValidate() => TestValidate(Validate);
    private static void RunValidateOracle() => TestValidate(ReflectedValidate());
    private static void RunHeader()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        string[] names = { "System.Resources.ResourceReader", "System.Resources.ResourceReader, mscorlib",
            "System.Resources.ResourceReader, MSCORLIB, Version=0.0.0.0", "System.Resources.ResourceReaderExtra",
            "system.resources.ResourceReader", "System.Resources.ResourceReader, Wrong", "System.Resources.ResourceReader," };
        for (int i = 0; i < names.Length; i++)
        {
            using var stream = new MemoryStream();
            using var header = new MemoryStream();
            using (var writer = new BinaryWriter(header, Encoding.UTF8, true))
            {
                writer.Write(names[i]); writer.Write("System.Resources.RuntimeResourceSet");
            }
            using (var writer = new BinaryWriter(stream, Encoding.UTF8, true))
            {
                writer.Write(unchecked((int)0xBEEFCACE)); writer.Write(1);
                writer.Write((int)header.Length); writer.Write(header.ToArray());
                writer.Write(2); writer.Write(0); writer.Write(0);
                while (stream.Position % 8 != 0) writer.Write((byte)0);
                writer.Write((int)stream.Position + 4);
            }
            stream.Position = 0;
            try
            {
                using var reader = new ResourceReader(stream);
                Console.WriteLine("header:" + i + ":empty=" + !reader.GetEnumerator().MoveNext());
            }
            catch (Exception error) { Console.WriteLine("header:" + i + ":" + error.GetType().FullName); }
        }
        Console.WriteLine("resource-type-header:end");
    }
    private static void Main() => Test(Direct);
    private static void RunAddress() => Test(Address());
    private static void RunReflection() => Test(Reflected());
    private static void RunInvoke() => Test(Invoke, true);
    private static void RunAll() { Main(); RunAddress(); RunReflection(); RunInvoke(); }
}
