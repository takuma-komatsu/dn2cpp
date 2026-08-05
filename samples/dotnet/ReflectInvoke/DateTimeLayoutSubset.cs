#nullable enable
// SUBJECT: System.DateTime's LAYOUT, live-diffed against real .NET. dn2cpp
// lowers DateTime to a hand-written C++ struct, so its size is that struct's
// sizeof — 8, with the kind packed into the top two bits of the ticks word, as
// .NET lays it out. Sizes print as NUMBERS here; ReflectIntrinsicSizeOfSubset's
// companion lines assert reader agreement instead, and neither subsumes the
// other. Both matter because a static Unsafe.SizeOf<T> or IL sizeof is an
// emit-time C++ expression no runtime override can reach.
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DateTimeLayoutSubset;

// Sequential so both sides lay the fields out in declaration order: a
// DateTime-typed field moves every field after it. The ABI contract's F line
// reads "F 1 D System.DateTime" at either width, so a change to DateTime's
// representation must bump AbiContract.LayoutPolicyVersion.
[StructLayout(LayoutKind.Sequential)]
struct Holder
{
    public byte B;
    public DateTime D;
    public int N;
}

static class Program
{
    static int ReflectedSizeOf(Type t) =>
        (int)typeof(Unsafe).GetMethod("SizeOf")!.MakeGenericMethod(t).Invoke(null, null)!;

    // The 64-bit image as bytes: a kind packed anywhere but .NET's top two bits,
    // or ticks at another width, diverges here where no size number would say so.
    static string Raw(DateTime d) =>
        Unsafe.ReadUnaligned<long>(ref Unsafe.As<DateTime, byte>(ref d)).ToString("X16");

    static void Throws(string what, Action a)
    {
        try { a(); Console.WriteLine(what + " -> no throw"); }
        catch (Exception e) { Console.WriteLine(what + " -> " + e.GetType().Name); }
    }

    internal static unsafe void Run()
    {
        Console.WriteLine("== DateTime is eight bytes, on every reader ==");
        Console.WriteLine($"Unsafe.SizeOf<DateTime> = {Unsafe.SizeOf<DateTime>()}");
        Console.WriteLine($"IL sizeof(DateTime) = {sizeof(DateTime)}");
        Console.WriteLine($"reflected SizeOf(DateTime) = {ReflectedSizeOf(typeof(DateTime))}");
        DateTime[] two = new DateTime[2];
        Console.WriteLine($"DateTime[] stride = {(long)Unsafe.ByteOffset(ref two[0], ref two[1])}");

        // Nullable<DateTime> is hasValue + value, so it tracks DateTime's width.
        Console.WriteLine($"Unsafe.SizeOf<DateTime?> = {Unsafe.SizeOf<DateTime?>()}");
        Console.WriteLine($"IL sizeof(DateTime?) = {sizeof(DateTime?)}");
        Console.WriteLine($"reflected SizeOf(DateTime?) = {ReflectedSizeOf(typeof(DateTime?))}");
        DateTime?[] twoN = new DateTime?[2];
        Console.WriteLine($"DateTime?[] stride = {(long)Unsafe.ByteOffset(ref twoN[0], ref twoN[1])}");

        Console.WriteLine($"Unsafe.SizeOf<Holder> = {Unsafe.SizeOf<Holder>()}");

        // Marshal.SizeOf is deliberately not one of the readers: real .NET refuses
        // it for DateTime (AutoLayout), so dn2cpp refuses it too.
        Throws("Marshal.SizeOf(typeof(DateTime))", () => Marshal.SizeOf(typeof(DateTime)));

        Console.WriteLine("== `n * SizeOf` sizes the buffer the arithmetic walks ==");
        // If SizeOf disagreed with the element stride, a buffer sized `n * SizeOf`
        // would under-allocate. Printed as two numbers so a divergence names both.
        foreach (int n in new[] { 1, 2, 7, 64 })
        {
            DateTime[] a = new DateTime[n];
            long span = (long)Unsafe.ByteOffset(ref a[0], ref a[n - 1]) + Unsafe.SizeOf<DateTime>();
            Console.WriteLine($"n={n}: n*SizeOf={n * Unsafe.SizeOf<DateTime>()} spanned={span}");
        }
        foreach (int n in new[] { 1, 3, 16 })
        {
            DateTime?[] a = new DateTime?[n];
            long span = (long)Unsafe.ByteOffset(ref a[0], ref a[n - 1]) + Unsafe.SizeOf<DateTime?>();
            Console.WriteLine($"n?={n}: n*SizeOf={n * Unsafe.SizeOf<DateTime?>()} spanned={span}");
        }

        Console.WriteLine("== the bit image, not merely the width ==");
        Console.WriteLine($"unspecified = {Raw(new DateTime(2024, 3, 5, 6, 7, 8, DateTimeKind.Unspecified))}");
        Console.WriteLine($"utc         = {Raw(new DateTime(2024, 3, 5, 6, 7, 8, DateTimeKind.Utc))}");
        Console.WriteLine($"local       = {Raw(new DateTime(2024, 3, 5, 6, 7, 8, DateTimeKind.Local))}");
        Console.WriteLine($"MinValue    = {Raw(DateTime.MinValue)}");
        Console.WriteLine($"MaxValue    = {Raw(DateTime.MaxValue)}");
        Console.WriteLine($"default     = {Raw(default)}");

        Console.WriteLine("== the kind survives the packing ==");
        DateTime baseline = new DateTime(2024, 3, 5, 6, 7, 8);
        foreach (DateTimeKind k in new[] { DateTimeKind.Unspecified, DateTimeKind.Utc, DateTimeKind.Local })
        {
            DateTime d = DateTime.SpecifyKind(baseline, k);
            Console.WriteLine($"{k}: kind={d.Kind} ticks={d.Ticks} back={DateTime.SpecifyKind(d, DateTimeKind.Unspecified).Ticks}");
        }
        // The tick field is 62 bits, so the widest legal value must still round-trip
        // beside a kind in the top two.
        DateTime wide = DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Local);
        Console.WriteLine($"MaxValue as Local: ticks={wide.Ticks} kind={wide.Kind}");

        Console.WriteLine("== comparison and arithmetic ignore the packed kind ==");
        DateTime u = DateTime.SpecifyKind(baseline, DateTimeKind.Utc);
        DateTime l = DateTime.SpecifyKind(baseline, DateTimeKind.Local);
        Console.WriteLine($"u==l {u == l}, Equals {u.Equals(l)}, CompareTo {u.CompareTo(l)}, hash== {u.GetHashCode() == l.GetHashCode()}");
        Console.WriteLine($"u<l+1t {u < l.AddTicks(1)}, (l+1t)-u = {(l.AddTicks(1) - u).Ticks}");
        Console.WriteLine($"AddDays keeps kind: {u.AddDays(1).Kind} {l.AddDays(1).Kind}");
        Console.WriteLine($"AddMonths keeps kind: {u.AddMonths(1).Kind} {l.AddMonths(1).Kind}");
        Console.WriteLine($"Date keeps kind: {l.Date.Kind} ticks={l.Date.Ticks}");

        Console.WriteLine("== an unrepresentable value is the exception .NET raises ==");
        // Packed, an out-of-range tick count would smear into the kind bits, so the
        // construction path must validate rather than answer.
        Throws("new DateTime(-1)", () => { DateTime r = new DateTime(-1); });
        Throws("new DateTime(long.MaxValue)", () => { DateTime r = new DateTime(long.MaxValue); });
        Throws("MaxValue.AddTicks(1)", () => { DateTime r = DateTime.MaxValue.AddTicks(1); });
        Throws("MinValue.AddTicks(-1)", () => { DateTime r = DateTime.MinValue.AddTicks(-1); });
        Throws("MaxValue.AddDays(1)", () => { DateTime r = DateTime.MaxValue.AddDays(1); });
        Throws("MinValue.AddDays(-1)", () => { DateTime r = DateTime.MinValue.AddDays(-1); });
        Throws("MaxValue.AddYears(1)", () => { DateTime r = DateTime.MaxValue.AddYears(1); });
        Throws("MinValue - 1 tick", () => { DateTime r = DateTime.MinValue - TimeSpan.FromTicks(1); });
        Throws("new DateTime(0, 1, 1)", () => { DateTime r = new DateTime(0, 1, 1); });
        Throws("new DateTime(10000, 1, 1)", () => { DateTime r = new DateTime(10000, 1, 1); });
        Throws("new DateTime(2020, 13, 1)", () => { DateTime r = new DateTime(2020, 13, 1); });
        Throws("new DateTime(2020, 2, 30)", () => { DateTime r = new DateTime(2020, 2, 30); });
        Throws("new DateTime(2020, 1, 1, 24, 0, 0)", () => { DateTime r = new DateTime(2020, 1, 1, 24, 0, 0); });
        // The cumulative day tables are 13 entries, so an unchecked month of 13
        // reads one past the end and answers a plausible number.
        Throws("DateTime.DaysInMonth(2020, 13)", () => { int r = DateTime.DaysInMonth(2020, 13); });
        Console.WriteLine($"DaysInMonth in range: {DateTime.DaysInMonth(2020, 2)} {DateTime.DaysInMonth(2021, 2)} {DateTime.DaysInMonth(2020, 12)}");
        // The boundaries themselves stay constructible.
        Console.WriteLine($"boundaries: {new DateTime(0).Ticks} {new DateTime(3155378975999999999L).Ticks}");

        Console.WriteLine("== round-trip through the wire form ==");
        foreach (DateTimeKind k in new[] { DateTimeKind.Unspecified, DateTimeKind.Utc, DateTimeKind.Local })
        {
            DateTime d = DateTime.SpecifyKind(new DateTime(2024, 3, 5, 6, 7, 8).AddTicks(1234567), k);
            string wire = d.ToString("yyyy-MM-ddTHH:mm:ss.fffffff");
            DateTime back = DateTime.ParseExact(wire, "yyyy-MM-ddTHH:mm:ss.fffffff", null);
            Console.WriteLine($"{k}: {wire} -> ticks match {back.Ticks == d.Ticks}");
        }
    }
}
