using System;
using System.Collections.Generic;
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

            if (args.Length > 0 && args[0] == "culture-iso-language")
            {
                RunIsoLanguage();
                return;
            }

            if (args.Length > 0 && args[0] == "culture-iso-language-undefined")
            {
                RunIsoLanguageUndefined();
                return;
            }

            if (args.Length > 0 && args[0] == "culture-legacy-chinese")
            {
                RunLegacyChineseCultures(args.Length > 1 ? args[1] : "constructor-first");
                return;
            }

            if (args.Length > 0 && args[0] == "culture-platform-name-validation")
            {
                RunPlatformNameValidation();
                return;
            }

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

            if (args.Length > 0 && args[0] == "culture-fallback-identities")
            {
                RunFallbackIdentities(args.Length > 1 ? args[1] : null);
                return;
            }

            if (args.Length > 0 && args[0] == "culture-primary-currency")
            {
                RunPrimaryCurrency();
                return;
            }
            if (args.Length > 0 && args[0] == "culture-variant-order")
            {
                RunVariantOrder(args.Length > 1 ? args[1] : null);
                return;
            }
            if (args.Length > 0 && args[0] == "culture-invariant-lcid-cache")
            {
                RunInvariantLcidCache(args.Length > 1 ? args[1] : "lcid-empty-missing");
                return;
            }
            if (args.Length > 0 && args[0] == "culture-instance-number-format")
            {
                RunInstanceNumberFormat(args.Length > 1 ? args[1] : null);
                return;
            }
            if (args.Length > 0 && args[0] == "culture-integer-constructors")
            {
                RunIntegerCultureConstructors();
                return;
            }
            if (args.Length > 0 && args[0] == "culture-instance-equality")
            {
                RunInstanceEquality(args.Length > 1 ? args[1] : null, args.Length > 2 ? args[2] : null);
                return;
            }
            if (args.Length > 0 && args[0] == "culture-rejected-extension-identities")
            {
                RunRejectedExtensionIdentities(args.Length > 1 ? args[1] : null);
                return;
            }
            if (args.Length > 0 && args[0] == "culture-extension-identities")
            {
                RunExtensionIdentities(args.Length > 1 ? args[1] : null);
                return;
            }
            if (args.Length > 0 && args[0] == "culture-undefined-platform-identities")
            {
                RunUndefinedPlatformIdentities(args.Length > 1 ? args[1] : null);
                return;
            }
            if (args.Length > 3 && args[0] == "culture-undefined-platform-cache")
            {
                RunUndefinedPlatformCache(args[1], args[2], args[3] == "constructor-first");
                return;
            }
            if (args.Length > 0 && args[0] == "culture-undefined-extension-metadata")
            {
                RunUndefinedExtensionMetadata(args.Length > 1 ? args[1] : null);
                return;
            }
            if (args.Length > 3 && args[0] == "culture-extension-order")
            {
                RunExtensionCacheOrder(args[1], args[2], args[3] == "constructor-first");
                return;
            }

            if (args.Length > 0 && args[0] == "culture-missing-number-format")
            {
                RunMissingNumberFormat(args.Length > 1 ? args[1] : null);
                return;
            }
            if (args.Length > 0 && (args[0] == "culture-missing-cache-first"
                || args[0] == "culture-missing-cache-empty-first"))
            {
                RunMissingNumberFormatCache(args[0] == "culture-missing-cache-first");
                return;
            }
            if (args.Length > 0 && (args[0] == "culture-missing-upper-constructor-first"
                || args[0] == "culture-missing-upper-cached-first"))
            {
                RunMissingUpperCache(args[0] == "culture-missing-upper-constructor-first");
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
            if (args.Length > 0 && args[0] == "before-iso-language")
                return;
            RunIsoLanguage();
            if (args.Length > 0 && args[0] == "before-legacy-chinese")
                return;
            RunLegacyChineseCultures("constructor-first");
        }

        private static void RunIsoLanguage()
        {
            Console.WriteLine("== ISO language names ==");
            foreach (string name in new[]
            {
                "", "EN-us", "de-DE", "zh-Hans", "zh-CHS",
                "iw", "heb", "eng", "fil", "haw",
                "tlh", "sr-Latn-RS", "az-Cyrl-AZ", "no-bok", "qaa",
                "in", "ji", "de-DE_phoneb", "eng-US", "fil-PH",
                "zul", "bod", "sqi", "yid", "fry",
                "i-enochian", "x-foo", "i-klingon", "x-foo_phoneb", "x-foobarbaz",
                "i-foo", "i-foobar", "i-mingo", "und-x-foo", "jaw",
                "mol", "tgl", "ger", "no-bok-u-nu-arab", "x-foo-US_phoneb",
                "x-123456789",
            })
                WriteIsoLanguage(name);
            try
            {
                Console.WriteLine("iso null=" + ((CultureInfo)null!).TwoLetterISOLanguageName);
            }
            catch (NullReferenceException)
            {
                Console.WriteLine("iso null=NullReferenceException");
            }
            Console.WriteLine("ISO language names end");
        }

        // Apple's ICU keeps "und" for some of these names; standard ICU reports none.
        private static void RunIsoLanguageUndefined()
        {
            foreach (string name in new[] { "und-US", "und-u-nu-arab" })
                WriteIsoLanguage(name);
            Console.WriteLine("ISO undefined language names end");
        }

        private static void WriteIsoLanguage(string name)
        {
            Console.WriteLine("iso '" + name + "'=" + ReadIsoLanguage(name, false)
                + ":" + ReadIsoLanguage(name, true));
        }

        private static string ReadIsoLanguage(string name, bool cached)
        {
            try
            {
                CultureInfo culture = cached ? CultureInfo.GetCultureInfo(name) : new CultureInfo(name);
                return culture.TwoLetterISOLanguageName;
            }
            catch (CultureNotFoundException exception)
            {
                return "CultureNotFoundException hr=" + exception.HResult.ToString("X8")
                    + " param=" + (exception.ParamName ?? "<null>")
                    + " name=" + (exception.InvalidCultureName ?? "<null>")
                    + " id=" + (exception.InvalidCultureId.HasValue
                        ? exception.InvalidCultureId.Value.ToString(CultureInfo.InvariantCulture) : "<null>");
            }
        }

        private static void RunLegacyChineseCultures(string order)
        {
            Console.WriteLine("== legacy Chinese cultures ==");
            Console.WriteLine("order=" + order);
            foreach (string name in new[] { "zh-CHS", "zh-CHT", "zh-CHS\0ignored", "zh-CHT\0ignored" })
            {
                string modern = name.StartsWith("zh-CHS", StringComparison.Ordinal) ? "zh-Hans" : "zh-Hant";
                int lcid = modern == "zh-Hans" ? 4 : 31748;
                Console.WriteLine("legacy '" + name.Replace("\0", "<NUL>") + "'");
                if (order == "cached-first")
                    WriteLegacyChineseCulture("first cache", CultureInfo.GetCultureInfo(name.ToUpperInvariant()));
                if (order == "lcid-first")
                    WriteLegacyChineseCulture("first LCID", CultureInfo.GetCultureInfo(lcid));
                WriteLegacyChineseCulture("constructor", new CultureInfo(name));
                WriteLegacyChineseCulture("no override", new CultureInfo(name, false));
                CultureInfo cached = CultureInfo.GetCultureInfo(name);
                WriteLegacyChineseCulture("cache", cached);
                WriteLegacyChineseCulture("modern", new CultureInfo(modern, false));
                WriteLegacyChineseCulture("LCID", new CultureInfo(lcid, false));
                WriteLegacyChineseCulture("LCID cache", CultureInfo.GetCultureInfo(lcid));
                WriteLegacyChineseCulture("specific", CultureInfo.CreateSpecificCulture(name));
                Console.WriteLine("  cache casing=" + ReferenceEquals(cached, CultureInfo.GetCultureInfo(name.ToUpperInvariant()))
                    + " modern=" + ReferenceEquals(cached, CultureInfo.GetCultureInfo(modern))
                    + " LCID=" + ReferenceEquals(cached, CultureInfo.GetCultureInfo(lcid)));
            }
            foreach (string name in new[] { "zh-CHS_phoneb", "zh-CHT_phoneb", "zh-CHS-u-nu-arab", "zh-CHT-u-nu-arab" })
                WriteIsoLanguage(name);
            Console.WriteLine("legacy Chinese cultures end");
        }

        private static void WriteLegacyChineseCulture(string label, CultureInfo culture)
        {
            Console.WriteLine("  " + label + "=" + culture.Name + ":" + culture.ToString()
                + " LCID=" + culture.LCID + " neutral=" + culture.IsNeutralCulture
                + " iso=" + culture.TwoLetterISOLanguageName + " readonly=" + culture.IsReadOnly
                + " n=" + (1234.5).ToString("N2", culture) + " currency=" + culture.NumberFormat.CurrencySymbol
                + " digits=" + culture.NumberFormat.NumberDecimalDigits
                + " symbols=" + culture.NumberFormat.NaNSymbol + "|" + culture.NumberFormat.PositiveInfinitySymbol
                + "|" + culture.NumberFormat.NegativeInfinitySymbol);
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
            RunFallbackIdentities();
            RunPrimaryCurrency();
            RunMissingNumberFormat();
            RunVariantOrder();
            RunInvariantLcidCache("lcid-empty-missing");
            RunInstanceNumberFormat();
            RunIntegerCultureConstructors();
            RunInstanceEquality();
            RunExtensionIdentities();
            RunUndefinedExtensionMetadata();
            RunUndefinedPlatformIdentities();
            RunRejectedExtensionIdentities();
            RunPlatformNameValidation();
        }

        private static void RunPlatformNameValidation()
        {
            Console.WriteLine("== culture platform name validation ==");
            foreach (string name in new[]
            {
                "de-FOO", "zh-min", "zh-min\0ignored", "de-FOO-u-nu-latn",
                "zh-min-u-nu-foo", "zh-min-nan", "de-foo-bar-US-u-nu-latn",
                "i-enochian-US-U-NU-ARAB", "i-enochian-US-u-nu-arab",
                "en-US", "de-DE_phoneb"
            })
            {
                Console.WriteLine("  input='" + name.Replace("\0", "<NUL>") + "'");
                CaptureCultureCreation("constructor", () => new CultureInfo(name));
                CaptureCultureCreation("without override", () => new CultureInfo(name, false));
                CaptureCultureCreation("cached", () => CultureInfo.GetCultureInfo(name));
                CaptureCultureCreation("specific", () => CultureInfo.CreateSpecificCulture(name));
                CaptureCultureCreation("constructor again", () => new CultureInfo(name));
            }
            Console.WriteLine("culture platform name validation end");
        }

        private static void CaptureCultureCreation(string label, Func<CultureInfo> create)
        {
            try
            {
                Console.WriteLine("    " + label + "=" + VariantCultureIdentity(create()));
            }
            catch (CultureNotFoundException exception)
            {
                Console.WriteLine("    " + label + "=" + nameof(CultureNotFoundException));
                WriteCultureFault(exception);
            }
        }

        private static bool TryCreateCulture(string name, out CultureInfo culture)
        {
            try
            {
                culture = new CultureInfo(name);
                return true;
            }
            catch (CultureNotFoundException exception)
            {
                culture = null;
                Console.WriteLine("  '" + name.Replace("\0", "<NUL>") + "': " + nameof(CultureNotFoundException));
                WriteCultureFault(exception);
                return false;
            }
        }

        private static string CultureName(Func<CultureInfo> create)
        {
            return CultureValue(() => create().Name);
        }

        private static bool TryCultureCreation(string label, Func<CultureInfo> create, out CultureInfo culture)
        {
            try
            {
                culture = create();
                return true;
            }
            catch (CultureNotFoundException exception)
            {
                culture = null;
                Console.WriteLine("    " + label + "=" + nameof(CultureNotFoundException));
                WriteCultureFault(exception);
                return false;
            }
        }

        private static string CultureValue(Func<string> read)
        {
            try
            {
                return read();
            }
            catch (CultureNotFoundException exception)
            {
                return nameof(CultureNotFoundException) + " param=" + exception.ParamName
                    + " name=" + exception.InvalidCultureName;
            }
        }

        private static void RunRejectedExtensionIdentities(string name = null)
        {
            Console.WriteLine("== culture rejected extension identities ==");
            foreach (string input in name is null ? new[]
            {
                "en-und-x-123456789", "en-und-x-lvariant", "en-und-u-a", "en-und-t-a", "en-und-a-a",
                "en-und-x-foo", "en-und-u-nu-latn", "en-und-t-de-DE", "en-und-a-foo",
                "en-und-a-foo-u-a", "en-und-u-ca-gregory-u-a",
                "en-und-Latn-x-123456789", "en-und-US-x-123456789", "en-und-abcde-x-123456789",
                "en-und", "en-und-US", "en-und-abcde", "und-u-a", "en-abc-u-a",
                "en-und-abc-def-ghi-a-foo", "en-und-abc-def-a-foo", "EN-und-U-A"
            } : new[] { name })
                RunExtensionIdentities(input);
            Console.WriteLine("culture rejected extension identities end");
        }

        private static void RunUndefinedPlatformIdentities(string name = null)
        {
            Console.WriteLine("== culture undefined platform identities ==");
            foreach (string input in name is null ? new[]
            {
                "", "und", "und-a-foo", "und-u-a", "und-t-a", "und-u-nu-latn",
                "und-U-nu-latn", "und-u-co-phonebk", "und-a-a", "und-a-a-u-nu-latn",
                "und-Latn-u-nu-latn", "und-US-u-nu-latn", "und-FOOBAR-u-nu-latn",
                "und-x-foo", "und-x-a", "und-x-foo-u-nu-latn", "und-x-lvariant", "und-x-foo-lvariant",
                "und-x-123456789", "und-a-foo-x-foo", "und-x-lvariant-FOOBAR", "en-und-a-foo"
            } : new[] { name })
            {
                try
                {
                    CultureInfo constructor = new CultureInfo(input);
                    CultureInfo withoutOverride = new CultureInfo(input, false);
                    CultureInfo cached = CultureInfo.GetCultureInfo(input);
                    CultureInfo specific = CultureInfo.CreateSpecificCulture(input);
                    Console.WriteLine("  input='" + input + "'");
                    Console.WriteLine("    constructor=" + UndefinedPlatformIdentity(constructor));
                    Console.WriteLine("    without override=" + UndefinedPlatformIdentity(withoutOverride));
                    Console.WriteLine("    cached=" + UndefinedPlatformIdentity(cached));
                    Console.WriteLine("    specific=" + UndefinedPlatformIdentity(specific));
                    Console.WriteLine("    constructor/cached=" + ReferenceEquals(constructor, cached)
                        + " repeat=" + ReferenceEquals(cached, CultureInfo.GetCultureInfo(input))
                        + " canonical=" + ReferenceEquals(cached, CultureInfo.GetCultureInfo(cached.Name)));
                }
                catch (CultureNotFoundException exception)
                {
                    Console.WriteLine("  input='" + input + "': " + nameof(CultureNotFoundException));
                    WriteCultureFault(exception);
                }
            }
            Console.WriteLine("culture undefined platform identities end");
        }

        private static string UndefinedPlatformIdentity(CultureInfo culture) => VariantCultureIdentity(culture)
            + " invariant=" + ReferenceEquals(culture, CultureInfo.InvariantCulture);

        private static void RunUndefinedPlatformCache(string firstName, string secondName, bool constructorFirst)
        {
            RunExtensionCacheOrder(firstName, secondName, constructorFirst);
            CultureInfo invariantLcid = CultureInfo.GetCultureInfo(127);
            CultureInfo emptyName = CultureInfo.GetCultureInfo("");
            Console.WriteLine("  invariant LCID=" + UndefinedPlatformIdentity(invariantLcid)
                + " repeat=" + ReferenceEquals(invariantLcid, CultureInfo.GetCultureInfo(127)));
            Console.WriteLine("  empty name=" + UndefinedPlatformIdentity(emptyName)
                + " invariant LCID=" + ReferenceEquals(emptyName, invariantLcid));
            Console.WriteLine("culture undefined platform cache end");
        }

        private static void RunUndefinedExtensionMetadata(string name = null)
        {
            Console.WriteLine("== culture undefined extension metadata ==");
            foreach (string input in name is null ? new[]
            {
                "und", "und-u-a", "und-t-a", "und-u-123456789", "und-a-a", "und-a-123456789"
            } : new[] { name })
            {
                try
                {
                    CultureInfo constructor = new CultureInfo(input);
                    CultureInfo withoutOverride = new CultureInfo(input, false);
                    CultureInfo cached = CultureInfo.GetCultureInfo(input);
                    CultureInfo specific = CultureInfo.CreateSpecificCulture(input);
                    Console.WriteLine("  input='" + input + "'");
                    Console.WriteLine("    constructor=" + UndefinedCultureMetadata(constructor));
                    Console.WriteLine("    without override=" + UndefinedCultureMetadata(withoutOverride));
                    Console.WriteLine("    cached=" + UndefinedCultureMetadata(cached));
                    Console.WriteLine("    specific=" + UndefinedCultureMetadata(specific));
                    Console.WriteLine("    constructor/cached=" + ReferenceEquals(constructor, cached)
                        + " cached repeat=" + ReferenceEquals(cached, CultureInfo.GetCultureInfo(input)));
                }
                catch (CultureNotFoundException exception)
                {
                    Console.WriteLine("  input='" + input + "': " + nameof(CultureNotFoundException));
                    WriteCultureFault(exception);
                }
            }
            Console.WriteLine("culture undefined extension metadata end");
        }

        private static string UndefinedCultureMetadata(CultureInfo culture) => "LCID=" + culture.LCID
            + " neutral=" + culture.IsNeutralCulture + " invariant=" + ReferenceEquals(culture, CultureInfo.InvariantCulture);

        private static void RunExtensionIdentities(string name = null)
        {
            Console.WriteLine("== culture extension identities ==");
            foreach (string input in name is null ? new[]
            {
                "en-US-a-foo", "en-US-x-foo", "en-US-b-foo", "en-US-0-foo", "en-US-a-foo-b-bar",
                "en-US-u-nu-latn", "en-US-u-ca-gregory", "en-US-u-foo", "en-US-u-ca",
                "en-US-u-1234", "en-US-u-00", "en-US-u", "en-US-u-a", "en-US-u-123456789",
                "en-US-t-de-DE", "en-US-t-foo", "en-US-t-a", "en-US-t",
                "en-US-a-foo-u-nu-latn", "en-US-x-foo-u-nu-latn", "en-US-a-foo-u-a",
                "en-US-a-foo-t-de-DE", "en-US-u-ca-gregory-123456789", "en-US-t-de-DE-123456789",
                "en-US-u-nu-latn-u-ca-gregory", "en-US-a-foo-a-bar-u-nu-latn",
                "en-US-U-NU-ARAB", "en-US-T-de-DE", "en-US-A-FOO", "de-DE-a-foo",
                "de-Latn-DE-a-foo", "i-klingon-a-foo", "en-GB-oed-a-foo", "en-US-POSIX-a-foo",
                "en-US-abcde", "en-US-zzzzz-abcde", "de-DE_phoneb",
                "en-US-x-foo-u-a", "en-US-u-a0", "en-US-u-aa", "en-US-U-ca-gregory-u-nu-latn",
                "en-US-a-foo-a-bar-u-a", "en-US-u-co-phoneb-a-foo",
                "de-DE-u-co-phonebk-nu-arab", "en-US-a-foo-u-co-phoneb",
                "en-US-x-lvariant-foo", "en-US-x-foo-lvariant-bar",
                "en-US-abcde-x-lvariant-abcde", "en-US-POSIX-x-lvariant-foo",
                "en-US-zzzzz-x-lvariant-abcde", "en-US-abcde-x-lvariant-ABCDE",
                "en-US-abcde-x-lvariant-abcde-bar", "en-US-x-lvariant-foo-foo-bar-foo",
                "en-US-zzzzz-abcde-x-lvariant-abcde-bar-zzzzz", "en-US-POSIX-x-lvariant-POSIX-bar",
                "en-US-POSIX-x-foo-lvariant-POSIX-bar", "en-US-POSIX-a-foo-x-lvariant-POSIX-bar",
                "en-US-POSIX-u-nu-arab-x-lvariant-POSIX-bar"
            } : new[] { name })
            {
                try
                {
                    CultureInfo constructor = new CultureInfo(input);
                    CultureInfo withoutOverride = new CultureInfo(input, false);
                    CultureInfo cached = CultureInfo.GetCultureInfo(input);
                    Console.WriteLine("  input='" + input + "'");
                    Console.WriteLine("    constructor=" + VariantCultureIdentity(constructor));
                    Console.WriteLine("    without override=" + VariantCultureIdentity(withoutOverride));
                    Console.WriteLine("    cached=" + VariantCultureIdentity(cached));
                    Console.WriteLine("    specific=" + VariantCultureIdentity(CultureInfo.CreateSpecificCulture(input)));
                    Console.WriteLine("    repeat=" + ReferenceEquals(cached, CultureInfo.GetCultureInfo(input))
                        + " canonical=" + ReferenceEquals(cached, CultureInfo.GetCultureInfo(cached.Name)));
                }
                catch (CultureNotFoundException exception)
                {
                    Console.WriteLine("  input='" + input + "': " + nameof(CultureNotFoundException));
                    WriteCultureFault(exception);
                }
            }
            Console.WriteLine("culture extension identities end");
        }

        private static void RunExtensionCacheOrder(string firstName, string secondName, bool constructorFirst)
        {
            CultureInfo constructor;
            CultureInfo second;
            if (constructorFirst)
            {
                TryCultureCreation("constructor", () => new CultureInfo(firstName, false), out constructor);
                TryCultureCreation("second", () => CultureInfo.GetCultureInfo(secondName), out second);
            }
            else
            {
                TryCultureCreation("second", () => CultureInfo.GetCultureInfo(secondName), out second);
                TryCultureCreation("constructor", () => new CultureInfo(firstName, false), out constructor);
            }
            TryCultureCreation("first", () => CultureInfo.GetCultureInfo(firstName), out CultureInfo first);
            Console.WriteLine("== culture extension cache order ==");
            Console.WriteLine("  first='" + firstName + "' second='" + secondName + "' constructor first=" + constructorFirst);
            if (constructor is not null)
                Console.WriteLine("  constructor=" + VariantCultureIdentity(constructor));
            if (first is not null && second is not null)
            {
                Console.WriteLine("  first=" + VariantCultureIdentity(first) + " second=" + VariantCultureIdentity(second));
                Console.WriteLine("  pair=" + ReferenceEquals(first, second)
                    + " first repeat=" + ReferenceEquals(first, CultureInfo.GetCultureInfo(firstName))
                    + " second repeat=" + ReferenceEquals(second, CultureInfo.GetCultureInfo(secondName)));
                Console.WriteLine("  first canonical=" + ReferenceEquals(first, CultureInfo.GetCultureInfo(first.Name))
                    + " second canonical=" + ReferenceEquals(second, CultureInfo.GetCultureInfo(second.Name)));
            }
            Console.WriteLine("culture extension cache order end");
        }

        private static void RunInstanceEquality(string leftName = null, string rightName = null)
        {
            Console.WriteLine("== culture instance equality ==");
            string[] leftNames = leftName is null ? new[]
            {
                "en-US", "en-US", "de-DE", "de-DE_phoneb", "i-enochian", "i-enochian", "i-klingon",
                "en-US-u-nu-latn", "en-US-u-nu-latn", "en-US-POSIX-abcde-u-nu-latn"
            } : new[] { leftName };
            string[] rightNames = leftName is null ? new[]
            {
                "en-US", "EN-us", "de-DE_phoneb", "DE-de_PHONEB", "", "i-enochian", "tlh",
                "en-US-u-nu-LATN", "en-US-u-nu-arab", "en-US-abcde-POSIX-u-nu-latn"
            } : new[] { rightName };
            for (int i = 0; i < leftNames.Length; i++)
            {
                CultureInfo left = new CultureInfo(leftNames[i]);
                CultureInfo right = new CultureInfo(rightNames[i]);
                object boxedLeft = left;
                object boxedRight = right;
                Console.WriteLine("  pair='" + leftNames[i] + "'/'" + rightNames[i] + "'");
                Console.WriteLine("    identity=" + left.Name + "/" + right.Name + " value=" + left + "/" + right);
                Console.WriteLine("    reference=" + ReferenceEquals(left, right));
                Console.WriteLine("    equals=" + object.Equals(left, right) + "/" + boxedLeft.Equals(boxedRight)
                    + "/" + EqualityComparer<CultureInfo>.Default.Equals(left, right));
                Console.WriteLine("    self/null/other=" + boxedLeft.Equals(boxedLeft) + "/" + boxedLeft.Equals(null)
                    + "/" + boxedLeft.Equals(leftNames[i]));
                Console.WriteLine("    equal hashes=" + (object.Equals(left, right)
                    ? (boxedLeft.GetHashCode() == boxedRight.GetHashCode()).ToString() : "<unequal>")
                    + "/" + (EqualityComparer<CultureInfo>.Default.Equals(left, right)
                    ? (EqualityComparer<CultureInfo>.Default.GetHashCode(left) == EqualityComparer<CultureInfo>.Default.GetHashCode(right)).ToString()
                    : "<unequal>"));
                CaptureInstanceNumber("NFI reference", () => ReferenceEquals(left.NumberFormat, right.NumberFormat).ToString());
                CaptureInstanceNumber("NFI equals", () => object.Equals(left.NumberFormat, right.NumberFormat)
                    + "/" + ((object)left.NumberFormat).Equals((object)right.NumberFormat)
                    + "/" + EqualityComparer<NumberFormatInfo>.Default.Equals(left.NumberFormat, right.NumberFormat));
                TextInfo leftText = left.TextInfo;
                TextInfo rightText = right.TextInfo;
                object boxedLeftText = leftText;
                object boxedRightText = rightText;
                Console.WriteLine("    text reference=" + ReferenceEquals(leftText, rightText));
                Console.WriteLine("    text equals=" + object.Equals(leftText, rightText) + "/" + boxedLeftText.Equals(boxedRightText)
                    + "/" + EqualityComparer<TextInfo>.Default.Equals(leftText, rightText));
                Console.WriteLine("    text null/other=" + boxedLeftText.Equals(null) + "/" + boxedLeftText.Equals(left));
                Console.WriteLine("    text equal hashes=" + (object.Equals(leftText, rightText)
                    ? (boxedLeftText.GetHashCode() == boxedRightText.GetHashCode()).ToString() : "<unequal>"));
                if (leftNames[i] == "en-US" && rightNames[i] == "en-US")
                {
                    Dictionary<CultureInfo, int> cultures = new Dictionary<CultureInfo, int>();
                    cultures[left] = 1;
                    cultures[right] = 2;
                    Console.WriteLine("    culture dictionary=" + cultures.Count + ":" + cultures[left] + ":" + cultures[right]);
                    HashSet<CultureInfo> cultureSet = new HashSet<CultureInfo>();
                    Console.WriteLine("    culture set=" + cultureSet.Add(left) + ":" + cultureSet.Add(right) + ":" + cultureSet.Count);
                    Dictionary<TextInfo, int> texts = new Dictionary<TextInfo, int>();
                    texts[leftText] = 1;
                    texts[rightText] = 2;
                    Console.WriteLine("    text dictionary=" + texts.Count + ":" + texts[leftText] + ":" + texts[rightText]);
                    HashSet<TextInfo> textSet = new HashSet<TextInfo>();
                    Console.WriteLine("    text set=" + textSet.Add(leftText) + ":" + textSet.Add(rightText) + ":" + textSet.Count);
                    HashSet<NumberFormatInfo> numberSet = new HashSet<NumberFormatInfo>();
                    Console.WriteLine("    NFI set=" + numberSet.Add(left.NumberFormat) + ":" + numberSet.Add(right.NumberFormat) + ":" + numberSet.Count);
                }
            }
            Console.WriteLine("== culture specific instances ==");
            foreach (string name in new[] { "en-US", "en", "" })
            {
                CultureInfo specific = CultureInfo.CreateSpecificCulture(name);
                CultureInfo repeated = CultureInfo.CreateSpecificCulture(name);
                WriteInstanceCulture("specific '" + name + "'", specific);
                Console.WriteLine("    specific reference=" + ReferenceEquals(specific, repeated)
                    + " invariant=" + ReferenceEquals(specific, CultureInfo.InvariantCulture)
                    + " NFI reference=" + ReferenceEquals(specific.NumberFormat, repeated.NumberFormat));
                Console.WriteLine("    specific number=" + InstanceNumberData(specific.NumberFormat));
                NumberFormatInfo number = new NumberFormatInfo { NumberDecimalSeparator = "@" };
                specific.NumberFormat = number;
                Console.WriteLine("    specific assignment=" + ReferenceEquals(specific.NumberFormat, number)
                    + " sibling=" + InstanceNumberData(repeated.NumberFormat));
            }
            Console.WriteLine("culture specific instances end");
            Console.WriteLine("culture instance equality end");
        }

        private static void RunInstanceNumberFormat(string name = null)
        {
            Console.WriteLine("== culture instance number format ==");
            foreach (string input in name is null ? new[] { "i-enochian", "en-US", "en", "" } : new[] { name })
            {
                Console.WriteLine("  input='" + input + "'");
                CultureInfo culture = new CultureInfo(input);
                NumberFormatInfo custom = new NumberFormatInfo
                {
                    NumberDecimalSeparator = "@", NumberGroupSeparator = "_", NegativeSign = "~"
                };
                WriteInstanceCulture("initial", culture);
                NumberFormatInfo previous = null;
                CaptureInstanceNumber("initial number", () =>
                {
                    previous = culture.NumberFormat;
                    return InstanceNumberData(previous) + ":stable=" + ReferenceEquals(previous, culture.NumberFormat)
                        + ":sibling=" + ReferenceEquals(previous, new CultureInfo(input).NumberFormat);
                });
                CaptureInstanceNumber("writable null", () => { culture.NumberFormat = null; return "ok"; });
                culture.NumberFormat = custom;
                WriteInstanceNumberRoutes("assigned", culture, custom);
                if (previous is not null)
                {
                    Console.WriteLine("    old number=" + InstanceNumberData(previous) + ":same=" + ReferenceEquals(previous, custom));
                    previous.NumberDecimalSeparator = "%";
                    Console.WriteLine("    old mutation=" + InstanceNumberData(previous) + "/" + InstanceNumberData(custom));
                }
                CaptureInstanceNumber("sibling number", () => InstanceNumberData(new CultureInfo(input).NumberFormat));
                CaptureInstanceNumber("cached number", () => InstanceNumberData(CultureInfo.GetCultureInfo(input).NumberFormat));
                CaptureInstanceNumber("double format", () => (-1234.5).ToString("N2", culture));
                CaptureInstanceNumber("decimal format", () => (-1234.5m).ToString("N2", culture));
                CaptureInstanceNumber("integer format", () => (-12).ToString(culture));
                CaptureInstanceNumber("double parse", () => double.Parse("~12@5", culture).ToString(CultureInfo.InvariantCulture));
                CaptureInstanceNumber("decimal parse", () => decimal.Parse("~12@5", culture).ToString(CultureInfo.InvariantCulture));
                CaptureInstanceNumber("integer parse", () => int.Parse("~12", culture).ToString(CultureInfo.InvariantCulture));
                CultureInfo savedCulture = CultureInfo.CurrentCulture;
                CultureInfo savedUI = CultureInfo.CurrentUICulture;
                try
                {
                    CultureInfo.CurrentCulture = culture;
                    CultureInfo.CurrentUICulture = culture;
                    Console.WriteLine("    current same=" + ReferenceEquals(CultureInfo.CurrentCulture, culture)
                        + " UI same=" + ReferenceEquals(CultureInfo.CurrentUICulture, culture));
                    Console.WriteLine("    current info=" + ReferenceEquals(NumberFormatInfo.CurrentInfo, custom)
                        + " UI number=" + ReferenceEquals(CultureInfo.CurrentUICulture.NumberFormat, custom));
                    Console.WriteLine("    current format=" + (-12).ToString());
                }
                finally
                {
                    CultureInfo.CurrentCulture = savedCulture;
                    CultureInfo.CurrentUICulture = savedUI;
                }

                CultureInfo clone = (CultureInfo)culture.Clone();
                WriteInstanceCulture("clone", clone);
                Console.WriteLine("    clone same=" + ReferenceEquals(clone, culture)
                    + " number same=" + ReferenceEquals(clone.NumberFormat, custom));
                Console.WriteLine("    clone number=" + InstanceNumberData(clone.NumberFormat));
                custom.NumberDecimalSeparator = "#";
                Console.WriteLine("    source mutation=" + InstanceNumberData(custom) + "/" + InstanceNumberData(clone.NumberFormat));
                clone.NumberFormat.NumberDecimalSeparator = "%";
                Console.WriteLine("    clone mutation=" + InstanceNumberData(custom) + "/" + InstanceNumberData(clone.NumberFormat));
                custom.NumberDecimalSeparator = "@";

                CultureInfo readOnly = CultureInfo.ReadOnly(culture);
                WriteInstanceCulture("readonly", readOnly);
                Console.WriteLine("    readonly same=" + ReferenceEquals(readOnly, culture)
                    + " repeat=" + ReferenceEquals(readOnly, CultureInfo.ReadOnly(readOnly))
                    + " number same=" + ReferenceEquals(readOnly.NumberFormat, custom));
                Console.WriteLine("    readonly number=" + InstanceNumberData(readOnly.NumberFormat));
                custom.NumberDecimalSeparator = "#";
                Console.WriteLine("    readonly source mutation=" + InstanceNumberData(custom) + "/" + InstanceNumberData(readOnly.NumberFormat));
                ValidateInstanceAssignment("readonly", readOnly, custom);
                CultureInfo readOnlyClone = (CultureInfo)readOnly.Clone();
                WriteInstanceCulture("readonly clone", readOnlyClone);
                Console.WriteLine("    readonly clone number same=" + ReferenceEquals(readOnlyClone.NumberFormat, readOnly.NumberFormat)
                    + " data=" + InstanceNumberData(readOnlyClone.NumberFormat));
                readOnlyClone.NumberFormat = custom;
                Console.WriteLine("    readonly clone assignment=" + ReferenceEquals(readOnlyClone.NumberFormat, custom));

                CultureInfo lazy = new CultureInfo(input);
                CultureInfo lazyClone = (CultureInfo)lazy.Clone();
                CultureInfo lazyReadOnly = CultureInfo.ReadOnly(lazy);
                WriteInstanceCulture("lazy clone", lazyClone);
                WriteInstanceCulture("lazy readonly", lazyReadOnly);
                CaptureInstanceNumber("lazy clone number", () => InstanceNumberData(lazyClone.NumberFormat));
                CaptureInstanceNumber("lazy readonly number", () => InstanceNumberData(lazyReadOnly.NumberFormat));
                CaptureInstanceNumber("lazy number refs", () => ReferenceEquals(lazy.NumberFormat, lazyClone.NumberFormat)
                    + "/" + ReferenceEquals(lazy.NumberFormat, lazyReadOnly.NumberFormat));
                lazyClone.NumberFormat = custom;
                Console.WriteLine("    lazy clone assignment=" + ReferenceEquals(lazyClone.NumberFormat, custom));

                NumberFormatInfo frozenNumber = NumberFormatInfo.ReadOnly(custom);
                Console.WriteLine("    NFI readonly=" + InstanceNumberData(frozenNumber) + ":same=" + ReferenceEquals(frozenNumber, custom)
                    + ":repeat=" + ReferenceEquals(frozenNumber, NumberFormatInfo.ReadOnly(frozenNumber)));
                culture.NumberFormat = frozenNumber;
                WriteInstanceNumberRoutes("readonly NFI assigned", culture, frozenNumber);
                Console.WriteLine("    readonly assigned NFI wrapper same="
                    + ReferenceEquals(CultureInfo.ReadOnly(culture).NumberFormat, frozenNumber));
                NumberFormatInfo clonedNumber = (NumberFormatInfo)frozenNumber.Clone();
                clonedNumber.NumberDecimalSeparator = "%";
                Console.WriteLine("    NFI clone=" + InstanceNumberData(clonedNumber) + ":same=" + ReferenceEquals(clonedNumber, frozenNumber));
                CultureInfo frozenClone = (CultureInfo)culture.Clone();
                Console.WriteLine("    assigned readonly NFI clone=" + InstanceNumberData(frozenClone.NumberFormat)
                    + ":same=" + ReferenceEquals(frozenClone.NumberFormat, frozenNumber));
                CaptureInstanceNumber("readonly decimal null", () => { frozenNumber.NumberDecimalSeparator = null; return "ok"; });
                CaptureInstanceNumber("readonly decimal empty", () => { frozenNumber.NumberDecimalSeparator = ""; return "ok"; });
                CaptureInstanceNumber("readonly decimal valid", () => { frozenNumber.NumberDecimalSeparator = "!"; return "ok"; });
                CaptureInstanceNumber("readonly sign null", () => { frozenNumber.NegativeSign = null; return "ok"; });
                CaptureInstanceNumber("readonly currency digits invalid", () => { frozenNumber.CurrencyDecimalDigits = -1; return "ok"; });
                CaptureInstanceNumber("readonly groups null", () => { frozenNumber.NumberGroupSizes = null; return "ok"; });
                CaptureInstanceNumber("readonly groups invalid", () => { frozenNumber.NumberGroupSizes = new[] { -1 }; return "ok"; });

                culture.NumberFormat = NumberFormatInfo.InvariantInfo;
                WriteInstanceNumberRoutes("invariant assigned", culture, NumberFormatInfo.InvariantInfo);
                Console.WriteLine("    retained clone=" + InstanceNumberData(clone.NumberFormat)
                    + " readonly=" + InstanceNumberData(readOnly.NumberFormat));
                ValidateInstanceAssignment("cached", CultureInfo.GetCultureInfo(input), custom);
                CultureInfo cachedClone = (CultureInfo)CultureInfo.GetCultureInfo(input).Clone();
                WriteInstanceCulture("cached clone", cachedClone);
                cachedClone.NumberFormat = custom;
                Console.WriteLine("    cached clone assignment=" + ReferenceEquals(cachedClone.NumberFormat, custom));
            }
            NumberFormatInfo value = new NumberFormatInfo();
            ValidateInstanceAssignment("invariant", CultureInfo.InvariantCulture, value);
            ValidateInstanceAssignment("integer invariant", CultureInfo.GetCultureInfo(127), value);
            ValidateInstanceAssignment("integer cached", CultureInfo.GetCultureInfo(1033), value);
            CaptureInstanceNumber("ReadOnly null", () => CultureInfo.ReadOnly(null).Name);
            Console.WriteLine("culture instance number format end");
        }

        private static string InstanceNumberData(NumberFormatInfo number) => number.NumberDecimalSeparator + ":"
            + number.NumberGroupSeparator + ":" + number.NegativeSign + ":" + number.NumberDecimalDigits
            + ":readonly=" + number.IsReadOnly;

        private static void WriteInstanceCulture(string label, CultureInfo culture) => Console.WriteLine("    " + label
            + "=" + MissingCultureIdentity(culture) + ":readonly=" + culture.IsReadOnly);

        private static void WriteInstanceNumberRoutes(string label, CultureInfo culture, NumberFormatInfo expected)
        {
            Console.WriteLine("    " + label + "=" + InstanceNumberData(culture.NumberFormat)
                + ":same=" + ReferenceEquals(culture.NumberFormat, expected));
            Console.WriteLine("    " + label + " routes=" + ReferenceEquals(culture.GetFormat(typeof(NumberFormatInfo)), expected)
                + "/" + ReferenceEquals(((IFormatProvider)culture).GetFormat(typeof(NumberFormatInfo)), expected)
                + "/" + ReferenceEquals(NumberFormatInfo.GetInstance(culture), expected)
                + "/" + ReferenceEquals(NumberFormatInfo.GetInstance((IFormatProvider)culture), expected));
        }

        private static void ValidateInstanceAssignment(string label, CultureInfo culture, NumberFormatInfo value)
        {
            WriteInstanceCulture(label, culture);
            CaptureInstanceNumber(label + " null", () => { culture.NumberFormat = null; return "ok"; });
            CaptureInstanceNumber(label + " nonnull", () => { culture.NumberFormat = value; return "ok"; });
        }

        private static void CaptureInstanceNumber(string label, Func<string> action)
        {
            try
            {
                Console.WriteLine("    " + label + "=" + action());
            }
            catch (ArgumentException exception)
            {
                Console.WriteLine("    " + label + "=" + exception.GetType().Name + ":" + exception.ParamName);
            }
            catch (InvalidOperationException)
            {
                Console.WriteLine("    " + label + "=" + nameof(InvalidOperationException));
            }
            catch (NullReferenceException)
            {
                Console.WriteLine("    " + label + "=" + nameof(NullReferenceException));
            }
        }

        private static void RunIntegerCultureConstructors()
        {
            Console.WriteLine("== culture integer constructors ==");
            foreach (int lcid in new[] { 127, 1033, -1, 0, 4096, 65535 })
            {
                foreach (string option in new[] { "default", "true", "false" })
                {
                    string label = lcid + ":" + option;
                    try
                    {
                        CultureInfo culture = option == "default" ? new CultureInfo(lcid) : new CultureInfo(lcid, option == "true");
                        WriteInstanceCulture(label, culture);
                        Console.WriteLine("    number=" + InstanceNumberData(culture.NumberFormat)
                            + ":stable=" + ReferenceEquals(culture.NumberFormat, culture.NumberFormat)
                            + ":sibling=" + ReferenceEquals(culture.NumberFormat, new CultureInfo(lcid).NumberFormat));
                    }
                    catch (CultureNotFoundException exception)
                    {
                        Console.WriteLine("    " + label + "=" + nameof(CultureNotFoundException) + ":" + exception.ParamName
                            + ":id=" + exception.InvalidCultureId.Value + ":name=" + (exception.InvalidCultureName ?? "<null>"));
                    }
                    catch (ArgumentOutOfRangeException exception)
                    {
                        Console.WriteLine("    " + label + "=" + nameof(ArgumentOutOfRangeException) + ":" + exception.ParamName);
                    }
                }
            }
            Console.WriteLine("culture integer constructors end");
        }

        private static void RunInvariantLcidCache(string lookupOrder)
        {
            Console.WriteLine("== culture invariant lcid cache ==");
            Console.WriteLine("  order=" + lookupOrder);
            string[] order = lookupOrder.Split('-');
            CultureInfo[] cultures = new CultureInfo[order.Length];
            for (int i = 0; i < order.Length; i++)
            {
                cultures[i] = GetInvariantCacheCulture(order[i]);
                WriteInvariantCacheCulture(order[i], cultures[i]);
            }
            for (int i = 0; i < order.Length; i++)
            {
                CultureInfo repeated = GetInvariantCacheCulture(order[i]);
                WriteInvariantCacheCulture(order[i] + " repeat", repeated);
                Console.WriteLine("  " + order[i] + " same=" + ReferenceEquals(cultures[i], repeated)
                    + " invariant=" + ReferenceEquals(repeated, CultureInfo.InvariantCulture));
                for (int j = i + 1; j < order.Length; j++)
                    Console.WriteLine("  " + order[i] + "/" + order[j] + "=" + ReferenceEquals(cultures[i], cultures[j]));
            }
            WriteInvariantCacheCulture("invariant", CultureInfo.InvariantCulture);
            Console.WriteLine("culture invariant lcid cache end");
        }

        private static CultureInfo GetInvariantCacheCulture(string key)
        {
            if (key == "lcid")
                return CultureInfo.GetCultureInfo(127);
            if (key == "empty")
                return CultureInfo.GetCultureInfo("");
            return CultureInfo.GetCultureInfo("i-enochian");
        }

        private static void WriteInvariantCacheCulture(string label, CultureInfo culture)
        {
            Console.WriteLine("  " + label + "=" + MissingCultureIdentity(culture) + ":value='" + culture.ToString() + "'");
            CaptureMissingNumber(label + " number", () => MissingNumberData(culture.NumberFormat)
                + ":invariant=" + ReferenceEquals(culture.NumberFormat, NumberFormatInfo.InvariantInfo));
            CaptureMissingNumber(label + " format", () => (1234.5).ToString("N2", culture));
        }

        private static void RunVariantOrder(string name = null)
        {
            Console.WriteLine("== culture variant order ==");
            foreach (string input in name is null ? new[]
            {
                "en-US-POSIX-abcde-abcde-u-nu-arab", "en-US-abcde-POSIX-abcde-u-nu-arab",
                "en-US-POSIX-abcde-u-nu-arab", "en-US-abcde-POSIX-u-nu-arab",
                "en-US-zzzzz-POSIX-u-nu-latn", "en-US-POSIX-zzzzz-u-nu-latn",
                "en-US-POSIX-1234-u-ca-gregory", "en-US-1234-POSIX-u-ca-gregory",
                "en-US-POSIX-abcde-ab-u-nu-arab", "en-US-POSIX-abcde-abcdefghi-u-nu-arab",
                "en-US-POSIX-abcde--u-nu-arab", "en-US-POSIX-abcde-ABCDE-u-nu-arab",
                "en-US-POSIX-abcde-x-foo", "en-US-POSIX-abcde-a-foo",
                "en-US-POSIX-abcde-t-de-DE", "en-US-POSIX-abcde-u-ca-gregory",
                "en-US-POSIX-abcde-U-NU-ARAB", "en-US-POSIX-abcde-T-de-DE",
                "en-US-POSIX-abcde-t-de-DE-123456789", "en-US-POSIX-abcde-u-ca-gregory-123456789",
                "en-US-POSIX-abcde-a-foo-u-nu-latn", "en-US-POSIX-abcde-x-foo-u-nu-latn",
                "en-US-POSIX-abcde", "en-US-abcde-POSIX", "en-US-POSIX-abcde-abcde",
                "en-US-POSIX-1234", "en-US-1234-POSIX",
                "de-DE-zzzzz-abcde-u-ca-gregory", "de-DE-abcde-zzzzz-u-ca-gregory",
                "de-DE-zzzzz-abcde", "de-DE-abcde-zzzzz", "en-US-zzzzz-abcde-u-nu-latn",
                "en-US-POSIX-abcde-u-a", "de-Latn-DE-zzzzz-abcde-u-nu-latn",
                "de-foo-US-zzzzz-abcde-u-nu-latn", "i-klingon-Latn-US-zzzzz-abcde-u-nu-latn",
                "de-foo-US-zzzzz-abcde-abcde-u-nu-latn",
                "en-GB-oed-zzzzz-abcde-u-nu-latn", "en-US-zzzzz-abcde_phoneb",
                "en-GB-oed-zzzzz-abcde-abcde-u-nu-latn",
                "en-GB-oed-zzzzz-abcde-u-a", "en-GB-oed-zzzzz-abcde-x-foo",
                "i-klingon-US-zzzzz-abcde"
            } : new[] { name })
            {
                Console.WriteLine("  '" + input + "'");
                try
                {
                    CultureInfo culture = new CultureInfo(input);
                    CultureInfo cached = CultureInfo.GetCultureInfo(input);
                    Console.WriteLine("    constructor=" + VariantCultureIdentity(culture));
                    Console.WriteLine("    without override=" + VariantCultureIdentity(new CultureInfo(input, false)));
                    Console.WriteLine("    cached=" + VariantCultureIdentity(cached));
                    Console.WriteLine("    specific=" + VariantCultureIdentity(CultureInfo.CreateSpecificCulture(input)));
                    Console.WriteLine("    repeat=" + ReferenceEquals(cached, CultureInfo.GetCultureInfo(input))
                        + " canonical=" + ReferenceEquals(cached, CultureInfo.GetCultureInfo(cached.Name)));
                }
                catch (CultureNotFoundException)
                {
                    Console.WriteLine("    " + nameof(CultureNotFoundException));
                }
            }
            Console.WriteLine("culture variant order end");
        }

        private static string VariantCultureIdentity(CultureInfo culture) => "'" + culture.Name
            + "' value='" + culture.ToString() + "' LCID=" + culture.LCID + " neutral=" + culture.IsNeutralCulture;

        private static void RunMissingNumberFormat(string caseName = null)
        {
            Console.WriteLine("== culture missing number format ==");
            foreach (string kind in caseName is null ? new[] { "bare", "nul", "upper", "lower", "invariant", "und" }
                : new[] { caseName })
            {
                string name = kind == "bare" ? "i-enochian" : kind == "nul" ? "i-enochian\0ignored"
                    : kind == "upper" ? "i-enochian-US-U-NU-ARAB" : kind == "lower" ? "i-enochian-US-u-nu-arab"
                    : kind == "und" ? "und" : "";
                if (!TryCreateCulture(name, out CultureInfo culture))
                    continue;
                Console.WriteLine("  " + kind + " '" + name.Replace("\0", "<NUL>") + "'=" + MissingCultureIdentity(culture));
                CaptureMissingNumber("getter", () => MissingNumberData(culture.NumberFormat));
                CaptureMissingNumber("getformat number", () => MissingNumberData((NumberFormatInfo)culture.GetFormat(typeof(NumberFormatInfo))));
                IFormatProvider provider = culture;
                CaptureMissingNumber("interface getformat number", () => MissingNumberData((NumberFormatInfo)provider.GetFormat(typeof(NumberFormatInfo))));
                CaptureMissingNumber("getformat string", () => culture.GetFormat(typeof(string)) is null ? "null" : "value");
                CaptureMissingNumber("getformat null", () => culture.GetFormat(null) is null ? "null" : "value");
                foreach (string format in new[] { "G", "N2" })
                {
                    CaptureMissingNumber("double " + format, () => (-12.5).ToString(format, culture));
                    CaptureMissingNumber("decimal " + format, () => (-12.5m).ToString(format, culture));
                    CaptureMissingNumber("int positive " + format, () => 12.ToString(format, culture));
                    CaptureMissingNumber("int negative " + format, () => (-12).ToString(format, culture));
                }
                object boxed = culture;
                CaptureMissingNumber("getinstance direct", () => MissingNumberData(NumberFormatInfo.GetInstance(culture)));
                CaptureMissingNumber("getinstance boxed", () => MissingNumberData(NumberFormatInfo.GetInstance((IFormatProvider)boxed)));
                CaptureMissingNumber("boxed provider", () => (-12.5).ToString("N2", (IFormatProvider)boxed));
                CaptureMissingNumber("int positive provider", () => 12.ToString(provider));
                CaptureMissingNumber("int negative provider", () => (-12).ToString(provider));
                CaptureMissingNumber("parse double", () => double.TryParse("12", NumberStyles.Float, culture, out double value) + ":" + value);
                CaptureMissingNumber("parse decimal", () => decimal.TryParse("12", NumberStyles.Float, culture, out decimal value) + ":" + value);
                CaptureMissingNumber("parse int", () => int.TryParse("12", NumberStyles.Integer, culture, out int value) + ":" + value);
                foreach (string input in new string[] { "12", null })
                {
                    string label = input is null ? "null" : input;
                    decimal decimalOut = 123m;
                    int intOut = 123;
                    double doubleOut = 123.0;
                    CaptureMissingNumber("sentinel decimal " + label, () => decimal.TryParse(input, NumberStyles.Float, culture, out decimalOut).ToString());
                    Console.WriteLine("    decimal out=" + decimalOut);
                    CaptureMissingNumber("sentinel int " + label, () => int.TryParse(input, NumberStyles.Integer, culture, out intOut).ToString());
                    Console.WriteLine("    int out=" + intOut);
                    CaptureMissingNumber("sentinel double " + label, () => double.TryParse(input, NumberStyles.Float, culture, out doubleOut).ToString());
                    Console.WriteLine("    double out=" + doubleOut);
                }
                foreach (string input in new string[] { null, "" })
                {
                    string label = input is null ? "null" : "empty";
                    CaptureMissingNumber("try double " + label, () => double.TryParse(input, NumberStyles.Float, culture, out double value) + ":" + value);
                    CaptureMissingNumber("try decimal " + label, () => decimal.TryParse(input, NumberStyles.Float, culture, out decimal value) + ":" + value);
                    CaptureMissingNumber("try int " + label, () => int.TryParse(input, NumberStyles.Integer, culture, out int value) + ":" + value);
                    CaptureMissingNumber("boxed try double " + label, () => double.TryParse(input, NumberStyles.Float, (IFormatProvider)boxed, out double value) + ":" + value);
                }
                CaptureMissingNumber("parse double null", () => double.Parse((string)null, NumberStyles.Float, culture).ToString(CultureInfo.InvariantCulture));
                CaptureMissingNumber("parse decimal null", () => decimal.Parse((string)null, NumberStyles.Float, culture).ToString(CultureInfo.InvariantCulture));
                CaptureMissingNumber("parse int null", () => int.Parse((string)null, NumberStyles.Integer, culture).ToString(CultureInfo.InvariantCulture));
                foreach (NumberStyles style in new[] { NumberStyles.HexNumber, NumberStyles.AllowBinarySpecifier })
                {
                    string input = style == NumberStyles.HexNumber ? "12" : "10";
                    CaptureMissingNumber("try int " + style, () => int.TryParse(input, style, culture, out int value) + ":" + value);
                    CaptureMissingNumber("parse int " + style, () => int.Parse(input, style, culture).ToString(CultureInfo.InvariantCulture));
                    CaptureMissingNumber("boxed try int " + style, () => int.TryParse(input, style, (IFormatProvider)boxed, out int value) + ":" + value);
                    CaptureMissingNumber("try uint " + style, () => uint.TryParse(input, style, culture, out uint value) + ":" + value);
                }
                CaptureMissingNumber("current assignment", () => WithMissingCulture(culture, () =>
                    MissingCultureIdentity(CultureInfo.CurrentCulture) + "|" + MissingCultureIdentity(CultureInfo.CurrentUICulture)));
                CaptureMissingNumber("current info", () => WithMissingCulture(culture, () => MissingNumberData(NumberFormatInfo.CurrentInfo)));
                CaptureMissingNumber("current double", () => WithMissingCulture(culture, () => (-12.5).ToString("N2")));
                CaptureMissingNumber("current int positive", () => WithMissingCulture(culture, () => 12.ToString("G")));
                CaptureMissingNumber("current int positive default", () => WithMissingCulture(culture, () => 12.ToString()));
                CaptureMissingNumber("current int negative default", () => WithMissingCulture(culture, () => (-12).ToString()));
                CaptureMissingNumber("current long positive default", () => WithMissingCulture(culture, () => 12L.ToString()));
                CaptureMissingNumber("current long negative default", () => WithMissingCulture(culture, () => (-12L).ToString()));
                CaptureMissingNumber("current short positive default", () => WithMissingCulture(culture, () => ((short)12).ToString()));
                CaptureMissingNumber("current short negative default", () => WithMissingCulture(culture, () => ((short)-12).ToString()));
                CaptureMissingNumber("current generic int positive", () => WithMissingCulture(culture, () => GenericToString(12)));
                CaptureMissingNumber("current generic int negative", () => WithMissingCulture(culture, () => GenericToString(-12)));
                CaptureMissingNumber("current generic long positive", () => WithMissingCulture(culture, () => GenericToString(12L)));
                CaptureMissingNumber("current generic long negative", () => WithMissingCulture(culture, () => GenericToString(-12L)));
                CaptureMissingNumber("current generic short positive", () => WithMissingCulture(culture, () => GenericToString((short)12)));
                CaptureMissingNumber("current generic short negative", () => WithMissingCulture(culture, () => GenericToString((short)-12)));
                CaptureMissingNumber("current boxed int positive", () => WithMissingCulture(culture, () => ((object)12).ToString()));
                CaptureMissingNumber("current boxed int negative", () => WithMissingCulture(culture, () => ((object)(-12)).ToString()));
                CaptureMissingNumber("current boxed long positive", () => WithMissingCulture(culture, () => ((object)12L).ToString()));
                CaptureMissingNumber("current boxed long negative", () => WithMissingCulture(culture, () => ((object)(-12L)).ToString()));
                CaptureMissingNumber("current boxed short positive", () => WithMissingCulture(culture, () => ((object)(short)12).ToString()));
                CaptureMissingNumber("current boxed short negative", () => WithMissingCulture(culture, () => ((object)(short)-12).ToString()));
                CaptureMissingNumber("current parse", () => WithMissingCulture(culture, () => double.TryParse("12", out double value) + ":" + value));
                CultureInfo clone = (CultureInfo)culture.Clone();
                Console.WriteLine("    clone=" + MissingCultureIdentity(clone));
                CaptureMissingNumber("clone number", () => MissingNumberData(clone.NumberFormat));
                CaptureMissingNumber("cached number", () => MissingNumberData(CultureInfo.GetCultureInfo(name).NumberFormat));
            }
            Console.WriteLine("culture missing number format end");
        }

        private static void RunMissingNumberFormatCache(bool missingFirst)
        {
            string firstName = missingFirst ? "i-enochian" : "";
            string secondName = missingFirst ? "" : "i-enochian";
            CultureInfo first = CultureInfo.GetCultureInfo(firstName);
            CultureInfo second = CultureInfo.GetCultureInfo(secondName);
            CultureInfo third = CultureInfo.GetCultureInfo(firstName);
            CultureInfo fourth = CultureInfo.GetCultureInfo(secondName);
            Console.WriteLine("== culture missing number format cache ==");
            Console.WriteLine("  first=" + MissingCultureIdentity(first) + " second=" + MissingCultureIdentity(second));
            Console.WriteLine("  pair=" + ReferenceEquals(first, second) + " first repeat=" + ReferenceEquals(first, third)
                + " second repeat=" + ReferenceEquals(second, fourth));
            CaptureMissingNumber("first number", () => MissingNumberData(first.NumberFormat));
            CaptureMissingNumber("second number", () => MissingNumberData(second.NumberFormat));
            CaptureMissingNumber("third number", () => MissingNumberData(third.NumberFormat));
            CaptureMissingNumber("fourth number", () => MissingNumberData(fourth.NumberFormat));
            Console.WriteLine("  invariant=" + MissingCultureIdentity(CultureInfo.InvariantCulture)
                + ":" + MissingNumberData(CultureInfo.InvariantCulture.NumberFormat));
            Console.WriteLine("culture missing number format cache end");
        }

        private static void RunMissingUpperCache(bool constructorFirst)
        {
            const string upper = "i-enochian-US-U-NU-ARAB";
            const string lower = "i-enochian-us-u-nu-arab";
            CultureInfo constructor;
            CultureInfo cachedLower;
            if (constructorFirst)
            {
                TryCultureCreation("constructor", () => new CultureInfo(upper, false), out constructor);
                TryCultureCreation("cached lower", () => CultureInfo.GetCultureInfo(lower), out cachedLower);
            }
            else
            {
                TryCultureCreation("cached lower", () => CultureInfo.GetCultureInfo(lower), out cachedLower);
                TryCultureCreation("constructor", () => new CultureInfo(upper, false), out constructor);
            }
            TryCultureCreation("cached upper", () => CultureInfo.GetCultureInfo(upper), out CultureInfo cachedUpper);
            Console.WriteLine("== culture missing number format uppercase cache ==");
            if (constructor is not null)
                Console.WriteLine("  constructor=" + MissingCultureIdentity(constructor));
            if (cachedLower is not null)
                Console.WriteLine("  cached lower=" + MissingCultureIdentity(cachedLower));
            if (cachedUpper is not null)
                Console.WriteLine("  cached upper=" + MissingCultureIdentity(cachedUpper));
            if (cachedLower is not null && cachedUpper is not null)
                Console.WriteLine("  cached pair=" + ReferenceEquals(cachedLower, cachedUpper));
            if (constructor is not null)
                CaptureMissingNumber("constructor number", () => MissingNumberData(constructor.NumberFormat));
            if (cachedLower is not null)
                CaptureMissingNumber("cached lower number", () => MissingNumberData(cachedLower.NumberFormat));
            if (cachedUpper is not null)
                CaptureMissingNumber("cached upper number", () => MissingNumberData(cachedUpper.NumberFormat));
            Console.WriteLine("culture missing number format uppercase cache end");
        }

        private static string MissingCultureIdentity(CultureInfo culture) => "'" + culture.Name + "':" + culture.LCID
            + ":neutral=" + culture.IsNeutralCulture;

        private static string MissingNumberData(NumberFormatInfo n) => "'" + n.NumberDecimalSeparator
            + "'|'" + n.NumberGroupSeparator + "':" + n.NumberDecimalDigits;

        private static void CaptureMissingNumber(string label, Func<string> action)
        {
            try
            {
                Console.WriteLine("    " + label + "=" + action());
            }
            catch (NullReferenceException)
            {
                Console.WriteLine("    " + label + "=" + nameof(NullReferenceException));
            }
            catch (CultureNotFoundException exception)
            {
                Console.WriteLine("    " + label + "=" + nameof(CultureNotFoundException));
                WriteCultureFault(exception);
            }
            catch (ArgumentNullException)
            {
                Console.WriteLine("    " + label + "=" + nameof(ArgumentNullException));
            }
        }

        private static string WithMissingCulture(CultureInfo culture, Func<string> action)
        {
            CultureInfo previous = CultureInfo.CurrentCulture;
            CultureInfo previousUI = CultureInfo.CurrentUICulture;
            try
            {
                CultureInfo.CurrentCulture = culture;
                CultureInfo.CurrentUICulture = culture;
                return action();
            }
            finally
            {
                CultureInfo.CurrentCulture = previous;
                CultureInfo.CurrentUICulture = previousUI;
            }
        }

        private static void RunFallbackIdentities(string? name = null)
        {
            Console.WriteLine("== culture fallback identities ==");
            foreach (string input in name is null ? new[]
            {
                "und-US", "und-DE", "und-FR", "und-JP", "und-Latn-US",
                "und-US-u-nu-arab", "und-US-u-nu-latn", "und-US-u-nu-native",
                "und-US-u-nu-finance", "und-US-u-nu-traditio", "und-US-u-nu-foo",
                "foo", "foo-US", "foo-u-nu-default", "foo-US-u-nu-default",
                "de-FOO", "de-FOO-u-ca-gregory", "de-FOO-u-nu-latn", "de-FOO-u-nu-arab",
                "de-FOO-u-nu-native", "de-FOO-u-nu-finance", "de-FOO-u-nu-traditio",
                "de-FOO-u-nu-default", "de-FOO-u-nu-foo", "en-GB-oed", "en-GB-oed-u-nu-foo",
                "i-klingon", "i-klingon-u-nu-foo", "i-default", "i-default-u-nu-foo",
                "i-enochian", "i-enochian-u-nu-foo", "i-mingo", "i-mingo-u-nu-foo",
                "zh-min", "zh-min-u-nu-foo", "zh-min-nan-u-nu-foo",
                "nv", "jbo", "cmn", "i-navajo-u-nu-native", "art-lojban-u-nu-native", "zh-guoyu-u-nu-native",
                "i-enochian\0ignored", "zh-min\0ignored",
                "de-foo-bar-u-nu-latn", "de-foo-bar-US-u-nu-latn", "de-foo-Latn-US-u-nu-latn",
                "de-foo-bar-baz-US-u-nu-latn", "de-foo-bar-baz-zzz-US-u-nu-latn",
                "foo-QQ", "foo-999", "und-QQ", "foo-ZZ", "i-default-US-u-nu-arab",
                "i-enochian-US-u-nu-arab", "i-mingo-US-u-nu-arab", "zh-min-US-u-nu-arab",
                "i-default-abcde-u-nu-arab", "i-enochian-US-U-NU-ARAB",
                "i-klingon-Latn-US", "i-klingon-Latn-US-u-nu-latn", "zh-min-nan-Latn-US-u-nu-latn"
            } : new[] { name })
            {
                if (!TryCreateCulture(input, out CultureInfo culture))
                    continue;
                Console.WriteLine("  '" + input.Replace("\0", "<NUL>") + "' -> '" + culture.Name + "' value='" + culture.ToString()
                    + "' LCID=" + culture.LCID + " neutral=" + culture.IsNeutralCulture);
                Console.WriteLine("    specific='" + CultureName(() => CultureInfo.CreateSpecificCulture(input))
                    + "' cached='" + CultureName(() => CultureInfo.GetCultureInfo(input))
                    + "' without override='" + CultureName(() => new CultureInfo(input, false)) + "'");
            }
            Console.WriteLine("culture fallback identities end");
        }

        private static void RunPrimaryCurrency()
        {
            Console.WriteLine("== culture primary currency ==");
            foreach (string input in new[] { "12-eng-US-u-nu-arab", "ab1-eng-US-u-nu-arab" })
                Console.WriteLine("  '" + input + "' currency=" + CultureValue(() => new CultureInfo(input).NumberFormat.CurrencySymbol));
            Console.WriteLine("culture primary currency end");
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
                if (!TryCreateCulture(name, out CultureInfo culture))
                    continue;
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
                Console.WriteLine("  uninterpreted prefix '" + prefix + "' keeps numeric data="
                    + CultureValue(() =>
                    {
                        NumberFormatInfo baseData = new CultureInfo(prefix).NumberFormat;
                        NumberFormatInfo extended = new CultureInfo(prefix + "-u-nu-foo").NumberFormat;
                        return (baseData.NumberDecimalSeparator == extended.NumberDecimalSeparator
                        && baseData.NumberGroupSeparator == extended.NumberGroupSeparator
                        && baseData.CurrencySymbol == extended.CurrencySymbol
                        && baseData.NaNSymbol == extended.NaNSymbol
                        && baseData.NegativeSign == extended.NegativeSign
                        && baseData.NumberDecimalDigits == extended.NumberDecimalDigits).ToString();
                    }));
            }
            foreach (string prefix in new[] { "i-default", "i-enochian", "i-mingo", "zh-min" })
            {
                if (!TryCreateCulture(prefix + "-u-nu-foo", out CultureInfo culture))
                    continue;
                NumberFormatInfo n = culture.NumberFormat;
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
            else if (mode == "culture-wrapper-private-first")
            {
                firstName = "i-default-u-nu-foo";
                secondName = "en-u-nu-foo";
            }
            else if (mode == "culture-wrapper-canonical-first")
            {
                firstName = "en-u-nu-foo";
                secondName = "i-default-u-nu-foo";
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
            if (mode == "culture-wrapper-private-first" || mode == "culture-wrapper-canonical-first")
            {
                WriteWrapperNumbering("first", first.NumberFormat);
                WriteWrapperNumbering("second", second.NumberFormat);
                WriteWrapperNumbering("first again", third.NumberFormat);
                WriteWrapperNumbering("second again", fourth.NumberFormat);
            }
            Console.WriteLine("culture wrapper cache end");
        }

        private static void WriteWrapperNumbering(string label, NumberFormatInfo n)
        {
            Console.WriteLine("  " + label + " has decimal=" + (n.NumberDecimalSeparator.Length > 0)
                + " negative=" + (n.NegativeSign.Length > 0) + " digits=" + n.NumberDecimalDigits
                + " group=" + n.NumberGroupSizes[0]);
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
            if (!TryCreateCulture(name, out CultureInfo culture))
                return;
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
