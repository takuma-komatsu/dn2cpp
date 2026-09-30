using System;
using System.Collections.Generic;
using System.Text;

// string.Concat<T> over every operand shape string.Join<T> takes — an array, a List<T>,
// a concrete collection and a bare IEnumerable<T> over an array or a list — with Join's
// element formatting: the unsigned widths print unsigned on both branches of the bare
// interface, and a null sequence of any shape raises ArgumentNullException for 'values'.
namespace ConcatSequenceSubset
{
    internal static class Program
    {
        private static string Try(Func<string> call)
        {
            try
            {
                return call();
            }
            catch (ArgumentNullException ex)
            {
                return ex.GetType().Name + ": " + ex.Message;
            }
        }

        internal static void __GateEntry()
        {
            Console.WriteLine("== Concat<T> and Join<T> sequence shapes ==");
            Console.WriteLine("array: " + string.Concat(new[] { 4000000000u, 7u }) + " " + string.Concat(new[] { ulong.MaxValue, 1ul })
                + " " + string.Concat(new[] { -1, 2 }) + " " + string.Concat(new[] { 1.5, -0.25 }) + " " + string.Concat(new[] { 'o', 'k' }));
            Console.WriteLine("list: " + string.Concat(new List<uint> { uint.MaxValue }) + " " + string.Concat(new List<ulong> { ulong.MaxValue })
                + " " + string.Concat(new HashSet<int> { 3, 1 }));
            IEnumerable<uint> uintArray = new[] { uint.MaxValue, 1u };
            IEnumerable<ulong> ulongArray = new[] { ulong.MaxValue, 2ul };
            IEnumerable<uint> uintList = new List<uint> { uint.MaxValue, 3u };
            Console.WriteLine("bare uint[]: " + string.Join(",", uintArray) + " | " + string.Concat(uintArray) + " | "
                + new StringBuilder().AppendJoin(';', uintArray));
            Console.WriteLine("bare ulong[]: " + string.Join(",", ulongArray) + " | " + string.Concat(ulongArray));
            Console.WriteLine("bare List<uint>: " + string.Join(",", uintList) + " | " + string.Concat(uintList));
            List<int> noList = null;
            int[] noArray = null;
            IEnumerable<int> noSequence = null;
            HashSet<int> noSet = null;
            Console.WriteLine("null Concat: " + Try(() => string.Concat(noList)) + " | " + Try(() => string.Concat(noArray)) + " | "
                + Try(() => string.Concat(noSequence)) + " | " + Try(() => string.Concat(noSet)));
            Console.WriteLine("null Join: " + Try(() => string.Join(",", noSequence)) + " | " + Try(() => string.Join(",", noSet)));
        }
    }
}
