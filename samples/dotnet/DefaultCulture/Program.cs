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
        private static void Main(string[] args)
        {
            if (args.Length > 0 && args[0] == "culture-variant-order")
            {
                WriteVariantIdentities(CultureInfo.CurrentCulture, CultureInfo.CurrentUICulture, CultureInfo.InstalledUICulture);
                return;
            }
            if (args.Length > 0 && args[0] == "culture-fallback")
            {
                RunFallbackData(args.Length > 1 ? args[1] : null);
                return;
            }

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
            Console.WriteLine("== default culture identities ==");
            Console.WriteLine("ui=" + CultureInfo.CurrentUICulture.Name);
            Console.WriteLine("installed=" + CultureInfo.InstalledUICulture.Name);
            Console.WriteLine("default culture identities end");
            CultureInfo originalUI = CultureInfo.CurrentUICulture;
            CultureInfo originalInstalled = CultureInfo.InstalledUICulture;
            RunFallbackData();
            WriteVariantIdentities(c, originalUI, originalInstalled);
        }

        private static void WriteVariantIdentities(CultureInfo current, CultureInfo ui, CultureInfo installed)
        {
            Console.WriteLine("== default culture variant identities ==");
            Console.WriteLine("  current=" + VariantIdentity(current));
            Console.WriteLine("  ui=" + VariantIdentity(ui));
            Console.WriteLine("  installed=" + VariantIdentity(installed));
            Console.WriteLine("culture variant order end");
        }

        private static string VariantIdentity(CultureInfo culture) => "'" + culture.Name
            + "' value='" + culture.ToString() + "' LCID=" + culture.LCID + " neutral=" + culture.IsNeutralCulture;

        private static void RunFallbackData(string name = null)
        {
            // ICU's fallback language is independent of the managed culture slots.
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

            Console.WriteLine("== culture host fallback data ==");
            foreach (string input in name is null ? new[]
            {
                "und-US", "de-FOO-u-nu-finance", "i-klingon", "i-default", "zh-min-u-nu-foo",
                "en-US", "de-DE", "ja-JP", "i-navajo-u-nu-native", "zh-min\0ignored",
                "de-foo-bar-US-u-nu-latn", "foo-QQ", "i-default-US-u-nu-arab",
                "en-US-POSIX", "en-US-POSIX-u-nu-arab"
            } : new[] { name })
            {
                CultureInfo culture = new CultureInfo(input);
                NumberFormatInfo n = culture.NumberFormat;
                Console.WriteLine("  '" + input.Replace("\0", "<NUL>") + "' -> '" + culture.Name + "' value='" + culture.ToString()
                    + "' LCID=" + culture.LCID + " neutral=" + culture.IsNeutralCulture);
                Console.WriteLine("    specific='" + CultureInfo.CreateSpecificCulture(input).Name
                    + "' cached='" + CultureInfo.GetCultureInfo(input).Name
                    + "' without override='" + new CultureInfo(input, false).Name + "'");
                Console.WriteLine("    separators=" + n.NumberDecimalSeparator + "|" + n.NumberGroupSeparator
                    + "|" + n.CurrencyDecimalSeparator + "|" + n.CurrencyGroupSeparator
                    + "|" + n.PercentDecimalSeparator + "|" + n.PercentGroupSeparator);
                Console.WriteLine("    symbols=" + n.NegativeSign + "|" + n.PositiveSign + "|" + n.NaNSymbol
                    + "|" + n.PositiveInfinitySymbol + "|" + n.NegativeInfinitySymbol
                    + "|" + n.CurrencySymbol + "|" + n.PercentSymbol + "|" + n.PerMilleSymbol);
                Console.WriteLine("    groups=" + Sizes(n.NumberGroupSizes) + "|" + Sizes(n.CurrencyGroupSizes)
                    + "|" + Sizes(n.PercentGroupSizes));
                Console.WriteLine("    digits=" + n.NumberDecimalDigits + "," + n.CurrencyDecimalDigits + "," + n.PercentDecimalDigits
                    + " patterns=" + n.NumberNegativePattern + "," + n.CurrencyPositivePattern + "," + n.CurrencyNegativePattern
                    + "," + n.PercentPositivePattern + "," + n.PercentNegativePattern);
                foreach (string format in new[] { "N", "N2", "C", "C2", "P", "P2", "E2", "0.00‰" })
                {
                    Console.WriteLine("    " + format + " double=" + FormatFallback(1234567.89, format, culture)
                        + " decimal=" + FormatFallback(-1234567.89m, format, culture));
                }
                Console.WriteLine("    special=" + double.NaN.ToString(culture)
                    + "|" + double.PositiveInfinity.ToString(culture)
                    + "|" + double.NegativeInfinity.ToString(culture));
            }
            Console.WriteLine("culture host fallback data end");
        }

        private static string FormatFallback(double value, string format, CultureInfo culture)
        {
            try
            {
                return value.ToString(format, culture);
            }
            catch (ArgumentOutOfRangeException)
            {
                return nameof(ArgumentOutOfRangeException);
            }
        }

        private static string FormatFallback(decimal value, string format, CultureInfo culture)
        {
            try
            {
                return value.ToString(format, culture);
            }
            catch (ArgumentOutOfRangeException)
            {
                return nameof(ArgumentOutOfRangeException);
            }
        }

        private static string Sizes(int[] sizes)
        {
            string value = "[";
            for (int i = 0; i < sizes.Length; i++)
                value += (i > 0 ? "," : "") + sizes[i];
            return value + "]";
        }
    }
}
