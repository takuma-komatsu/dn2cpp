#nullable disable
using System;
using System.Diagnostics.Tracing;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;

namespace ExceptionMessageSubset;

internal static unsafe class GeneralBclArgumentFieldsSubset
{
    private static string trace = "";

    private static string Show(string text) => text is null ? "<null>"
        : text.Replace("\0", "<nul>").Replace("\ud800", "<sur>").Replace("\r", "").Replace("\n", "|");

    private static void Probe(string label, Func<object> action)
    {
        trace = "";
        try
        {
            Console.WriteLine(label + " success=" + (action() ?? "<null>") + " trace=" + trace);
        }
        catch (Exception exception)
        {
            GC.Collect();
            var argument = exception as ArgumentException;
            var range = exception as ArgumentOutOfRangeException;
            Console.WriteLine(label + " type=" + exception.GetType().Name
                + " param=" + Show(argument?.ParamName)
                + " actual=" + (range?.ActualValue ?? "<null>")
                + " actual-type=" + (range?.ActualValue?.GetType().Name ?? "<null>")
                + " trace=" + trace);
            Console.WriteLine("  message=" + Show(exception.Message));
        }
    }

    private static int Touch(string name, int value)
    {
        trace += name;
        return value;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Log2(int value) => int.Log2(value);

    private static object Align(bool realloc, nuint alignment)
    {
        void* pointer = realloc ? NativeMemory.AlignedRealloc(null, 0, alignment)
            : NativeMemory.AlignedAlloc(0, alignment);
        NativeMemory.AlignedFree(pointer);
        return "allocated";
    }

    internal static void Run()
    {
        Console.WriteLine("-- general BCL argument fields --");
        foreach (int mode in new[] { -1, 0, 5 })
        {
            Probe("round double mode=" + mode, () => Math.Round(1.25, (MidpointRounding)mode));
            Probe("round float mode=" + mode, () => MathF.Round(1.25f, (MidpointRounding)mode));
            Probe("round decimal mode=" + mode, () => decimal.Round(1.25m, (MidpointRounding)mode));
            Probe("round double digits-first=" + mode, () => Math.Round(1.25, 16, (MidpointRounding)mode));
            Probe("round float digits-first=" + mode, () => MathF.Round(1.25f, 7, (MidpointRounding)mode));
            Probe("round decimal digits-first=" + mode, () => decimal.Round(1.25m, 29, (MidpointRounding)mode));
        }
        Probe("round double NaN digits mode", () => Math.Round(double.NaN, 0, (MidpointRounding)5));
        Probe("round float infinity digits mode", () => MathF.Round(float.PositiveInfinity, 0, (MidpointRounding)5));
        Probe("clamp signed", () => Math.Clamp(0, int.MaxValue, int.MinValue));
        Probe("clamp unsigned", () => Math.Clamp(0u, uint.MaxValue, 0u));
        Probe("clamp signed wide", () => Math.Clamp(0L, long.MaxValue, long.MinValue));
        Probe("clamp unsigned wide", () => Math.Clamp(0UL, ulong.MaxValue, 0UL));
        Probe("clamp float", () => Math.Clamp(0f, 1.25f, -2.5f));
        Probe("clamp float precision", () => Math.Clamp(0f, 1.234567f, -7.765432f));
        Probe("clamp native unsigned", () => nuint.Clamp(0, nuint.MaxValue, 0));
        Probe("clamp native unsigned control", () => nuint.Clamp(nuint.MaxValue, 0, 1));
        Probe("clamp primitive signed byte", () => sbyte.Clamp(0, 2, 1));
        Probe("clamp primitive unsigned byte", () => byte.Clamp(0, 2, 1));
        Probe("clamp primitive signed short", () => short.Clamp(0, 2, 1));
        Probe("clamp primitive unsigned short", () => ushort.Clamp(0, 2, 1));
        Probe("clamp primitive signed int", () => int.Clamp(0, 2, 1));
        Probe("clamp primitive unsigned int", () => uint.Clamp(0, 2, 1));
        Probe("clamp primitive signed long", () => long.Clamp(0, 2, 1));
        Probe("clamp primitive unsigned long", () => ulong.Clamp(0, 2, 1));
        Probe("clamp primitive native signed", () => nint.Clamp(0, nint.MaxValue, nint.MinValue));
        Probe("clamp double", () => Math.Clamp(0d, 1.25d, -2.5d));
        Probe("clamp decimal", () => Math.Clamp(0m, 1.25m, -2.5m));
        Probe("clamp operand order", () => Math.Clamp(Touch("v", 0), Touch("l", 2), Touch("h", 1)));
        foreach (string input in new[] { null, "1", "99999999999999999999999999999999999999" })
        {
            Probe("parse byte " + Show(input), () => byte.Parse(input, CultureInfo.InvariantCulture));
            Probe("parse short " + Show(input), () => short.Parse(input, CultureInfo.InvariantCulture));
            Probe("parse decimal " + Show(input), () => decimal.Parse(input, CultureInfo.InvariantCulture));
            Probe("parse null/style " + Show(input), () => int.Parse(input, (NumberStyles)0x800));
            Probe("tryparse null/style " + Show(input), () => int.TryParse(input, (NumberStyles)0x800, null, out _));
            Probe("parse float hex " + Show(input), () => float.Parse(input, NumberStyles.AllowHexSpecifier));
            Probe("parse double hex " + Show(input), () => double.Parse(input, NumberStyles.AllowHexSpecifier));
        }
        foreach (int radix in new[] { 2, 8, 10, 16, 3 })
        {
            Probe("radix byte " + radix, () => Convert.ToString(byte.MaxValue, radix));
            Probe("radix short " + radix, () => Convert.ToString(short.MinValue, radix));
        }
        const string overflow = "99999999999999999999999999999999999999";
        Probe("overflow signed byte", () => sbyte.Parse(overflow));
        Probe("overflow unsigned short", () => ushort.Parse(overflow));
        Probe("overflow signed int", () => int.Parse(overflow));
        Probe("overflow unsigned int", () => uint.Parse(overflow));
        Probe("overflow signed long", () => long.Parse(overflow));
        Probe("overflow unsigned long", () => ulong.Parse(overflow));
        Probe("overflow hex signed byte", () => sbyte.Parse("100", NumberStyles.HexNumber));
        Probe("overflow hex unsigned long", () => ulong.Parse("10000000000000000", NumberStyles.HexNumber));
        Probe("radix operand order", () => Convert.ToString((short)Touch("v", -1), Touch("b", 3)));
        Probe("convert boolean null", () => Convert.ToBoolean((string)null));
        Probe("log2 negative", () => Log2(int.Parse("-1", CultureInfo.InvariantCulture)));
        Probe("decimal getbits short", () => decimal.GetBits(1m, new int[3]));
        Probe("culture zero", () => CultureInfo.GetCultureInfo(0).Name);
        Probe("culture negative", () => CultureInfo.GetCultureInfo(-1).Name);
        Probe("GC suppress null", () => { GC.SuppressFinalize(null); return null; });
        Probe("GC reregister null", () => { GC.ReRegisterForFinalize(null); return null; });
        foreach (nuint alignment in new nuint[] { 0, 3 })
        {
            Probe("aligned alloc " + alignment, () => Align(false, alignment));
            Probe("aligned realloc " + alignment, () => Align(true, alignment));
        }
        Probe("marshal structure both-null", () => { Marshal.StructureToPtr(null, IntPtr.Zero, false); return null; });
        Probe("marshal size null", () => Marshal.SizeOf((Type)null));
        Probe("marshal ptr type null", () => Marshal.PtrToStructure(IntPtr.Zero, (Type)null));
        Probe("marshal UTF8 null negative", () => Marshal.PtrToStringUTF8(IntPtr.Zero, -1));
        Probe("encoding index", () => System.Text.Encoding.UTF8.GetString(new byte[4], -1, 1));
        Probe("encoding count", () => System.Text.Encoding.Unicode.GetString(new byte[4], 0, -1));
        Probe("encoding empty span", () => System.Text.Encoding.UTF8.GetString(ReadOnlySpan<byte>.Empty));
        Probe("assembly load string-null", () => Assembly.Load((string)null));
        Probe("assembly load object-null", () => Assembly.Load((AssemblyName)null));
        Probe("native load both-null", () => NativeLibrary.Load(null, null, null));
        Probe("native export both-null", () => NativeLibrary.GetExport(IntPtr.Zero, null));
        Probe("native resolver both-null", () => { NativeLibrary.SetDllImportResolver(null, null); return null; });
        Probe("environment null", () => Environment.GetEnvironmentVariable(null));
        Probe("environment empty", () => Environment.GetEnvironmentVariable(""));
        Probe("path combine2", () => Path.Combine(null, null));
        Probe("path combine3", () => Path.Combine("a", null, null));
        Probe("path combine4", () => Path.Combine("a", "b", "c", null));
        Probe("event name null", () => EventSource.GetName(null));
        Probe("event guid null", () => EventSource.GetGuid(null));
        Probe("valuetask source null", () => new ValueTask((IValueTaskSource)null, 0));
        Probe("valuetask generic source null", () => new ValueTask<int>((IValueTaskSource<int>)null, -1));
        Probe("vector array null first", () => { Vector128.Create(7).CopyTo((int[])null, -1); return null; });
        Probe("vector start", () => { Vector128.Create(7).CopyTo(new int[4], -1); return null; });
        Probe("vector remaining", () => { Vector128.Create(7).CopyTo(new int[4], 1); return null; });
        Probe("vector span", () => { Vector256.Create(7).CopyTo(Span<int>.Empty); return null; });
        Probe("resource ctor both-null", () => new ResourceManager(null, (Assembly)null));
        Probe("resource ctor assembly-null", () => new ResourceManager("Absent", (Assembly)null));
        Probe("resource ctor source-null", () => new ResourceManager((Type)null));
        var manager = new ResourceManager("Absent", typeof(GeneralBclArgumentFieldsSubset).Assembly);
        Probe("resource name-null", () => manager.GetString(null));
        ResourceManager missing = null;
        Probe("resource receiver-null", () => missing.GetString(null));
        Probe("resource receiver-null string culture", () => missing.GetString(null, CultureInfo.InvariantCulture));
        Probe("resource receiver-null object", () => missing.GetObject(null));
        Probe("resource receiver-null object culture", () => missing.GetObject(null, CultureInfo.InvariantCulture));
        Probe("resource receiver-null stream", () => missing.GetStream(null));
        Probe("resource receiver-null stream culture", () => missing.GetStream(null, CultureInfo.InvariantCulture));
        Probe("resource receiver-null base-name", () => missing.BaseName);
        Probe("resource receiver-null release", () => { missing.ReleaseAllResources(); return null; });
        Assembly assembly = typeof(GeneralBclArgumentFieldsSubset).Assembly;
        Probe("manifest name-null", () => assembly.GetManifestResourceStream((string)null));
        Probe("manifest name-empty", () => assembly.GetManifestResourceStream(""));
        Probe("manifest scoped type-null", () => assembly.GetManifestResourceStream(null, "absent"));
        Probe("manifest scoped name-null", () => assembly.GetManifestResourceStream(typeof(GeneralBclArgumentFieldsSubset), null) is null);
        Console.Write("console null array pair=[");
        Console.Write("<{0}><{1}>", (object[])null);
        Console.WriteLine("]");
        Probe("console null array out-of-range", () => { Console.Write("{2}", (object[])null); return null; });
        Probe("console null array null format", () => { Console.WriteLine(null, (object[])null); return null; });
        Probe("string format null array", () => string.Format("{0}", (object[])null));
        Console.WriteLine("general BCL argument fields end");
    }
}
