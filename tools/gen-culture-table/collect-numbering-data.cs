#:property JsonSerializerIsReflectionEnabledByDefault=true
#:property PublishAot=false
// Collect a frozen numbering-system oracle; run from the repository root:
// dotnet run tools/gen-culture-table/collect-numbering-data.cs output.jsonl
// Run once with Apple ICU and once with standard ICU, then pass both snapshots
// to gen-numbering-table.py. The runtime never loads ICU for these tables.
// On Windows, pass its snapshot to gen-numbering-table.py --windows instead.
// Select app-local ICU through DOTNET_SYSTEM_GLOBALIZATION_APPLOCALICU and the
// platform library search path; each JSONL header records the loaded version.
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.RegularExpressions;

CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
string root = Directory.GetCurrentDirectory();
string tool = Path.Combine(root, "tools", "gen-culture-table");
var bases = File.ReadAllLines(Path.Combine(tool, "culture-candidates.txt"))
    .Where(line => line.Length > 0 && !line.StartsWith('#')).ToList();
string supplemental = File.ReadAllText(Path.Combine(root, "runtime", "core", "intrinsics", "dn2cpp_culture_identifiers.inc"));
supplemental = supplemental[(supplemental.IndexOf("kCultureIdentifierFormats", StringComparison.Ordinal))..];
supplemental = supplemental[..supplemental.IndexOf("};", StringComparison.Ordinal)];
foreach (Match match in Regex.Matches(supplemental, "^\\s*\\{\\s*u\"([^\"]+)\"", RegexOptions.Multiline))
    bases.Add(match.Groups[1].Value);
var kinds = File.ReadAllLines(Path.Combine(tool, "numbering-systems.txt"))
    .Where(line => line.Length > 0 && !line.StartsWith('#'))
    .Concat(new[] { "native", "finance", "traditio", "default", "foo", "arab-foo", "none" }).ToArray();
using var output = new StreamWriter(args[0]);
var icuMethod = typeof(object).Assembly.GetType("Interop+Globalization")?
    .GetMethod("GetICUVersion", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
int icu = icuMethod?.Invoke(null, null) is int value ? value : 0;
output.WriteLine(JsonSerializer.Serialize(new {
    oracle = RuntimeInformation.FrameworkDescription,
    icu = $"{(icu >> 24) & 255}.{(icu >> 16) & 255}",
    host = RuntimeInformation.OSDescription + " (" + RuntimeInformation.OSArchitecture + ")"
}));
foreach (string name in bases.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal))
{
    Write(name);
    foreach (string kind in kinds)
        Write(name + "-u-nu-" + kind);
}
return;

void Write(string name)
{
    CultureInfo c;
    try
    {
        c = new CultureInfo(name);
    }
    catch (CultureNotFoundException)
    {
        output.WriteLine(JsonSerializer.Serialize(new { input = name, error = nameof(CultureNotFoundException) }));
        return;
    }
    var n = c.NumberFormat;
    output.WriteLine(JsonSerializer.Serialize(new {
        input = name,
        n.NumberDecimalSeparator, n.NumberGroupSeparator, n.NumberGroupSizes,
        n.NumberDecimalDigits, n.NumberNegativePattern,
        n.CurrencyDecimalSeparator, n.CurrencyGroupSeparator, n.CurrencyGroupSizes,
        n.CurrencyDecimalDigits, n.CurrencyPositivePattern, n.CurrencyNegativePattern, n.CurrencySymbol,
        n.PercentDecimalSeparator, n.PercentGroupSeparator, n.PercentGroupSizes,
        n.PercentDecimalDigits, n.PercentPositivePattern, n.PercentNegativePattern,
        n.PercentSymbol, n.PerMilleSymbol, n.NegativeSign, n.PositiveSign,
        n.NaNSymbol, n.PositiveInfinitySymbol, n.NegativeInfinitySymbol
    }));
}
