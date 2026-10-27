using System;
using System.Globalization;
using System.Reflection;

namespace RuntimeTypeIsSzArraySubset;

internal static class Program
{
    private class Owner<T> { }

    private static void Check(string label, bool success)
    {
        Console.WriteLine("runtime-type-szarray-" + label + ":" + success);
        if (!success) throw new InvalidOperationException(label);
    }

    private static Exception Error(Action action)
    {
        try { action(); }
        catch (TargetInvocationException error) { return error.InnerException; }
        catch (Exception error) { return error; }
        return null;
    }

    private static bool Public(Type type) => type.IsSZArray;
    private static bool Direct(Type type) => Public(type);
    private static bool Pointer(Type type) => Public(type);
    private static bool Getter(Type type) => Public(type);
    private static Func<Type, bool> Address() => Public;
    private static bool Reflection(Type type) => (bool)Address().Method.Invoke(null, new object[] { type });
    private static bool Generic<T>() => typeof(T).IsSZArray;

    private static void Route(string route, Func<Type, bool> query)
    {
        Type formal = typeof(Owner<>).GetGenericArguments()[0];
        Type nonSz = Array.CreateInstance(typeof(int), new[] { 2 }, new[] { 1 }).GetType();
        Type zeroBound = Array.CreateInstance(typeof(int), new[] { 2 }, new[] { 0 }).GetType();
        Type[] types = { typeof(int[]), typeof(string[]), typeof(int[][]), nonSz, typeof(int[,]),
            zeroBound, typeof(int), typeof(string), typeof(IDisposable), typeof(Array), typeof(Owner<>), formal,
            formal.MakeArrayType(), typeof(int).MakePointerType(), typeof(int).MakeByRefType() };
        string[] names = { "value-sz", "reference-sz", "jagged", "rank-one-md", "rank-two-md", "zero-bound",
            "value", "class", "interface", "array-base", "open", "formal", "formal-sz", "pointer", "byref" };
        bool[] expected = { true, true, true, false, false, true, false, false, false, false, false, false, true, false, false };
        for (int i = 0; i < types.Length; i++) Check(route + "-" + names[i], query(types[i]) == expected[i]);
        Check(route + "-rank-one-distinct", nonSz.GetArrayRank() == 1 && !query(nonSz) && query(typeof(int[])));
        Check(route + "-handle-sz", query(Type.GetTypeFromHandle(typeof(int[]).TypeHandle)));
        Check(route + "-handle-md", !query(Type.GetTypeFromHandle(nonSz.TypeHandle)));
        Check(route + "-null", Error(() => query(null)) is NullReferenceException);
        Check(route + "-default-handle", Error(() => query(Type.GetTypeFromHandle(default))) is NullReferenceException);
        Check(route + "-repeated", query(typeof(int[])) && !query(typeof(int)) && query(typeof(int[])));
    }

    internal static void Run()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Console.WriteLine("== Runtime type SZ-array contract ==");
        Route("public", Public);
        Check("generic", Generic<int[]>() && Generic<string[]>() && !Generic<int>() && !Generic<string>() && !Generic<int[,]>());
        Console.WriteLine("Runtime type SZ-array contract end");
    }

    // The CLR oracle calls public APIs; the fixture replaces these sites only for native execution.
    internal static void RunArtifact(string[] args)
    {
        Run();
        Route("direct", Direct);
        Route("pointer", Pointer);
        Route("getter", Getter);
        Route("address", Address());
        Route("reflection", Reflection);
        if (args.Length > 0 && args[0] == "native-identity")
            Check("reflection-identity", Address().Method.Name == "IsSZArray"
                && Address().Method.DeclaringType == typeof(RuntimeTypeHandle));
        Console.WriteLine("Runtime type SZ-array routes end");
    }
}
