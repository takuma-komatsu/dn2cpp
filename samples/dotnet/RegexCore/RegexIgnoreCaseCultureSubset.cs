#nullable enable
using System;
using System.Globalization;
using System.Text.RegularExpressions;
using SampleSupport;

// SUBJECT: culture-tiered IgnoreCase — RegexCaseEquivalences picks the Turkish
// / NonTurkish / Invariant tier from CultureInfo.CurrentCulture.Name (tr/az
// prefix => Turkish, "" => Invariant, else NonTurkish). Every tier is entered
// by explicit assignment; nothing culture-derived prints before the first one,
// and no numeric or date formatting appears here.
//
// The CultureScope's EXIT culture is invariant, not the entry culture, and that
// is the postcondition: later sections of this bucket must inherit neither
// tr-TR (wrong case tier) nor the host culture (host-dependent output). A plain
// save/restore scope would deliver the latter.
namespace RegexIgnoreCaseCultureSubset;

static class Program
{
    internal static void __GateEntry()
    {
        using (CultureScope.Use(new CultureInfo("tr-TR"), restoreTo: CultureInfo.InvariantCulture))
        {
            Console.WriteLine("-- Turkish tier (tr-TR) --");
            Console.WriteLine(CultureInfo.CurrentCulture.Name);
            Console.WriteLine(Regex.IsMatch("İ", "i", RegexOptions.IgnoreCase));       // İ ~ i
            Console.WriteLine(Regex.IsMatch("ı", "I", RegexOptions.IgnoreCase));       // ı ~ I
            Console.WriteLine(Regex.IsMatch("i", "I", RegexOptions.IgnoreCase));            // false in Turkish
            Console.WriteLine(Regex.IsMatch("I", "i", RegexOptions.IgnoreCase));            // false in Turkish
            Console.WriteLine(Regex.Replace("inİt ıq", "i", "*", RegexOptions.IgnoreCase));
            Console.WriteLine(Regex.Replace("fix FIX fıx fİx", "[iı]", "_", RegexOptions.IgnoreCase));

            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");
            Console.WriteLine(Regex.IsMatch("TİTLE", "title", RegexOptions.IgnoreCase)); // via GetCultureInfo

            Console.WriteLine("-- NonTurkish tier (en-US) --");
            CultureInfo.CurrentCulture = new CultureInfo("en-US");
            Console.WriteLine(CultureInfo.CurrentCulture.Name);
            Console.WriteLine(Regex.IsMatch("i", "I", RegexOptions.IgnoreCase));            // true again
            Console.WriteLine(Regex.IsMatch("İ", "i", RegexOptions.IgnoreCase));       // İ ~ i also NonTurkish
            Console.WriteLine(Regex.IsMatch("ı", "I", RegexOptions.IgnoreCase));       // ı !~ I NonTurkish
            Console.WriteLine(Regex.IsMatch("K", "k", RegexOptions.IgnoreCase));       // Kelvin sign ~ k
            Console.WriteLine(Regex.IsMatch("ſ", "s", RegexOptions.IgnoreCase));       // long s !~ s NonTurkish (Invariant-only row)

            Console.WriteLine("-- Turkish tier (az prefix) --");
            CultureInfo.CurrentCulture = new CultureInfo("az-Latn-AZ");
            Console.WriteLine(Regex.IsMatch("i", "I", RegexOptions.IgnoreCase));            // false: az is Turkish-cased
            Console.WriteLine(Regex.IsMatch("İ", "i", RegexOptions.IgnoreCase));

            Console.WriteLine("-- NonTurkish tier (name outside the built-in table) --");
            CultureInfo.CurrentCulture = new CultureInfo("de-AT");
            Console.WriteLine(CultureInfo.CurrentCulture.Name);
            Console.WriteLine(Regex.IsMatch("i", "I", RegexOptions.IgnoreCase));            // true: de-AT is NonTurkish

            Console.WriteLine("-- NonBacktracking parity --");
            CultureInfo.CurrentCulture = new CultureInfo("tr-TR");
            Console.WriteLine(Regex.IsMatch("i", "I", RegexOptions.IgnoreCase | RegexOptions.NonBacktracking));
            Console.WriteLine(Regex.IsMatch("İ", "i", RegexOptions.IgnoreCase | RegexOptions.NonBacktracking));
            CultureInfo.CurrentCulture = new CultureInfo("en-US");
            Console.WriteLine(Regex.IsMatch("i", "I", RegexOptions.IgnoreCase | RegexOptions.NonBacktracking));
            Console.WriteLine(Regex.IsMatch("K", "k", RegexOptions.IgnoreCase | RegexOptions.NonBacktracking));

            Console.WriteLine("-- inline (?i) and CultureInvariant override --");
            CultureInfo.CurrentCulture = new CultureInfo("tr-TR");
            Console.WriteLine(Regex.IsMatch("I", "(?i)i"));                                  // inline: Turkish tier, false
            Console.WriteLine(Regex.IsMatch("I", "i", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)); // true: Invariant tier wins
            Console.WriteLine(Regex.IsMatch("İ", "i", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)); // false: invariant has no İ~i
        }
    }
}
