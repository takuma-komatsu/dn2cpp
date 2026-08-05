using System;
using System.Globalization;

namespace DefaultCulture
{
    // The DEFAULT CultureInfo.CurrentCulture — i.e. the one a program that never
    // names a culture formats through. This sample deliberately carries
    // NO invariant pin: the pin is what every other bucket driver uses to keep its
    // verdict off the developer's locale, and this is the one program whose
    // subject matter IS the developer's locale.
    //
    // It is asserted as a subject-vs-oracle DIFF under a forced locale, never
    // against a snapshot: a snapshot would freeze one host's answer, and the claim
    // under test is not "the default is ja-JP", it is "the default is whatever real
    // .NET says it is". Only the numeric half is printed — per-culture DATE
    // formatting is a standing carve-out (docs/STATUS.md), so a date here would
    // assert a divergence dn2cpp does not claim to have closed.
    internal static class Program
    {
        private static void Main()
        {
            CultureInfo c = CultureInfo.CurrentCulture;
            Console.WriteLine("Name=" + c.Name);
            NumberFormatInfo nf = c.NumberFormat;
            Console.WriteLine("dec=" + nf.NumberDecimalSeparator);
            Console.WriteLine("grp=" + nf.NumberGroupSeparator);
            Console.WriteLine("neg=" + nf.NegativeSign);
            Console.WriteLine("posinf=" + nf.PositiveInfinitySymbol);
            Console.WriteLine("neginf=" + nf.NegativeInfinitySymbol);
            Console.WriteLine("nan=" + nf.NaNSymbol);
            Console.WriteLine("cur=" + nf.CurrencySymbol);
            Console.WriteLine("curdigits=" + nf.CurrencyDecimalDigits);

            // Provider-less formatting: the whole point of the default culture.
            Console.WriteLine("d=" + (1234567.891).ToString());
            Console.WriteLine("n=" + (1234567.891).ToString("N3"));
            Console.WriteLine("p=" + (0.1234).ToString("P2"));
            Console.WriteLine("c=" + (1234.5).ToString("C"));
            Console.WriteLine("inf=" + double.PositiveInfinity);
            Console.WriteLine("i=" + (-1234567).ToString("N0"));
            Console.WriteLine("interp=" + $"{1234.5:N2}|{-0.5:P1}");
        }
    }
}
