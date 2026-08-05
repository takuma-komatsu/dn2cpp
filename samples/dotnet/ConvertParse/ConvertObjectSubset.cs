#nullable disable
using System;
using System.Globalization;

// The Convert.To*(object, IFormatProvider)
// overloads dn2cpp itself uses for attribute-value rendering (CppEmitter.RenderAttrValue
// + RenderPrimitiveAttrLiteral) but that had no intrinsic mapping. These take a *boxed*
// source (object) and dispatch on its runtime type via IConvertible — distinct from the
// existing statically-typed primitive/string overloads. The provider is InvariantCulture
// everywhere in dn2cpp (it is ignored: an integer/float's invariant text is the default).
//
// Covered: ToInt64 / ToInt32 / ToBoolean / ToChar / ToUInt32 / ToUInt64 / ToSingle /
// ToDouble / ToString.
// Unsigned results are widened/split to a signed type before printing so the gate proves
// the *conversion* without coupling to the separate uint/ulong ToString path. Output
// diffs exact vs real .NET. CoreLib only (no Linq shim).
namespace ConvertObjectSubset;

internal static class Program
{
    internal static int __GateEntry()
    {
        IFormatProvider ci = CultureInfo.InvariantCulture;

        // ToInt64(object): boxed integral sources widen (the Int16/Byte/enum-underlying
        // attr code paths route through ToInt64), boxed long is identity.
        object i32 = 42;
        object neg = -7;
        object i64 = 9000000000L;
        object i16 = (short)-5;
        object u8 = (byte)200;
        Console.WriteLine($"i64={Convert.ToInt64(i32, ci)}/{Convert.ToInt64(neg, ci)}/{Convert.ToInt64(i64, ci)}/{Convert.ToInt64(i16, ci)}/{Convert.ToInt64(u8, ci)}");

        // ToInt32(object[, provider]): boxed integral sources narrow through the
        // range-checked i64 path; a boxed double banker's-rounds (2.5 -> 2, 3.5 -> 4);
        // a boxed numeric string parses; null is 0. Both the 1-arg and provider forms.
        object d25 = 2.5;
        object d35 = 3.5;
        object s123 = "123";
        object inRange = (long)int.MaxValue;
        Console.WriteLine($"i32={Convert.ToInt32(i32, ci)}/{Convert.ToInt32(neg)}/{Convert.ToInt32(i16, ci)}/{Convert.ToInt32(u8)}/{Convert.ToInt32(inRange, ci)}");
        Console.WriteLine($"i32r={Convert.ToInt32(d25, ci)}/{Convert.ToInt32(d35)}/{Convert.ToInt32(s123, ci)}/{Convert.ToInt32((object)null, ci)}");

        // ToBoolean(object): boxed bool is identity; a boxed number is != 0.
        object bt = true;
        object bf = false;
        object n5 = 5;
        object n0 = 0;
        Console.WriteLine($"bool={(Convert.ToBoolean(bt, ci) ? 1 : 0)}/{(Convert.ToBoolean(bf, ci) ? 1 : 0)}/{(Convert.ToBoolean(n5, ci) ? 1 : 0)}/{(Convert.ToBoolean(n0, ci) ? 1 : 0)}");

        // ToChar(object): boxed char -> its code point.
        object cA = 'A';
        object cz = 'z';
        Console.WriteLine($"char={(int)Convert.ToChar(cA, ci)}/{(int)Convert.ToChar(cz, ci)}");

        // ToUInt32(object): widen the uint result to long so it prints unsigned-correct.
        object us = (uint)123;
        object ubig = (uint)3000000000;
        Console.WriteLine($"u32={(long)Convert.ToUInt32(us, ci)}/{(long)Convert.ToUInt32(ubig, ci)}");

        // ToUInt64(object): split into hi/lo 32-bit halves (both fit a signed long) so a
        // value above long.MaxValue prints correctly regardless of the ulong ToString path.
        object ul = (ulong)123;
        object ulbig = 18000000000000000000UL;
        ulong r = Convert.ToUInt64(ulbig, ci);
        Console.WriteLine($"u64={(long)Convert.ToUInt64(ul, ci)} hi={(long)(r >> 32)} lo={(long)(r & 0xFFFFFFFFUL)}");

        // ToSingle / ToDouble(object): widen numeric source to float/double (clean values).
        object f1 = 1.5f;
        object f2 = -2.25f;
        object d1 = 3.25;
        object d2 = -10.5;
        Console.WriteLine($"single={Convert.ToSingle(f1, ci)}/{Convert.ToSingle(f2, ci)}");
        Console.WriteLine($"double={Convert.ToDouble(d1, ci)}/{Convert.ToDouble(d2, ci)}");

        // ToString(object[, provider]): the boxed value's virtual ToString —
        // boxed primitives format like their own ToString, a string passes through,
        // an overridden ToString dispatches, and null is string.Empty (bracketed so
        // the "" is visible in the diff), unlike Convert.ToString((string)null),
        // which stays null. Both the 1-arg and provider forms.
        object si = 42;
        object sl = -9000000000L;
        object sd = 3.25;
        object sb = true;
        object sc = 'A';
        object ss = "pass-through";
        Console.WriteLine($"str={Convert.ToString(si, ci)}/{Convert.ToString(sl)}/{Convert.ToString(sd, ci)}/{Convert.ToString(sb)}/{Convert.ToString(sc, ci)}");
        Console.WriteLine($"strs=[{Convert.ToString(ss, ci)}] null=[{Convert.ToString((object)null, ci)}] null1=[{Convert.ToString((object)null)}]");
        object custom = new NamedThing();
        Console.WriteLine($"strv={Convert.ToString(custom, ci)}/{Convert.ToString(custom)}");

        GetTypeCodes();
        return 0;
    }

    // A user type implementing IConvertible — the one shape where "ask the value" and
    // "look at the type" disagree: real .NET answers Convert.GetTypeCode by calling
    // ic.GetTypeCode(), which can report anything (Int32 here would be a lie about a
    // class whose own TypeCode is Object). dn2cpp cannot dispatch that body, so it
    // refuses loudly instead of reporting Object.
    //
    // GetTypeCode throws PlatformNotSupportedException so the row is DIFFABLE against
    // real .NET, the same trick SpanComparerSubset's throwing comparer uses: real .NET
    // honors IConvertible and reaches this throw; dn2cpp refuses the dispatch and
    // throws before reaching it; both print the same line. What the row pins is the
    // invariant that outlives either implementation — this case is not silently
    // answered with Object. A regression to answering Object prints "Object" here and
    // goes red against real .NET.
    private sealed class ThrowingConvertible : IConvertible
    {
        public TypeCode GetTypeCode() => throw new PlatformNotSupportedException("asked");
        public bool ToBoolean(IFormatProvider p) => true;
        public byte ToByte(IFormatProvider p) => 1;
        public char ToChar(IFormatProvider p) => 'x';
        public DateTime ToDateTime(IFormatProvider p) => DateTime.MinValue;
        public decimal ToDecimal(IFormatProvider p) => 1m;
        public double ToDouble(IFormatProvider p) => 1;
        public short ToInt16(IFormatProvider p) => 1;
        public int ToInt32(IFormatProvider p) => 1;
        public long ToInt64(IFormatProvider p) => 1;
        public sbyte ToSByte(IFormatProvider p) => 1;
        public float ToSingle(IFormatProvider p) => 1;
        public string ToString(IFormatProvider p) => "1";
        public object ToType(Type t, IFormatProvider p) => 1;
        public ushort ToUInt16(IFormatProvider p) => 1;
        public uint ToUInt32(IFormatProvider p) => 1;
        public ulong ToUInt64(IFormatProvider p) => 1;
    }

    // A user class whose overridden ToString is what Convert.ToString(object) must
    // reach — the virtual-dispatch half of the mapping, distinct from the
    // boxed-primitive formatters.
    private sealed class NamedThing
    {
        public override string ToString() => "NamedThing:ok";
    }

    private enum LongEnum : long { A = 1 }
    private struct PlainStruct { public int X; }

    // Convert.GetTypeCode(object) over every code reachable from a value. Each is
    // printed as name+number: the numbers are an ABI real .NET pins, and this is a
    // diff gate, so real .NET is what says they are right.
    private static void GetTypeCodes()
    {
        Show("null", null);
        Show("bool", true);
        Show("char", 'a');
        Show("sbyte", (sbyte)1);
        Show("byte", (byte)1);
        Show("short", (short)1);
        Show("ushort", (ushort)1);
        Show("int", 1);
        Show("uint", 1u);
        Show("long", 1L);
        Show("ulong", 1UL);
        Show("float", 1f);
        Show("double", 1d);
        Show("decimal", 1m);
        Show("DateTime", DateTime.MinValue);
        Show("string", "s");
        Show("DBNull", DBNull.Value);
        // An enum reports its UNDERLYING integer's code, not Object.
        Show("enum:long", LongEnum.A);
        // Types with no code of their own: Object, and dn2cpp agrees without asking.
        Show("object", new object());
        Show("int[]", new int[1]);
        Show("struct", new PlainStruct());
        Show("IntPtr", (IntPtr)1);

        // The unanswerable shape: loud and catchable on both sides (see the type's
        // comment for why the two reach the same line by different routes). Only the
        // exception type is printed — the messages differ and are not the assertion.
        try
        {
            Console.WriteLine("iconvertible=" + Convert.GetTypeCode(new ThrowingConvertible()));
        }
        catch (PlatformNotSupportedException)
        {
            Console.WriteLine("iconvertible=PlatformNotSupportedException");
        }
    }

    private static void Show(string label, object v) =>
        Console.WriteLine($"typecode {label}={Convert.GetTypeCode(v)}({(int)Convert.GetTypeCode(v)})");
}
