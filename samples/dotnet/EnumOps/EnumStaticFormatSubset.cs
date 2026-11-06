using System;
using System.Globalization;

namespace EnumStaticFormatSubset;

[Flags]
internal enum ByteFlags : byte { None = 0, One = 1, Two = 2 }

internal static class Program
{
    // The private-call fixture replaces these with the original generic CoreLib helpers.
    internal static string Direct(Type type, ref byte value) => ((ByteFlags)value).ToString();
    internal static string Format(Type type, char format, ref byte value) => ((ByteFlags)value).ToString(format.ToString());
    internal static string Pointer(Type type, ref byte value) => ((ByteFlags)value).ToString();
    internal static string FormatPointer(Type type, char format, ref byte value) => ((ByteFlags)value).ToString(format.ToString());

    internal static void RunProbe(string[] args)
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        byte value = 3;
        Console.WriteLine("== enum static formatting real bodies ==");
        Console.WriteLine("static-default=" + Direct(typeof(ByteFlags), ref value));
        Console.WriteLine("static-pointer=" + Pointer(typeof(ByteFlags), ref value));
        Console.WriteLine("static-G=" + Format(typeof(ByteFlags), 'G', ref value));
        Console.WriteLine("static-D=" + Format(typeof(ByteFlags), 'D', ref value));
        Console.WriteLine("static-X=" + Format(typeof(ByteFlags), 'X', ref value));
        Console.WriteLine("static-F=" + FormatPointer(typeof(ByteFlags), 'F', ref value));
        value = 200;
        Console.WriteLine("static-undefined=" + Direct(typeof(ByteFlags), ref value));
        Console.WriteLine("static-hex-pointer=" + FormatPointer(typeof(ByteFlags), 'x', ref value));
        try { Format(typeof(ByteFlags), 'Q', ref value); Console.WriteLine("static-invalid=returned"); }
        catch (FormatException) { Console.WriteLine("static-invalid=FormatException"); }
        Console.WriteLine("enum static formatting real bodies end");
    }
}
