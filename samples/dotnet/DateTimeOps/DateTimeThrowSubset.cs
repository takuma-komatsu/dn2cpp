#nullable disable
using System;
using System.Globalization;

namespace DateTimeThrowSubset
{
    // The DateTime-family validation faults, observed from a catch handler.
    // Parsing and formatting a date from untrusted text is the canonical
    // recoverable failure — real .NET raises FormatException for a malformed
    // string and for an unrecognized format specifier, and ArgumentNullException
    // for a null string or a null format — so
    // `try { DateTime.Parse(line) } catch (FormatException)` is a normal shape
    // and must not end the process. Type name only; the messages are localized.
    //
    // The two are told apart deliberately: the runtime's *_try_parse helpers
    // report only that they failed, so a conversion that routed every failure to
    // FormatException would hand a caller catching ArgumentNullException the
    // wrong type. Both ends of each Parse/ParseExact pair are probed here.
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

        internal static void __GateEntry()
        {
            CultureInfo ci = CultureInfo.InvariantCulture;

            // ---- bad format specifier: FormatException ----
            Catches("DateTime.ToString(\"Q\")",
                () => { string r = new DateTime(2020, 1, 2, 3, 4, 5).ToString("Q", ci); });
            Catches("DateTimeOffset.ToString(\"Q\")",
                () => { string r = new DateTimeOffset(2020, 1, 2, 3, 4, 5, TimeSpan.Zero).ToString("Q", ci); });
            Catches("TimeSpan.ToString(\"Q\")",
                () => { string r = TimeSpan.FromHours(1).ToString("Q", ci); });
            Catches("DateOnly.ToString(\"Q\")",
                () => { string r = new DateOnly(2020, 1, 2).ToString("Q", ci); });
            Catches("TimeOnly.ToString(\"Q\")",
                () => { string r = new TimeOnly(3, 4).ToString("Q", ci); });

            // ---- malformed input: FormatException ----
            Catches("DateTime.Parse(\"nope\")", () => { DateTime r = DateTime.Parse("nope", ci); });
            Catches("DateTime.ParseExact(\"zz\")",
                () => { DateTime r = DateTime.ParseExact("zz", "yyyy-MM-dd", ci); });
            Catches("DateTimeOffset.Parse(\"nope\")",
                () => { DateTimeOffset r = DateTimeOffset.Parse("nope", ci); });
            Catches("DateTimeOffset.ParseExact(\"zz\")",
                () => { DateTimeOffset r = DateTimeOffset.ParseExact("zz", "yyyy-MM-dd", ci); });
            Catches("TimeSpan.Parse(\"nope\")", () => { TimeSpan r = TimeSpan.Parse("nope", ci); });
            Catches("TimeSpan.ParseExact(\"zz\")", () => { TimeSpan r = TimeSpan.ParseExact("zz", "c", ci); });
            Catches("TimeOnly.Parse(\"nope\")", () => { TimeOnly r = TimeOnly.Parse("nope", ci); });
            Catches("TimeOnly.ParseExact(\"zz\")", () => { TimeOnly r = TimeOnly.ParseExact("zz", "HH:mm"); });

            // ---- null input / null format: ArgumentNullException ----
            Catches("DateTime.Parse(null)", () => { DateTime r = DateTime.Parse((string)null, ci); });
            Catches("DateTime.ParseExact(null, fmt)",
                () => { DateTime r = DateTime.ParseExact((string)null, "yyyy-MM-dd", ci); });
            Catches("DateTime.ParseExact(s, null)",
                () => { DateTime r = DateTime.ParseExact("2020-01-02", (string)null, ci); });
            Catches("DateTimeOffset.Parse(null)",
                () => { DateTimeOffset r = DateTimeOffset.Parse((string)null, ci); });
            Catches("DateTimeOffset.ParseExact(null, fmt)",
                () => { DateTimeOffset r = DateTimeOffset.ParseExact((string)null, "yyyy-MM-dd", ci); });
            Catches("DateTimeOffset.ParseExact(s, null)",
                () => { DateTimeOffset r = DateTimeOffset.ParseExact("2020-01-02", (string)null, ci); });
            Catches("TimeSpan.Parse(null)", () => { TimeSpan r = TimeSpan.Parse((string)null, ci); });
            Catches("TimeSpan.ParseExact(null, fmt)",
                () => { TimeSpan r = TimeSpan.ParseExact((string)null, "c", ci); });
            Catches("TimeSpan.ParseExact(s, null)",
                () => { TimeSpan r = TimeSpan.ParseExact("01:00:00", (string)null, ci); });
            Catches("TimeOnly.Parse(null)", () => { TimeOnly r = TimeOnly.Parse((string)null, ci); });
            Catches("TimeOnly.ParseExact(null, fmt)",
                () => { TimeOnly r = TimeOnly.ParseExact((string)null, "HH:mm"); });
            Catches("TimeOnly.ParseExact(s, null)",
                () => { TimeOnly r = TimeOnly.ParseExact("03:04", (string)null); });

            // A typed catch selects the fault over a broader one.
            try
            {
                DateTime r = DateTime.Parse("nope", ci);
                Console.WriteLine("unreachable " + r);
            }
            catch (FormatException)
            {
                Console.WriteLine("typed catch: FormatException");
            }
            catch (Exception)
            {
                Console.WriteLine("typed catch: fell through to Exception");
            }

            try
            {
                DateTime r = DateTime.Parse((string)null, ci);
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

            // A finally on the unwind path runs.
            try
            {
                try
                {
                    TimeSpan r = TimeSpan.Parse("nope", ci);
                    Console.WriteLine("unreachable " + r);
                }
                finally
                {
                    Console.WriteLine("finally ran");
                }
            }
            catch (FormatException)
            {
                Console.WriteLine("caught after finally");
            }

            // Recovery is real: the surface keeps working after the faults, and
            // the Try* forms still answer false rather than throwing.
            Console.WriteLine("after faults: " + DateTime.Parse("2020-01-02", ci).ToString("yyyy-MM-dd", ci));
            DateTime ok;
            Console.WriteLine("TryParse(\"nope\") -> " + DateTime.TryParse("nope", ci, DateTimeStyles.None, out ok));
            TimeSpan okts;
            Console.WriteLine("TimeSpan.TryParse(\"nope\") -> " + TimeSpan.TryParse("nope", ci, out okts));

            // A parse loop over mixed input: the shape this whole section exists
            // for — one bad row must not take the program down.
            string[] rows = { "2020-01-01", "x", "2020-01-03", null, "2020-01-05" };
            int days = 0, bad = 0;
            foreach (string row in rows)
            {
                try
                {
                    days += DateTime.Parse(row, ci).Day;
                }
                catch (FormatException)
                {
                    bad++;
                }
                catch (ArgumentNullException)
                {
                    bad++;
                }
            }
            Console.WriteLine("parse loop: days=" + days + " bad=" + bad);
        }
    }
}
