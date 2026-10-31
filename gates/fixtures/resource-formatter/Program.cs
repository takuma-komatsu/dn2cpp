using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Resources;

namespace ResourceFormatter;
internal static class Program
{
    internal delegate object Create();
    internal delegate object Decode(object? formatter, Stream? stream);
    private static object DirectCreate() => throw new InvalidOperationException("rewritten fixture required");
    private static object DirectDecode(object? formatter, Stream? stream) => throw new InvalidOperationException("rewritten fixture required");
    private static object PointerCreate() => throw new InvalidOperationException("rewritten fixture required");
    private static object PointerDecode(object? formatter, Stream? stream) => throw new InvalidOperationException("rewritten fixture required");
    private static Create AddressCreate() => DirectCreate;
    private static Decode AddressDecode() => DirectDecode;
    private static object DelegateCreate() => AddressCreate()();
    private static object DelegateDecode(object? formatter, Stream? stream) => AddressDecode()(formatter, stream);
    private static object ReflectCreate() => typeof(ResourceReader).GetMethod("CreateBinaryFormatter", BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, null)!;
    private static object ReflectDecode(object? formatter, Stream? stream) => typeof(ResourceReader).GetMethod("Deserialize", BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, new object?[] { formatter, stream })!;
    private static int trace;
    private static object? Formatter(bool throwing)
    {
        trace = trace * 10 + 1;
        if (throwing) throw new FormatException("formatter argument");
        return null;
    }
    private static Stream? Input(bool throwing)
    {
        trace = trace * 10 + 2;
        if (throwing) throw new FormatException("stream argument");
        return null;
    }
    private static bool Refused(Func<object> action)
    {
        try { action(); }
        catch (Exception error)
        {
            if (error is TargetInvocationException wrapped) error = wrapped.InnerException!;
            return error.GetType() == typeof(NotSupportedException) && error.Message.Contains("BinaryFormatter") && error.Message.Contains("primitive");
        }
        return false;
    }
    private static void Cases(string route, Create create, Decode decode)
    {
        Console.WriteLine("resource-formatter-" + route + "-create:" + Refused(() => create()));
        foreach (bool formatter in new[] { false, true })
            foreach (bool stream in new[] { false, true })
                Console.WriteLine("resource-formatter-" + route + "-decode-" + formatter + "-" + stream + ":" + Refused(() => decode(formatter ? new object() : null, stream ? new MemoryStream() : null)));
        trace = 0;
        bool refusal = Refused(() => decode(Formatter(false), Input(false)));
        Console.WriteLine("resource-formatter-" + route + "-order:" + (refusal && trace == 12));
        foreach (bool first in new[] { true, false })
        {
            trace = 0; bool threw = false;
            try { decode(Formatter(first), Input(!first)); }
            catch (FormatException) { threw = true; }
            Console.WriteLine("resource-formatter-" + route + "-argument-" + first + ":" + (threw && trace == (first ? 1 : 12)));
        }
    }
    private static void Culture()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
    }
    public static void Main() { Culture(); Cases("direct", DirectCreate, DirectDecode); Console.WriteLine("resource-formatter:end"); }
    public static void RunPointer() { Culture(); Cases("pointer", PointerCreate, PointerDecode); Console.WriteLine("resource-formatter:end"); }
    public static void RunAddress() { Culture(); Cases("address", DelegateCreate, DelegateDecode); Console.WriteLine("resource-formatter:end"); }
    public static void RunReflection() { Culture(); Cases("reflection", ReflectCreate, ReflectDecode); Console.WriteLine("resource-formatter:end"); }
    public static void RunAll()
    {
        Culture(); Cases("direct", DirectCreate, DirectDecode); Cases("pointer", PointerCreate, PointerDecode);
        Cases("address", DelegateCreate, DelegateDecode); Cases("reflection", ReflectCreate, ReflectDecode);
        Console.WriteLine("resource-formatter:end");
    }
}
