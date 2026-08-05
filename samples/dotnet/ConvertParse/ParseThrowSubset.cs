#nullable disable
using System;
using System.Globalization;

namespace ParseThrowSubset
{
    // The Parse / Convert faults, observed from a catch handler. Parsing
    // untrusted text is the canonical recoverable failure — real .NET raises
    // FormatException for malformed input, OverflowException for input that is
    // well-formed but outside the target's range, and ArgumentNullException for
    // a null string — so `try { int.Parse(line) } catch (FormatException)` is a
    // normal shape and must not end the process. Type name only; the messages
    // are localized.
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
            Catches("int.Parse(\"abc\")", () => { int r = int.Parse("abc"); });
            Catches("int.Parse(\"\")", () => { int r = int.Parse(""); });
            Catches("int.Parse(\"1.5\")", () => { int r = int.Parse("1.5"); });
            Catches("int.Parse(\"2147483648\")", () => { int r = int.Parse("2147483648"); });
            Catches("int.Parse(\"-2147483649\")", () => { int r = int.Parse("-2147483649"); });

            Catches("long.Parse(\"abc\")", () => { long r = long.Parse("abc"); });
            Catches("long.Parse(\"\")", () => { long r = long.Parse(""); });

            Catches("double.Parse(\"abc\")",
                () => { double r = double.Parse("abc", CultureInfo.InvariantCulture); });
            Catches("float.Parse(\"abc\")",
                () => { float r = float.Parse("abc", CultureInfo.InvariantCulture); });

            Catches("Convert.ToChar((string)null)", () => { char r = Convert.ToChar((string)null); });
            Catches("Convert.ToChar(\"ab\")", () => { char r = Convert.ToChar("ab"); });
            Catches("Convert.ToChar(\"\")", () => { char r = Convert.ToChar(""); });
            Catches("Convert.ToChar(70000)", () => { char r = Convert.ToChar(70000); });
            Catches("Convert.ToChar(-1)", () => { char r = Convert.ToChar(-1); });

            Catches("Convert.ToBoolean(\"maybe\")", () => { bool r = Convert.ToBoolean("maybe"); });

            Catches("Convert.ToInt32(3e9)", () => { int r = Convert.ToInt32(3000000000.0); });
            Catches("Convert.ToInt64(1e30)", () => { long r = Convert.ToInt64(1e30); });
            Catches("Convert.ToInt32(long.MaxValue)", () => { int r = Convert.ToInt32(long.MaxValue); });

            Catches("Convert.ToString(255, 3)", () => { string r = Convert.ToString(255, 3); });
            Catches("Convert.ToString(255L, 3)", () => { string r = Convert.ToString(255L, 3); });

            Catches("Convert.ToHexString(null)", () => { string r = Convert.ToHexString((byte[])null); });
            Catches("Convert.FromHexString(\"abc\")", () => { byte[] r = Convert.FromHexString("abc"); });
            Catches("Convert.FromHexString(\"zz\")", () => { byte[] r = Convert.FromHexString("zz"); });

            // Recovery is real: parsing keeps working after the faults, and the
            // Try* forms still answer false rather than throwing.
            Console.WriteLine("after faults: " + int.Parse("42"));
            int ok;
            Console.WriteLine("TryParse(\"abc\") -> " + int.TryParse("abc", out ok) + " " + ok);

            try
            {
                int r = int.Parse("nope");
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
                int r = int.Parse("9999999999");
                Console.WriteLine("unreachable " + r);
            }
            catch (OverflowException)
            {
                Console.WriteLine("typed catch: OverflowException");
            }
            catch (Exception)
            {
                Console.WriteLine("typed catch: fell through to Exception");
            }

            // A parse loop over mixed input: the shape this whole section
            // exists for — one bad row must not take the program down.
            string[] rows = { "1", "x", "3", "2147483648", "5" };
            int sum = 0, bad = 0;
            foreach (string row in rows)
            {
                try
                {
                    sum += int.Parse(row);
                }
                catch (FormatException)
                {
                    bad++;
                }
                catch (OverflowException)
                {
                    bad++;
                }
            }
            Console.WriteLine("parse loop: sum=" + sum + " bad=" + bad);
        }
    }
}
