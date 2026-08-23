using System;
using System.Globalization;

namespace ExceptionMessageSubset
{
    // System.Exception.get_Message + GetType, the pair dn2cpp's own catch+inspect
    // code reads to build a wrapped exception or a measure-gap record. An
    // Exception-derived newobj is intercepted to a message-carrying object
    // (dn2cpp_exception_new); get_Message returns the stored message, GetType reads
    // the object header — both exact vs real .NET. HResult has a real prefix slot,
    // so even a parameterless Argument*/FileNotFound* exception's per-type default
    // Message is exact (its get_Message override picks the resource default via the
    // `HResult == COR_E_*` probe). The only approximation left is the type-less base
    // default of a bare `new Exception()`, which matches real .NET anyway.
    //
    // get_Source is exercised only on FRESH (un-thrown) exceptions, where both sides
    // answer null. A THROWN exception's Source diverges — real .NET fills the
    // throwing assembly's name, dn2cpp keeps null, symmetric with the set_Source
    // no-op — so it is deliberately not printed, to keep the gate an exact diff.

    // A derived exception whose ctor sets an instance field after base(message). The
    // (TCode, string) shape's message arg is recovered by
    // Compilation.ExceptionMessageArgIndex and seeded into the base
    // Dn2CppExceptionObject prefix by the intercept; the derived ctor body must then
    // still run to write Code, and a regression that skips it leaves Code at 0 while
    // .Message keeps working.
    internal sealed class ProbeCodeError : Exception
    {
        public int Code { get; }
        public ProbeCodeError(int code, string message) : base(message) { Code = code; }
    }

    // (TCode, string, Exception) shape: the intercept seeds both message and inner,
    // then the ctor body writes Code.
    internal sealed class ProbeCodeErrorWithInner : Exception
    {
        public int Code { get; }
        public ProbeCodeErrorWithInner(int code, string message, Exception inner)
            : base(message, inner) { Code = code; }
    }

    // Plain (string) shape but with a derived instance field the ctor body
    // initializes. Regression protection for the case where the intercept
    // recognizes the base message but the derived field still needs the ctor
    // body to run — a skipped body would leave Tag null.
    internal sealed class ProbeTaggedError : Exception
    {
        public string Tag { get; }
        public ProbeTaggedError(string message) : base(message) { Tag = "default-tag"; }
    }

    internal static class Program
    {
        private static string Show(Exception ex) =>
            $"[{ex.Message}]|{ex.GetType().Name}|{ex.GetType().FullName}";

        // The own-code shape: typed catch of NotSupportedException, read message,
        // re-throw a wrapped one (exactly MethodCompiler.Compile's pattern).
        private static string Compile(int n)
        {
            try
            {
                if (n == 1)
                    throw new NotSupportedException("op A is not supported");
                if (n == 2)
                    throw new NotSupportedException("op B uses an unmapped feature");
                return "compiled";
            }
            catch (NotSupportedException ex)
            {
                return "wrap: Program.Compile: " + ex.Message;
            }
        }

        // The MeasureGap.From shape: ex.GetType().Name + ex.Message via catch (Exception).
        private static string Measure(int n)
        {
            try
            {
                if (n == 1)
                    throw new NotSupportedException("gap C");
                throw new ArgumentException("gap D");
            }
            catch (Exception ex)
            {
                return ex.GetType().Name + ": " + ex.Message;
            }
        }

        private static void Main()
        {
            // Pin both cultures first: gate output must not depend on the host locale (see AGENTS.md).
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

            // with-message: Message verbatim, GetType().Name / .FullName exact.
            Console.WriteLine(Show(new NotSupportedException("foo")));
            Console.WriteLine(Show(new InvalidOperationException("bar baz")));
            Console.WriteLine(Show(new ArgumentException("qux")));
            Console.WriteLine(Show(new Exception("plain")));
            Console.WriteLine(Show(new System.IO.IOException("io trouble")));
            // empty-string message is preserved (not replaced by a default).
            Console.WriteLine(Show(new NotSupportedException("")));
            // (string message, Exception inner): the first arg is the message.
            Console.WriteLine(Show(new InvalidOperationException("with inner", new Exception("inner"))));

            // base Exception default message (null / parameterless) — this exact text
            // ("Exception of type 'System.Exception' was thrown.") matches real .NET.
            Console.WriteLine(Show(new Exception()));
            Console.WriteLine(Show(new Exception(null)));

            // Parameterless BCL exceptions whose ctor chains base(SR.<key>): the real
            // per-type default message, recovered from the embedded .resources blob and
            // stored by the ctor chain. These types do not override
            // get_Message, so no dispatch is involved — the stored text reads back.
            Console.WriteLine("nse-default: " + new NotSupportedException().Message);
            Console.WriteLine("ioe-default: " + new InvalidOperationException().Message);

            // own-code catch+inspect shapes.
            Console.WriteLine(Compile(1));
            Console.WriteLine(Compile(2));
            Console.WriteLine(Compile(3));
            Console.WriteLine(Measure(1));
            Console.WriteLine(Measure(2));

            // nested catch: inner thrown+caught, then a new outer thrown+caught.
            try
            {
                try
                {
                    throw new InvalidOperationException("level 1");
                }
                catch (InvalidOperationException e1)
                {
                    throw new NotSupportedException("level 2 wrapping: " + e1.Message);
                }
            }
            catch (NotSupportedException e2)
            {
                Console.WriteLine("nested: " + e2.Message + " (" + e2.GetType().Name + ")");
            }

            // get_Source on FRESH (un-thrown) exceptions: null in both real .NET and
            // dn2cpp (exact). The thrown-exception divergence is the documented
            // carve-out, not printed (see the class-header note).
            Console.WriteLine("source: " + (new Exception("s").Source ?? "<null>"));
            Console.WriteLine("source: " + (new NotSupportedException("s2").Source ?? "<null>"));
            Console.WriteLine("source: " + (new InvalidOperationException("s3").Source ?? "<null>"));

            // User-defined derived exception with an instance field set by the ctor
            // body after base(message). Prints Message + Code so a regression that
            // skips the derived ctor body (Code stays at 0) fails the exact diff.
            try
            {
                throw new ProbeCodeError(42, "code error boom");
            }
            catch (ProbeCodeError e)
            {
                Console.WriteLine("derived: " + e.Message + " / code=" + e.Code);
            }

            // (TCode, string, Exception) shape: message + inner both seeded by the
            // intercept, derived Code set by the ctor body.
            try
            {
                throw new ProbeCodeErrorWithInner(7, "outer boom", new InvalidOperationException("inner boom"));
            }
            catch (ProbeCodeErrorWithInner e)
            {
                Console.WriteLine("derived-inner: " + e.Message + " / code=" + e.Code
                    + " / inner=" + (e.InnerException?.Message ?? "<null>"));
            }

            // Plain (string) shape on a derived type with a non-code field. The base
            // message path is the original intercepted shape; the ctor body still needs
            // to run so Tag is set.
            try
            {
                throw new ProbeTaggedError("tagged boom");
            }
            catch (ProbeTaggedError e)
            {
                Console.WriteLine("derived-tag: " + e.Message + " / tag=" + e.Tag);
            }

            // Confirm the freshly-constructed (un-thrown) form reads the derived
            // field too, so the intercept path stays consistent whether or not the
            // object goes through a throw.
            var fresh = new ProbeCodeError(99, "fresh");
            Console.WriteLine("fresh: " + fresh.Message + " / code=" + fresh.Code);

            // Bare `catch {}` (IL `catch (object)`): must intercept BOTH a
            // thrown exception object and a runtime-raised trap. The emitted
            // clause once isinst'd against the runtime object type-info — which
            // no exception's base chain reaches — so the clause silently rethrew
            // (the managed-vs-native self-host fixpoint divergence).
            string bare = "no";
            try
            {
                throw new NotSupportedException("bare catch");
            }
            catch
            {
                bare = "caught";
            }
            Console.WriteLine("bare-catch thrown: " + bare);
            try
            {
                object box = "not an int";
                _ = (int)box; // runtime-raised InvalidCastException
                bare = "no";
            }
            catch
            {
                bare = "caught trap";
            }
            Console.WriteLine("bare-catch trap: " + bare);

            // HResult: real storage on the Dn2CppExceptionObject prefix. The base
            // Exception default is COR_E_EXCEPTION; each derived BCL ctor's set_HResult
            // stamps its per-type value. All exact vs real .NET.
            Console.WriteLine("hr Exception: " + new Exception("x").HResult);
            Console.WriteLine("hr ArgumentException: " + new ArgumentException("m").HResult);
            Console.WriteLine("hr ArgumentNullException: " + new ArgumentNullException("p").HResult);
            Console.WriteLine("hr InvalidOperationException: " + new InvalidOperationException("y").HResult);
            var hrSet = new Exception("s"); hrSet.HResult = 123;
            Console.WriteLine("hr set: " + hrSet.HResult);

            // GetBaseException: the innermost exception of the inner-chain, or the
            // exception itself when there is none.
            var lv5Inner = new Exception("root");
            var lv5Mid = new Exception("mid", lv5Inner);
            var lv5Outer = new Exception("outer", lv5Mid);
            Console.WriteLine("gbe 3-deep: " + ReferenceEquals(lv5Outer.GetBaseException(), lv5Inner));
            var lv5Solo = new Exception("solo");
            Console.WriteLine("gbe no-inner==self: " + ReferenceEquals(lv5Solo.GetBaseException(), lv5Solo));

            // A parameterless Argument*/FileNotFound* exception's get_Message override picks
            // its resource default via `_message == null && HResult == COR_E_*`, so these
            // read the exact per-type text only while HResult has real storage. Never hit by
            // self-host, which always constructs with a message.
            Console.WriteLine("ae-default: " + new ArgumentException().Message);
            Console.WriteLine("ane-default: " + new ArgumentNullException().Message);
            Console.WriteLine("aoore-default: " + new ArgumentOutOfRangeException().Message);
            Console.WriteLine("fnf-default: " + new System.IO.FileNotFoundException().Message);

            // The members an exception type declares itself (ArgumentException.ParamName
            // and friends), plus the Argument*/FileNotFound .Message family — see
            // ExceptionVirtualMembers.cs.
            ExceptionVirtualMembers.Run();

            // Exception.GetObjectData — the ISerializable base hook a subclass override
            // calls as base.GetObjectData — see ExceptionGetObjectData.cs.
            ExceptionGetObjectData.Run();

            // The null-receiver faults of the exception surface itself.
            ExceptionNullFaultSubset.Run();

            // And the null-receiver faults of ORDINARY code — a call's receiver, a field
            // offset, a vtable load — which are the EMITTED body's rather than a runtime
            // entry point's. It sits beside its sibling because the two are distinguishable
            // only by whose dereference it is, and a reader pruning by theme would merge them.
            EmittedNullFaultSubset.Run();

            // These two are not about a message or a type name: they are the two EH REGION
            // KINDS the rest of this bucket never enters — a `fault` region and a
            // `filter`/`endfilter` pair — which is why they live here and why a reader
            // pruning by theme would wrongly delete them. Keep them at the tail so the
            // bucket's earlier output stays an unchanged prefix.
            FaultSubset.Program.Run();
            FilterSubset.Program.Run();

            // TypeLoadException's public constructors never need its VM-only lazy
            // formatter, but get_Message makes that body statically reachable.
            TypeLoadExceptionMessageSubset.Run();
        }
    }
}
