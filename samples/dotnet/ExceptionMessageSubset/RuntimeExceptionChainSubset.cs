using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace ExceptionMessageSubset;

internal static class RuntimeExceptionChainSubset
{
    private sealed class NoDefault
    {
        public NoDefault(int value) { }
    }

    private static string Chain(Type type)
    {
        var names = new List<string>();
        for (; type is not null; type = type.BaseType)
            names.Add(type.Name);
        return string.Join(" > ", names);
    }

    private static void Probe(string label, Action action)
    {
        try
        {
            action();
            Console.WriteLine(label + ": no exception");
        }
        catch (Exception ex)
        {
            GC.Collect();
            Console.WriteLine(label + ": " + Chain(ex.GetType()));
        }
    }

    internal static void Run()
    {
        Console.WriteLine("== runtime exception ancestry ==");
        Probe("no parameterless ctor", () => Activator.CreateInstance(typeof(NoDefault)));
        Probe("array index", () => { int[] a = new int[1]; a[a.Length] = 1; });
        Exception com = Marshal.GetExceptionForHR(unchecked((int)0x80004005));
        GC.Collect();
        Console.WriteLine("HRESULT E_FAIL: " + Chain(com.GetType()));
        Console.WriteLine("runtime exception ancestry end");
    }
}
