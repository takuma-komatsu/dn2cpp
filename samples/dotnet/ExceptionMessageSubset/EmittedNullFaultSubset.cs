#nullable disable
using System;
using System.Runtime.CompilerServices;

namespace ExceptionMessageSubset
{
    // The null-receiver faults of ORDINARY code, as distinct from
    // ExceptionNullFaultSubset, which probes the exception SURFACE's own. That
    // distinction is why both files exist: those land in a runtime entry point
    // that takes the receiver as an ordinary pointer argument, so the fault is
    // the runtime's to raise, while these are dereferences the EMITTED BODY
    // performs — a call's receiver, a field offset, a vtable load.
    //
    // Every receiver arrives through a NoInlining boundary, and that is
    // load-bearing: dereferencing a statically-null receiver is C++ undefined
    // behaviour, so clang at -O2 deletes the dispatch outright and the probe
    // prints neither a value nor an exception. A null the optimizer cannot see is
    // both the shape a real program has and the only one that asks the question.
    //
    // Execution continuing past each probe is half the assertion: a runtime that
    // aborted at one of these could not produce the expected output at all. Type
    // name only; messages are not part of the contract.
    internal static class EmittedNullFaultSubset
    {
        private interface IShape { int Area(); }

        private class Shape : IShape
        {
            internal int Side;
            public virtual int Area() => Side * Side;
            internal int NonVirtual() => Side + 1;
        }

        // Sealed, so a callvirt against it is DEVIRTUALIZED to a direct call —
        // the Thrive shape, and the one where the receiver used to reach the
        // callee body untested.
        private sealed class Square : Shape
        {
            public override int Area() => Side * Side * 2;
        }

        private struct Holder { internal Shape Inner; }

        private sealed class Node
        {
            internal Node Next;
            internal string Name;
            internal int[] Data;
        }

        // The optimizer must not see through these.
        [MethodImpl(MethodImplOptions.NoInlining)] private static Shape NullShape() => null;
        [MethodImpl(MethodImplOptions.NoInlining)] private static Square NullSquare() => null;
        [MethodImpl(MethodImplOptions.NoInlining)] private static IShape NullIface() => null;
        [MethodImpl(MethodImplOptions.NoInlining)] private static Node NullNode() => null;
        [MethodImpl(MethodImplOptions.NoInlining)] private static string NullStr() => null;
        [MethodImpl(MethodImplOptions.NoInlining)] private static Shape RealShape() => new Shape { Side = 3 };

        private static object s_sink;

        // A ref parameter, so the ldflda above has somewhere to go.
        private static void TakeRef(ref int[] r) { s_sink = r; }

        // An unconstrained-shape generic: the C# compiler emits `constrained. T`
        // before the callvirt, and the transpiler's reference/boxed arm then
        // loads the object pointer out of the byref. Instantiated at both a class
        // and an interface below.
        private static int ConstrainedArea<T>(T v) where T : IShape => v.Area();

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
            // ── the four call mouths ─────────────────────────────────────────
            // A devirtualized callvirt (sealed override): the receiver used to
            // reach the callee body raw.
            Catches("null-sealed.Area()", () => { s_sink = NullSquare().Area(); });
            // A real vtable dispatch: the load is off the receiver's header.
            Catches("null-virtual.Area()", () => { s_sink = NullShape().Area(); });
            // Interface dispatch: the load goes through the interface table.
            Catches("null-iface.Area()", () => { s_sink = NullIface().Area(); });
            // A non-virtual instance method — still a `callvirt` in the IL the C#
            // compiler emits, which is exactly why the guard hangs off callvirt.
            Catches("null-obj.NonVirtual()", () => { s_sink = NullShape().NonVirtual(); });

            // ── field access, all three opcodes ──────────────────────────────
            Catches("null-node.Name (ldfld)", () => { s_sink = NullNode().Name; });
            Catches("null-node.Name = (stfld)", () => { NullNode().Name = "x"; });
            // ldflda: the ADDRESS of a field of a null object. The guard has to
            // survive being taken the address of, not merely being read — the
            // splice sits inside the cast for exactly this reason, so the result
            // is still an lvalue.
            Catches("ldflda null-node.Data", () => { TakeRef(ref NullNode().Data); });
            // A chained read: the FIRST null is the one that faults.
            Catches("null-node.Next.Name", () => { s_sink = NullNode().Next.Name; });

            // ── String.get_Length: the hottest dereference in the corpus ─────
            Catches("null-str.Length", () => { s_sink = NullStr().Length; });

            // ── ldvirtftn: binding a delegate to a null receiver's override ──
            // .NET raises the NRE where the delegate is BOUND, not where it is
            // later invoked, and the slot load is what would otherwise fault.
            Catches("delegate from null-virtual", () => { Func<int> f = NullShape().Area; s_sink = f; });
            Catches("delegate from null-iface", () => { Func<int> f = NullIface().Area; s_sink = f; });

            // ── a `constrained.` callvirt whose T turns out to be a class ────
            // The byref is the address of live storage; the OBJECT POINTER it
            // holds is the null, which is the one the guard has to test.
            Catches("constrained<Shape>(null)", () => { s_sink = ConstrainedArea<Shape>(null); });
            Catches("constrained<IShape>(null)", () => { s_sink = ConstrainedArea<IShape>(null); });

            // ── a null receiver reached through a struct field ───────────────
            Holder h = default;
            Catches("default-holder.Inner.Area()", () => { s_sink = h.Inner.Area(); });

            // ── the catch hierarchy and the recovery ─────────────────────────
            try
            {
                s_sink = NullShape().Area();
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

            int ran = 0;
            try
            {
                try
                {
                    s_sink = NullNode().Name;
                    Console.WriteLine("unreachable " + s_sink);
                }
                finally
                {
                    ran++;
                }
            }
            catch (NullReferenceException)
            {
                Console.WriteLine("emitted finally ran " + ran + " time(s) before the catch");
            }

            // ── the non-null path is untouched ───────────────────────────────
            // The other half of the assertion. A guard that threw here, or one
            // whose spliced expression evaluated its receiver twice, would show
            // up as a wrong answer rather than as a missing exception.
            Shape live = RealShape();
            Console.WriteLine("live.Area=" + live.Area() + " live.Side=" + live.Side
                + " live.NonVirtual=" + live.NonVirtual());
            IShape liveI = live;
            Console.WriteLine("liveI.Area=" + liveI.Area());
            var node = new Node { Name = "n", Data = new int[] { 1, 2, 3 } };
            node.Next = new Node { Name = "m" };
            Console.WriteLine("node=" + node.Name + " next=" + node.Next.Name
                + " len=" + node.Data.Length + " strlen=" + node.Name.Length);
            node.Name = "renamed";
            Console.WriteLine("after stfld: " + node.Name);
        }
    }
}
