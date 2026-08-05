using System;

namespace HotUpdateBadPatchDelegate;

// A deliberately out-of-fence patch: binding a method into a *generic* delegate
// (Func<>/Action<>). A generic delegate is a closed generic instantiation the
// AOT base image must already carry (the missing-AOT-instantiation
// boundary); the patch surface supports only non-generic base-image delegate
// types. `dn2cpp --emit-patch` must reject this assembly with a clear message.
// The hotupdate gate asserts the rejection (exit code 2 + the generic-delegate
// fence message on stderr).
//
// The delegate is constructed over an instance method of a fresh object (not a
// static method group), so Roslyn does not cache it in a Func<>-typed field —
// the generic delegate first surfaces at the `newobj Func<int>::.ctor`, which
// is exactly the rejection point.
internal sealed class Box
{
    public int V;

    public int Get()
    {
        return V;
    }
}

internal static class Program
{
    private static void Main()
    {
        Box b = new Box();
        b.V = 5;
        Console.WriteLine(new Func<int>(b.Get)());
    }
}
