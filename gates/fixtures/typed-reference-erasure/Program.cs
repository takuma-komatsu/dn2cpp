using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading;

internal static class Program
{
    private sealed class Container<T> { }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static string Probe<T>(ref T value)
    {
        try
        {
            return __reftype(__makeref(value)) is null ? "null" : "accepted";
        }
        catch (PlatformNotSupportedException)
        {
            return "PlatformNotSupportedException";
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static string Read<T>(TypedReference reference)
    {
        try
        {
            _ = __refvalue(reference, T);
            return "accepted";
        }
        catch (PlatformNotSupportedException)
        {
            return "PlatformNotSupportedException";
        }
    }

    private static void Main()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        ThreadLocal<int> integer = null;
        ThreadLocal<string> text = null;
        BlockingCollection<int> queue = null;
        ThreadLocal<int>[,] matrix = null;
        Container<ThreadLocal<int>> container = null;
        try
        {
            TypedReference reference = __makeref(integer);
            Console.WriteLine("direct=" + (__reftype(reference) is null ? "null" : "accepted"));
        }
        catch (PlatformNotSupportedException)
        {
            Console.WriteLine("direct=PlatformNotSupportedException");
        }
        Console.WriteLine("generic integer=" + Probe(ref integer));
        Console.WriteLine("generic string=" + Probe(ref text));
        Console.WriteLine("generic queue=" + Probe(ref queue));
        Console.WriteLine("generic matrix=" + Probe(ref matrix));
        Console.WriteLine("generic container=" + Probe(ref container));
        object source = null;
        TypedReference ordinary = __makeref(source);
        Console.WriteLine("target integer=" + Read<ThreadLocal<int>>(ordinary));
        Console.WriteLine("target matrix=" + Read<ThreadLocal<int>[,]>(ordinary));
    }
}
