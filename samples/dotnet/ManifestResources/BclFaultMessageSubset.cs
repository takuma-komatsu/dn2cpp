#nullable disable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

// SUBJECT: not resources — EXCEPTIONS, and their MESSAGE TEXT. Every probe below faults
// inside the BCL, catches, and reads the caught exception's Message. In the arms that pass
// --no-manifest-resources System.Private.CoreLib that read is the one thing a resource drop
// could turn into a second, uncatchable throw: real .NET renders these messages out of
// CoreLib's own Strings.resources, so an SR-shaped implementation would go through the very
// table the drop empties. dn2cpp's does not — System.SR is intercepted and its text folded
// in as a literal at transpile time (CoreIntrinsics, ResourceStrings), and the same fold
// carries the C++ runtime's own throw sites (Dn2Cpp.BclMessages -> dn2cpp_bcl_messages).
// Delete this section and a future SR lowering that reaches ResourceManager.GetString ships
// a game whose first BCL fault dies while building its own diagnostic.
//
// The TEXT is diffed, not just the type, and that is the point: it is what makes the fold
// assertable at all. The probes cover the three routes a message can take — a transpiled
// BCL ctor reading System.SR (guid-parse, encoding-932), a C++ runtime trap reading the
// emitted table (array-index, divide-by-zero, int-parse, substring-negative), and a
// ThrowHelper sink whose ExceptionResource the emitter resolved at the call site
// (list-ctor-neg, dict-duplicate-key, dict-missing).
//
// FaultTypeOnly is for the one probe whose text cannot match: see its call site.
namespace BclFaultMessageSubset
{
    internal static class Program
    {
        internal static void __GateEntry()
        {
            Console.WriteLine("== bcl fault message reads ==");

            Fault("int-parse", static () => int.Parse("x", CultureInfo.InvariantCulture));
            Fault("convert-toint32", static () => Convert.ToInt32("zz", CultureInfo.InvariantCulture));
            Fault("substring-negative", static () => "abc".Substring(-1));
            Fault("substring-past-end", static () => "abc".Substring(5, 0));
            Fault("array-index", static () => { int[] a = new int[2]; int i = 5; Consume(a[i]); });
            Fault("invalid-cast", static () => { object o = 42; Consume(((string)o).Length); });
            Fault("null-deref", static () => { string s = null; Consume(s.Length); });
            Fault("dict-duplicate-key", static () =>
            {
                Dictionary<string, int> d = new Dictionary<string, int>();
                d.Add("k", 1);
                d.Add("k", 2);
            });
            Fault("dict-missing-key", static () =>
            {
                Dictionary<string, int> d = new Dictionary<string, int>();
                Consume(d["zz"]);
            });
            // ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.capacity,
            // ExceptionResource.ArgumentOutOfRange_NeedNonNegNum): the form that states its
            // resource AND its argument name at the call site, which is what the emitter
            // reads. Both halves of the sentence are in the diff.
            Fault("list-ctor-negative", static () => { int n = -1; Consume(new List<int>(n).Count); });
            // The other ThrowHelper shape: ThrowArgumentOutOfRange_IndexMustBeLessException()
            // takes NO arguments and names its resource inside its own body, which is the half
            // only a metadata read of the sink recovers (ThrowHelperResources).
            Fault("list-index", static () => { int i = 10; Consume(new List<int>()[i]); });
            // The one probe whose message the transpiler cannot reproduce: .NET's tail
            // ("There is an unknown word starting at index '0'.") is a position its own
            // DateTimeParse tokenizer reports, and dn2cpp's hand-written date parser answers
            // only yes/no. It raises the sibling resource .NET itself uses for ParseExact
            // (Format_BadDateTime), naming the string — so the read is still a real sentence.
            FaultTypeOnly("datetime-parse", static () => DateTime.Parse("nope", CultureInfo.InvariantCulture));
            Fault("timespan-parse", static () => TimeSpan.Parse("nope", CultureInfo.InvariantCulture));
            Fault("divide-by-zero", static () => { int z = 0; Consume(1 / z); });
            Fault("enum-parse", static () => Enum.Parse(typeof(DayOfWeek), "nope"));
            Fault("guid-parse", static () => Guid.Parse("nope"));
            Fault("checked-overflow", static () => { int big = int.MaxValue; Consume(checked(big + 1)); });
            Fault("default-ctor-message", static () => throw new InvalidOperationException());
            Fault("encoding-932", static () => Consume(Encoding.GetEncoding(932).WebName.Length));
        }

        private static void Consume(int _)
        {
        }

        private static void Fault(string label, Action body) => Report(label, body, withText: true);

        private static void FaultTypeOnly(string label, Action body) => Report(label, body, withText: false);

        // The Message read sits in its own try: a nested throw out of it is the exact
        // failure this section exists to catch, and it must be reported as one rather
        // than escaping as an unhandled exception that names nothing.
        private static void Report(string label, Action body, bool withText)
        {
            Exception caught = null;
            try
            {
                body();
            }
            catch (Exception e)
            {
                caught = e;
            }
            if (caught == null)
            {
                Console.WriteLine("fault " + label + " NO-THROW");
                return;
            }
            string message;
            try
            {
                message = caught.Message;
            }
            catch (Exception e2)
            {
                Console.WriteLine("fault " + label + " " + caught.GetType().FullName
                    + " msg-read THREW " + e2.GetType().FullName);
                return;
            }
            // A newline inside the text (ArgumentOutOfRangeException appends one before
            // "Actual value was ...") would make one probe two output lines and hide which
            // is which; escape it so a probe is always one line. That newline is
            // Environment.NewLine, so folding CRLF to LF first is what keeps the escaped
            // text — and the frozen snapshots holding it — the same on every host.
            string shown = message == null
                ? "<null>"
                : message.Replace("\r\n", "\n").Replace("\r", "\\r").Replace("\n", "\\n");
            Console.WriteLine("fault " + label + " " + caught.GetType().FullName
                + (withText
                    ? " msg[" + shown + "]"
                    : " msg-read ok non-empty " + (message != null && message.Length > 0)));
        }
    }
}
