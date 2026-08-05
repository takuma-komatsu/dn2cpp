#nullable disable
using System;

namespace StringNullFaultSubset
{
    // The String intrinsics' null-receiver faults, which must be catchable NREs rather
    // than aborts. Mixed sites are probed from both ends: a null receiver is an NRE, a
    // null ARGUMENT on a live receiver an ArgumentNullException. The overloads whose
    // missing bound is `Length - startIndex` are probed directly as well as through
    // their explicit-bound siblings, since computing that bound at the call site would
    // dereference the null receiver before the intrinsic's own check.
    internal static class Program
    {
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
            string ns = null;

            // -- the slice/pad/remove/insert family --
            Catches("null.Substring(1, 1)", () => { var r = ns.Substring(1, 1); });
            Catches("null.Substring(1)", () => { var r = ns.Substring(1); });
            Catches("null.PadLeft(5)", () => { var r = ns.PadLeft(5); });
            Catches("null.PadRight(3, '.')", () => { var r = ns.PadRight(3, '.'); });
            Catches("null.Remove(1, 1)", () => { var r = ns.Remove(1, 1); });
            Catches("null.Remove(1)", () => { var r = ns.Remove(1); });
            Catches("null.Insert(0, \"x\")", () => { var r = ns.Insert(0, "x"); });

            // -- the search family, char and string, plain and ranged --
            Catches("null.IndexOf('a')", () => { var r = ns.IndexOf('a'); });
            Catches("null.IndexOf('a', 0)", () => { var r = ns.IndexOf('a', 0); });
            Catches("null.IndexOf('a', 0, 0)", () => { var r = ns.IndexOf('a', 0, 0); });
            Catches("null.IndexOf(\"a\")", () => { var r = ns.IndexOf("a"); });
            Catches("null.IndexOf(\"a\", 0)", () => { var r = ns.IndexOf("a", 0); });
            Catches("null.LastIndexOf('a')", () => { var r = ns.LastIndexOf('a'); });
            Catches("null.LastIndexOf(\"a\", 0, 0)", () => { var r = ns.LastIndexOf("a", 0, 0); });
            Catches("null.IndexOfAny(['a'])", () => { var r = ns.IndexOfAny(new[] { 'a' }); });
            Catches("null.IndexOfAny(['a'], 0)", () => { var r = ns.IndexOfAny(new[] { 'a' }, 0); });
            Catches("null.IndexOfAny(['a'], 0, 0)", () => { var r = ns.IndexOfAny(new[] { 'a' }, 0, 0); });
            Catches("null.LastIndexOfAny(['a'])", () => { var r = ns.LastIndexOfAny(new[] { 'a' }); });
            Catches("null.LastIndexOfAny(['a'], 0, 0)", () => { var r = ns.LastIndexOfAny(new[] { 'a' }, 0, 0); });

            // -- the comparison-taking search family --
            Catches("null.StartsWith(\"a\")", () => { var r = ns.StartsWith("a"); });
            Catches("null.EndsWith(\"a\")", () => { var r = ns.EndsWith("a"); });
            Catches("null.StartsWith('a')", () => { var r = ns.StartsWith('a'); });
            Catches("null.EndsWith('a')", () => { var r = ns.EndsWith('a'); });
            Catches("null.StartsWith(\"a\", Ordinal)", () => { var r = ns.StartsWith("a", StringComparison.Ordinal); });
            Catches("null.EndsWith(\"a\", Ordinal)", () => { var r = ns.EndsWith("a", StringComparison.Ordinal); });
            Catches("null.Contains(\"a\", Ordinal)", () => { var r = ns.Contains("a", StringComparison.Ordinal); });
            Catches("null.IndexOf(\"a\", Ordinal)", () => { var r = ns.IndexOf("a", StringComparison.Ordinal); });
            Catches("null.IndexOf(\"a\", 0, Ordinal)", () => { var r = ns.IndexOf("a", 0, StringComparison.Ordinal); });
            Catches("null.IndexOf('a', Ordinal)", () => { var r = ns.IndexOf('a', StringComparison.Ordinal); });
            Catches("null.LastIndexOf(\"a\", Ordinal)", () => { var r = ns.LastIndexOf("a", StringComparison.Ordinal); });
            Catches("null.LastIndexOf('a', 0, 0)", () => { var r = ns.LastIndexOf('a', 0, 0); });

            // -- the argument half of the mixed checks: a live receiver, a null argument.
            //    The two operands of one `s == null || arg == null` check take different
            //    exception types. --
            string live = "abc";
            Catches("\"abc\".IndexOf((string)null)", () => { var r = live.IndexOf((string)null); });
            Catches("\"abc\".StartsWith((string)null)", () => { var r = live.StartsWith((string)null); });
            Catches("\"abc\".EndsWith((string)null)", () => { var r = live.EndsWith((string)null); });
            Catches("\"abc\".StartsWith(null, Ordinal)", () => { var r = live.StartsWith(null, StringComparison.Ordinal); });
            Catches("\"abc\".EndsWith(null, Ordinal)", () => { var r = live.EndsWith(null, StringComparison.Ordinal); });
            Catches("\"abc\".Contains(null, Ordinal)", () => { var r = live.Contains((string)null, StringComparison.Ordinal); });
            Catches("\"abc\".IndexOf(null, Ordinal)", () => { var r = live.IndexOf((string)null, StringComparison.Ordinal); });

            // -- casing / replace / normalize --
            Catches("null.ToUpperInvariant()", () => { var r = ns.ToUpperInvariant(); });
            Catches("null.ToLowerInvariant()", () => { var r = ns.ToLowerInvariant(); });
            Catches("null.Replace('a', 'b')", () => { var r = ns.Replace('a', 'b'); });
            Catches("null.Replace(\"a\", \"b\")", () => { var r = ns.Replace("a", "b"); });
            Catches("null.Replace(\"a\", \"b\", Ordinal)", () => { var r = ns.Replace("a", "b", StringComparison.Ordinal); });
            Catches("null.Normalize()", () => { var r = ns.Normalize(); });
            Catches("null.IsNormalized()", () => { var r = ns.IsNormalized(); });

            // -- chars out: indexer, ToCharArray, CopyTo, Trim, Split --
            Catches("null[0]", () => { var r = ns[0]; });
            Catches("null.ToCharArray()", () => { var r = ns.ToCharArray(); });
            Catches("null.ToCharArray(0, 0)", () => { var r = ns.ToCharArray(0, 0); });
            Catches("null.CopyTo(0, chars, 0, 0)", () => ns.CopyTo(0, new char[1], 0, 0));
            Catches("null.Trim()", () => { var r = ns.Trim(); });
            Catches("null.Trim('x')", () => { var r = ns.Trim('x'); });
            Catches("null.Split(',')", () => { var r = ns.Split(','); });
            Catches("null.Split(\",\")", () => { var r = ns.Split(","); });

            // The recovery is real: string work continues after the faults.
            Console.WriteLine("after null faults: [" + live.Substring(1) + "]");

            // A typed catch selects the NRE over a broader handler.
            try
            {
                var r = ns.Substring(1, 1);
                Console.WriteLine("unreachable " + r);
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
                    var r = ns.Trim();
                    Console.WriteLine("unreachable " + r);
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
