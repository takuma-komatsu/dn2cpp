#nullable disable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace ExceptionMessageSubset;

// ParamName, ActualValue and Message of argument exceptions no constructor builds:
// the runtime raises them for a parameter one of its helpers names, for the
// ExceptionArgument a ThrowHelper sink passes, and for a value an out-of-range check
// reports, and bare where .NET names nothing. Message is read through the exception's
// own type and through System.Exception.
internal static class RuntimeArgumentFieldsSubset
{
    private static string Show(string text) => text is null ? "<null>"
        : text.Replace("\0", "<nul>").Replace("\ud800", "<sur>").Replace("\r", "").Replace("\n", "|");

    private static void Probe(string label, Action action)
    {
        try
        {
            action();
            Console.WriteLine(label + ": no exception");
        }
        catch (ArgumentException e)
        {
            string actual = e is ArgumentOutOfRangeException range
                ? " actual=" + (range.ActualValue ?? "<null>")
                    + " actual-type=" + (range.ActualValue is null ? "<null>" : range.ActualValue.GetType().Name)
                : "";
            Console.WriteLine(label + ": " + e.GetType().Name + " param=" + Show(e.ParamName) + actual);
            Console.WriteLine("  message=" + Show(e.Message));
            Exception asBase = e;
            Console.WriteLine("  same through Exception=" + (asBase.Message == e.Message));
        }
    }

    internal static void Run()
    {
        Console.WriteLine("-- runtime-raised argument fields --");
        Probe("dictionary null key", () => new Dictionary<string, int>().Add(null, 1));
        Probe("list index", () => new List<int>().RemoveAt(3));
        Probe("IList null item", () => ((IList)new List<int>()).Add(null));
        Probe("substring start", () => "abc".Substring(5));
        Probe("substring negative start", () => "abc".Substring(-1));
        Probe("substring start and length", () => "abc".Substring(5, -1));
        Probe("substring negative length", () => "abc".Substring(1, -1));
        Probe("substring range", () => "abc".Substring(1, 5));
        Probe("string count", () => new string('a', -1));
        Probe("join null", () => string.Join(",", (string[])null));
        Probe("concat null", () => string.Concat((object[])null));
        Probe("AppendJoin null", () => new StringBuilder().AppendJoin(',', (string[])null));
        Probe("builder repeat", () => new StringBuilder().Append('x', -1));
        Probe("builder window start", () => new StringBuilder().Append("ab", -1, 1));
        Probe("builder window count", () => new StringBuilder().Append("ab", 0, -1));
        Probe("builder window null", () => new StringBuilder().Append((string)null, 1, 0));
        Probe("builder window range", () => new StringBuilder().Append("ab", 1, 2));
        try
        {
            new Dictionary<string, int>().Add(null, 1);
        }
        catch (Exception e)
        {
            Console.WriteLine("ToString: " + e.ToString().Split('\n')[0].TrimEnd());
        }
    }

    // The parameter a runtime helper or an inline lowering names where .NET names
    // one.
    internal static void RunNamedSites()
    {
        Console.WriteLine("-- runtime-raised parameter names --");
        Probe("polyfill IfNullOrWhitespace null", () => ThrowHelper.IfNullOrWhitespace(null, "p"));
        Probe("polyfill IfNullOrWhitespace empty", () => ThrowHelper.IfNullOrWhitespace("", "p"));
        Probe("polyfill IfNullOrWhitespace blank", () => ThrowHelper.IfNullOrWhitespace("   ", "p"));
        Probe("polyfill null name", () => ThrowHelper.IfNullOrWhitespace(null, null));
        Probe("polyfill empty name", () => ThrowHelper.IfNullOrWhitespace(null, ""));
        Probe("polyfill unicode name", () => ThrowHelper.IfNullOrWhitespace(null, "項目"));
        Probe("polyfill blank null message", () => ThrowHelper.IfNullOrWhitespace(" ", null));
        Probe("polyfill blank empty message", () => ThrowHelper.IfNullOrWhitespace(" ", ""));
        Probe("polyfill blank unicode message", () => ThrowHelper.IfNullOrWhitespace(" ", "項目"));
        Probe("polyfill null NUL name", () => ThrowHelper.IfNullOrWhitespace(null, "a\0b"));
        Probe("polyfill null surrogate name", () => ThrowHelper.IfNullOrWhitespace(null, "a\ud800b"));
        Probe("polyfill blank NUL message", () => ThrowHelper.IfNullOrWhitespace(" ", "a\0b"));
        Probe("polyfill blank surrogate message", () => ThrowHelper.IfNullOrWhitespace(" ", "a\ud800b"));
        Console.WriteLine("polyfill IfNullOrWhitespace value: " + ThrowHelper.IfNullOrWhitespace("x", "p"));
    }
    internal static void RunArrayShapes()
    {
        Console.WriteLine("-- runtime Array argument fields --");
        Probe("array-createinstance-length2", () => Array.CreateInstance(typeof(int), 1, -1));
        Probe("array-createinstance-lengths-element", () => Array.CreateInstance(typeof(int), new int[] { 1, -1 }));
        Probe("array-fromarraytype-not-array", () => Array.CreateInstanceFromArrayType(typeof(int), 1));
        Probe("array-getvalue-huge-index", () => ((Array)new int[2, 2]).GetValue(5000000000L));
        Probe("array-getvalue-null-indices", () => ((Array)new int[3]).GetValue((int[])null));
        Console.WriteLine("runtime Array argument fields end");
    }

}
