#nullable disable
using System;

namespace ArrayAllocThrowSubset
{
    // Allocating an array with a length computed at run time: a negative length
    // is bad data, and ECMA-335 has `newarr` raise OverflowException for it —
    // catchable, so a caller that computed the length from input can report and
    // continue. The lengths come from a method the optimizer cannot fold, so
    // the check is the runtime's rather than the C# compiler's.
    internal static class Program
    {
        private static int Neg(int n)
        {
            return -n;
        }

        private static void Catches(string what, Action body)
        {
            try
            {
                body();
                Console.WriteLine(what + " -> no throw");
            }
            catch (Exception e)
            {
                Console.WriteLine(what + " -> " + e.GetType().Name);
            }
        }

        internal static void Run()
        {
            Catches("new int[-1]", () => { int[] a = new int[Neg(1)]; });
            Catches("new object[-2]", () => { object[] a = new object[Neg(2)]; });
            Catches("new byte[-3]", () => { byte[] a = new byte[Neg(3)]; });
            Catches("new DateTime[-4]", () => { DateTime[] a = new DateTime[Neg(4)]; });
            Catches("new int[-1, 2]", () => { int[,] a = new int[Neg(1), 2]; });

            // Recovery is real: allocation still works after the faults.
            int[] good = new int[3];
            good[0] = 7;
            Console.WriteLine("after faults: len=" + good.Length + " [0]=" + good[0]);

            try
            {
                int[] a = new int[Neg(9)];
                Console.WriteLine("unreachable " + a.Length);
            }
            catch (OverflowException)
            {
                Console.WriteLine("typed catch: OverflowException");
            }
            catch (Exception)
            {
                Console.WriteLine("typed catch: fell through to Exception");
            }
        }
    }
}
