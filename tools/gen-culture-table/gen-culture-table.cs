// gen-culture-table.cs — regenerates runtime/core/intrinsics/dn2cpp_culture_table.inc,
// the named-culture table the C++ runtime answers `new CultureInfo(x)` from.
//
//   Run from the repository root:  dotnet run tools/gen-culture-table/gen-culture-table.cs
//
// A .NET 10 file-based program on purpose: it is a manual aid, not a build step and not
// a gate, so it must not add a csproj to the tree, must not be referenced by anything the
// CLI ships, and must not be transpiled by the self-host. Nothing in the suite runs it.
//
// WHY IT IS NOT A GATE, and cannot be made one. Its oracle is the ICU on the machine that
// runs it. A gate that regenerated and diffed would be red on any developer whose ICU is
// not the one the checked-in table was cut from — which is precisely the "diff gate red on
// one machine only" failure the table exists to remove. What the table needs is not a
// continuously-verified value but a RECORDED PROVENANCE: the header this program stamps
// says which .NET, which ICU and which host answered, so a later disagreement is a fact
// somebody can locate instead of a mystery.
//
// WHAT IT REFUSES, and why refusal is the right outcome. The modeled row cannot represent
// every culture: it carries ONE decimal separator, ONE group separator and ONE group-size
// array for the number, currency and percent axes alike, and it derives
// PositiveInfinitySymbol. A candidate that breaks one of those is dropped with a printed
// reason and gets no row — it keeps the runtime's honest fallback (invariant symbols under
// the requested name) instead of a row that would put a correct currency symbol on wrongly
// punctuated digits. Every check below is a stated invariant of the emitted table, so the
// table's own comment can assert them rather than hope for them.

using System.Globalization;
using System.Reflection;
using System.Text;

string repoRoot = Directory.GetCurrentDirectory();
string candidatesPath = Path.Combine(repoRoot, "tools", "gen-culture-table", "culture-candidates.txt");
string outPath = args.Length > 0
    ? args[0]
    : Path.Combine(repoRoot, "runtime", "core", "intrinsics", "dn2cpp_culture_table.inc");

if (!File.Exists(candidatesPath))
{
    Console.Error.WriteLine($"error: {candidatesPath} not found — run this from the repository root.");
    return 2;
}

// The one measured platform split. macOS's ICU spells ja-JP's currency symbol U+00A5 and
// glibc/Linux ICU spells it U+FFE5 (fullwidth yen); both are that platform's real .NET
// answer, and the gates diff against whichever one the host runs. A generator run on
// either host can only observe its own, so the other is carried here rather than lost on
// the next regeneration. Add a row here only for a difference actually MEASURED on the
// other platform — and only for a spelling that differs by PLATFORM, never for a value CLDR
// revised between ICU releases: that axis is the host's ICU version, which no `#if` names.
var platformSplits = new (string Culture, string Field, string Guard, string Value)[]
{
    ("ja-JP", "currency", "defined(__linux__)", "￥"),
};

var candidates = File.ReadAllLines(candidatesPath)
    .Select(l => l.Trim())
    .Where(l => l.Length > 0 && !l.StartsWith('#'))
    .ToList();

var rows = new List<string>();
var refused = new List<string>();

foreach (string name in candidates)
{
    CultureInfo c;
    try
    {
        c = new CultureInfo(name);
    }
    catch (CultureNotFoundException)
    {
        refused.Add($"{name}: real .NET cannot materialize it");
        continue;
    }
    if (!string.Equals(c.Name, name, StringComparison.Ordinal))
    {
        // The candidate list is written in .NET's own canonical casing; a rewrite means
        // the list is stale, and a row keyed on the wrong spelling would never be found.
        refused.Add($"{name}: .NET canonicalizes the name to '{c.Name}'");
        continue;
    }

    NumberFormatInfo n = c.NumberFormat;
    string? why = Unrepresentable(n);
    if (why is not null)
    {
        refused.Add($"{name}: {why}");
        continue;
    }

    int[] gs = n.NumberGroupSizes;
    string g0 = gs[0].ToString(CultureInfo.InvariantCulture);
    string g1 = (gs.Length > 1 ? gs[1] : -1).ToString(CultureInfo.InvariantCulture);

    string Row(string currency) => "    { "
        + string.Join(", ",
            Lit(c.Name), Lit(n.NumberDecimalSeparator), Lit(n.NumberGroupSeparator),
            Lit(n.NegativeSign), Lit(n.NaNSymbol), Lit(n.NegativeInfinitySymbol),
            Lit(currency), Lit(n.PercentSymbol))
        + ", " + string.Join(", ",
            c.LCID.ToString(CultureInfo.InvariantCulture),
            n.CurrencyDecimalDigits.ToString(CultureInfo.InvariantCulture),
            n.CurrencyPositivePattern.ToString(CultureInfo.InvariantCulture),
            n.CurrencyNegativePattern.ToString(CultureInfo.InvariantCulture),
            n.PercentPositivePattern.ToString(CultureInfo.InvariantCulture),
            n.PercentNegativePattern.ToString(CultureInfo.InvariantCulture),
            c.IsNeutralCulture ? "1" : "0", g0, g1)
        + " },";

    var split = platformSplits.FirstOrDefault(p => p.Culture == c.Name);
    if (split.Culture is not null)
    {
        if (split.Field != "currency")
            throw new InvalidOperationException($"unmodeled platform-split field '{split.Field}'");
        rows.Add($"#if {split.Guard}");
        rows.Add(Row(split.Value));
        rows.Add("#else");
        rows.Add(Row(n.CurrencySymbol));
        rows.Add("#endif");
    }
    else
    {
        rows.Add(Row(n.CurrencySymbol));
    }
}

var sb = new StringBuilder();
sb.Append(Header(rows.Count(r => r.StartsWith("    {", StringComparison.Ordinal))
                 - platformSplits.Length, refused.Count));
sb.AppendLine("static const Dn2CppCultureRow kCultures[] = {");
foreach (string r in rows)
    sb.AppendLine(r);
sb.AppendLine("};");

File.WriteAllText(outPath, sb.ToString());
Console.Error.WriteLine($"wrote {outPath}");
Console.Error.WriteLine($"  candidates {candidates.Count}, rows {candidates.Count - refused.Count}, refused {refused.Count}");
foreach (string r in refused)
    Console.Error.WriteLine($"  REFUSED {r}");
return 0;

// A row carries one decimal separator, one group separator and one group-size array for
// all three axes, and derives PositiveInfinitySymbol. Returns null when the culture fits.
static string? Unrepresentable(NumberFormatInfo n)
{
    if (n.CurrencyDecimalSeparator != n.NumberDecimalSeparator
        || n.PercentDecimalSeparator != n.NumberDecimalSeparator)
        return "decimal separators differ across the number/currency/percent axes";
    if (n.CurrencyGroupSeparator != n.NumberGroupSeparator
        || n.PercentGroupSeparator != n.NumberGroupSeparator)
        return "group separators differ across the number/currency/percent axes";
    if (!n.CurrencyGroupSizes.SequenceEqual(n.NumberGroupSizes)
        || !n.PercentGroupSizes.SequenceEqual(n.NumberGroupSizes))
        return "group sizes differ across the number/currency/percent axes";
    if (n.PositiveInfinitySymbol != "∞")
        return $"PositiveInfinitySymbol is not U+221E ('{n.PositiveInfinitySymbol}')";
    int[] gs = n.NumberGroupSizes;
    // The row spends two int8 columns on the group sizes (see the struct's comment): the
    // first size and either the second or -1 for "there is no second". Everything a real
    // culture has is [3] or [3,2]; a longer array is refused rather than truncated,
    // because truncating one would punctuate a large number in the wrong places.
    if (gs.Length is 0 or > 2)
        return $"NumberGroupSizes has {gs.Length} elements; the row shape carries at most 2";
    if (gs[0] < 1 || gs[0] > 9)
        return $"NumberGroupSizes[0] is {gs[0]}; the row shape carries 1..9";
    if (gs.Length == 2 && (gs[1] < 0 || gs[1] > 9))
        return $"NumberGroupSizes[1] is {gs[1]}; the row shape carries 0..9";
    return null;
}

// A C++ u"..." literal: ASCII printable stays literal, everything else becomes \uXXXX, so
// the table is readable in an editor with no font and diffable without a Unicode-aware eye.
static string Lit(string s)
{
    var b = new StringBuilder("u\"");
    foreach (char ch in s)
    {
        if (ch == '"' || ch == '\\')
            b.Append('\\').Append(ch);
        else if (ch >= 0x20 && ch < 0x7f)
            b.Append(ch);
        else
            b.Append("\\u").Append(((int)ch).ToString("x4", CultureInfo.InvariantCulture));
    }
    return b.Append('"').ToString();
}

static string Header(int rowCount, int refusedCount)
{
    string icu = "unknown";
    // The ICU version is what makes this header worth having, and .NET exposes it nowhere
    // public: Interop.Globalization.GetICUVersion is internal to CoreLib. Read through
    // reflection, and say "unknown" rather than guess if a future runtime moves it — a
    // header that invented a version would be worse than one that admits it has none.
    var t = typeof(object).Assembly.GetType("Interop+Globalization");
    var mi = t?.GetMethod("GetICUVersion", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
    if (mi?.Invoke(null, null) is int v)
        icu = $"{(v >> 24) & 0xff}.{(v >> 16) & 0xff}";

    return $"""
// GENERATED FILE — do not edit by hand.
//
// The named-culture table. Regenerate with:
//
//     dotnet run tools/gen-culture-table/gen-culture-table.cs
//
// which states what this data IS and what it refuses; the row struct, the membership rule
// and the invariants every row satisfies are documented at the include site
// (dn2cpp_system_globalization.cpp). The candidate NAMES come from
// tools/gen-culture-table/culture-candidates.txt and never from the generating machine;
// the VALUES come from the oracle below, and are the reason this header exists.
//
//   oracle    : {System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription}
//   ICU       : {icu}
//   host      : {System.Runtime.InteropServices.RuntimeInformation.OSDescription} ({System.Runtime.InteropServices.RuntimeInformation.OSArchitecture})
//   generated : {DateTime.UtcNow:yyyy-MM-dd} (UTC)
//   rows      : {rowCount} ({refusedCount} candidate(s) refused as unrepresentable)
//
// WHAT THIS TABLE IS: a FROZEN CLDR SNAPSHOT at the ICU named above — not a mirror of the
// ICU the running host ships, which is a different release on every platform and moves many
// rows. So a gate may live-diff a value from here against the host's real .NET only where
// CLDR has not revised it; everything else belongs in a frozen bucket, and regenerating is
// a deliberate act that re-cuts the whole table against one machine's ICU.

""";
}
