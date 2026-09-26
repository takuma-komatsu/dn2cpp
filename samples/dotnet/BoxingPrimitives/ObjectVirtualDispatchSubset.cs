using System;
using System.Runtime.CompilerServices;

// Base calls to the Object virtuals from inside an override. A base call is
// non-virtual, so it runs Object's own body instead of dispatching back into the
// override: the type name, reference equality and the identity hash. The identity
// hash is one function: RuntimeHelpers.GetHashCode agrees with the default
// Object.GetHashCode and is non-negative.
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
    }
}
