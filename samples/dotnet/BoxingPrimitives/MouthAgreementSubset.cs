using System;
using System.Globalization;

// ONE surface, several MOUTHS: the question is not whether each answer is reachable but
// whether the mouths AGREE, so every block asks the same thing more than one way and
// prints all of them on one line. Three surfaces, each of which can answer differently
// per mouth:
//
//   (a) A boxed enum's ISpanFormattable.TryFormat. CvEnumTryFormat cuts System.Enum's
//       impl (its IL reaches an InternalCall), so the cut needs a route on the INTERFACE
//       mouth as well as the constrained one — without it the call aborts through the
//       enum map's dispatch trap rather than throwing. Asserted beside the IFormattable
//       mouth (the transpiled System.Enum body), because routing it is only right if the
//       two produce the same text.
//   (b) The self-instantiated IComparable<T>/IEquatable<T> over a boxed built-in. The TEST
//       is printed beside the CALL for every one: a pair the test admits that dispatch
//       cannot serve does not print "throws", it aborts.
//   (c) A format specifier on the date family, through all four mouths (interpolation
//       hole, string.Format hole, IFormattable dispatch, ISpanFormattable dispatch), which
//       must render the spec exactly as real .NET does at each.
//
// The bucket carries no reference beyond CoreLib; everything here is System.
namespace MouthAgreementSubset;

[Flags]
internal enum Mask { None = 0, A = 1, B = 2, C = 4 }

// Underlying long, with a member past int32 range: the (name, value) table the runtime
// enum formatter reads used to truncate it, so "Big" formatted as its low word.
internal enum Wide : long { Small = 3, Big = 5000000000L }

internal static class Program
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    // ok:written:text, or the exception's name — a spec the type refuses is part of the
    // contract (Enum.TryFormat raises FormatException on an unknown one).
    private static string SpanFmt(object boxed, string format, int capacity)
    {
        char[] buf = new char[capacity];
        try
        {
            bool ok = ((ISpanFormattable)boxed).TryFormat(buf.AsSpan(), out int written,
                format is null ? default : format.AsSpan(), Inv);
            return ok + ":" + written + ":" + new string(buf, 0, written);
        }
        catch (Exception e)
        {
            return e.GetType().Name;
        }
    }

    private static string Try(Func<string> f)
    {
        try
        {
            return f();
        }
        catch (Exception e)
        {
            return e.GetType().Name;
        }
    }

    internal static void Run()
    {
        // -- (a) boxed enum: the span mouth beside the string mouth. --
        object mon = DayOfWeek.Monday;
        foreach (string spec in new string[] { null, "G", "D", "X", "F", "Q" })
        {
            Console.WriteLine("enum " + (spec ?? "<none>")
                + ": span=" + SpanFmt(mon, spec, 64)
                + " str=" + Try(() => ((IFormattable)mon).ToString(spec, Inv)));
        }
        // Destination too short: untouched, 0 written, false — not an exception.
        Console.WriteLine("enum short: " + SpanFmt(mon, null, 2));
        object flags = Mask.A | Mask.C;
        Console.WriteLine("flags: span=" + SpanFmt(flags, null, 64)
            + " X=" + SpanFmt(flags, "X", 64));
        object wide = Wide.Big;
        Console.WriteLine("wide: span=" + SpanFmt(wide, null, 64)
            + " D=" + SpanFmt(wide, "D", 64) + " X=" + SpanFmt(wide, "X", 64));
        Console.WriteLine("undefined: " + SpanFmt((DayOfWeek)99, null, 64));

        // -- (b) IComparable<T> / IEquatable<T>: the test beside the call. --
        // CompareTo is Sign'd. .NET's sub-word CompareTo returns the DIFFERENCE
        // ('q'.CompareTo('a') == 16) and dn2cpp's static lowering matches that, but
        // every dispatched mouth here — this one and the constrained generic one —
        // answers through the ordering ladder, which is -1/0/+1. Both satisfy
        // IComparable's documented contract; Sign is what the two mouths share.
        SelfGen<int>(5, 4, 5);
        SelfGen<long>(7L, 9L, 7L);
        SelfGen<double>(2.5, 1.0, 2.5);
        SelfGen<float>(1.5f, 9f, 1.5f);
        SelfGen<decimal>(3.5m, 1m, 3.5m);
        SelfGen<bool>(true, false, true);
        SelfGen<char>('q', 'a', 'q');
        SelfGen<short>((short)-3, (short)1, (short)-3);
        SelfGen<byte>((byte)200, (byte)3, (byte)200);
        SelfGen<uint>(6u, 1u, 6u);
        SelfGen<ulong>(8UL, 1UL, 8UL);
        SelfGen<nint>((nint)9, (nint)1, (nint)9);
        SelfGen<DateTime>(new DateTime(2020, 1, 2, 3, 4, 5, DateTimeKind.Utc), DateTime.MinValue,
            new DateTime(2020, 1, 2, 3, 4, 5, DateTimeKind.Utc));
        SelfGen<DateTimeOffset>(new DateTimeOffset(2020, 1, 2, 3, 4, 5, TimeSpan.Zero),
            DateTimeOffset.MinValue, new DateTimeOffset(2020, 1, 2, 3, 4, 5, TimeSpan.Zero));
        SelfGen<TimeSpan>(TimeSpan.FromSeconds(1), TimeSpan.Zero, TimeSpan.FromSeconds(1));
        SelfGen<DateOnly>(new DateOnly(2020, 1, 2), DateOnly.MinValue, new DateOnly(2020, 1, 2));
        SelfGen<TimeOnly>(new TimeOnly(3, 4, 5), TimeOnly.MinValue, new TimeOnly(3, 4, 5));
        // String is NOT served by the fallback — its own map carries both rows — so it is
        // here as the control that adding the fallback did not take the relation over.
        SelfGen<string>("s", "a", "s");

        // The relation the fallback must keep DENYING: a different argument, and an
        // enum, which implements neither generic interface in .NET.
        object five = 5;
        Console.WriteLine("int is IComparable<long>: " + (five is IComparable<long>)
            + " " + Try(() => ((IComparable<long>)five).CompareTo(1L).ToString()));
        Console.WriteLine("int is IEquatable<long>: " + (five is IEquatable<long>));
        Console.WriteLine("enum is IComparable<DayOfWeek>: " + (mon is IComparable<DayOfWeek>));
        Console.WriteLine("enum is IEquatable<DayOfWeek>: " + (mon is IEquatable<DayOfWeek>));
        // The reflection surface answers off the same rule (dn2cpp_typeinfo_assignable).
        Console.WriteLine("asgn IComparable<int> <- int: "
            + typeof(IComparable<int>).IsAssignableFrom(typeof(int))
            + " <- long: " + typeof(IComparable<int>).IsAssignableFrom(typeof(long))
            + " IEquatable<int> <- int: " + typeof(IEquatable<int>).IsAssignableFrom(typeof(int))
            + " IComparable<DayOfWeek> <- DayOfWeek: "
            + typeof(IComparable<DayOfWeek>).IsAssignableFrom(typeof(DayOfWeek)));

        // -- (c) the date family's format spec, all four mouths on one line. --
        var dto = new DateTimeOffset(2020, 1, 2, 3, 4, 5, TimeSpan.Zero);
        Console.WriteLine("dto: interp=" + Try(() => $"{dto:O}")
            + " fmt=" + Try(() => string.Format(Inv, "{0:O}", dto))
            + " itf=" + Try(() => ((IFormattable)(object)dto).ToString("O", Inv))
            + " span=" + SpanFmt(dto, "O", 64));
        var don = new DateOnly(2020, 1, 2);
        Console.WriteLine("don: interp=" + Try(() => $"{don:yyyy-MM-dd}")
            + " fmt=" + Try(() => string.Format(Inv, "{0:yyyy-MM-dd}", don))
            + " itf=" + Try(() => ((IFormattable)(object)don).ToString("yyyy-MM-dd", Inv))
            + " span=" + SpanFmt(don, "yyyy-MM-dd", 64));
        var ton = new TimeOnly(3, 4, 5);
        Console.WriteLine("ton: interp=" + Try(() => $"{ton:HH-mm}")
            + " fmt=" + Try(() => string.Format(Inv, "{0:HH-mm}", ton))
            + " itf=" + Try(() => ((IFormattable)(object)ton).ToString("HH-mm", Inv))
            + " span=" + SpanFmt(ton, "HH-mm", 64));
        // The hole's fallback for a type that is genuinely not IFormattable stays the
        // default text — real .NET ignores the spec there, so the silent answer that
        // was wrong above is right here, and only the mouths must not diverge.
        Console.WriteLine("plain: " + string.Format(Inv, "{0:Q}", new object())
            + " char=" + string.Format(Inv, "{0:Q}", 'z')
            + " bool=" + string.Format(Inv, "{0:Q}", true)
            + " string=" + string.Format(Inv, "{0:Q}", "abc"));
    }

    // The boxed self-instantiated pair for one T: the type test, the dispatched
    // CompareTo/Equals, and the constrained-generic mouth beside them.
    private static void SelfGen<T>(T self, T lower, T same) where T : IComparable<T>, IEquatable<T>
    {
        object box = self;
        Console.WriteLine(typeof(T).Name
            + " isCmp=" + (box is IComparable<T>)
            + " cmp=" + Try(() => Math.Sign(((IComparable<T>)box).CompareTo(lower)).ToString())
            + " isEq=" + (box is IEquatable<T>)
            + " eq=" + Try(() => ((IEquatable<T>)box).Equals(same).ToString())
            + " ceq=" + Try(() => (!self.Equals(lower)).ToString()));
    }
}
