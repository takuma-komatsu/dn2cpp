#nullable disable
using System;
using System.Collections.Generic;

// string.Join / string.Concat over a List<T> arriving from a call result or a field
// rather than a local: TryListBacking needs the declared type threaded through
// EmitCallResult and ldfld, not just ldloc/ldarg.
namespace JoinCallResultSubset;

internal sealed class Holder
{
    public List<string> Names = new List<string> { "alice", "bob" };
    public List<int> Nums = new List<int> { 10, 20, 30 };
}

internal static class Program
{
    private static List<int> MakeInts() => new List<int> { 3, 1, 2 };

    private static List<string> MakeStrs() => new List<string> { "x", "y", "z" };

    internal static void Run()
    {
        // Call-result List<T>: Join over int / string elements, Concat over strings.
        Console.WriteLine(string.Join(",", MakeInts()));
        Console.WriteLine(string.Join("-", MakeStrs()));
        Console.WriteLine(string.Concat(MakeStrs()));

        // Field List<T>: Join + Concat over a reference-element field, Join over ints.
        Holder h = new Holder();
        Console.WriteLine(string.Join("/", h.Names));
        Console.WriteLine(string.Concat(h.Names));
        Console.WriteLine(string.Join(";", h.Nums));

        // Chained call result (a method returning a List, joined inline).
        Console.WriteLine(string.Join("|", MakeStrs()));
    }
}
