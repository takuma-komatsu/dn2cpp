#nullable disable
using System;
using System.Runtime.InteropServices;

namespace MarshalNullFaultSubset
{
    // The non-generic Marshal surface's null-argument faults, observed from a
    // catch handler with real .NET as the oracle: each must be a CATCHABLE
    // exception, never a dn2cpp_fail abort that never reaches the handler.
    //
    // The Type-taking overloads are the reachable shape: a Type chosen at run
    // time (out of a table, off a field, from Type.GetType) is exactly the
    // argument that can be null, and no static analysis in the caller rules it out.
    //
    // Two rows are here because they are NOT faults: Marshal.PtrToStructure over
    // IntPtr.Zero returns null rather than throwing, and a successful round-trip
    // still works after the faults. The rest of the bucket covers the happy paths.
    internal static class Program
    {
        // Stored, never read: a dead store is dropped along with the expression
        // that fed it, which would silently turn a probe into a no-op.
        private static object s_sink;

        [StructLayout(LayoutKind.Sequential)]
        private struct Blit
        {
            public int A;
            public int B;
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

        internal static void __GateEntry()
        {
            Type nt = null;

            Catches("Marshal.SizeOf((Type)null)", () => { s_sink = Marshal.SizeOf(nt); });

            IntPtr p = Marshal.AllocHGlobal(16);
            try
            {
                Catches("Marshal.PtrToStructure(ptr, (Type)null)",
                    () => { s_sink = Marshal.PtrToStructure(p, nt); });
                Catches("Marshal.StructureToPtr(null, ptr, false)",
                    () => Marshal.StructureToPtr((object)null, p, false));
                // The receiver is boxed on purpose, so C# overload resolution picks
                // the NON-generic StructureToPtr(object, IntPtr, bool) — the one
                // that reaches dn2cpp_marshal_structure_to_ptr and its guard.
                // Passing the struct by value binds the generic StructureToPtr<T>,
                // which the emitter lowers to an inline store off the raw pointer
                // with no test at all, so a zero destination faults the EMITTED
                // body — a separate emit-side null-check question. Real .NET
                // answers both overloads with ArgumentNullException("ptr"); the two
                // disagree here, so only this overload is asserted.
                object boxed = new Blit { A = 1, B = 2 };
                Catches("Marshal.StructureToPtr((object)v, IntPtr.Zero, false)",
                    () => Marshal.StructureToPtr(boxed, IntPtr.Zero, false));

                // Not a fault: a zero source pointer answers null, so that arm must
                // sit below the null-type check rather than above it.
                Catches("Marshal.PtrToStructure(IntPtr.Zero, typeof(Blit))", () =>
                {
                    object r = Marshal.PtrToStructure(IntPtr.Zero, typeof(Blit));
                    Console.Write("[" + (r is null ? "null" : r.ToString()) + "] ");
                });

                // A typed catch selects the fault over a broader handler.
                try
                {
                    s_sink = Marshal.SizeOf(nt);
                    Console.WriteLine("unreachable " + s_sink);
                }
                catch (ArgumentNullException)
                {
                    Console.WriteLine("typed catch: ArgumentNullException");
                }
                catch (Exception)
                {
                    Console.WriteLine("typed catch: fell through to Exception");
                }

                // Recovery is real: the surface still round-trips afterwards.
                Marshal.StructureToPtr(new Blit { A = 3, B = 4 }, p, false);
                Blit back = (Blit)Marshal.PtrToStructure(p, typeof(Blit));
                Console.WriteLine("after faults: A=" + back.A + " B=" + back.B
                    + " size=" + Marshal.SizeOf(typeof(Blit)));

                // A loop over a mixed type table: one null row must not take
                // the program down.
                Type[] types = { typeof(Blit), null, typeof(int), null };
                int sized = 0, bad = 0;
                foreach (Type t in types)
                {
                    try
                    {
                        sized += Marshal.SizeOf(t);
                    }
                    catch (ArgumentNullException)
                    {
                        bad++;
                    }
                }
                Console.WriteLine("sizeof loop: sized=" + sized + " bad=" + bad);
            }
            finally
            {
                Marshal.FreeHGlobal(p);
            }
        }
    }
}
