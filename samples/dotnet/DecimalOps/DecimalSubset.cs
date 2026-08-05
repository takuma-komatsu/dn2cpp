using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

// System.Decimal modeled as an intrinsic value type backed
// by the runtime 96-bit Dn2CppDecimal. Exercises literals (the decimal ctors),
// the arithmetic/comparison/unary operators, numeric conversions, ToString
// (default preserves scale + standard format specifiers), the rounding statics,
// Parse/TryParse, and the LINQ decimal Sum/Average overloads (which ride this).
// Compiled against the real BCL; the gate passes the real System.Linq via -r
// (the decimal Sum/Average ride the generic-math lowering). All output is diffed
// exactly against real .NET (InvariantCulture).

namespace DecimalSubset;

internal static class Program
{
    static string S(decimal? v) => v.HasValue ? v.Value.ToString(CultureInfo.InvariantCulture) : "null";

    internal static void Run()
    {
        var ci = CultureInfo.InvariantCulture;

        // Literals + scale preservation.
        decimal a = 1.5m, b = 0.3m, c = 1.50m, big = 79228162514264337593543950335m; // decimal.MaxValue
        Console.WriteLine(a.ToString(ci));     // 1.5
        Console.WriteLine(c.ToString(ci));     // 1.50  (trailing zero preserved)
        Console.WriteLine(big.ToString(ci));   // MaxValue
        Console.WriteLine((-12.34m).ToString(ci));

        // Arithmetic operators.
        Console.WriteLine((a + b).ToString(ci));   // 1.8
        Console.WriteLine((a - b).ToString(ci));   // 1.2
        Console.WriteLine((a * b).ToString(ci));   // 0.45
        Console.WriteLine((a / b).ToString(ci));   // 5
        Console.WriteLine((10m % 3m).ToString(ci)); // 1
        Console.WriteLine((1m / 3m).ToString(ci));  // 0.3333...28 threes
        Console.WriteLine((c * 1.00m).ToString(ci));// 1.5000
        Console.WriteLine((-a).ToString(ci));       // -1.5

        // Comparison operators.
        Console.WriteLine(a > b);          // True
        Console.WriteLine(a == c);         // True (1.5 == 1.50)
        Console.WriteLine(b <= a);         // True
        Console.WriteLine(a.CompareTo(b)); // 1
        Console.WriteLine(decimal.Compare(b, a)); // -1

        // Conversions.
        decimal fromInt = 42;              // implicit int -> decimal
        decimal fromLong = 9_000_000_000L; // implicit long -> decimal
        Console.WriteLine(fromInt.ToString(ci));
        Console.WriteLine(fromLong.ToString(ci));
        Console.WriteLine(((decimal)0.1).ToString(ci));  // double -> decimal: 0.1
        Console.WriteLine((double)(1m / 4m));            // decimal -> double: 0.25
        Console.WriteLine((int)123.99m);                 // decimal -> int (truncates): 123
        Console.WriteLine((long)(-7.5m));                // -7

        // Standard format specifiers.
        Console.WriteLine((1234.5678m).ToString("F2", ci));   // 1234.57
        Console.WriteLine((1234567.891m).ToString("N2", ci)); // 1,234,567.89
        Console.WriteLine((0.1234m).ToString("P1", ci));      // 12.3 %

        // Rounding statics.
        Console.WriteLine(Math.Round(2.5m).ToString(ci));                 // 2 (banker's)
        Console.WriteLine(Math.Round(3.5m).ToString(ci));                 // 4
        Console.WriteLine(Math.Round(1.2345m, 2).ToString(ci));           // 1.23  (1.2345 -> 1.23, ToEven)
        Console.WriteLine(Math.Round(2.5m, MidpointRounding.AwayFromZero).ToString(ci)); // 3
        Console.WriteLine(decimal.Truncate(1.99m).ToString(ci));          // 1
        Console.WriteLine(decimal.Floor(-1.1m).ToString(ci));             // -2
        Console.WriteLine(decimal.Ceiling(1.1m).ToString(ci));            // 2
        Console.WriteLine(Math.Abs(-3.14m).ToString(ci));                 // 3.14

        // Generic-math statics (direct-call forms).
        Console.WriteLine(decimal.IsCanonical(1.1m) + " " + decimal.IsCanonical(1.100m)); // True False
        Console.WriteLine(decimal.IsEvenInteger(2.00m) + " " + decimal.IsOddInteger(2.5m)); // True False
        Console.WriteLine(decimal.MaxMagnitude(-5m, 4m).ToString(ci));    // -5
        Console.WriteLine(decimal.MinMagnitude(-5m, 4m).ToString(ci));    // 4
        Console.WriteLine(decimal.MaxMagnitude(1.0m, 1.00m).ToString(ci)); // 1.0 (tie keeps x)
        Console.WriteLine(decimal.MinMagnitude(1.0m, 1.00m).ToString(ci)); // 1.00 (tie keeps y)

        // Parse / TryParse.
        Console.WriteLine(decimal.Parse("123.456", ci).ToString(ci));     // 123.456
        Console.WriteLine(decimal.TryParse("1.5", NumberStyles.Number, ci, out decimal p) + " " + p.ToString(ci)); // True 1.5
        Console.WriteLine(decimal.TryParse("nope", NumberStyles.Number, ci, out decimal _)); // False

        // LINQ Sum/Average (bound to the real System.Linq via -r).
        var prices = new List<decimal> { 19.99m, 5.50m, 100.00m, 0.01m };
        Console.WriteLine(prices.Sum().ToString(ci));      // 125.50
        Console.WriteLine(prices.Average().ToString(ci));  // 31.375
        var nums = new List<decimal> { 1m, 2m, 6m };
        Console.WriteLine(nums.Sum(x => x * 2m).ToString(ci));   // 18
        Console.WriteLine(nums.Average().ToString(ci));          // 3

        // Nullable LINQ.
        var maybe = new List<decimal?> { 10.5m, null, 4.5m };
        Console.WriteLine(S(maybe.Sum()));        // 15.0
        Console.WriteLine(S(maybe.Average()));    // 7.5
        var allNull = new List<decimal?> { null, null };
        Console.WriteLine(S(allNull.Sum()));      // 0
        Console.WriteLine(S(allNull.Average()));  // null

        // ---- T2: GetBits / ToDouble / TryFormat ----
        // GetBits(decimal) -> int[4] { lo, mid, hi, flags } over representative values.
        foreach (decimal d in new[] { 0m, 1m, -1m, decimal.MaxValue, 1.25m, -12.340m })
        {
            int[] bits = decimal.GetBits(d);
            Console.WriteLine($"{d.ToString(ci)}: {bits[0]} {bits[1]} {bits[2]} {bits[3]:X8}");
        }
        // GetBits(decimal, Span<int>) -> count written, same words into a caller span.
        Span<int> span = stackalloc int[4];
        int n = decimal.GetBits(79228162514264337593543950335m, span);
        Console.WriteLine($"span n={n}: {span[0]} {span[1]} {span[2]} {span[3]:X8}");

        // decimal.ToDouble(decimal) static.
        Console.WriteLine(decimal.ToDouble(3.5m).ToString(ci));           // 3.5
        Console.WriteLine(decimal.ToDouble(0m).ToString(ci));            // 0
        Console.WriteLine(decimal.ToDouble(-12.34m).ToString(ci));       // -12.34

        // decimal.TryFormat(Span<char>, out, format, provider) — success + too-short.
        Span<char> dbuf = stackalloc char[32];
        bool tf1 = (1234.56m).TryFormat(dbuf, out int w1, default, ci);
        Console.WriteLine($"tf1 {tf1} {w1} [{new string(dbuf.Slice(0, w1))}]");   // True 7 [1234.56]
        bool tf2 = (1.50m).TryFormat(dbuf, out int w2, "F2".AsSpan(), ci);
        Console.WriteLine($"tf2 {tf2} {w2} [{new string(dbuf.Slice(0, w2))}]");   // True 4 [1.50]
        Span<char> tiny = stackalloc char[3];
        bool tf3 = (1234.56m).TryFormat(tiny, out int w3, default, ci);
        Console.WriteLine($"tf3 {tf3} {w3}");                                     // False 0

        // ---- F2: (UInt128) cast reads Decimal.Low64 + High ----
        // UInt128.op_Explicit(decimal) truncates the fraction, then rebuilds
        // `new UInt128(value.High, value.Low64)` — so the internal Low64 accessor (the
        // low 64 mantissa bits) is exercised alongside the already-mapped High. The 2^64
        // case pins Low64=0 with High=1 (placement), MaxValue pins the full 96 bits.
        foreach (decimal d in new decimal[] { 0m, 123.9m, 18446744073709551616m, 79228162514264337593543950335m })
            Console.WriteLine(((UInt128)d).ToString(ci));

        // ---- the decimal faults, observed from a catch handler ----
        // Money arithmetic overflowing its 96-bit mantissa is a RECOVERABLE failure in
        // .NET (OverflowException), so it must not end the process. Type name only; the
        // messages are localized.
        //
        // decimal / 0m and % 0m are deliberately NOT probed: the runtime has no
        // DivideByZeroException, so they still abort, and a section asserting them would
        // be asserting the abort.
        // decimal.MaxValue is a const, so every operand here goes through a local
        // first — otherwise Roslyn folds the expression and refuses to compile it
        // (CS0463) instead of letting the runtime raise the fault under test.
        decimal max = decimal.MaxValue;
        double huge = 1e30;
        Catches("decimal.MaxValue * 2", () => { decimal r = max * 2m; });
        Catches("decimal.MaxValue + MaxValue", () => { decimal r = max + max; });
        Catches("decimal.MaxValue * MaxValue", () => { decimal r = max * max; });
        Catches("-decimal.MaxValue - MaxValue", () => { decimal r = -max - max; });
        Catches("(decimal)1e30", () => { decimal r = (decimal)huge; });
        // Convert.ChangeType(string) is the one caller that reaches the runtime's
        // plain decimal parse entry point (decimal.Parse lowers elsewhere).
        Catches("ChangeType(\"nope\", decimal)",
            () => { object r = Convert.ChangeType("nope", typeof(decimal), ci); });
        Catches("ChangeType(\"1.25\", decimal)",
            () => { object r = Convert.ChangeType("1.25", typeof(decimal), ci); });

        // A typed catch selects the fault over a broader one, and a finally on
        // the unwind path runs.
        try
        {
            decimal r = max * 2m;
            Console.WriteLine("unreachable " + r.ToString(ci));
        }
        catch (OverflowException)
        {
            Console.WriteLine("typed catch: OverflowException");
        }
        catch (Exception)
        {
            Console.WriteLine("typed catch: fell through to Exception");
        }

        try
        {
            try
            {
                decimal r = max * 2m;
                Console.WriteLine("unreachable " + r.ToString(ci));
            }
            finally
            {
                Console.WriteLine("finally ran");
            }
        }
        catch (OverflowException)
        {
            Console.WriteLine("caught after finally");
        }

        // Recovery is real: decimal arithmetic keeps working after the faults.
        Console.WriteLine("after faults: " + (1.5m + 0.25m).ToString(ci));

        // A running total over rows that overflow partway: the shape this
        // section exists for — one bad row must not take the program down.
        decimal[] rows = { 1.5m, max, 2.25m, max, 3m };
        decimal total = 0m;
        int bad = 0;
        foreach (decimal row in rows)
        {
            try
            {
                total = checked(total + row * 2m);
            }
            catch (OverflowException)
            {
                bad++;
            }
        }
        Console.WriteLine("sum loop: total=" + total.ToString(ci) + " bad=" + bad);
    }

    static void Catches(string what, Action body)
    {
        try
        {
            body();
            Console.WriteLine(what + " -> no throw");
        }
        catch (Exception e)
        {
            Console.WriteLine(what + " -> " + e.GetType().Name);
        }
    }
}
