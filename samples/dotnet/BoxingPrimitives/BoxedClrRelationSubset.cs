using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Runtime.Serialization;
using System.Text;

// The CLR interfaces a boxed built-in implements beyond the ones its dispatch arm
// serves: generic math, parsing, UTF-8 formatting and serialization. The hand-written
// type-infos answer them from the relation rows the init prologue installs, which name
// only interfaces the image defines; every interface here is named by its own test.
// Each line asks the type test and IsAssignableFrom together, since both read the same
// rows, and the negatives guard against rows that over-admit.
//
// String is asked through IsAssignableFrom alone. IParsable<string> and
// ISpanParsable<string> have no instance members, so String's dispatch map never
// carries them and only its relation rows can answer.
//
// IUtf8SpanFormattable is the one such interface whose call is served: TryFormat
// through the interface writes the same text as the UTF-16 twin, and a destination that
// is too small reports false with nothing written.
namespace BoxedClrRelationSubset;

internal static class Program
{
    private static void Rel(string label, bool test, Type itf, object boxed) =>
        Console.WriteLine(label + ": is=" + test + " assignable=" + itf.IsAssignableFrom(boxed.GetType()));

    private static void Str(string label, Type itf) =>
        Console.WriteLine("string " + label + ": " + itf.IsAssignableFrom(typeof(string)));

    private static void Utf8(string label, object boxed, string format, int capacity)
    {
        string row;
        if (boxed is IUtf8SpanFormattable formattable)
        {
            byte[] buffer = new byte[capacity];
            bool ok = formattable.TryFormat(buffer, out int written, format, CultureInfo.InvariantCulture);
            var hex = new StringBuilder();
            for (int i = 0; i < written; i++)
                hex.Append(buffer[i].ToString("X2", CultureInfo.InvariantCulture));
            row = ok + ":" + written + ":" + hex;
        }
        else
        {
            row = "not IUtf8SpanFormattable";
        }
        Console.WriteLine("utf8 " + label + ": " + row);
    }

    internal static void Run()
    {
        Console.WriteLine("== boxed CLR relations ==");
        object oi = 5;
        object ol = 7L;
        object ob = (byte)2;
        object oc = 'q';
        object ou = 8UL;
        object on = (nint)9;
        object of = 1.5f;
        object od = 2.5;
        object obo = true;
        object om = 3.5m;
        object odt = new DateTime(2020, 1, 2, 3, 4, 5, DateTimeKind.Utc);
        object odto = new DateTimeOffset(2020, 1, 2, 3, 4, 5, TimeSpan.Zero);
        object ots = TimeSpan.FromSeconds(1);
        object odo = new DateOnly(2020, 1, 2);
        object oto = new TimeOnly(3, 4, 5);

        Rel("int INumber<int>", oi is INumber<int>, typeof(INumber<int>), oi);
        Rel("int IBinaryInteger<int>", oi is IBinaryInteger<int>, typeof(IBinaryInteger<int>), oi);
        Rel("int IAdditionOperators<int,int,int>", oi is IAdditionOperators<int, int, int>,
            typeof(IAdditionOperators<int, int, int>), oi);
        Rel("int IMinMaxValue<int>", oi is IMinMaxValue<int>, typeof(IMinMaxValue<int>), oi);
        Rel("int IParsable<int>", oi is IParsable<int>, typeof(IParsable<int>), oi);
        Rel("int ISpanParsable<int>", oi is ISpanParsable<int>, typeof(ISpanParsable<int>), oi);
        Rel("int IUtf8SpanFormattable", oi is IUtf8SpanFormattable, typeof(IUtf8SpanFormattable), oi);
        Rel("int ISerializable", oi is ISerializable, typeof(ISerializable), oi);
        Rel("int INumber<long>", oi is INumber<long>, typeof(INumber<long>), oi);
        Rel("int IMinMaxValue<long>", oi is IMinMaxValue<long>, typeof(IMinMaxValue<long>), oi);
        Rel("long INumber<long>", ol is INumber<long>, typeof(INumber<long>), ol);
        Rel("long IBinaryInteger<long>", ol is IBinaryInteger<long>, typeof(IBinaryInteger<long>), ol);
        Rel("long IMinMaxValue<long>", ol is IMinMaxValue<long>, typeof(IMinMaxValue<long>), ol);
        Rel("long INumber<int>", ol is INumber<int>, typeof(INumber<int>), ol);
        Rel("byte IBinaryInteger<byte>", ob is IBinaryInteger<byte>, typeof(IBinaryInteger<byte>), ob);
        Rel("byte IUtf8SpanFormattable", ob is IUtf8SpanFormattable, typeof(IUtf8SpanFormattable), ob);
        Rel("char IBinaryInteger<char>", oc is IBinaryInteger<char>, typeof(IBinaryInteger<char>), oc);
        Rel("char ISpanParsable<char>", oc is ISpanParsable<char>, typeof(ISpanParsable<char>), oc);
        Rel("char IUtf8SpanFormattable", oc is IUtf8SpanFormattable, typeof(IUtf8SpanFormattable), oc);
        Rel("ulong IBinaryInteger<ulong>", ou is IBinaryInteger<ulong>, typeof(IBinaryInteger<ulong>), ou);
        Rel("nint INumber<nint>", on is INumber<nint>, typeof(INumber<nint>), on);
        Rel("nint ISerializable", on is ISerializable, typeof(ISerializable), on);
        Rel("float ISpanParsable<float>", of is ISpanParsable<float>, typeof(ISpanParsable<float>), of);
        Rel("double INumber<double>", od is INumber<double>, typeof(INumber<double>), od);
        Rel("double IAdditionOperators<double,double,double>", od is IAdditionOperators<double, double, double>,
            typeof(IAdditionOperators<double, double, double>), od);
        Rel("double IMinMaxValue<double>", od is IMinMaxValue<double>, typeof(IMinMaxValue<double>), od);
        Rel("double IBinaryInteger<int>", od is IBinaryInteger<int>, typeof(IBinaryInteger<int>), od);
        Rel("bool ISpanParsable<bool>", obo is ISpanParsable<bool>, typeof(ISpanParsable<bool>), obo);
        Rel("bool IUtf8SpanFormattable", obo is IUtf8SpanFormattable, typeof(IUtf8SpanFormattable), obo);
        Rel("decimal INumber<decimal>", om is INumber<decimal>, typeof(INumber<decimal>), om);
        Rel("decimal IAdditionOperators<decimal,decimal,decimal>", om is IAdditionOperators<decimal, decimal, decimal>,
            typeof(IAdditionOperators<decimal, decimal, decimal>), om);
        Rel("decimal IMinMaxValue<decimal>", om is IMinMaxValue<decimal>, typeof(IMinMaxValue<decimal>), om);
        Rel("decimal ISpanParsable<decimal>", om is ISpanParsable<decimal>, typeof(ISpanParsable<decimal>), om);
        Rel("decimal IUtf8SpanFormattable", om is IUtf8SpanFormattable, typeof(IUtf8SpanFormattable), om);
        Rel("decimal ISerializable", om is ISerializable, typeof(ISerializable), om);
        Rel("decimal IDeserializationCallback", om is IDeserializationCallback, typeof(IDeserializationCallback), om);
        Rel("decimal INumber<double>", om is INumber<double>, typeof(INumber<double>), om);
        Rel("DateTime ISpanParsable<DateTime>", odt is ISpanParsable<DateTime>, typeof(ISpanParsable<DateTime>), odt);
        Rel("DateTime IUtf8SpanFormattable", odt is IUtf8SpanFormattable, typeof(IUtf8SpanFormattable), odt);
        Rel("DateTime ISerializable", odt is ISerializable, typeof(ISerializable), odt);
        Rel("DateTime IDeserializationCallback", odt is IDeserializationCallback, typeof(IDeserializationCallback), odt);
        Rel("DateTimeOffset ISpanParsable<DateTimeOffset>", odto is ISpanParsable<DateTimeOffset>,
            typeof(ISpanParsable<DateTimeOffset>), odto);
        Rel("DateTimeOffset IUtf8SpanFormattable", odto is IUtf8SpanFormattable, typeof(IUtf8SpanFormattable), odto);
        Rel("DateTimeOffset ISerializable", odto is ISerializable, typeof(ISerializable), odto);
        Rel("DateTimeOffset IDeserializationCallback", odto is IDeserializationCallback,
            typeof(IDeserializationCallback), odto);
        Rel("TimeSpan IParsable<TimeSpan>", ots is IParsable<TimeSpan>, typeof(IParsable<TimeSpan>), ots);
        Rel("TimeSpan ISpanParsable<TimeSpan>", ots is ISpanParsable<TimeSpan>, typeof(ISpanParsable<TimeSpan>), ots);
        Rel("TimeSpan IUtf8SpanFormattable", ots is IUtf8SpanFormattable, typeof(IUtf8SpanFormattable), ots);
        Rel("TimeSpan ISerializable", ots is ISerializable, typeof(ISerializable), ots);
        Rel("DateOnly ISpanParsable<DateOnly>", odo is ISpanParsable<DateOnly>, typeof(ISpanParsable<DateOnly>), odo);
        Rel("DateOnly IUtf8SpanFormattable", odo is IUtf8SpanFormattable, typeof(IUtf8SpanFormattable), odo);
        Rel("DateOnly ISerializable", odo is ISerializable, typeof(ISerializable), odo);
        Rel("TimeOnly IParsable<TimeOnly>", oto is IParsable<TimeOnly>, typeof(IParsable<TimeOnly>), oto);
        Rel("TimeOnly IUtf8SpanFormattable", oto is IUtf8SpanFormattable, typeof(IUtf8SpanFormattable), oto);

        Str("IEnumerable", typeof(IEnumerable));
        Str("IEnumerable<char>", typeof(IEnumerable<char>));
        Str("IComparable<string>", typeof(IComparable<string>));
        Str("ICloneable", typeof(ICloneable));
        Str("IParsable<string>", typeof(IParsable<string>));
        Str("ISpanParsable<string>", typeof(ISpanParsable<string>));
        Str("IUtf8SpanFormattable", typeof(IUtf8SpanFormattable));

        Utf8("int X4", 255, "X4", 16);
        Utf8("int default", -42, null, 16);
        Utf8("int too small", 123456, null, 3);
        Utf8("sbyte", (sbyte)-5, null, 16);
        Utf8("ushort N0", (ushort)65535, "N0", 16);
        Utf8("ulong", ulong.MaxValue, null, 32);
        Utf8("long D10", 42L, "D10", 16);
        Utf8("nint X", (nint)255, "X", 16);
        Utf8("float R", 1.25f, "R", 16);
        Utf8("double F2", 3.14159, "F2", 16);
        Utf8("decimal N1", 1234.56m, "N1", 16);
        Utf8("decimal too small", 1234.56m, "N1", 4);
        Utf8("char", 'Z', null, 16);
        Utf8("DateTime", odt, "yyyy-MM-dd HH:mm:ss", 32);
        Utf8("DateTime too small", odt, "O", 8);
        Utf8("DateTimeOffset O", new DateTimeOffset(2020, 1, 2, 3, 4, 5, TimeSpan.FromHours(9)), "O", 64);
        Utf8("TimeSpan c", new TimeSpan(1, 2, 3, 4), "c", 32);
        Utf8("DateOnly O", odo, "O", 32);
        Utf8("TimeOnly t", new TimeOnly(13, 45), "t", 32);
        Utf8("bool", true, null, 16);
        Utf8("string", "abc", null, 16);
    }
}
