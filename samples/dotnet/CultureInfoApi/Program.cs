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

            // CultureInfo.GetFormat(Type) — the direct IFormatProvider implementation on
            // CultureInfo (a plain call token, not the interface slot). Returns a non-null
            // format object for a format type the invariant culture serves. typeof is on
            // DateTimeFormatInfo (a real transpiled class with a TypeInfo) rather than
            // NumberFormatInfo (value-mapped to the runtime struct pointer, so it has no
            // reflectable Type token); the arm ignores the requested type and hands back
            // the invariant NFI either way — non-null, as real .NET returns non-null here.
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
                "", "C", "c", "POSIX", "PoSiX", "de-DE-EURO", "DE-de-euro",
                "de-DE-1901", "fr-FR-EURO", "de-Latn-DE-EURO", "xx-YY", "xx-Latn-YY",
                "de-DE-u-nu-latn", "de-DE-t-en-us", "de-DE-u-nu-LaTn", "de-DE-t-EN-us",
                "en-US-POSIX", "de-u-nu-latn",
                "de-ab12", "de-DE\0ignored", "aaaaaaaaaaa", "de-DE-" + new string('a', 79)
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
            foreach (string name in new[] { "de-XX", "de-FOO", "fr-FOO", "de-XX-FOO" })
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

        // NumberGroupSizes spelled out. The three axes are one modeled array here and three
        // properties in .NET, so all three are printed: a row whose axes disagreed would be
        // refused by the table generator, and this asserts they agree at run time too.
        private static string Sizes(int[] a)
        {
            string s = "[";
            for (int i = 0; i < a.Length; i++)
                s += (i > 0 ? "," : "") + a[i];
            return s + "]";
        }
    }
}
