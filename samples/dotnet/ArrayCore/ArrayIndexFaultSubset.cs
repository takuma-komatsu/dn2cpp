#nullable disable
using System;

namespace ArrayIndexFaultSubset
{
    // The array bounds faults, observed from a catch handler with real .NET as the
    // oracle: read and write, past-the-end and negative, one dimension and several —
    // .NET answers every shape with IndexOutOfRangeException, returning to the handler
    // rather than aborting.
    //
    // The sibling ArrayNullFaultSubset covers the OTHER branch of the same helpers — a
    // null receiver, i.e. NullReferenceException — so the two together pin both answers
    // dn2cpp_bounds_check can give. Keep them apart: they are different exception types.
    //
    // Every element representation is probed, because the bounds check is reached through
    // a different helper in each: int32 through dn2cpp_ldelem_i4 / dn2cpp_stelem_i4,
    // references through dn2cpp_ldelem_ref / dn2cpp_stelem_ref, packed structs and
    // sub-word primitives through dn2cpp_elem_addr, and `ref arr[i]` through the same
    // address helper by a different emit arm. A conversion reaching only one of them
    // would look complete from any single probe.
    internal static class Program
    {
        // Stored, never read: a dead store is dropped along with the expression
        // that fed it, which would silently turn a probe into a no-op.
        private static object s_sink;

        private struct Pair { public byte A; public byte B; }

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

        private static ref int First(int[] a, int i) => ref a[i];

        internal static void Run()
        {
            int[] ai = new int[3];
            string[] ar = new string[3];
            Pair[] ap = new Pair[3];
            byte[] ab = new byte[3];

            // -- one dimension, every representation, read then write --
            Catches("i4[5] read", () => { s_sink = ai[5]; });
            Catches("i4[-1] read", () => { s_sink = ai[-1]; });
            Catches("i4[3] read (== Length)", () => { s_sink = ai[3]; });
            Catches("i4[5] write", () => { ai[5] = 1; });
            Catches("i4[-1] write", () => { ai[-1] = 1; });
            Catches("ref[5] read", () => { s_sink = ar[5]; });
            Catches("ref[5] write", () => { ar[5] = "x"; });
            Catches("packed[5] read", () => { s_sink = ap[5]; });
            Catches("packed[5] write", () => { ap[5] = default(Pair); });
            Catches("byte[5] read", () => { s_sink = ab[5]; });
            Catches("byte[5] write", () => { ab[5] = 1; });

            // -- `ref arr[i]`: the address arm of the same check --
            Catches("ref arr[5]", () => { s_sink = First(ai, 5); });

            // -- the runtime-dispatched shape: Array.GetValue/SetValue --
            Array dyn = ai;
            Catches("Array.GetValue(5)", () => { s_sink = dyn.GetValue(5); });
            Catches("Array.SetValue(1, 5)", () => dyn.SetValue(1, 5));

            // -- several dimensions: one arm per rank (2, 3, and the general N) --
            int[,] m2 = new int[2, 2];
            int[,,] m3 = new int[2, 2, 2];
            int[,,,] m4 = new int[2, 2, 2, 2];
            Catches("md2[0,5] read", () => { s_sink = m2[0, 5]; });
            Catches("md2[5,0] read", () => { s_sink = m2[5, 0]; });
            Catches("md2[0,-1] read", () => { s_sink = m2[0, -1]; });
            Catches("md2[0,5] write", () => { m2[0, 5] = 1; });
            Catches("md3[0,0,5] read", () => { s_sink = m3[0, 0, 5]; });
            Catches("md3[5,0,0] read", () => { s_sink = m3[5, 0, 0]; });
            Catches("md3[0,0,5] write", () => { m3[0, 0, 5] = 1; });
            Catches("md4[0,0,0,5] read", () => { s_sink = m4[0, 0, 0, 5]; });
            Catches("md4[0,0,0,5] write", () => { m4[0, 0, 0, 5] = 1; });

            // A typed catch selects the fault over a broader handler, and does
            // not collide with the null branch of the very same check.
            try
            {
                s_sink = ai[5];
                Console.WriteLine("unreachable " + s_sink);
            }
            catch (NullReferenceException)
            {
                Console.WriteLine("typed catch: wrongly NullReferenceException");
            }
            catch (IndexOutOfRangeException)
            {
                Console.WriteLine("typed catch: IndexOutOfRangeException");
            }
            catch (Exception)
            {
                Console.WriteLine("typed catch: fell through to Exception");
            }

            // A fault raised inside a finally-guarded region still runs the
            // finally on the way out.
            int ran = 0;
            try
            {
                try
                {
                    s_sink = m2[0, 5];
                    Console.WriteLine("unreachable " + s_sink);
                }
                finally
                {
                    ran++;
                }
            }
            catch (IndexOutOfRangeException)
            {
                Console.WriteLine("finally ran " + ran + " time(s) before the catch");
            }

            // The recovery is real, and the array is intact: an out-of-range
            // WRITE must not have landed anywhere. This is the assertion that
            // distinguishes a converted throw from a check that was skipped.
            ai[0] = 7;
            ai[2] = 9;
            Console.WriteLine("after index faults: " + ai[0] + "/" + ai[1] + "/" + ai[2]
                + " len=" + ai.Length);
            Console.WriteLine("md2 intact: " + m2[0, 0] + "/" + m2[1, 1]);

            // The shape this section exists for: one bad row in a table walk
            // must not take the program down.
            int[] rows = { 0, 4, 1, -2, 2 };
            int got = 0, bad = 0;
            foreach (int r in rows)
            {
                try
                {
                    got += ai[r];
                }
                catch (IndexOutOfRangeException)
                {
                    bad++;
                }
            }
            Console.WriteLine("index loop: got=" + got + " bad=" + bad);
        }
    }
}
