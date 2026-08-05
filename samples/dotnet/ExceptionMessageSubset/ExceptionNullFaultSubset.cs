#nullable disable
using System;
using System.Diagnostics;

namespace ExceptionMessageSubset
{
    // The exception SURFACE's own null-receiver faults. Reading .Message /
    // .InnerException / .HResult / .StackTrace, calling .GetBaseException() or
    // .ToString() through a null Exception reference, the same for a null
    // StackTrace's FrameCount/GetFrames/GetFrame and a null AggregateException's
    // InnerExceptions, and `throw null;` are all NullReferenceException in real
    // .NET — and each lands in a runtime entry point that takes the receiver as an
    // ordinary pointer argument, so the fault is the runtime's to raise rather than
    // an emitted-body dereference (which is EmittedNullFaultSubset's subject).
    //
    // ToString() is the awkward one, and its two halves must stay in one section.
    // Roslyn emits every virtual x.ToString() against System.Object::ToString, so
    // the dispatch route needs its own entry point (dn2cpp_object_tostring_virtual)
    // that raises the NRE, while the plain formatter dn2cpp_object_tostring keeps
    // folding a null operand to string.Empty for concatenation. Probe only the
    // throwing half and a "fix" that deletes the fold — breaking every
    // interpolation in the corpus — passes; ConcatFoldSurvives is that fold's
    // regression oracle and must stay beside the throw block.
    //
    // Execution CONTINUING past each probe is half the assertion: a runtime that
    // aborts at one of these cannot produce the expected output at all. Type name
    // only — the messages are not part of the contract.
    internal static class ExceptionNullFaultSubset
    {
        // A class that DOES override ToString, to show the call still binds to
        // System.Object::ToString (Roslyn emits the least-overridden slot), so the
        // null receiver faults before any override can run.
        private sealed class Overrider
        {
            public override string ToString() => "overridden";
        }

        // Every probe stores its result HERE rather than into an unused local: a
        // `var r = x.Member;` whose r is never read is a dead store, and the
        // transpiler drops the whole expression with it, leaving a probe that
        // reports "no throw" while asserting nothing. A store to a static field is
        // a side effect the elision cannot reach past.
        private static object s_sink;

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
            Exception ne = null;

            Catches("null-ex.Message", () => { s_sink = ne.Message; });
            Catches("null-ex.InnerException", () => { s_sink = ne.InnerException; });
            Catches("null-ex.HResult", () => { s_sink = ne.HResult; });
            Catches("null-ex.GetBaseException()", () => { s_sink = ne.GetBaseException(); });
            Catches("null-ex.StackTrace", () => { s_sink = ne.StackTrace; });
            Catches("null-ex.ToString()", () => { s_sink = ne.ToString(); });

            // The same virtual mouth reached through the base of everything: an
            // `object`-typed null, and a null whose static type overrides ToString
            // without that changing which method Roslyn emits the call against.
            object no = null;
            Catches("null-obj.ToString()", () => { s_sink = no.ToString(); });
            Overrider nov = null;
            Catches("null-overrider.ToString()", () => { s_sink = nov.ToString(); });

            AggregateException nagg = null;
            Catches("null-agg.InnerExceptions", () => { s_sink = nagg.InnerExceptions; });

            StackTrace nst = null;
            Catches("null-st.FrameCount", () => { s_sink = nst.FrameCount; });
            Catches("null-st.GetFrames()", () => { s_sink = nst.GetFrames(); });
            Catches("null-st.GetFrame(0)", () => { s_sink = nst.GetFrame(0); });

            // IL `throw` with a null operand.
            Catches("throw null", () => { throw ne; });

            // The recovery is real: a live exception still reads back afterwards.
            var live = new InvalidOperationException("still here");
            Console.WriteLine("after null faults: " + live.Message);

            // A typed catch selects the NRE over a broader handler.
            try
            {
                s_sink = ne.Message;
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
                    s_sink = ne.HResult;
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

            ConcatFoldSurvives(no);
        }

        /// <summary>Every CONCATENATION funnel must keep folding a null operand to
        /// string.Empty, which is what real .NET does in each of them. These share one
        /// formatter with the throwing ToString() above, so a "fix" that closed the
        /// throw by deleting the fold shows up here as wrong answers rather than as a
        /// missing exception. The operand arrives as a parameter so nothing folds the
        /// null at compile time.</summary>
        private static void ConcatFoldSurvives(object nul)
        {
            // string + object: String.Concat(object, object[, object]).
            Console.WriteLine("concat2: [" + nul + "]");
            Console.WriteLine("concat3: [" + nul + "|" + nul + "]");
            // The explicit call, at the arity the operator does not produce.
            Console.WriteLine("Concat(4): [" + string.Concat("a", nul, "b", nul) + "]");
            // An interpolation hole.
            Console.WriteLine($"interp: [{nul}]");
            // string.Join over an object[] with null elements.
            Console.WriteLine("join: [" + string.Join(",", new object[] { "a", nul, "b" }) + "]");
            // StringBuilder.Append(object) / Insert(object).
            var sb = new System.Text.StringBuilder();
            sb.Append('<').Append(nul).Append('>');
            sb.Insert(1, nul);
            Console.WriteLine("sb: [" + sb.ToString() + "]");
            // Console.Write(object) writes nothing rather than faulting.
            Console.Write("write: [");
            Console.Write(nul);
            Console.WriteLine("]");
            // Convert.ToString(object) answers string.Empty, not null.
            string cs = Convert.ToString(nul);
            Console.WriteLine("Convert.ToString: [" + cs + "] isNull=" + (cs is null));
        }
    }
}
