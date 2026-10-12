using System;
using System.Globalization;

namespace CultureInfoApi
{
    // CultureInfo's object model beyond the NumberFormatInfo symbols: the parts that
    // answer from per-culture METADATA rather than from separators.
    //
    // dn2cpp does NOT simply model InvariantGlobalization=true: CurrentCulture reads the
    // host, and LCID, IsNeutralCulture and the named-culture table give real per-culture
    // answers. What is still a constant is stated where it is emitted, not assumed here.
    //
    // Two reasons this is a frozen snapshot rather than a live `dotnet $app` diff, and
    // the second is the newer one: dn2cpp constructs no Calendar object and traps
    // loudly (PlatformNotSupportedException) where real .NET returns a
    // GregorianCalendar; and GetCultures / NativeName / DisplayName answer for the
    // invariant culture alone, because those are ICU inventory and display-name
    // questions and dn2cpp carries neither table (see the emit arms).
    internal static class Program
    {
        private static void Main(string[] args)
        {
            // Pin both cultures first: gate output must not depend on the host locale (see AGENTS.md).
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

            if (args.Length > 0 && args[0] == "culture-names")
            {
                RunCultureNames();
                return;
            }

            if (args.Length > 0 && args[0] == "culture-locale-upper")
            {
                Console.WriteLine("== culture uppercase extensions ==");
                foreach (string name in new[] { "de-DE-U-NU-LATN", "en-US-U-NU-ARAB" })
                {
                    WriteLocaleData(name, false);
                    Console.WriteLine("  cached after constructor=" + CultureInfo.GetCultureInfo(name).Name);
                }
                Console.WriteLine("culture uppercase extensions end");
                return;
            }

            if (args.Length > 0 && (args[0] == "culture-locale-cache-true"
                || args[0] == "culture-locale-cache-false"))
            {
                RunCultureDataCache(args[0] == "culture-locale-cache-true");
                return;
            }

            if (args.Length > 0 && args[0] == "culture-numbering-boundaries")
            {
                RunNumberingBoundaries();
                return;
            }

            if (args.Length > 0 && args[0] == "culture-numbering-data")
            {
                RunNumberingData(args.Length > 1 ? args[1] : null);
                return;
            }

            if (args.Length > 0 && (args[0] == "culture-sort-strings" || args[0] == "culture-sort-upper-key"))
            {
                RunSortStrings(args[0] == "culture-sort-upper-key");
                return;
            }
            if (args.Length > 0 && args[0].StartsWith("culture-wrapper-", StringComparison.Ordinal))
            {
                RunWrapperCache(args[0]);
                return;
            }

            CultureInfo inv = CultureInfo.InvariantCulture;
            Console.WriteLine("== CultureInfo object model ==");
            Console.WriteLine("NativeName=" + inv.NativeName);
            Console.WriteLine("DisplayName=" + inv.DisplayName);
            Console.WriteLine("LCID=" + inv.LCID);
            Console.WriteLine("IsNeutralCulture=" + inv.IsNeutralCulture);
            Console.WriteLine("Name='" + inv.Name + "'");

            // Both axes are PINNED above, so these read the pin and not the host — which is
            // the whole point of printing them in a frozen bucket. That they read the pin
            // rather than each other is asserted below.
            Console.WriteLine("CurrentCulture.LCID=" + CultureInfo.CurrentCulture.LCID);
            Console.WriteLine("CurrentUICulture.LCID=" + CultureInfo.CurrentUICulture.LCID);

            // GetCultures answers for the invariant culture alone (an ICU inventory question
            // dn2cpp does not carry a table for — see the emit arm), whatever the size of the
            // named-culture table the sections below exercise.
            CultureInfo[] all = CultureInfo.GetCultures(CultureTypes.AllCultures);
            Console.WriteLine("GetCultures(All).Length=" + all.Length);
            for (int i = 0; i < all.Length; i++)
                Console.WriteLine("  [" + i + "] LCID=" + all[i].LCID + " Name='" + all[i].Name
                    + "' Native=" + all[i].NativeName);

            // ---- LCID is per-culture, in both directions ----
            // The third answer is the one worth a test: a culture the table does not carry
            // must report 4096 (LOCALE_CUSTOM_UNSPECIFIED), as real .NET does — answering
            // the invariant 127 is indistinguishable from actually being invariant.
            Console.WriteLine("-- LCID --");
            foreach (string nm in new[] { "en-US", "ja-JP", "en-GB", "pt-BR", "en", "xx-YY" })
            {
                CultureInfo c = new CultureInfo(nm);
                Console.WriteLine("  '" + nm + "' LCID=" + c.LCID + " neutral=" + c.IsNeutralCulture);
            }
            // The reverse lookup. 127 is the invariant culture; a modeled culture's own LCID
            // resolves it; 4096 is refused BECAUSE it names the absence of an LCID (several
            // real cultures carry it, so honouring it would hand back whichever came first),
            // and so is an LCID no modeled culture has.
            foreach (int id in new[] { 127, 1041, 2057, 4096, 12345 })
            {
                try
                {
                    Console.WriteLine("  GetCultureInfo(" + id + ")='" + CultureInfo.GetCultureInfo(id).Name + "'");
                }
                catch (ArgumentException)
                {
                    Console.WriteLine("  GetCultureInfo(" + id + "): ArgumentException");
                }
            }

            // ---- The breadth of the named-culture table ----
            // A missing entry resolves to invariant SYMBOLS under a correct NAME, which goes
            // red on one developer's machine only. The FORMATTING is asserted, not just the
            // name — a name-only assert passes the whole time it is broken. Every value is
            // produced with an EXPLICIT culture, so the host cannot move this section.
            Console.WriteLine("-- table breadth --");
            foreach (string nm in new[] { "en-GB", "pt-BR", "es-ES", "ru-RU", "sv-SE", "de-CH", "pt-br" })
            {
                CultureInfo c = new CultureInfo(nm);
                Console.WriteLine("  '" + nm + "' -> '" + c.Name + "' n=" + (-1234.5).ToString("N2", c)
                    + " c=" + (1234.5).ToString("C", c) + " nan=" + double.NaN.ToString(c));
            }
            // ur-PK's 3-then-2 grouping and its zero CurrencyDecimalDigits are rows CLDR has
            // revised, so the ICU a host ships answers differently from the one the table was
            // cut from and no live host diff can hold them. A frozen bucket is the only place
            // they can be asserted at all, which is why they live here and not with the rest
            // of the multi-level grouping in ConsoleIo's CultureSubset.
            CultureInfo urPK = new CultureInfo("ur-PK");
            NumberFormatInfo urNfi = urPK.NumberFormat;
            Console.WriteLine("  'ur-PK' N=" + Sizes(urNfi.NumberGroupSizes)
                + " C=" + Sizes(urNfi.CurrencyGroupSizes) + " P=" + Sizes(urNfi.PercentGroupSizes)
                + " digits=" + urNfi.CurrencyDecimalDigits);
            Console.WriteLine("  'ur-PK' n=" + (-12345678.9).ToString("N2", urPK)
                + " c=" + (-1234567.89m).ToString("C", urPK));

            // en-US's CurrencyNegativePattern is another such ICU-revised row, moved here out
            // of ConsoleIo's CultureSubset (its Show now takes skipCurrency) for the same
            // reason: only a frozen bucket can assert it.
            CultureInfo enUS = new CultureInfo("en-US");
            CultureInfo jaJP = new CultureInfo("ja-JP");
            double money = 1234.5, neg = -1234567.891;
            Console.WriteLine("  'en-US' c-=" + (-money).ToString("C", enUS));

            // ja-JP's currency SYMBOL is the one field dn2cpp itself intentionally varies by
            // build OS (dn2cpp_culture_table.inc: U+FFE5 on Linux vs U+00A5 elsewhere, matching
            // glibc's ICU) — a single frozen literal can't hold both right answers, so assert
            // dn2cpp's own OS split instead of the symbol text: this stays True on every host
            // as long as the #if here agrees with the C# check.
            char expectedYen = OperatingSystem.IsLinux() ? '\uffe5' : '\u00a5';
            bool jaJPSymbolMatchesOsSplit = money.ToString("C", jaJP).Contains(expectedYen)
                && (-money).ToString("C", jaJP).Contains(expectedYen)
                && string.Format(jaJP, "{0:N2} / {1:C}", neg, money).Contains(expectedYen);
            Console.WriteLine("  'ja-JP' symbolMatchesOsSplit=" + jaJPSymbolMatchesOsSplit);

            // A culture the table does not carry keeps its requested name over invariant
            // symbols — deliberately, and unchanged by (b): widening the table moves the line
            // between these two answers, it does not remove the second one.
            //
            // Its NAME is asserted and its FORMATTING deliberately is not, and the reason is
            // a real-.NET property worth knowing: an unrecognized culture is a CUSTOM culture
            // there, and .NET builds its NumberFormatInfo from the OS LOCALE — not from
            // CurrentCulture. So `(1234.5).ToString("N2", new CultureInfo("xx-YY"))` is
            // "1.234,50" on a de-DE host and "1,234.50" on an en-US one WITH BOTH DRIVER PINS
            // IN PLACE: the pin cannot reach it, because the pin moves a thread's culture and
            // this reads the machine's. Printing it made this bucket host-dependent and
            // gates/verify-culture-invariance.sh said so by name — which is the one shape of
            // hole a driver pin does not close, so do not re-add the value here.
            CultureInfo unknown = new CultureInfo("xx-YY");
            Console.WriteLine("  unmodeled name='" + unknown.Name + "' LCID=" + unknown.LCID);

            // A culture serves the requested DateTimeFormatInfo as a managed object.
            object gf = inv.GetFormat(typeof(DateTimeFormatInfo));
            Console.WriteLine("GetFormat(DTFI) null? " + (gf == null));

            // Calendar is not modeled: it traps loudly rather than returning null.
            try
            {
                Calendar cal = inv.Calendar;
                Console.WriteLine("Calendar: no throw (" + cal.GetType().Name + ")");
            }
            catch (PlatformNotSupportedException)
            {
                Console.WriteLine("Calendar: PlatformNotSupportedException");
            }

            // ---- CurrentUICulture is a SLOT, and a different one ----
            // SUBJECT: the second culture axis. Not "UI culture formatting" — the UI culture
            // formats nothing; what is under test is that it is a real, settable,
            // independently readable slot, because it was a constant fold to invariant whose
            // setter discarded its argument, and the SUITE COULD NOT SEE THAT. Every bucket
            // driver pins this axis as its second statement, so a no-op setter and a working
            // one are indistinguishable from any bucket's output — which is exactly why the
            // pin may not be removed here to test it. This section sets the axis EXPLICITLY,
            // asserts the properties that distinguish a slot from a fold, and restores the
            // pin before anything else runs (the same shape regex-core uses at its culture
            // layer). Removing the restore would leave the sections below reading a ja-JP UI
            // culture, which is the failure mode the pin exists to prevent.
            Console.WriteLine("-- CurrentUICulture --");
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");
            CultureInfo.CurrentUICulture = new CultureInfo("ja-JP");
            // The two axes are independent. A setter that wrote the CurrentCulture slot, or a
            // getter aliased to it, fails right here — and nothing else in the corpus would.
            Console.WriteLine("  ui='" + CultureInfo.CurrentUICulture.Name
                + "' cur='" + CultureInfo.CurrentCulture.Name + "'");
            // Numbers follow CurrentCulture, never CurrentUICulture: this must be the de-DE
            // rendering even though the UI culture is ja-JP.
            Console.WriteLine("  provider-less N2=" + (1234.5).ToString("N2"));
            // The setter takes the argument it is given (a discarded one reads back invariant).
            CultureInfo.CurrentUICulture = new CultureInfo("en-GB");
            Console.WriteLine("  after second set ui='" + CultureInfo.CurrentUICulture.Name
                + "' LCID=" + CultureInfo.CurrentUICulture.LCID);
            // InstalledUICulture is the OS's own language and NO setter moves it. Its VALUE is
            // the host's, so it cannot be printed in a frozen bucket; the host-independent
            // property — that the two assignments above did not perturb it — is what is
            // asserted, and it is the property that would break if InstalledUICulture were
            // wired to either slot.
            string installedBefore = CultureInfo.InstalledUICulture.Name;
            CultureInfo.CurrentUICulture = new CultureInfo("tr-TR");
            CultureInfo.CurrentCulture = new CultureInfo("fr-FR");
            Console.WriteLine("  InstalledUICulture unmoved by either setter: "
                + (CultureInfo.InstalledUICulture.Name == installedBefore));
            // Restore the driver's pin for both axes before the next section.
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
            Console.WriteLine("  restored cur='" + CultureInfo.CurrentCulture.Name
                + "' ui='" + CultureInfo.CurrentUICulture.Name + "'");

            // CultureInfo/NumberFormatInfo/TextInfo escaping into `object`
            // contexts (the headerless-intrinsic wrap boundary).
            CultureEscapeSubset.Program.Run();

            // The residue of that boundary: the escapes whose IL spells no
            // conversion (a type-erased byref, a delegate variance conversion,
            // a `constrained.` GetType, an erased interface slot, a reflection
            // Invoke thunk), and the two headerless representations that trap
            // instead of wrapping. See that file's header.
            CultureEscapeResidueSubset.Program.Run();

            // Span TryFormat with provider != CurrentCulture — the one shape every
            // other bucket's pinned call sites cannot see. See that file's header.
            SpanTryFormatProviderSubset.Program.Run();

            Console.WriteLine("== TextInfo ToString ==");
            TextInfo deText = new CultureInfo("de-DE").TextInfo;
            Console.WriteLine("  direct=" + deText.ToString());
            object boxedText = deText;
            Console.WriteLine("  object=" + boxedText.ToString());
            Console.WriteLine("  generic=" + GenericToString(deText));
            Console.WriteLine($"  interp={deText}");

            if (args.Length > 0 && args[0] == "before-culture-names")
                return;
            RunCultureNames();
        }

        private static void RunCultureNames()
        {
            Console.WriteLine("== culture name resolution ==");
            foreach (string name in new[]
            {
                "", "C", "c", "de-DE-EURO", "DE-de-euro",
                "de-DE-1901", "fr-FR-EURO", "de-Latn-DE-EURO", "xx-YY", "xx-Latn-YY",
                "de-DE-u-nu-latn", "de-DE-t-en-us", "de-DE-u-nu-LaTn", "de-DE-t-EN-us",
                "en-US-POSIX", "de-u-nu-latn",
                "de-DE\0ignored"
            })
            {
                CultureInfo culture = new CultureInfo(name);
                Console.WriteLine("  '" + name.Replace("\0", "<NUL>") + "' -> '" + culture.Name + "' LCID="
                    + culture.LCID + " neutral=" + culture.IsNeutralCulture);
                if (name.StartsWith("de-", StringComparison.OrdinalIgnoreCase)
                    || name.StartsWith("fr-", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("    n=" + (1234.5).ToString("N2", culture)
                        + " c=" + (1234.5).ToString("C", culture));
                    Console.WriteLine("    cached=" + CultureInfo.GetCultureInfo(name).Name
                        + " override=" + new CultureInfo(name, false).Name);
                }
            }
            foreach (string name in new[]
            {
                "C.UTF-8", "a", "-de", "de-", "de--DE", "de_-DE", "de__DE", "de_DE_x",
                "de DE", "de.DE", "de@euro", "dé-DE", "}", new string('a', 12),
                "de-DE-" + new string('a', 80)
            })
            {
                try
                {
                    Console.WriteLine("  invalid '" + name + "': accepted=" + new CultureInfo(name).Name);
                }
                catch (CultureNotFoundException exception)
                {
                    Console.WriteLine("  invalid '" + name + "': CultureNotFoundException hr="
                        + exception.HResult.ToString("X8"));
                    WriteCultureFault(exception);
                }
            }
            try
            {
                Console.WriteLine("  invalid cached: accepted=" + CultureInfo.GetCultureInfo("C.UTF-8").Name);
            }
            catch (CultureNotFoundException exception)
            {
                Console.WriteLine("  invalid cached: CultureNotFoundException hr=" + exception.HResult.ToString("X8"));
                WriteCultureFault(exception);
            }
            try
            {
                Console.WriteLine("  invalid cached Unicode: accepted=" + CultureInfo.GetCultureInfo("DÉ-DE").Name);
            }
            catch (CultureNotFoundException exception)
            {
                Console.WriteLine("  invalid cached Unicode: CultureNotFoundException hr=" + exception.HResult.ToString("X8"));
                WriteCultureFault(exception);
            }
            try
            {
                Console.WriteLine("  invalid override: accepted=" + new CultureInfo("C.UTF-8", false).Name);
            }
            catch (CultureNotFoundException exception)
            {
                Console.WriteLine("  invalid override: CultureNotFoundException hr=" + exception.HResult.ToString("X8"));
                WriteCultureFault(exception);
            }
            CultureInfo first = CultureInfo.GetCultureInfo("de-DE-EURO");
            CultureInfo second = CultureInfo.GetCultureInfo("DE-de-euro");
            CultureInfo parent = CultureInfo.GetCultureInfo("de-DE");
            Console.WriteLine("  identity=" + ReferenceEquals(first, second)
                + " parent=" + ReferenceEquals(first, parent));
            Console.WriteLine("  extension identity=" + ReferenceEquals(CultureInfo.GetCultureInfo("de-DE-u-nu-LaTn"),
                CultureInfo.GetCultureInfo("DE-de-u-nu-latn")));
            foreach (string name in new[] { "de-DE-U-NU-LATN", "de-DE-T-EN-US" })
                Console.WriteLine("  cached extension '" + name + "' -> '" + CultureInfo.GetCultureInfo(name).Name + "'");
            GC.Collect();
            Console.WriteLine("  after GC=" + first.Name + ":" + (1234.5).ToString("N2", first));
            foreach (string name in new[] { "de-XX", "de-XX-FOO" })
            {
                CultureInfo culture = new CultureInfo(name);
                Console.WriteLine("  missing region=" + culture.Name + " neutral=" + culture.IsNeutralCulture);
            }
            foreach (int lcid in new[] { 4096, 12345 })
            {
                try
                {
                    Console.WriteLine("  invalid LCID=" + lcid + ": accepted=" + CultureInfo.GetCultureInfo(lcid).Name);
                }
                catch (CultureNotFoundException exception)
                {
                    Console.WriteLine("  invalid LCID=" + lcid + ": CultureNotFoundException hr="
                        + exception.HResult.ToString("X8"));
                    WriteCultureFault(exception);
                }
            }
            foreach (string name in new[] { "de-DE", "de-DE.UTF-8", "en-US;q=0.9", "de-", "-de", "C.UTF-8", "de.DE", "a" })
            {
                try
                {
                    CultureInfo.CreateSpecificCulture(name);
                    Console.WriteLine("  specific '" + name + "': accepted");
                }
                catch (CultureNotFoundException exception)
                {
                    Console.WriteLine("  specific '" + name + "': CultureNotFoundException");
                    WriteCultureFault(exception);
                }
            }
            foreach (string name in new[] { "zh-Hans-HK", "zh-Hans-TW", "de-Latn-DE", "de-Latn-DE-EURO" })
            {
                CultureInfo culture = new CultureInfo(name);
                Console.WriteLine("  script '" + culture.Name + "': nan=" + culture.NumberFormat.NaNSymbol);
            }
            Console.WriteLine("culture name resolution end");
            RunLocaleData();
        }

        private static void RunLocaleData()
        {
            Console.WriteLine("== culture locale data ==");
            foreach (string name in new[]
            {
                "eng", "eng-US", "deu-DE", "fra-FR", "und", "de_DE",
                "de-DE_phoneb", "de-DE_PHONEB", "de-DE-u-co-phoneb", "de-DE-u-co-phonebk",
                "de-DE-u-co-phoneb-foo", "en-US-u-co-phoneb-nu-arab", "de-DE-u-co-phonebk-nu-arab",
                "en-US-u-nu-arab", "en-US-u-ca-gregory-x-nu-arab", "de-NO", "de-FOO", "zh-Hans-HK", "zh-Hans-TW",
                "de-Latn", "de-Latn-DE", "zh-Hans", "zh-Hant"
            })
                WriteLocaleData(name);
            CultureInfo undefinedLanguage = new CultureInfo("und-US");
            Console.WriteLine("  undefined language name='" + undefinedLanguage.Name
                + "' LCID=" + undefinedLanguage.LCID + " neutral=" + undefinedLanguage.IsNeutralCulture
                + " override=" + new CultureInfo("und-US", false).Name);
            foreach (string name in new[]
            {
                "", "und", "eng", "de", "de-Latn", "zh", "zh-Hans", "zh-Hant", "de_DE",
                "bn", "sr", "sr-Latn", "de-DE.UTF-8", "de-", "en-US;q=0.9"
            })
                Console.WriteLine("  specific '" + name + "' -> '" + CultureInfo.CreateSpecificCulture(name).Name + "'");
            foreach (int lcid in new[] { 66567, 1034, 133124 })
            {
                CultureInfo reverse = CultureInfo.GetCultureInfo(lcid);
                string sortName = lcid == 66567 ? "de-DE_phoneb"
                    : lcid == 1034 ? "es-ES_tradnl" : "zh-CN_stroke";
                Console.WriteLine("  sort LCID=" + lcid + " -> '" + reverse.Name + "' LCID=" + reverse.LCID
                    + " named=" + ReferenceEquals(reverse, CultureInfo.GetCultureInfo(sortName))
                    + " repeat=" + ReferenceEquals(reverse, CultureInfo.GetCultureInfo(lcid)));
            }
            CultureInfo baseNumeric = CultureInfo.GetCultureInfo(1031);
            Console.WriteLine("  base LCID named=" + ReferenceEquals(baseNumeric, CultureInfo.GetCultureInfo("de-DE"))
                + " repeat=" + ReferenceEquals(baseNumeric, CultureInfo.GetCultureInfo(1031)));
            CultureInfo sort = CultureInfo.GetCultureInfo("de-DE_phoneb");
            Console.WriteLine("  sort cache case=" + ReferenceEquals(sort, CultureInfo.GetCultureInfo("DE-de_PHONEB"))
                + " base=" + ReferenceEquals(sort, CultureInfo.GetCultureInfo("de-DE")));
            CultureInfo arab = CultureInfo.GetCultureInfo("en-US-u-nu-arab");
            Console.WriteLine("  numbering cache case=" + ReferenceEquals(arab, CultureInfo.GetCultureInfo("EN-us-U-NU-ARAB"))
                + " base=" + ReferenceEquals(arab, CultureInfo.GetCultureInfo("en-US")));
            Console.WriteLine("culture locale data end");
            RunNumberingData();
        }

        private static void RunNumberingData(string? name = null)
        {
            Console.WriteLine("== culture numbering data ==");
            foreach (string cultureName in name is null ? new[]
            {
                "en-US-u-nu-arab-foo", "en-US-u-nu-unknown", "hi-IN-u-nu-arab",
                "bn-IN-u-nu-arab", "fr-FR-u-nu-arab", "ja-JP-u-nu-arab",
                "en-US-u-nu", "en-US-u-nu-roman", "zh-CN-u-nu-hans",
                "ar-EG-u-nu-native", "ar-EG-u-nu-finance", "ja-JP-u-nu-traditio",
                "az-Latn-AZ-u-nu-arab"
            } : new[] { name })
            {
                CultureInfo culture = new CultureInfo(cultureName);
                NumberFormatInfo n = culture.NumberFormat;
                Console.WriteLine("  locale '" + cultureName + "' -> '" + culture.Name + "' LCID=" + culture.LCID
                    + " neutral=" + culture.IsNeutralCulture
                    + " override=" + new CultureInfo(cultureName, false).Name);
                Console.WriteLine("    separators=" + n.NumberDecimalSeparator + "|" + n.NumberGroupSeparator
                    + "|" + n.CurrencyDecimalSeparator + "|" + n.CurrencyGroupSeparator
                    + "|" + n.PercentDecimalSeparator + "|" + n.PercentGroupSeparator);
                Console.WriteLine("    symbols=" + n.NegativeSign + "|" + n.NaNSymbol + "|" + n.PositiveInfinitySymbol
                    + "|" + n.NegativeInfinitySymbol + "|" + n.CurrencySymbol + "|" + n.PercentSymbol);
                Console.WriteLine("    groups=" + Sizes(n.NumberGroupSizes) + "|" + Sizes(n.CurrencyGroupSizes)
                    + "|" + Sizes(n.PercentGroupSizes)
                    + " patterns=" + n.CurrencyPositivePattern + "," + n.CurrencyNegativePattern
                    + "," + n.PercentPositivePattern + "," + n.PercentNegativePattern);
                Console.WriteLine("    positive=" + n.PositiveSign
                    + " per mille=" + n.PerMilleSymbol
                    + " digits=" + n.NumberDecimalDigits + "," + n.CurrencyDecimalDigits + "," + n.PercentDecimalDigits
                    + " negative pattern=" + n.NumberNegativePattern);
                foreach (string format in new[]
                {
                    "N", "N2", "C", "C2", "P", "P2", "F", "F2", "G", "E2", "0.00E+00", "0.00‰", "#,##0.00"
                })
                {
                    Console.WriteLine("    " + format + " negative double=" + FormatNumbering(-1234567.89, format, culture)
                        + " decimal=" + FormatNumbering(-1234567.89m, format, culture));
                    Console.WriteLine("    " + format + " positive double=" + FormatNumbering(1234567.89, format, culture)
                        + " decimal=" + FormatNumbering(1234567.89m, format, culture));
                }
                Console.WriteLine("    special=" + double.NaN.ToString(culture)
                    + "|" + double.PositiveInfinity.ToString(culture)
                    + "|" + double.NegativeInfinity.ToString(culture));
                Console.WriteLine("    N minimum double=" + FormatNumbering(double.MinValue, "N", culture));
                string paddedFormat = "#,##" + new string('0', 55) + ".00";
                Console.WriteLine("    custom padded double=" + FormatNumbering(1.0, paddedFormat, culture)
                    + " decimal=" + FormatNumbering(1m, paddedFormat, culture));
                string literalFormat = "'" + new string('x', 252) + "'0E+0";
                Console.WriteLine("    custom literal double=" + FormatNumbering(1.0, literalFormat, culture)
                    + " decimal=" + FormatNumbering(1m, literalFormat, culture));
                foreach (string format in new[] { "N2", "C2", "P2" })
                {
                    Console.WriteLine("    " + format + " zero double=" + FormatNumbering(0.0, format, culture)
                        + " decimal=" + FormatNumbering(0m, format, culture));
                    Console.WriteLine("    " + format + " one double=" + FormatNumbering(1.0, format, culture)
                        + " decimal=" + FormatNumbering(1m, format, culture));
                    foreach (decimal fraction in new[] { 0.25m, 0.1m, 0.004m })
                    {
                        Console.WriteLine("    " + format + " fraction " + fraction + " double="
                            + FormatNumbering((double)fraction, format, culture)
                            + " decimal=" + FormatNumbering(fraction, format, culture));
                        Console.WriteLine("    " + format + " negative fraction " + fraction + " double="
                            + FormatNumbering(-(double)fraction, format, culture)
                            + " decimal=" + FormatNumbering(-fraction, format, culture));
                    }
                }
                foreach (string format in new[] { "N", "N2", "F", "F2", "C", "C2", "P", "P2" })
                {
                    foreach (long integer in new[] { 0L, 1L, -1234567L })
                        Console.WriteLine("    " + format + " integer " + integer + "=" + FormatNumbering(integer, format, culture));
                }
                foreach (string input in new[]
                {
                    n.PositiveSign + "12", n.NegativeSign + "12", "1E" + n.PositiveSign + "2",
                    "", n.PositiveSign + n.PositiveInfinitySymbol
                })
                {
                    bool doubleParsed = double.TryParse(input, NumberStyles.Float, culture, out double doubleValue);
                    bool decimalParsed = decimal.TryParse(input, NumberStyles.Float, culture, out decimal decimalValue);
                    Console.WriteLine("    parse '" + input + "' double=" + doubleParsed + ":" + doubleValue
                        + " decimal=" + decimalParsed + ":" + decimalValue);
                }
            }
            Console.WriteLine("culture numbering data end");
        }

        private static string FormatNumbering(double value, string format, CultureInfo culture)
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

        private static string FormatNumbering(decimal value, string format, CultureInfo culture)
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

        private static string FormatNumbering(long value, string format, CultureInfo culture)
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

        private static void RunNumberingBoundaries()
        {
            Console.WriteLine("== culture numbering boundaries ==");
            foreach (string name in new[]
            {
                "en-US-x-u-nu-arab", "en-US-u-nu-latn-nu-arab", "sv-SE-u-nu-arab",
                "en-US\0-u-nu-arab", "en-US-u-nu-arab\0ignored", "en-US_phoneb-u-nu-arab",
                "en-US-u-nu-arab_phoneb", "en-US-u-aa-123456789-nu-foo",
                "en-US-u-nu-arab-123456789", "en-US-a-u-nu-foo", "en-US-t-u-nu-foo",
                "en-US-0-u-nu-foo", "en-US-a-b-u-nu-foo", "en-US-a-foo-a-bar-u-nu-foo",
                "en-US-a-foo-A-bar-u-nu-foo", "en-US-abcde-abcde-u-nu-foo",
                "en-US-abc-u-nu-foo", "en-US-abcdefghi-u-nu-foo", "en-US-a-123456789-u-nu-foo",
                "en-US-Abcd-u-nu-foo", "en-US-abc1-u-nu-foo", "en-US-001-u-nu-foo",
                "en-US-US-u-nu-foo", "en-Latn-Latn-u-nu-foo", "en-US-abcde-fghij-ABCDE-u-nu-foo",
                "en-US-t-en-u-nu-foo", "en-US-a-en-u-nu-foo", "en-US-a-fo-u-nu-foo",
                "en-US-a-1-u-nu-foo", "en-US-1abc-u-nu-foo", "en-US-1234-u-nu-foo",
                "en-US-u-ca-foo-ca-bar-nu-foo", "en-US-u-attr-attr-nu-foo",
                "en-US-u-aa-123456789-nu", "en-US-u-aa-12345678-nu",
                "en-US-u-nu-123456789", "en-US-u-foo-u-nu-foo",
                "art-lojban-u-nu-foo", "cel-gaulish-u-nu-foo", "en-GB-oed-u-nu-foo",
                "i-ami-u-nu-foo", "i-bnn-u-nu-foo", "i-hak-u-nu-foo", "i-klingon-u-nu-foo",
                "i-lux-u-nu-foo", "i-navajo-u-nu-foo", "i-pwn-u-nu-foo", "i-tao-u-nu-foo",
                "i-tay-u-nu-foo", "i-tsu-u-nu-foo", "no-bok-u-nu-foo", "no-nyn-u-nu-foo",
                "sgn-BE-FR-u-nu-foo", "sgn-BE-NL-u-nu-foo", "sgn-CH-DE-u-nu-foo",
                "zh-guoyu-u-nu-foo", "zh-hakka-u-nu-foo", "zh-min-nan-u-nu-foo", "zh-xiang-u-nu-foo",
                "en-US-u-nu-arab-00-foo", "en-US-u-nu-arab-a0-foo", "en-US-u-nu-arab-11-foo",
                "en-US-u-nu-arab-a1-foo", "en-US-u-nu-arab-0a-foo", "en-US-u-nu-arab-1a-foo",
                "en-US-u-nu-arab-aa-foo", "en-US-u-nu-roman-a0-foo", "en-US-u-nu-roman-0a-foo",
                "en-US-u-00-nu-arab", "en-US-u-a0-nu-arab", "en-US-u-aa-00-nu-arab",
                "en-US-u-a0-foo-nu-arab", "en-US-u-nu-00-arab", "en-US-u-aa-00-nu-roman"
            })
            {
                CultureInfo culture = new CultureInfo(name);
                NumberFormatInfo n = culture.NumberFormat;
                Console.WriteLine("  '" + name + "': decimal=" + n.NumberDecimalSeparator
                    + " group=" + n.NumberGroupSeparator + " negative=" + n.NegativeSign
                    + " currency=" + n.CurrencySymbol + " percent=" + n.PercentSymbol);
                Console.WriteLine("    patterns=" + n.CurrencyPositivePattern + "," + n.CurrencyNegativePattern
                    + "," + n.PercentPositivePattern + "," + n.PercentNegativePattern
                    + " n=" + FormatNumbering(-1234.5, "N2", culture)
                    + " c=" + FormatNumbering(1234.5, "C", culture)
                    + " p=" + FormatNumbering(-0.125, "P2", culture));
            }
            foreach (string prefix in new[] { "e-US", "123-US", "abcdefghi-US" })
            {
                NumberFormatInfo baseData = new CultureInfo(prefix).NumberFormat;
                NumberFormatInfo extended = new CultureInfo(prefix + "-u-nu-foo").NumberFormat;
                Console.WriteLine("  uninterpreted prefix '" + prefix + "' keeps numeric data="
                    + (baseData.NumberDecimalSeparator == extended.NumberDecimalSeparator
                        && baseData.NumberGroupSeparator == extended.NumberGroupSeparator
                        && baseData.CurrencySymbol == extended.CurrencySymbol
                        && baseData.NaNSymbol == extended.NaNSymbol
                        && baseData.NegativeSign == extended.NegativeSign
                        && baseData.NumberDecimalDigits == extended.NumberDecimalDigits));
            }
            foreach (string prefix in new[] { "i-default", "i-enochian", "i-mingo", "zh-min" })
            {
                NumberFormatInfo n = new CultureInfo(prefix + "-u-nu-foo").NumberFormat;
                Console.WriteLine("  private prefix '" + prefix + "' ignores nu: decimal="
                    + (n.NumberDecimalSeparator.Length > 0) + " negative=" + (n.NegativeSign.Length > 0)
                    + " groups=" + (n.NumberGroupSizes[0] >= 0));
            }
            Console.WriteLine("culture numbering boundaries end");
        }

        private static void RunSortStrings(bool upperKey)
        {
            Console.WriteLine("== culture sort strings ==");
            string[] names = upperKey ? new[] { "de-DE-u-CO-phoneb" }
                : new[] { "de-DE_PHONEB", "de-DE-u-co-phoneb", "de-DE-u-co-phonebk" };
            foreach (string name in names)
            {
                CultureInfo culture = new CultureInfo(name);
                object boxed = culture;
                TextInfo text = culture.TextInfo;
                object boxedText = text;
                Console.WriteLine("  '" + name + "': name=" + culture.Name + " direct=" + culture.ToString()
                    + " object=" + boxed.ToString() + " generic=" + GenericToString(culture)
                    + " concat=" + culture + $" interp={culture}");
                Console.WriteLine("    text=" + text.ToString() + " object=" + boxedText.ToString()
                    + " generic=" + GenericToString(text) + " concat=" + text + $" interp={text}");
            }
            Console.WriteLine("culture sort strings end");
        }

        private static void RunWrapperCache(string mode)
        {
            string firstName;
            string secondName;
            if (mode == "culture-wrapper-alias-first")
            {
                firstName = "eng";
                secondName = "en";
            }
            else if (mode == "culture-wrapper-base-first")
            {
                firstName = "en";
                secondName = "eng";
            }
            else if (mode == "culture-wrapper-sort-first")
            {
                firstName = "de-DE_phoneb";
                secondName = "de-DE-u-co-phoneb";
            }
            else
            {
                firstName = "de-DE-u-co-phoneb";
                secondName = "de-DE_phoneb";
            }
            CultureInfo first = CultureInfo.GetCultureInfo(firstName);
            CultureInfo second = CultureInfo.GetCultureInfo(secondName);
            CultureInfo third = CultureInfo.GetCultureInfo(firstName);
            CultureInfo fourth = CultureInfo.GetCultureInfo(secondName);
            Console.WriteLine("== " + mode + " ==");
            Console.WriteLine("  first=" + first.Name + ":" + first.LCID
                + " second=" + second.Name + ":" + second.LCID);
            Console.WriteLine("  pair=" + ReferenceEquals(first, second)
                + " repeat first=" + ReferenceEquals(first, third)
                + " repeat second=" + ReferenceEquals(second, fourth));
            Console.WriteLine("culture wrapper cache end");
        }

        // CultureData keys lowercase input and the override flag; GetCultureInfo uses false.
        private static void RunCultureDataCache(bool useUserOverride)
        {
            Console.WriteLine("== culture data cache override=" + useUserOverride + " ==");
            foreach (string name in new[] { "de-DE-U-NU-LATN", "en-US-U-NU-ARAB" })
            {
                string lower = name.ToLowerInvariant();
                CultureInfo first = new CultureInfo(name, useUserOverride);
                CultureInfo cached = CultureInfo.GetCultureInfo(lower);
                CultureInfo otherOverride = new CultureInfo(lower, !useUserOverride);
                CultureInfo sameOverride = new CultureInfo(lower, useUserOverride);
                Console.WriteLine("  '" + name + "': first='" + first.Name + "' LCID=" + first.LCID
                    + " cached='" + cached.Name + "' LCID=" + cached.LCID);
                Console.WriteLine("    other override='" + otherOverride.Name + "' LCID=" + otherOverride.LCID
                    + " same override='" + sameOverride.Name + "' LCID=" + sameOverride.LCID);
                Console.WriteLine("    number=" + (1234.5).ToString("N2", first)
                    + "|" + (1234.5).ToString("N2", cached)
                    + "|" + (1234.5).ToString("N2", otherOverride)
                    + "|" + (1234.5).ToString("N2", sameOverride));
                Console.WriteLine("    cache identity=" + ReferenceEquals(cached, CultureInfo.GetCultureInfo(name)));
            }
            Console.WriteLine("culture data cache end");
        }

        private static void WriteLocaleData(string name, bool includeOverride = true)
        {
            CultureInfo culture = new CultureInfo(name);
            NumberFormatInfo n = culture.NumberFormat;
            Console.WriteLine("  locale '" + name + "' -> '" + culture.Name + "' LCID=" + culture.LCID
                + " neutral=" + culture.IsNeutralCulture
                + (includeOverride ? " override=" + new CultureInfo(name, false).Name : ""));
            Console.WriteLine("    separators=" + n.NumberDecimalSeparator + "|" + n.NumberGroupSeparator
                + "|" + n.CurrencyDecimalSeparator + "|" + n.CurrencyGroupSeparator
                + "|" + n.PercentDecimalSeparator + "|" + n.PercentGroupSeparator);
            Console.WriteLine("    symbols=" + n.NegativeSign + "|" + n.NaNSymbol + "|" + n.PositiveInfinitySymbol
                + "|" + n.NegativeInfinitySymbol + "|" + n.CurrencySymbol + "|" + n.PercentSymbol);
            Console.WriteLine("    groups=" + Sizes(n.NumberGroupSizes) + "|" + Sizes(n.CurrencyGroupSizes)
                + "|" + Sizes(n.PercentGroupSizes) + " currencyDigits=" + n.CurrencyDecimalDigits
                + " patterns=" + n.CurrencyPositivePattern + "," + n.CurrencyNegativePattern
                + "," + n.PercentPositivePattern + "," + n.PercentNegativePattern);
            Console.WriteLine("    n=" + (-1234567.89).ToString("N2", culture)
                + " c=" + (1234.5).ToString("C", culture) + " p=" + (-0.125).ToString("P2", culture));
        }

        private static void WriteCultureFault(CultureNotFoundException exception)
        {
            int? id = exception.InvalidCultureId;
            Console.WriteLine("    param=" + (exception.ParamName ?? "<null>")
                + " name=" + (exception.InvalidCultureName ?? "<null>")
                + " id=" + (id.HasValue ? id.Value.ToString(CultureInfo.InvariantCulture) : "<null>")
                + " message=" + exception.Message.Replace("\r\n", "\n").Replace("\n", "|"));
        }

        private static string GenericToString<T>(T value) => value!.ToString()!;

        // Number, currency and percent grouping can differ for the same culture.
        private static string Sizes(int[] a)
        {
            string s = "[";
            for (int i = 0; i < a.Length; i++)
                s += (i > 0 ? "," : "") + a[i];
            return s + "]";
        }
    }
}
