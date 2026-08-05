using System;
using System.Collections.Generic;

namespace PrimitiveCompareHashSubset
{
    // Primitive value-type instance methods int.CompareTo(int) /
    // uint.CompareTo(uint) / int.GetHashCode(),
    // intercepted as inline expressions next to the Char predicates.
    //   * int.CompareTo:  signed, returns strictly -1/0/1 (the SIGN, not the diff)
    //   * uint.CompareTo: UNSIGNED compare, returns -1/0/1
    //   * int.GetHashCode: returns the value itself
    //   * ulong/long.GetHashCode: fold the two 32-bit halves (low ^ high)
    //   * IntPtr.GetHashCode: defers to the long fold (asserted as consistency,
    //     not raw values, to stay pointer-width-neutral)
    // Exercised both directly and through comparer lambdas (List.Sort/Array.Sort
    // with (a,b) => a.CompareTo(b)), mirroring the dn2cpp own-code gap bodies
    // (catch-clause sort, flag-enum descending-magnitude sort, SRM handle hashes).
    // uint operands are produced via `(uint)` casts of int-typed values — exactly
    // the gap shape `((uint)b.value).CompareTo((uint)a.value)` — rather than uint[]
    // element loads (which are an unrelated array-rep concern). CoreLib only (no
    // Linq shim). Diffs exact vs real .NET.
    internal static class Program
    {
        internal static void Run()
        {
            DirectIntCompareTo();
            DirectUIntCompareTo();
            DirectULongCompareTo();
            DirectIntGetHashCode();
            DirectULongGetHashCode();
            DirectLongGetHashCode();
            DirectIntPtrGetHashCode();
            ComparerSortInt();
            ComparerSortUIntDescending();
            DirectBoolCompareTo();
            DirectBoolGetHashCode();
            DirectIntegerEquals();
            DirectBoolEquals();
        }

        // bool.CompareTo(bool): false < true, strictly -1/0/1. The array-element
        // receiver (`arr[i].CompareTo(...)` emits ldelema, a bool* into packed storage)
        // exercises the 1-byte deref path; the comparer-lambda sort mirrors the direct
        // gap shape Thrive surfaced.
        private static void DirectBoolCompareTo()
        {
            bool[] a = { false, false, true, true };
            bool[] b = { false, true, false, true };
            for (int i = 0; i < a.Length; i++)
            {
                bool x = a[i];
                bool y = b[i];
                Console.WriteLine("bool.CompareTo: " + x + " vs " + y + " = " + x.CompareTo(y));
                Console.WriteLine("bool[].CompareTo: " + a[i].CompareTo(b[i]));
            }

            var list = new List<bool> { true, false, true, false, false, true };
            list.Sort((p, q) => p.CompareTo(q));
            var parts = new string[list.Count];
            for (int i = 0; i < list.Count; i++)
                parts[i] = list[i].ToString();
            Console.WriteLine("sorted bool: " + string.Join(",", parts));
        }

        // bool.GetHashCode(): real .NET returns m_value ? 1 : 0 — asserted as raw values.
        private static void DirectBoolGetHashCode()
        {
            bool t = true;
            bool f = false;
            Console.WriteLine("bool.GetHashCode: True = " + t.GetHashCode());
            Console.WriteLine("bool.GetHashCode: False = " + f.GetHashCode());
            bool[] arr = { true, false };
            Console.WriteLine("bool[].GetHashCode: " + arr[0].GetHashCode() + "," + arr[1].GetHashCode());
        }

        // Self-typed Equals(T) for the whole integer family: the statically-typed
        // direct call (`x.Equals(y)`), which C# emits only when written explicitly (== is
        // ceq) — plain `==` under the hood, no NaN/-0. The sign-boundary bit patterns
        // prove the 32/64-bit compares are width-exact and signedness-neutral.
        private static void DirectIntegerEquals()
        {
            int i1 = 3;
            Console.WriteLine("int.Equals: " + i1.Equals(3) + "," + i1.Equals(-3)
                + "," + int.MinValue.Equals(int.MinValue) + "," + int.MaxValue.Equals(int.MinValue));
            uint u1 = unchecked((uint)0x80000000);
            uint u2 = 0x7FFFFFFF;
            Console.WriteLine("uint.Equals: " + u1.Equals(u1) + "," + u1.Equals(u2)
                + "," + uint.MaxValue.Equals(uint.MaxValue) + "," + 0u.Equals(uint.MaxValue));
            long l1 = 0x1_0000_0000L;
            long l2 = 0xFFFF_FFFFL;
            Console.WriteLine("long.Equals: " + l1.Equals(l1) + "," + l1.Equals(l2)
                + "," + long.MinValue.Equals(long.MinValue) + "," + long.MinValue.Equals(long.MaxValue));
            ulong ul1 = 0x8000_0000_0000_0000UL;
            ulong ul2 = 0x7FFF_FFFF_FFFF_FFFFUL;
            Console.WriteLine("ulong.Equals: " + ul1.Equals(ul1) + "," + ul1.Equals(ul2)
                + "," + ulong.MaxValue.Equals(ulong.MaxValue) + "," + 0UL.Equals(ulong.MaxValue));
        }

        // bool.Equals(bool): the self-typed direct call, plus the array-element
        // (bool*) receiver.
        private static void DirectBoolEquals()
        {
            bool t = true;
            bool f = false;
            Console.WriteLine("bool.Equals: " + t.Equals(true) + "," + t.Equals(false)
                + "," + f.Equals(false) + "," + f.Equals(true));
            bool[] arr = { true, false };
            Console.WriteLine("bool[].Equals: " + arr[0].Equals(true) + "," + arr[1].Equals(true));
        }

        // int.CompareTo(int): the sign only. The boundary cases prove it is NOT a
        // subtraction (which would overflow for far-apart operands).
        private static void DirectIntCompareTo()
        {
            int[] a = { 0, 1, 0, 5, int.MinValue, int.MaxValue, int.MinValue, -1, 2000000000, -2000000000 };
            int[] b = { 0, 0, 1, 5, int.MaxValue, int.MinValue, 0, 1, -2000000000, 2000000000 };
            for (int i = 0; i < a.Length; i++)
            {
                int x = a[i];
                int y = b[i];
                Console.WriteLine("int.CompareTo: " + x + " vs " + y + " = " + x.CompareTo(y));
            }
        }

        // uint.CompareTo(uint): unsigned. The sign-boundary cases prove it does not
        // treat the int32 bit pattern as signed. The unchecked int literals are the
        // bit patterns 0xFFFFFFFF / 0x80000000 / 0x7FFFFFFF reinterpreted as uint.
        // Operands are printed in hex (ToString("X8"), the unsigned formatter path) so
        // the assertion isolates uint.CompareTo's result, not uint decimal formatting.
        private static void DirectUIntCompareTo()
        {
            int[] ai = unchecked(new int[] { 0, 1, 0, 5, (int)0xFFFFFFFF, 0, (int)0xFFFFFFFF, (int)0x80000000, 0x7FFFFFFF });
            int[] bi = unchecked(new int[] { 0, 0, 1, 5, 0, (int)0xFFFFFFFF, (int)0xFFFFFFFE, 0x7FFFFFFF, (int)0x80000000 });
            for (int i = 0; i < ai.Length; i++)
            {
                uint x = (uint)ai[i];
                uint y = (uint)bi[i];
                Console.WriteLine("uint.CompareTo: 0x" + x.ToString("X8") + " vs 0x" + y.ToString("X8") + " = " + x.CompareTo(y));
            }
        }

        // ulong.CompareTo(ulong): unsigned at 64 bits. The sign-boundary cases prove it
        // does not treat the int64 bit pattern as signed, and the high/low-word cases
        // prove the comparison is not a truncated 32-bit one. Operands are printed in
        // hex (ToString("X16"), the unsigned formatter path) so the assertion isolates
        // ulong.CompareTo's result, not ulong decimal formatting. This is the sibling
        // BigInteger.CompareTo's limb comparison reaches.
        private static void DirectULongCompareTo()
        {
            ulong[] a = { 0UL, 1UL, 0UL, 5UL, ulong.MaxValue, 0UL, ulong.MaxValue,
                0x8000_0000_0000_0000UL, 0x7FFF_FFFF_FFFF_FFFFUL, 0x1_0000_0000UL, 0xFFFF_FFFFUL };
            ulong[] b = { 0UL, 0UL, 1UL, 5UL, 0UL, ulong.MaxValue, ulong.MaxValue - 1UL,
                0x7FFF_FFFF_FFFF_FFFFUL, 0x8000_0000_0000_0000UL, 0xFFFF_FFFFUL, 0x1_0000_0000UL };
            for (int i = 0; i < a.Length; i++)
            {
                ulong x = a[i];
                ulong y = b[i];
                Console.WriteLine("ulong.CompareTo: 0x" + x.ToString("X16") + " vs 0x" + y.ToString("X16")
                    + " = " + x.CompareTo(y));
            }

            // Comparer path: descending by unsigned 64-bit magnitude.
            var list = new List<ulong> { 1UL, 0x8000_0000_0000_0000UL, ulong.MaxValue, 0UL,
                0x7FFF_FFFF_FFFF_FFFFUL, 2UL };
            list.Sort((p, q) => q.CompareTo(p));
            var parts = new string[list.Count];
            for (int i = 0; i < list.Count; i++)
                parts[i] = list[i].ToString("X16");
            Console.WriteLine("sorted ulong desc: " + string.Join(",", parts));
        }

        // int.GetHashCode(): the value itself.
        private static void DirectIntGetHashCode()
        {
            int[] vs = { 0, 1, -1, 5, int.MinValue, int.MaxValue, 12345, -12345 };
            foreach (int v in vs)
            {
                int w = v;
                Console.WriteLine("int.GetHashCode: " + w + " = " + w.GetHashCode());
            }
        }

        // ulong.GetHashCode(): folds the two 32-bit halves (low ^ high). The boundary
        // values exercise the high word, the low word, and both — the exact shape STJ's
        // PropertyRef.GetHashCode reaches on the source-gen property-name cache key.
        private static void DirectULongGetHashCode()
        {
            ulong[] vs = { 0UL, 1UL, ulong.MaxValue, 0x1_0000_0000UL, 0xFFFF_FFFFUL,
                0x1234_5678_9ABC_DEF0UL, 0xDEAD_BEEF_0000_0000UL, 12345UL };
            foreach (ulong v in vs)
            {
                ulong w = v;
                Console.WriteLine("ulong.GetHashCode: 0x" + w.ToString("X16") + " = " + w.GetHashCode());
            }
        }

        // long.GetHashCode(): the signed twin of the ulong fold —
        // unchecked((int)m_value) ^ (int)(m_value >> 32). The boundary values
        // exercise the sign bit, the high word, the low word, and both.
        private static void DirectLongGetHashCode()
        {
            long[] vs = { 0L, 1L, -1L, long.MinValue, long.MaxValue, 0x1_0000_0000L,
                0xFFFF_FFFFL, unchecked((long)0x1234_5678_9ABC_DEF0UL), 12345L, -12345L };
            foreach (long v in vs)
            {
                long w = v;
                Console.WriteLine("long.GetHashCode: 0x" + w.ToString("X16") + " = " + w.GetHashCode());
            }
        }

        // IntPtr.GetHashCode(): defers to the long fold (`((long)_value).GetHashCode()`
        // in the 64-bit CoreLib). Printing raw pointer-width-dependent hashes would tie
        // the output to the build width, so assert the CONSISTENCY with long.GetHashCode
        // over deterministic constant values instead.
        private static void DirectIntPtrGetHashCode()
        {
            long[] vs = { 0L, 1L, -1L, 12345L, -12345L, 0x1_0000_0000L, long.MaxValue, long.MinValue };
            foreach (long v in vs)
            {
                IntPtr p = (IntPtr)v;
                Console.WriteLine("IntPtr.GetHashCode==long.GetHashCode: 0x" + v.ToString("X16")
                    + " = " + (p.GetHashCode() == v.GetHashCode()));
            }
        }

        // Comparer path: List<int>.Sort((a,b) => a.CompareTo(b)) — the exact shape of
        // dn2cpp's catch-clause sort comparer.
        private static void ComparerSortInt()
        {
            var list = new List<int> { 7, -3, 0, int.MaxValue, int.MinValue, 2, -2, 100 };
            list.Sort((a, b) => a.CompareTo(b));
            Console.WriteLine("sorted int: " + string.Join(",", list));

            // Array.Sort with the same comparer lambda.
            int[] arr = { 9, 4, -1, 4, 0, -7 };
            Array.Sort(arr, (a, b) => a.CompareTo(b));
            Console.WriteLine("sorted int arr: " + string.Join(",", arr));
        }

        // Comparer path: descending by unsigned magnitude — the exact shape of
        // dn2cpp's flag-enum sort comparers ((a,b) => ((uint)b).CompareTo((uint)a)).
        // Sorts int-typed values reinterpreted as unsigned.
        private static void ComparerSortUIntDescending()
        {
            int[] raw = unchecked(new int[] { 1, (int)0x80000000, (int)0xFFFFFFFF, 0, 0x7FFFFFFF, 2 });
            var list = new List<int>();
            foreach (int r in raw)
                list.Add(r);
            list.Sort((a, b) => ((uint)b).CompareTo((uint)a));
            var parts = new string[list.Count];
            for (int i = 0; i < list.Count; i++)
                parts[i] = ((uint)list[i]).ToString("X8"); // hex = unsigned formatter path
            Console.WriteLine("sorted uint desc: " + string.Join(",", parts));
        }
    }
}
