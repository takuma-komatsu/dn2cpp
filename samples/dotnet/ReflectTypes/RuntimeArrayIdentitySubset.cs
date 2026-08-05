using System;
using System.Collections;
using System.Globalization;
using System.Threading.Tasks;

namespace RuntimeArrayIdentitySubset
{
    // What handle does an array the RUNTIME allocated carry?
    //
    // The transpiler's untyped array allocators stamp one shared handle per representation
    // — System.Int32[] for the i4 rep, System.Object[] for the ref and the PACKED-VALUE
    // reps alike — so a helper that returns a fresh array without being told its element
    // hands back an array whose GetType() is a lie. Two grades of that, and only one is
    // visible from the type NAME: a byte[] or T[] stamped System.Object[] reads wrong, but
    // an int[] stamped with the runtime's SHARED int-array handle spells
    // "System.Int32[]" while its identity is not the image's ti_arr_Int32.
    //
    // So every probe asks for identity AND a non-generic interface dispatch, never just
    // GetType(): a retag that emits the precise handle without wiring that element's
    // SZArray interface map is WORSE than no retag — the array then claims an
    // IEnumerable<T> it has no slots for, and the first interface call through it loads a
    // null slot and calls it.
    //
    // Every line matches real .NET.
    internal static class Program
    {
        // The identity triple, over the erased Array surface so no generic body is minted:
        // the reflected name, the typeof identity, and whether the interface map is wired.
        private static void Show(string label, Array arr, Type expected)
        {
            Console.WriteLine($"{label} {arr.GetType()} {arr.GetType() == expected} "
                + $"itf={((ICollection)arr).Count}");
        }

        internal static void Run()
        {
            Console.WriteLine("== runtime array identity ==");

            // 1. Convert's byte[] results. Allocated by the packed-value allocator, which
            //    tags EVERY byte/char/long/double/struct-element array System.Object[].
            Show("frombase64", Convert.FromBase64String("SGVsbG8="), typeof(byte[]));
            Show("frombase64chararray",
                Convert.FromBase64CharArray("SGVsbG8=".ToCharArray(), 0, 8), typeof(byte[]));
            Show("fromhexstring", Convert.FromHexString("48656C6C6F"), typeof(byte[]));

            // 2. decimal.GetBits — the shared-int-handle grade. The name was already right.
            Show("getbits", decimal.GetBits(123.45m), typeof(int[]));

            // 3. NumberFormatInfo group sizes. Public surface, and the one most likely to be
            //    LINQ'd over by real code. One runtime site backs all three properties.
            var nfi = CultureInfo.InvariantCulture.NumberFormat;
            Show("numbergroupsizes", nfi.NumberGroupSizes, typeof(int[]));
            Show("currencygroupsizes", nfi.CurrencyGroupSizes, typeof(int[]));
            Show("percentgroupsizes", nfi.PercentGroupSizes, typeof(int[]));

            // 4. Task.WhenAll<T>'s result array — the only one of the six with no call site
            //    left to retag, because the array is materialized inside the completion
            //    callback. The handle rides on the join state instead. All three element
            //    representations, since each has its own allocation arm: reference, i4, and
            //    the raw 8-byte slot.
            Show("whenall-ref",
                Task.WhenAll(Task.FromResult("a"), Task.FromResult("b")).GetAwaiter().GetResult(),
                typeof(string[]));
            Show("whenall-i4",
                Task.WhenAll(Task.FromResult(1), Task.FromResult(2)).GetAwaiter().GetResult(),
                typeof(int[]));
            Show("whenall-i8",
                Task.WhenAll(Task.FromResult(1L), Task.FromResult(2L)).GetAwaiter().GetResult(),
                typeof(long[]));

            // The values still round-trip — a retag that moved the payload would be a much
            // worse bug than the one being fixed.
            Console.WriteLine("values "
                + Convert.ToHexString(Convert.FromBase64String("SGVsbG8=")) + " "
                + string.Join(",", decimal.GetBits(123.45m)) + " "
                + string.Join(",", nfi.NumberGroupSizes) + " "
                + string.Join(",",
                    Task.WhenAll(Task.FromResult("a"), Task.FromResult("b")).GetAwaiter().GetResult()));

            // 5. AggregateException's inner array. The runtime sites that build it use the
            //    untyped allocator, but get_InnerExceptions stamps the precise handle on
            //    the way out — and returns a real ReadOnlyCollection<Exception>, so reading
            //    .Count off the property's DECLARED type is a wrapper call rather than a
            //    raw Exception[] misread.
            var agg = new AggregateException(new InvalidOperationException("x"), new FormatException("y"));
            var inner = agg.InnerExceptions;
            Console.WriteLine("agg-inner " + inner.Count + " " + inner[0].GetType().Name);
        }
    }
}
