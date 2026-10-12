#:property JsonSerializerIsReflectionEnabledByDefault=true
// Collect fallback profiles under an externally selected host locale.
// dotnet run tools/gen-culture-table/collect-fallback-data.cs output.jsonl cases.json
// Each host locale needs a fresh process: managed current cultures cannot change
// ICU resource fallback. The runtime uses the resulting frozen data without ICU.
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;

CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
string input = args[1];
var cases = JsonSerializer.Deserialize<Dictionary<string, string[]>>(File.ReadAllText(input))!;
var names = cases["names"];
using var output = new StreamWriter(args[0]);
var icuMethod = typeof(object).Assembly.GetType("Interop+Globalization")?
    .GetMethod("GetICUVersion", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
int icu = icuMethod?.Invoke(null, null) is int value ? value : 0;
output.WriteLine(JsonSerializer.Serialize(new {
    oracle = RuntimeInformation.FrameworkDescription,
    icu = $"{(icu >> 24) & 255}.{(icu >> 16) & 255}",
    host = RuntimeInformation.OSDescription + " (" + RuntimeInformation.OSArchitecture + ")"
}));
output.WriteLine(JsonSerializer.Serialize(new {
    resourceLanguages = CultureInfo.GetCultures(CultureTypes.AllCultures)
        .Select(c => c.Name.Split('-')[0]).Where(n => n.Length > 0)
        .Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray(),
    locale = Environment.GetEnvironmentVariable("LC_ALL")
}));
foreach (string name in names)
    Write(name);
return;

void Write(string name)
{
    var c = new CultureInfo(name);
    var n = c.NumberFormat;
    output.WriteLine(JsonSerializer.Serialize(new {
        input = name, c.Name, c.LCID, c.IsNeutralCulture,
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
