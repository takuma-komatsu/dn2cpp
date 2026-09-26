using System;
using System.Runtime.CompilerServices;

// Base calls to the Object and ValueType virtuals from inside an override. A base
// call is non-virtual, so it runs the base body instead of dispatching back into the
// override: Object's type name, reference equality and identity hash, and ValueType's
// type name and field-by-field equality and hash. The identity hash is one function:
// RuntimeHelpers.GetHashCode agrees with the default Object.GetHashCode and is
// non-negative.
namespace ObjectVirtualDispatchSubset;

internal sealed class BaseCalls
{
    public override string ToString() => "base-calls:" + base.ToString();

    public override bool Equals(object obj) => base.Equals(obj);

    public override int GetHashCode() => base.GetHashCode();
}

internal sealed class Plain
{
}

// The string field keeps .NET off its bitwise fast path, so the base Equals compares
// field by field.
internal struct StructBaseCalls
{
    public int X;
    public string S;

    public StructBaseCalls(int x, string s)
    {
        X = x;
        S = s;
    }

    public override string ToString() => "struct:" + base.ToString();

    public override bool Equals(object obj) => base.Equals(obj);

    public override int GetHashCode() => base.GetHashCode();
}

internal static class Program
{
    internal static void Run()
    {
        Console.WriteLine("== object virtual dispatch ==");
        var a = new BaseCalls();
        var b = new BaseCalls();
        Console.WriteLine("class base ToString: " + a.ToString());
        Console.WriteLine("class base Equals: " + a.Equals(a) + "/" + a.Equals(b) + "/" + a.Equals(null));
        Console.WriteLine("class base GetHashCode: " + (a.GetHashCode() == RuntimeHelpers.GetHashCode(a)));
        var p = new Plain();
        Console.WriteLine("identity hash: " + (RuntimeHelpers.GetHashCode(p) == p.GetHashCode())
            + "/" + (RuntimeHelpers.GetHashCode(p) >= 0) + "/" + (RuntimeHelpers.GetHashCode(null) == 0));
        var s1 = new StructBaseCalls(1, "x");
        var s2 = new StructBaseCalls(1, "x");
        var s3 = new StructBaseCalls(2, "x");
        Console.WriteLine("struct base ToString: " + s1.ToString());
        Console.WriteLine("struct base Equals: " + s1.Equals(s2) + "/" + s1.Equals(s3)
            + "/" + s1.Equals(null) + "/" + s1.Equals(1));
        Console.WriteLine("struct base GetHashCode: " + (s1.GetHashCode() == s2.GetHashCode()));
    }
}
