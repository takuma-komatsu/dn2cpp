#nullable disable
using System;

namespace ArrayNullFaultSubset
{
    // The array helpers' null-RECEIVER faults, observed from a catch handler with real
    // .NET as the oracle. Indexing a null SZArray or MD array, reading a null Array's
    // Rank/GetLength/GetValue/Length, and Clone() on a null array of each element
    // representation all land in a runtime helper that takes the array as a pointer
    // argument, so the fault is the runtime's to raise — and .NET raises
    // NullReferenceException for every one of them.
    //
    // Array.Copy and Array.Clear are the counter-examples, and that is why they are
    // probed here: they are STATIC methods taking the array as an ARGUMENT, so .NET
    // answers a null with ArgumentNullException, not an NRE. Both are driven through BOTH
    // lowerings — System.Array-typed operands reaching dn2cpp_array_copy_dyn / _clear_dyn,
    // and statically int[] ones reaching the emitter's inline memmove/memset — because
    // only the operand guard makes the two agree on the exception type.
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

        internal static void Run()
        {
            int[] ni = null;
            string[] nr = null;
            Pair[] np = null;
            Array nb = null;
            int[] live = new int[2];

            // -- SZArray element access: one shared bounds check --
            Catches("null-i4[0] read", () => { s_sink = ni[0]; });
            Catches("null-i4[0] write", () => { ni[0] = 1; });
            Catches("null-ref[0] read", () => { s_sink = nr[0]; });
            Catches("null-ref[0] write", () => { nr[0] = "x"; });
            Catches("null-packed[0] read", () => { s_sink = np[0]; });

            // -- the SZArray length read: ldlen, one per element representation,
            //    plus Array.CopyTo, whose length comes off the same receiver --
            Catches("null-i4.Length", () => { s_sink = ni.Length; });
            Catches("null-ref.Length", () => { s_sink = nr.Length; });
            Catches("null-packed.Length", () => { s_sink = np.Length; });
            Catches("null-i4.CopyTo(live, 0)", () => ni.CopyTo(live, 0));

            // -- Clone(), one arm per element representation --
            Catches("null-i4.Clone()", () => { s_sink = ni.Clone(); });
            Catches("null-ref.Clone()", () => { s_sink = nr.Clone(); });
            Catches("null-packed.Clone()", () => { s_sink = np.Clone(); });
            Catches("null-Array.Clone()", () => { s_sink = nb.Clone(); });

            // -- the runtime-dispatched shape queries on a System.Array-typed
            //    receiver (the emitter's inline fast path is gone here) --
            Catches("null-Array.Rank", () => { s_sink = nb.Rank; });
            Catches("null-Array.GetLength(0)", () => { s_sink = nb.GetLength(0); });
            Catches("null-Array.GetValue(0)", () => { s_sink = nb.GetValue(0); });

            // -- multi-dimensional: the flat-index helpers, one per rank arm --
            int[,] nmd2 = null;
            int[,,] nmd3 = null;
            int[,,,] nmd4 = null;
            Catches("null-md2[0,0]", () => { s_sink = nmd2[0, 0]; });
            Catches("null-md3[0,0,0]", () => { s_sink = nmd3[0, 0, 0]; });
            Catches("null-md4[0,0,0,0]", () => { s_sink = nmd4[0, 0, 0, 0]; });
            Catches("null-md2.Length", () => { s_sink = nmd2.Length; });

            // -- the STATIC pair: an argument fault, not a receiver fault. Both
            //    lowerings are probed. System.Array-typed operands reach
            //    dn2cpp_array_copy_dyn / _clear_dyn; statically int[] ones reach
            //    the emitter's inline memmove/memset, which carries the operand
            //    guard so the two answer the same type. The ONE-argument
            //    Array.Clear(array), whose missing length is the array's own, is
            //    probed on both too. --
            Array liveArr = live;
            Catches("Array.Copy(null, live, 1)", () => Array.Copy(nb, liveArr, 1));
            Catches("Array.Copy(live, null, 1)", () => Array.Copy(liveArr, nb, 1));
            Catches("Array.Clear(null, 0, 0)", () => Array.Clear(nb, 0, 0));
            Catches("Array.Clear(null)", () => Array.Clear(nb));
            Catches("Array.Copy(null-i4, live, 1)", () => Array.Copy(ni, live, 1));
            Catches("Array.Copy(live, null-i4, 1)", () => Array.Copy(live, ni, 1));
            Catches("Array.Copy(null-ref, ref, 1)", () => Array.Copy(nr, new string[2], 1));
            Catches("Array.Copy(null-packed, packed, 1)", () => Array.Copy(np, new Pair[2], 1));
            Catches("Array.Clear(null-i4, 0, 0)", () => Array.Clear(ni, 0, 0));
            Catches("Array.Clear(null-i4)", () => Array.Clear(ni));
            Catches("Array.Clear(null-ref)", () => Array.Clear(nr));
            Catches("Array.Clear(null-packed)", () => Array.Clear(np));

            // The recovery is real: array work continues after the faults.
            live[0] = 7;
            Console.WriteLine("after null faults: " + live[0] + "/" + live.Length);

            // A typed catch selects the NRE over a broader handler.
            try
            {
                s_sink = ni[0];
                Console.WriteLine("unreachable " + s_sink);
            }
            catch (NullReferenceException)
            {
                Console.WriteLine("typed catch: NullReferenceException");
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
                    s_sink = nmd2[0, 0];
                    Console.WriteLine("unreachable " + s_sink);
                }
                finally
                {
                    ran++;
                }
            }
            catch (NullReferenceException)
            {
                Console.WriteLine("finally ran " + ran + " time(s) before the catch");
            }
        }
    }
}
