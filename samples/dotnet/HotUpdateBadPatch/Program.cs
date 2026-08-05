using System;
using HotUpdateBase;

namespace HotUpdateBadPatch;

// A deliberately out-of-fence patch: declaring a brand-new virtual slot
// (newslot) would extend the frozen base-image vtable layout, so
// `dn2cpp --emit-patch` must reject this assembly with a clear message —
// only overrides of existing base-image virtual slots are supported. The
// hotupdate gate asserts the rejection (exit code 2 + the new-virtual-slot
// fence message on stderr).
internal static class Program
{
    private static void Main()
    {
        Console.WriteLine(new LoudCounter(1).Shout());
    }
}

internal class LoudCounter : Counter
{
    public LoudCounter(int start)
        : base(start)
    {
    }

    public virtual string Shout()
    {
        return "loud";
    }
}
