#nullable disable
using System;

namespace StringFormatThrowSubset
{
    // string.Format faults are catchable: a malformed format string is bad data, so
    // .NET raises FormatException (ArgumentNullException for a null format or argument
    // array) and the caller carries on. Type name only — messages are localized.
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
            Catches("Format(null, arg)", () => { string r = string.Format((string)null, (object)1); });
            Catches("Format(fmt, (object[])null)", () => { string r = string.Format("{0}", (object[])null); });

            Catches("Format(\"{0\")", () => { string r = string.Format("{0", (object)1); });
            Catches("Format(\"{1}\")", () => { string r = string.Format("{1}", (object)1); });
            Catches("Format(\"{}\")", () => { string r = string.Format("{}", (object)1); });
            Catches("Format(\"a}b\")", () => { string r = string.Format("a}b", (object)1); });
            Catches("Format(\"{0:X\")", () => { string r = string.Format("{0:X", (object)1); });

            // Recovery is real: a well-formed call after the faults still works.
            Console.WriteLine("after faults: " + string.Format("{0}-{1}", 7, "ok"));

            try
            {
                string r = string.Format("{5}", (object)1);
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
                string r = string.Format((string)null, (object)1);
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
        }
    }
}
