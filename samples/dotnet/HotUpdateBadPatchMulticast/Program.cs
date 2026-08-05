using System;
using HotUpdateBase;

namespace HotUpdateBadPatchMulticast;

// A deliberately out-of-fence patch: building a *multicast* delegate with `+=`
// (Delegate.Combine). Only single-target delegates are supported — the combined
// invocation list is not baked — so `dn2cpp --emit-patch` must reject this
// assembly with a clear message. The hotupdate gate asserts the rejection (exit
// code 2 + the multicast fence message on stderr).
internal static class Program
{
    private static int A(int x)
    {
        return x + 1;
    }

    private static int B(int x)
    {
        return x * 2;
    }

    private static void Main()
    {
        IntTransform f = A;
        f += B;
        Console.WriteLine(f(3));
    }
}
