using System;
using System.Globalization;

// Itable DISPATCH on a boxed built-in, for every interface whose type TEST answers True.
// The hand-written primitive/Decimal/date-time type-infos are const, so no init prologue
// can wire a map onto them (the route enums and String take); dn2cpp_wellknown_itf_mask/
// _bit answers both questions instead. Each block prints the TEST beside the CALL,
// because the PAIR is the invariant — a test that admits a pair dispatch cannot serve
// does not print "throws", it aborts.
//
// Every line is diffed against real .NET. A line printing bare "throws" is asserting
// that both sides refuse, not which exception they raise — IConvertible.ToDateTime off
// a numeric box is InvalidCast there and the runtime matrix's InvalidOperation here.
namespace BoxedBuiltinItfDispatchSubset;

internal static class Program
{
    private static void Tst(string label, bool test, string call) =>
        Console.WriteLine(label + ": is=" + test + " call=" + call);

    private static string Throws(Func<string> f)
    {
        try
        {
            return f();
        }
        catch (Exception)
        {
            return "throws";
        }
    }

    internal static void Run()
    {
        IFormatProvider inv = CultureInfo.InvariantCulture;

        // -- IFormattable, reached through the shared table. --
        object oi = 42;
        Tst("int IFormattable", oi is IFormattable, ((IFormattable)oi).ToString("X", inv));
        object ol = -1L;
        Tst("long IFormattable", ol is IFormattable, ((IFormattable)ol).ToString("X", inv));
        object ob = (byte)200;
        Tst("byte IFormattable", ob is IFormattable, ((IFormattable)ob).ToString("X", inv));
        object od = 2.5;
        Tst("double IFormattable", od is IFormattable, ((IFormattable)od).ToString("F2", inv));
        object om = 3.5m;
        Tst("decimal IFormattable", om is IFormattable, ((IFormattable)om).ToString("F1", inv));
        object on = (nint)255;
        Tst("nint IFormattable", on is IFormattable, ((IFormattable)on).ToString("X", inv));
        object oc = 'q';
        // Char.ToString(format, provider) ignores the spec on both sides.
        Tst("char IFormattable", oc is IFormattable, ((IFormattable)oc).ToString("X", inv));

        // -- IComparable (non-generic): the CompareTo(object) every built-in declares. --
        Tst("int IComparable", oi is IComparable,
            ((IComparable)oi).CompareTo(40) + "," + ((IComparable)oi).CompareTo(42)
            + "," + ((IComparable)oi).CompareTo(99) + "," + ((IComparable)oi).CompareTo(null));
        Tst("double IComparable", od is IComparable,
            ((IComparable)od).CompareTo(1.5) + "," + ((IComparable)od).CompareTo(2.5));
        // Math.Sign, because .NET's SUB-WORD CompareTo bodies return `m_value - value`
        // — the difference, not the sign ('q'.CompareTo('a') is 16 there and 1 here.
        // Int32/Int64/the floats do return the sign, so those lines above are exact).
        // dn2cpp normalizes every ordering to -1/0/+1 at one window
        // (MethodCompiler.TryCompareLValue), so the direct call diverges by the same
        // magnitude and the two mouths agree with each other, which is what matters.
        Tst("char IComparable", oc is IComparable, Math.Sign(((IComparable)oc).CompareTo('a')).ToString());
        Tst("byte IComparable", ob is IComparable, Math.Sign(((IComparable)ob).CompareTo((byte)1)).ToString());
        object osh = (short)-5;
        Tst("short IComparable", osh is IComparable, Math.Sign(((IComparable)osh).CompareTo((short)7)).ToString());
        Tst("decimal IComparable", om is IComparable,
            ((IComparable)om).CompareTo(2.5m) + "," + ((IComparable)om).CompareTo(3.5m));
        object ots = TimeSpan.FromSeconds(5);
        Tst("TimeSpan IComparable", ots is IComparable,
            ((IComparable)ots).CompareTo(TimeSpan.FromSeconds(3)).ToString());
        object odt = new DateTime(2020, 1, 2, 3, 4, 5, DateTimeKind.Utc);
        Tst("DateTime IComparable", odt is IComparable,
            ((IComparable)odt).CompareTo(new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc)).ToString());
        object odo = new DateOnly(2020, 1, 2);
        Tst("DateOnly IComparable", odo is IComparable,
            ((IComparable)odo).CompareTo(new DateOnly(2020, 1, 3)).ToString());
        object oto = new TimeOnly(3, 4, 5);
        Tst("TimeOnly IComparable", oto is IComparable,
            ((IComparable)oto).CompareTo(new TimeOnly(3, 4, 5)).ToString());
        Tst("nint IComparable", on is IComparable, ((IComparable)on).CompareTo((nint)3).ToString());
        // A foreign runtime type is an ArgumentException on both sides (subtype elided).
        Tst("int IComparable x long", oi is IComparable, Throws(() => ((IComparable)oi).CompareTo(40L).ToString()));

        // -- IConvertible: all seventeen declared slots, off one boxed int. --
        IConvertible ci = (IConvertible)oi;
        Tst("int IConvertible", oi is IConvertible, ci.GetTypeCode().ToString());
        Console.WriteLine("int IConvertible bool/char: " + ci.ToBoolean(inv) + "," + (int)ci.ToChar(inv));
        Console.WriteLine("int IConvertible i1/u1/i2/u2: " + ci.ToSByte(inv) + "," + ci.ToByte(inv)
            + "," + ci.ToInt16(inv) + "," + ci.ToUInt16(inv));
        Console.WriteLine("int IConvertible i4/u4/i8/u8: " + ci.ToInt32(inv) + "," + ci.ToUInt32(inv)
            + "," + ci.ToInt64(inv) + "," + ci.ToUInt64(inv));
        Console.WriteLine("int IConvertible r4/r8/dec: " + ci.ToSingle(inv) + "," + ci.ToDouble(inv)
            + "," + ci.ToDecimal(inv));
        Console.WriteLine("int IConvertible str/type: " + ci.ToString(inv) + "," + ci.ToType(typeof(long), inv));
        // A numeric source has no DateTime conversion on either side.
        Console.WriteLine("int IConvertible datetime: " + Throws(() => ci.ToDateTime(inv).ToString()));
        // The range check is the Convert.To* one: 300 does not fit a byte.
        Console.WriteLine("int IConvertible byte overflow: "
            + Throws(() => ((IConvertible)(object)300).ToByte(inv).ToString()));

        // Other boxed built-ins through the same table.
        Console.WriteLine("long IConvertible: " + ((IConvertible)ol).ToInt64(inv)
            + "," + ((IConvertible)ol).GetTypeCode());
        Console.WriteLine("double IConvertible: " + ((IConvertible)od).ToInt32(inv)
            + "," + ((IConvertible)od).ToDouble(inv) + "," + ((IConvertible)od).ToString(inv));
        Console.WriteLine("decimal IConvertible: " + ((IConvertible)om).ToInt32(inv)
            + "," + ((IConvertible)om).ToDouble(inv) + "," + ((IConvertible)om).GetTypeCode());
        Console.WriteLine("bool IConvertible: " + ((IConvertible)(object)true).ToInt32(inv)
            + "," + ((IConvertible)(object)true).ToString(inv) + "," + ((IConvertible)(object)true).GetTypeCode());
        Console.WriteLine("char IConvertible: " + ((IConvertible)oc).ToInt32(inv)
            + "," + ((IConvertible)oc).ToString(inv));

        // -- The symmetry itself, as a cross product. -- Every boxed built-in the
        // runtime models against all four interfaces: where the TEST says yes, the row
        // CALLS. A pair the test admits but dispatch cannot serve does not print
        // "throws" here — it aborts the process, so this matrix is the assert.
        // CompareTo is against the value itself and Sign'd, so the sub-word difference
        // convention above cannot enter; formatting asks for no spec, whose answer is
        // the same default text ToString() gives.
        object[] vals =
        {
            true, 'q', (sbyte)-1, (byte)2, (short)-3, (ushort)4, 5, 6u, 7L, 8UL,
            1.5f, 2.5, 3.5m, (nint)9, (nuint)10,
            new DateTime(2020, 1, 2, 3, 4, 5, DateTimeKind.Utc),
            new DateTimeOffset(2020, 1, 2, 3, 4, 5, TimeSpan.Zero),
            TimeSpan.FromSeconds(1), new DateOnly(2020, 1, 2), new TimeOnly(3, 4, 5),
            "s", DayOfWeek.Monday, default(Guid),
        };
        foreach (object v in vals)
        {
            string row = v.GetType().Name;
            row += " Cv=" + (v is IConvertible ? ((IConvertible)v).GetTypeCode().ToString() : "-");
            row += " Cm=" + (v is IComparable ? Math.Sign(((IComparable)v).CompareTo(v)).ToString() : "-");
            row += " Fm=" + (v is IFormattable ? ((IFormattable)v).ToString(null, inv) : "-");
            // A boxed ENUM is the control that a REAL map, found through the base chain,
            // still wins over the fallback. Its ISpanFormattable slot is the one that is
            // never an impl: the cut TryFormat, routed to the runtime formatter.
            row += " Sf=" + (v is not ISpanFormattable ? "-" : SpanFmt(v, null, 64));
            Console.WriteLine(row);
        }

        // -- ISpanFormattable: the span-write contract off a boxed built-in. --
        Console.WriteLine("int ISpanFormattable: " + (oi is ISpanFormattable) + " " + SpanFmt(oi, "D5", 16));
        Console.WriteLine("double ISpanFormattable: " + (od is ISpanFormattable) + " " + SpanFmt(od, "F3", 16));
        Console.WriteLine("long ISpanFormattable: " + (ol is ISpanFormattable) + " " + SpanFmt(ol, "X", 16));
        // Too short: untouched, 0 written, false — the same on both sides.
        Console.WriteLine("int ISpanFormattable short: " + SpanFmt(oi, "D5", 2));
        // No spec at all: the default text.
        Console.WriteLine("int ISpanFormattable nospec: " + SpanFmt(oi, null, 16));
    }

    private static string SpanFmt(object boxed, string format, int capacity)
    {
        char[] buf = new char[capacity];
        bool ok = ((ISpanFormattable)boxed).TryFormat(
            buf.AsSpan(), out int written, format is null ? default : format.AsSpan(), CultureInfo.InvariantCulture);
        return ok + ":" + written + ":" + new string(buf, 0, written);
    }
}
