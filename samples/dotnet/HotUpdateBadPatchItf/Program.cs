using System;

namespace HotUpdateBadPatchItf;

// A deliberately out-of-fence patch: declaring a brand-new interface inside the
// patch. A patch type may *implement* a base-image interface, but *declaring*
// one is not supported (the interface's slots are not frozen in the base ABI,
// so nothing anchors its dispatch). `dn2cpp --emit-patch` must reject this
// assembly with a clear "must be a plain class" message. The hotupdate gate
// asserts the rejection (exit code 2 + that message on stderr).
internal interface ILocal
{
    int Value();
}

internal sealed class Holder : ILocal
{
    public int Value()
    {
        return 1;
    }
}

internal static class Program
{
    private static void Main()
    {
        ILocal h = new Holder();
        Console.WriteLine(h.Value());
    }
}
