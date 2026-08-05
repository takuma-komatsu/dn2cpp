#nullable enable
using System;
using System.Globalization;

// TimeSpan reached through a generic constrained callvirt, and the span-based TryParseExact.
// ArgumentOutOfRangeException.ThrowIf{GreaterThan,LessThanOrEqual}<TimeSpan> emit a
// `constrained. TimeSpan callvirt IComparable<TimeSpan>::CompareTo(!0)`; a generic helper
// bounded by IComparable<T> / IEquatable<T> does the same for CompareTo / Equals. The callee
// signature decodes with the interface's own !0 unresolved (it is not the caller's method
// type parameter), so the TimeSpan intrinsic must accept the constrained-self argument
// (IsTimeSpanOrConstrainedSelf). TryParseExact's ReadOnlySpan<char> overload — the reach
// shape of TimeZoneInfo.TZif_ParseOffsetString — lowers by materializing both spans to
// strings and reusing the string parser. Diffed exactly against real .NET (culture-invariant).
namespace TimeSpanConstrained;

static class Program
{
    static readonly CultureInfo ci = CultureInfo.InvariantCulture;

    static int GenCompare<T>(T x, T y) where T : IComparable<T> => x.CompareTo(y);
    static bool GenEquals<T>(T x, T y) where T : IEquatable<T> => x.Equals(y);

    internal static void __GateEntry()
    {
        var a = TimeSpan.FromSeconds(5);
        var b = TimeSpan.FromSeconds(10);

        Console.WriteLine("-- constrained CompareTo --");
        Console.WriteLine(GenCompare(a, b));   // negative
        Console.WriteLine(GenCompare(b, a));   // positive
        Console.WriteLine(GenCompare(a, a));   // 0

        Console.WriteLine("-- constrained Equals --");
        Console.WriteLine(GenEquals(a, a));    // True
        Console.WriteLine(GenEquals(a, b));    // False

        Console.WriteLine("-- ArgumentOutOfRangeException.ThrowIfGreaterThan(TimeSpan) --");
        ArgumentOutOfRangeException.ThrowIfGreaterThan(a, b);   // 5 > 10 : no throw
        Console.WriteLine("no-throw a<=b");
        try { ArgumentOutOfRangeException.ThrowIfGreaterThan(b, a); Console.WriteLine("MISSED"); }
        catch (ArgumentOutOfRangeException) { Console.WriteLine("threw b>a"); }

        Console.WriteLine("-- ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(TimeSpan) --");
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(b, a); // 10 <= 5 : no throw
        Console.WriteLine("no-throw b>a");
        try { ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(a, b); Console.WriteLine("MISSED"); }
        catch (ArgumentOutOfRangeException) { Console.WriteLine("threw a<=b"); }

        Console.WriteLine("-- TimeSpan.TryParseExact (span) --");
        ReadOnlySpan<char> in1 = "01:02:03".AsSpan();
        ReadOnlySpan<char> fmtC = "c".AsSpan();
        Console.WriteLine(TimeSpan.TryParseExact(in1, fmtC, ci, out var r1) + " " + r1.Ticks);
        ReadOnlySpan<char> in2 = "1.02:03:04.0050000".AsSpan();
        Console.WriteLine(TimeSpan.TryParseExact(in2, fmtC, ci, out var r2) + " " + r2.Ticks);
        ReadOnlySpan<char> bad = "not a span".AsSpan();
        Console.WriteLine(TimeSpan.TryParseExact(bad, fmtC, ci, out var r3) + " " + r3.Ticks);
    }
}
