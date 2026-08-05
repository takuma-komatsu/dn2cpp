#nullable disable
using System;
using Dn2Cpp.Runtime;

namespace HotPathGenericSubset
{
    // [HotPath] on a generic method: the reference-type instantiations, which
    // canonical sharing would otherwise group, each compile their own
    // monomorphic body instead (the gate asserts generated_hot.cpp holds no
    // rgctx machinery); value-type instantiations were monomorphic already.
    internal static class Program
    {
        [HotPath]
        private static T PickLast<T>(T[] items)
        {
            T last = items[0];
            for (int i = 1; i < items.Length; i++)
                last = items[i];
            return last;
        }

        internal static void __GateEntry()
        {
            Console.WriteLine(PickLast(new[] { "alpha", "beta", "gamma" }));
            Console.WriteLine(PickLast(new[] { new[] { 1 }, new[] { 2, 3 } }).Length);
            Console.WriteLine(PickLast(new[] { 11, 22, 33 }));
            Console.WriteLine(BitConverter.DoubleToInt64Bits(PickLast(new[] { 1.25, 2.5 })).ToString("X16"));
        }
    }
}
