using System;
using HotUpdateBase;

namespace HotUpdateBadPatchGvm;

// A deliberately out-of-fence patch: a patch type overriding a base-image generic
// virtual method. The override is a generic method, which has no patch body, so
// `dn2cpp --emit-patch` must reject this assembly rather than bake a type whose
// receivers run the base image's body. The hotupdate gate asserts the rejection
// (exit code 2 + a "must not declare generic virtual methods" message on stderr).
internal sealed class Crate : Shelf
{
    public override string Label<T>(T item)
    {
        return "crate:" + item;
    }
}

internal static class Program
{
    private static void Main()
    {
        Shelf crate = new Crate();
        Console.WriteLine(crate.Label<int>(1));
    }
}
