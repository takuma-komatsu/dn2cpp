using System;

namespace GenericCctorFirstUseSubset;

// Closed generic statics are independent caches. Reaching Cache<T> during AOT
// discovery must not run its initializer before the program first reads that cache.
internal static class Registry
{
    private static bool s_registered;

    internal static void Register() => s_registered = true;
    internal static bool IsRegistered => s_registered;
}

internal static class Cache<T>
{
    internal static readonly string State = Registry.IsRegistered ? "registered" : "too-early";
}

internal static class StaticMethod<T>
{
    static StaticMethod() => State.StaticMethod = Registry.IsRegistered ? "registered" : "too-early";
    internal static string Read() => State.StaticMethod;
}

internal sealed class Constructed<T>
{
    static Constructed() => State.Constructor = Registry.IsRegistered ? "registered" : "too-early";
}

internal static class State
{
    internal static string StaticMethod = "not-run";
    internal static string Constructor = "not-run";
    internal static string EarlyRoute = "not-run";
    internal static string MethodSpecRoute = "not-run";
}

internal static class Program
{
    internal static void Run()
    {
        Registry.Register();
        Console.WriteLine($"genericCctor.firstUse={Cache<int>.State}");
        Console.WriteLine($"genericCctor.independent={Cache<string>.State}");
        Console.WriteLine($"genericCctor.staticCall={StaticMethod<int>.Read()}");
        _ = new Constructed<int>();
        Console.WriteLine($"genericCctor.newobj={State.Constructor}");
        _ = System.Runtime.Intrinsics.FirstUseRoute<int>.IsSupported;
        Console.WriteLine($"genericCctor.earlyRoute={State.EarlyRoute}");
        _ = System.Numerics.INumberBase_<int>.CreateTruncating<int>(7);
        Console.WriteLine($"genericCctor.methodSpecRoute={State.MethodSpecRoute}");
    }
}
