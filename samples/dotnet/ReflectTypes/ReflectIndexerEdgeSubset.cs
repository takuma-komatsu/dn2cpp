#nullable enable
// Indexed-property error-path edges where dn2cpp INTENTIONALLY diverges from
// real .NET (which is why these lines live in the freeze gate, not the
// reflect-invoke live diff):
//   - an index-count mismatch surfaces as the accessor invoker's
//     ArgumentException (real .NET: TargetParameterCountException, a type the
//     runtime does not model — same posture as MethodInfo.Invoke);
//   - reading a set-only / writing a get-only property surfaces as the null
//     accessor's InvalidOperationException (real .NET: ArgumentException
//     "Property Get/Set method was not found.").
// The happy paths (and everything .NET-parity) live in ReflectActivatorSubset
// in the reflect-invoke live-diff gate.
using System;
using System.Reflection;

namespace ReflectIndexerEdgeSubset;

class Cell
{
    private readonly string[] _d = { "x", "y" };
    public string this[int i] { get => _d[i]; set => _d[i] = value; }
    public string Plain { get; set; } = "p";
}

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
        var c = new Cell();
        PropertyInfo item = typeof(Cell).GetProperty("Item")!;
        PropertyInfo plain = typeof(Cell).GetProperty("Plain")!;
        Try("indexed get, no index", () => item.GetValue(c));
        Try("indexed get, null index", () => item.GetValue(c, null));
        Try("indexed get, extra index", () => item.GetValue(c, new object[] { 1, 2 }));
        Try("plain get, stray index", () => plain.GetValue(c, new object[] { 1 }));
        Try("set-only get", () => typeof(SetOnly).GetProperty("Item")!.GetValue(new SetOnly(), new object[] { 0 }));
        Try("get-only set", () => typeof(GetOnly).GetProperty("Item")!.SetValue(new GetOnly(), "v", new object[] { 0 }));
    }
}
