#nullable enable
// Method-dimension canonical sharing: generic METHODS whose instantiations share
// one body per layout group, over the whole per-method rgctx machinery — a boxing
// body (TypeInfo slot through the hidden __rgctx parameter), exact array identity
// (ArrayTypeInfo slot), a generic method calling a context-needing one (the
// forwarding slot), self-recursion (__rgctx pass-through), and a generic method on
// a generic class (both dimensions in one registry). Real System.Private.CoreLib
// (-r), diffed against .NET.
using System;
namespace MethodShareSubset;

class Widget
{
    public string Tag;
    public Widget(string tag) { Tag = tag; }
    public override string ToString() => "W(" + Tag + ")";
}

static class Ops
{
    // A no-op for the reference group; for the width group the box must carry
    // the REAL argument's identity out of the shared body.
    public static object Box<T>(T v) => (object)v!;

    // The created array's exact type per real instantiation, read out of the
    // per-method table.
    public static string ArrName<T>(int n) => new T[n].GetType().Name;

    // ArrName<T>'s per-method table travels through a forwarding slot in THIS
    // body's own method registry.
    public static string Describe<T>(int n) => n + ":" + ArrName<T>(n);

    // Self-recursion forwards __rgctx unchanged before the context is used.
    public static string Deep<T>(int i) => i > 0 ? Deep<T>(i - 1) : ArrName<T>(1);
}

// Both dimensions at once: the class-argument and method-argument tokens land in
// one method registry and re-resolve under the full context per real pair.
class Bag<T>
{
    public string Both<U>(int n) => new T[n].GetType().Name + "+" + new U[n].GetType().Name;
}

class Program
{
    internal static void __GateEntry()
    {
        Console.WriteLine("mm box str=" + Ops.Box("s").GetType().Name);
        Console.WriteLine("mm box wid=" + Ops.Box(new Widget("q")));
        Console.WriteLine("mm box int=" + Ops.Box(42).GetType().Name);

        Console.WriteLine("mm arr str=" + Ops.ArrName<string>(2));
        Console.WriteLine("mm arr wid=" + Ops.ArrName<Widget>(1));
        Console.WriteLine("mm arr obj=" + Ops.ArrName<object>(0));
        Console.WriteLine("mm arr int=" + Ops.ArrName<int>(3));

        Console.WriteLine("mm desc str=" + Ops.Describe<string>(2));
        Console.WriteLine("mm desc wid=" + Ops.Describe<Widget>(1));

        Console.WriteLine("mm deep str=" + Ops.Deep<string>(3));
        Console.WriteLine("mm deep wid=" + Ops.Deep<Widget>(2));

        Console.WriteLine("mm both sw=" + new Bag<string>().Both<Widget>(2));
        Console.WriteLine("mm both wo=" + new Bag<Widget>().Both<object>(1));
        Console.WriteLine("mm both is=" + new Bag<int>().Both<string>(1));
    }
}
