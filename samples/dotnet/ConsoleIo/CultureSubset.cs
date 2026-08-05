#nullable enable
using System;
using System.Globalization;

// Culture: IFormatProvider / CultureInfo / NumberFormatInfo.
// Exercises culture-aware number/currency/percent formatting via both the
// value.ToString(..., provider) overloads and string.Format(provider, ...).
// Cultures are named explicitly out of the table dn2cpp ships (nothing here
// reads the host), plus an explicitly-built NumberFormatInfo.
//
// Its LAST section is about the modeled NumberFormatInfo's SHAPE rather than
// about culture data: multi-level digit grouping (NumberGroupSizes), the one
// field whose width decided which cultures could have a table row at all.
namespace CultureSubset;


static class Program
{
    static void Show(string tag, CultureInfo c)
    {
        double money = 1234.5, neg = -1234567.891, pct = 0.1234;
        Console.WriteLine($"[{tag}]");
        Console.WriteLine("  N2  = " + neg.ToString("N2", c));
        Console.WriteLine("  F2  = " + (-12.5).ToString("F2", c));
        Console.WriteLine("  C+  = " + money.ToString("C", c));
        Console.WriteLine("  C-  = " + (-money).ToString("C", c));
        Console.WriteLine("  P2  = " + pct.ToString("P2", c));
        Console.WriteLine("  E2  = " + (12345.678).ToString("E2", c));
        Console.WriteLine("  def = " + (1.0 / 3).ToString(c));
        Console.WriteLine("  nan = " + double.NaN.ToString(c));
        Console.WriteLine("  inf = " + double.PositiveInfinity.ToString(c));
        Console.WriteLine("  -inf= " + double.NegativeInfinity.ToString(c));
        Console.WriteLine("  iN0 = " + (-1234567).ToString("N0", c));
        Console.WriteLine("  fmt = " + string.Format(c, "{0:N2} / {1:C}", neg, money));
    }

    internal static void __GateEntry()
    {
        Show("invariant", CultureInfo.InvariantCulture);
        Show("en-US", new CultureInfo("en-US"));
        Show("de-DE", new CultureInfo("de-DE"));
        Show("fr-FR", CultureInfo.GetCultureInfo("fr-FR"));
        Show("ja-JP", new CultureInfo("ja-JP"));

        // The NumberFormat property is itself an IFormatProvider.
        var de = new CultureInfo("de-DE");
        Console.WriteLine("[via NumberFormat]");
        Console.WriteLine("  N2  = " + (9876.5).ToString("N2", de.NumberFormat));
        Console.WriteLine("  fmt = " + string.Format(de.NumberFormat, "{0:N2}", 9876.5));

        // A hand-built NumberFormatInfo (object initializer + setters).
        var nfi = new NumberFormatInfo
        {
            NumberDecimalSeparator = "·",
            NumberGroupSeparator = "_",
            NegativeSign = "~",
            NaNSymbol = "NN",
            PositiveInfinitySymbol = "UP",
            NegativeInfinitySymbol = "DN",
        };
        Console.WriteLine("[custom NFI]");
        Console.WriteLine("  N2  = " + (-1234567.891).ToString("N2", nfi));
        Console.WriteLine("  def = " + (1.0 / 3).ToString(nfi));
        Console.WriteLine("  nan = " + double.NaN.ToString(nfi));
        Console.WriteLine("  inf = " + double.PositiveInfinity.ToString(nfi));
        Console.WriteLine("  -inf= " + double.NegativeInfinity.ToString(nfi));
        Console.WriteLine("  fmt = " + string.Format(nfi, "{0:N2}", 9876.5));
        Console.WriteLine("  i   = " + (98765).ToString("N0", nfi));

        // Culture-aware parsing: the decimal separator / negative sign are read
        // from the provider. (Round-trip the parsed value through invariant text.)
        var inv = CultureInfo.InvariantCulture;
        Console.WriteLine("[parse]");
        Console.WriteLine("  de d = " + double.Parse("1234,5", de).ToString(inv));
        Console.WriteLine("  inv d= " + double.Parse("1234.5", inv).ToString(inv));
        Console.WriteLine("  nfi d= " + double.Parse("12·5", nfi).ToString(inv));
        Console.WriteLine("  nfi n= " + double.Parse("~12·5", nfi).ToString(inv));
        Console.WriteLine("  de f = " + float.Parse("3,25", de).ToString(inv));
        Console.WriteLine("  de i = " + int.Parse("-42", de).ToString(inv));
        bool ok = double.TryParse("9,75", NumberStyles.Float, de, out double tp);
        Console.WriteLine("  try  = " + ok + " " + tp.ToString(inv));
        bool bad = double.TryParse("x,y", de, out double tp2);
        Console.WriteLine("  bad  = " + bad + " " + tp2.ToString(inv));

        // Large integers (> 2^53) keep full precision through N/F/G/C — the double
        // path would round these; the exact integer path does not.
        long big = 9223372036854775807;     // long.MaxValue
        ulong ubig = 18446744073709551615;  // ulong.MaxValue
        Console.WriteLine("[big int]");
        Console.WriteLine("  G   = " + big.ToString("G", inv));
        Console.WriteLine("  N0  = " + big.ToString("N0", inv));
        Console.WriteLine("  N0de= " + big.ToString("N0", de));
        Console.WriteLine("  F1  = " + big.ToString("F1", inv));
        Console.WriteLine("  neg = " + long.MinValue.ToString("N0", inv));
        Console.WriteLine("  uG  = " + ubig.ToString("G", inv));
        Console.WriteLine("  uN0 = " + ubig.ToString("N0", inv));
        Console.WriteLine("  C   = " + (1234567890123L).ToString("C", de));

        // TextInfo.ListSeparator — the invariant culture's separator is "," (the
        // required-property-missing throw helper joins missing names with
        // CurrentUICulture.TextInfo.ListSeparator). dn2cpp folds every culture to
        // invariant, so only the invariant value is byte-stable across machines.
        Console.WriteLine("[textinfo]");
        Console.WriteLine("  sep = " + CultureInfo.InvariantCulture.TextInfo.ListSeparator);

        // ---- Multi-level digit grouping ----
        // The SUBJECT is the SHAPE of the modeled NumberFormatInfo, not these cultures: a
        // uniform group size cannot express the thirteen cultures that group 3-then-2, and
        // the failure is silent (they fall back to invariant symbols under their own name).
        // Naming real cultures stays deterministic because every value below is produced
        // with an EXPLICIT culture argument, so nothing here reads the host.
        //
        // Name only cultures whose grouping CLDR has not revised: this bucket's oracle is the
        // HOST's real .NET, whose ICU is a different release from the one the table was cut
        // from, so a row CLDR moved reddens it on that host alone. ur-PK is such a row —
        // gates/build-and-run-culture-info-api.sh covers it frozen, in "-- table breadth --".
        var hiIN = new CultureInfo("hi-IN");
        var enIN = new CultureInfo("en-IN");
        Console.WriteLine("[group sizes]");
        ShowSizes("inv", inv);
        ShowSizes("de-DE", de);
        ShowSizes("hi-IN", hiIN);
        ShowSizes("en-IN", enIN);
        Console.WriteLine("[3-then-2]");
        foreach (double v in new double[] { 0, 1, 123, 1234, 12345, 123456, 1234567,
                                            12345678, 123456789, 1234567890, 12345678901234.0 })
            Console.WriteLine("  N0=" + v.ToString("N0", hiIN) + " N2=" + v.ToString("N2", hiIN)
                              + " C0=" + v.ToString("C0", hiIN) + " P0=" + v.ToString("P0", hiIN));
        Console.WriteLine("  long  = " + (123456789012345678L).ToString("N0", hiIN));
        Console.WriteLine("  ulong = " + ulong.MaxValue.ToString("N0", enIN));
        Console.WriteLine("  minint= " + int.MinValue.ToString("N0", hiIN));
        Console.WriteLine("  neg C = " + (-1234567.89m).ToString("C", enIN));
        Console.WriteLine("  F2    = " + (123456789.0).ToString("F2", hiIN)); // F never groups
        Console.WriteLine("  G     = " + (123456789.0).ToString("G", hiIN));
        Console.WriteLine("  E2    = " + (123456789.0).ToString("E2", hiIN));
        Console.WriteLine("  fmt   = " + string.Format(hiIN, "{0:N0} / {1:C}", 123456789, 1234567.5));
        Console.WriteLine("  dec   = " + (12345678901.25m).ToString("N2", enIN));

        // Parsing does NOT consult the group sizes, in EITHER direction: .NET's parser
        // accepts a group separator between any two digits, so a 3-then-2 culture
        // round-trips its own output and also reads text grouped the other way. This
        // is the half a widening of the formatter could silently have broken — the
        // strip is keyed on the separator, never on the positions.
        Console.WriteLine("[3-then-2 parse]");
        foreach (string s in new[] { "12,34,56,789", "123,456,789", "1,2,3", "12,3456,789", "1,23,456.75" })
            Console.WriteLine("  '" + s + "' -> " + double.Parse(s, NumberStyles.Number, hiIN).ToString(inv));
        Console.WriteLine("  int   = " + int.Parse("12,34,56,789", NumberStyles.Number, hiIN).ToString(inv));
        Console.WriteLine("  rt    = "
            + double.Parse((1234567890.0).ToString("N0", hiIN), NumberStyles.Number, hiIN).ToString(inv));

        // A grouped double with MANY digits, here for the RENDERER rather than the culture:
        // `%.2f` of 1e300 is 304 characters, so any fixed scratch budget overruns, and
        // indexing by snprintf's RETURNED length reads past what it actually wrote. Both
        // buffers are sized from the digits. The largest double's grouped form is the
        // widest output the N path can produce, and the 3-then-2 line has the most
        // separators — where a fixed budget goes first.
        Console.WriteLine("[wide]");
        Console.WriteLine("  1e300 N2 = " + (1e300).ToString("N2", inv));
        Console.WriteLine("  max   N0 = " + double.MaxValue.ToString("N0", inv));
        Console.WriteLine("  1e21  N2 = " + (1e21).ToString("N2", hiIN));
        Console.WriteLine("  N40      = " + (123456.75).ToString("N40", inv));
    }

    // NumberGroupSizes / CurrencyGroupSizes / PercentGroupSizes, spelled out. The
    // three are separate properties in .NET and one modeled array here, which is why
    // all three are printed: a row whose axes disagreed would be refused by the table
    // generator, and this is the assert that the three agree at run time too.
    static void ShowSizes(string tag, CultureInfo c)
    {
        Console.WriteLine("  " + tag + " N=" + Sizes(c.NumberFormat.NumberGroupSizes)
                          + " C=" + Sizes(c.NumberFormat.CurrencyGroupSizes)
                          + " P=" + Sizes(c.NumberFormat.PercentGroupSizes));
    }

    static string Sizes(int[] a)
    {
        var b = new System.Text.StringBuilder("[");
        for (int i = 0; i < a.Length; i++)
        {
            if (i > 0)
                b.Append(',');
            b.Append(a[i]);
        }
        return b.Append(']').ToString();
    }
}
