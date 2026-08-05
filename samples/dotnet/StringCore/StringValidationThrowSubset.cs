#nullable disable
using System;

namespace StringValidationThrowSubset
{
    // The String argument-validation faults, observed from a catch handler. The point
    // is that execution continues afterwards: a runtime that aborted the process here
    // would print nothing past the first case. Type name only — the messages are
    // localized and carry parameter names that are not part of the contract.
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
            string s = "abc";

            Catches("Substring(10)", () => { string r = s.Substring(10); });
            Catches("Substring(-1)", () => { string r = s.Substring(-1); });
            Catches("Substring(1, 5)", () => { string r = s.Substring(1, 5); });
            Catches("Substring(1, -1)", () => { string r = s.Substring(1, -1); });

            Catches("PadLeft(-1)", () => { string r = s.PadLeft(-1); });
            Catches("PadRight(-1, '.')", () => { string r = s.PadRight(-1, '.'); });

            Catches("Remove(5)", () => { string r = s.Remove(5); });
            Catches("Remove(-1)", () => { string r = s.Remove(-1); });
            Catches("Remove(1, 9)", () => { string r = s.Remove(1, 9); });

            Catches("Insert(-1, \"x\")", () => { string r = s.Insert(-1, "x"); });
            Catches("Insert(9, \"x\")", () => { string r = s.Insert(9, "x"); });
            Catches("Insert(0, null)", () => { string r = s.Insert(0, null); });

            Catches("string.Intern(null)", () => { string r = string.Intern(null); });
            Catches("string.IsInterned(null)", () => { string r = string.IsInterned(null); });

            // The indexer is the odd one out: every row above is an
            // ArgumentOutOfRangeException, while `s[i]` out of range is an
            // IndexOutOfRangeException — for a negative index as much as a
            // past-the-end one. The value is consumed so the read is not dropped as a
            // dead store.
            Catches("s[5]", () => { char c = s[5]; Console.Write("[" + c + "] "); });
            Catches("s[3] (== Length)", () => { char c = s[3]; Console.Write("[" + c + "] "); });
            Catches("s[-1]", () => { char c = s[-1]; Console.Write("[" + c + "] "); });
            Catches("\"\"[0]", () => { char c = string.Empty[0]; Console.Write("[" + c + "] "); });

            // A scanner that walks off the end recovers rather than ending the process.
            int scanned = 0, over = 0;
            for (int i = 0; i < 6; i++)
            {
                try
                {
                    if (s[i] != '\0') scanned++;
                }
                catch (IndexOutOfRangeException)
                {
                    over++;
                }
            }
            Console.WriteLine("scan: scanned=" + scanned + " over=" + over);

            // The recovery is real: the same receiver still works afterwards.
            Console.WriteLine("after faults: [" + s.Substring(1) + "]");

            // A caught fault is an ordinary exception object: a typed catch selects it
            // over a broader one.
            try
            {
                string r = s.Substring(10);
                Console.WriteLine("unreachable " + r);
            }
            catch (ArgumentOutOfRangeException)
            {
                Console.WriteLine("typed catch: ArgumentOutOfRangeException");
            }
            catch (Exception)
            {
                Console.WriteLine("typed catch: fell through to Exception");
            }

            try
            {
                string r = s.Insert(0, null);
                Console.WriteLine("unreachable " + r);
            }
            catch (ArgumentNullException)
            {
                Console.WriteLine("typed catch: ArgumentNullException");
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
                    string r = s.Substring(99);
                    Console.WriteLine("unreachable " + r);
                }
                finally
                {
                    ran++;
                }
            }
            catch (ArgumentOutOfRangeException)
            {
                Console.WriteLine("finally ran " + ran + " time(s) before the catch");
            }
        }
    }
}
