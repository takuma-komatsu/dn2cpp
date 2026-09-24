#nullable enable
// Reading a set-only / writing a get-only property surfaces as the null
// accessor's InvalidOperationException (real .NET: ArgumentException).
// The .NET-parity indexer paths live in the reflect-invoke live diff.
using System;

namespace ReflectIndexerEdgeSubset;

class SetOnly
{
    public string this[int i] { set { } }
}

class GetOnly
{
    public string this[int i] => "g" + i;
}

static class Program
{
    static void Try(string label, Action a)
    {
        try { a(); Console.WriteLine($"{label}: OK"); }
        catch (Exception e) { Console.WriteLine($"{label}: {e.GetType().Name}"); }
    }

    internal static void Run()
    {
        Console.WriteLine("== indexer error edges ==");
        Try("set-only get", () => typeof(SetOnly).GetProperty("Item")!.GetValue(new SetOnly(), new object[] { 0 }));
        Try("get-only set", () => typeof(GetOnly).GetProperty("Item")!.SetValue(new GetOnly(), "v", new object[] { 0 }));
    }
}
