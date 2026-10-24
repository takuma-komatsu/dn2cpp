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
    // The default-culture run is asserted against real .NET under a forced locale;
    // the unknown-region currency has its own frozen mode because ICU revises
    // that symbol. Only the numeric half is printed — per-culture DATE
    // formatting is a standing carve-out (docs/STATUS.md), so a date here would
    // assert a divergence dn2cpp does not claim to have closed.
    internal static class Program
    {
        private static void Main(string[] args)
        {
            if (args.Length > 0 && args[0] == "culture-user-numbers")
            {
                CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
                CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
                WriteUserNumbers(args[1], int.Parse(args[2], CultureInfo.InvariantCulture), args[3]);
                return;
            }
            if (args.Length > 0 && args[0] == "culture-user-lcid")
            {
                CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
                CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
                Console.WriteLine(new CultureInfo(args[1], false).LCID);
                return;
            }
            if (args.Length > 0 && args[0] == "culture-zz-currency")
            {
                CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
                CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
                Console.WriteLine("== frozen unknown-region currency ==");
                Console.WriteLine("foo-ZZ=" + new CultureInfo("foo-ZZ", false).NumberFormat.CurrencySymbol);
                Console.WriteLine("zz-ZZ=" + new CultureInfo("zz-ZZ", false).NumberFormat.CurrencySymbol);
                Console.WriteLine("frozen unknown-region currency end");
                return;
            }
            if (args.Length > 0 && args[0] == "culture-extension-identities")
            {
                WriteExtensionIdentities(CultureInfo.CurrentCulture, CultureInfo.CurrentUICulture, CultureInfo.InstalledUICulture);
                return;
            }
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
            WriteExtensionIdentities(c, originalUI, originalInstalled);
            WriteUserNumberDigits(c);
            if (args.Length > 0 && args[0] == "before-user-numbers")
                return;
            if (OperatingSystem.IsWindows())
                WriteUserNumbers(c.Name, c.LCID, "name-true");
        }

        private static void WriteUserNumbers(string name, int lcid, string first)
        {
            Console.WriteLine("== Windows user numeric settings ==");
            Console.WriteLine("  name=" + name + " LCID=" + lcid + " first=" + first);
            WriteUserNumberCase("first", name, lcid, first);
            foreach (string kind in new[] { "name-true", "name-false", "lcid-true", "lcid-false", "cached-name", "cached-lcid" })
                WriteUserNumberCase(kind, name, lcid, kind);
            CultureInfo mutable = new CultureInfo(name, true);
            mutable.NumberFormat.CurrencySymbol = "changed";
            mutable.NumberFormat.NumberDecimalSeparator = "!";
            foreach (string kind in new[] { "name-true", "name-false", "lcid-true", "lcid-false", "cached-name", "cached-lcid" })
                WriteUserNumberCase("after mutation " + kind, name, lcid, kind);
            Console.WriteLine("Windows user numeric settings end");
        }

        private static void WriteUserNumberCase(string label, string name, int lcid, string kind)
        {
            try
            {
                WriteUserNumberProfile(label, UserNumberCulture(name, lcid, kind));
            }
            catch (CultureNotFoundException)
            {
                Console.WriteLine("  " + label + "=" + nameof(CultureNotFoundException));
            }
        }

        private static CultureInfo UserNumberCulture(string name, int lcid, string kind)
        {
            if (kind == "name-true")
                return new CultureInfo(name, true);
            if (kind == "name-false")
                return new CultureInfo(name, false);
            if (kind == "lcid-true")
                return new CultureInfo(lcid, true);
            if (kind == "lcid-false")
                return new CultureInfo(lcid, false);
            if (kind == "cached-name")
                return CultureInfo.GetCultureInfo(name);
            return CultureInfo.GetCultureInfo(lcid);
        }

        private static void WriteUserNumberProfile(string label, CultureInfo culture)
        {
            NumberFormatInfo n = culture.NumberFormat;
            Console.WriteLine("  " + label + " name=" + culture.Name + " readonly=" + culture.IsReadOnly);
            Console.WriteLine("    signs=" + n.PositiveSign + "|" + n.NegativeSign);
            Console.WriteLine("    number=" + n.NumberDecimalSeparator + "|" + n.NumberGroupSeparator
                + "|" + n.NumberDecimalDigits + "|" + n.NumberNegativePattern + "|" + Sizes(n.NumberGroupSizes));
            Console.WriteLine("    currency=" + n.CurrencySymbol + "|" + n.CurrencyDecimalSeparator
                + "|" + n.CurrencyGroupSeparator + "|" + n.CurrencyDecimalDigits + "|" + n.CurrencyPositivePattern
                + "|" + n.CurrencyNegativePattern + "|" + Sizes(n.CurrencyGroupSizes));
            Console.WriteLine("    percent=" + n.PercentSymbol + "|" + n.PerMilleSymbol + "|" + n.PercentDecimalSeparator
                + "|" + n.PercentGroupSeparator + "|" + n.PercentDecimalDigits + "|" + n.PercentPositivePattern
                + "|" + n.PercentNegativePattern + "|" + Sizes(n.PercentGroupSizes));
            Console.WriteLine("    special=" + n.NaNSymbol + "|" + n.PositiveInfinitySymbol + "|" + n.NegativeInfinitySymbol);
            Console.WriteLine("    formatted=" + (1234567.89).ToString("N", culture)
                + "|" + (-1234567.89).ToString("N", culture) + "|" + (1234567.89).ToString("C", culture)
                + "|" + (-1234567.89).ToString("C", culture) + "|" + (12345.6789).ToString("P", culture)
                + "|" + (-12345.6789).ToString("P", culture));
        }

        private static void WriteUserNumberDigits(CultureInfo original)
        {
            Console.WriteLine("== default culture user number digits ==");
            string name = original.Name;
            int lcid = original.LCID;
            Console.WriteLine("  constructor=" + NumberDigits(() => new CultureInfo(name)));
            Console.WriteLine("  without override=" + NumberDigits(() => new CultureInfo(name, false)));
            Console.WriteLine("  cached name=" + NumberDigits(() => CultureInfo.GetCultureInfo(name)));
            Console.WriteLine("  integer constructor=" + NumberDigits(() => new CultureInfo(lcid)));
            Console.WriteLine("  integer without override=" + NumberDigits(() => new CultureInfo(lcid, false)));
            Console.WriteLine("  cached integer=" + NumberDigits(() => CultureInfo.GetCultureInfo(lcid)));
            Console.WriteLine("  constructor again=" + NumberDigits(() => new CultureInfo(name)));
            Console.WriteLine("  without override again=" + NumberDigits(() => new CultureInfo(name, false)));
            foreach (string suffix in new[]
            {
                "-a-foo", "-x-foo", "-u-a", "-t-a", "-x-lvariant",
                "-u-ca-gregory", "-U-NU-LATN", "-u-nu-latn", "-u-co-phoneb"
            })
            {
                string extended = name + suffix;
                Console.WriteLine("  extended '" + extended + "'=" + NumberDigits(() => new CultureInfo(extended))
                    + " without override=" + NumberDigits(() => new CultureInfo(extended, false)));
            }
            Console.WriteLine("default culture user number digits end");
        }

        private static string NumberDigits(Func<CultureInfo> create)
        {
            try
            {
                CultureInfo culture = create();
                NumberFormatInfo number = culture.NumberFormat;
                return number.NumberDecimalDigits + "," + number.PercentDecimalDigits
                    + " N=" + (1234.5).ToString("N", culture) + " P=" + (0.125).ToString("P", culture);
            }
            catch (CultureNotFoundException)
            {
                return nameof(CultureNotFoundException);
            }
        }

        private static void WriteExtensionIdentities(CultureInfo current, CultureInfo ui, CultureInfo installed)
        {
            Console.WriteLine("== default culture extension identities ==");
            Console.WriteLine("  current=" + VariantIdentity(current));
            Console.WriteLine("  ui=" + VariantIdentity(ui));
            Console.WriteLine("  installed=" + VariantIdentity(installed));
            Console.WriteLine("culture extension identities end");
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
                CultureInfo culture;
                try
                {
                    culture = new CultureInfo(input);
                }
                catch (CultureNotFoundException exception)
                {
                    Console.WriteLine("  '" + input.Replace("\0", "<NUL>") + "': " + nameof(CultureNotFoundException)
                        + " param=" + exception.ParamName + " name=" + exception.InvalidCultureName.Replace("\0", "<NUL>"));
                    continue;
                }
                NumberFormatInfo n = culture.NumberFormat;
                Console.WriteLine("  '" + input.Replace("\0", "<NUL>") + "' -> '" + culture.Name + "' value='" + culture.ToString()
                    + "' LCID=" + culture.LCID + " neutral=" + culture.IsNeutralCulture);
                Console.WriteLine("    specific='" + CultureName(() => CultureInfo.CreateSpecificCulture(input))
                    + "' cached='" + CultureName(() => CultureInfo.GetCultureInfo(input))
                    + "' without override='" + CultureName(() => new CultureInfo(input, false)) + "'");
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

        private static string CultureName(Func<CultureInfo> create)
        {
            try
            {
                return create().Name;
            }
            catch (CultureNotFoundException exception)
            {
                return nameof(CultureNotFoundException) + " param=" + exception.ParamName
                    + " name=" + exception.InvalidCultureName;
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
