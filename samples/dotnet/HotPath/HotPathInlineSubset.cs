#nullable disable
using System;
using System.Runtime.CompilerServices;
using Dn2Cpp.Runtime;

namespace HotPathInlineSubset
{
    // [HotPath] + [MethodImpl(AggressiveInlining)]: the marked body must stay in
    // generated_hot.cpp rather than being promoted into the shared header (an
    // inline copy would compile under each including TU's plain flags). The
    // unmarked twin IS promoted — the gate greps both, so the exclusion is
    // asserted against a working promotion mechanism, not vacuously.
    internal static class Program
    {
        [HotPath]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int HotTinyAdd(int a, int b) => a + b;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int ColdTinyAdd(int a, int b) => a + b;

        internal static void __GateEntry()
        {
            int acc = 0;
            for (int i = 0; i < 10; i++)
                acc = HotTinyAdd(acc, ColdTinyAdd(i, 1));
            Console.WriteLine(acc);
        }
    }
}
