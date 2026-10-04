using System;
using System.Globalization;
using System.Text;

namespace Dn2Cpp;

// Call each public scalar Parse/TryParse overload directly so no reflection
// or generic forwarding can hide a missing input form. Compare wide values by bits.
internal static class PrimitiveParseSubset
{
    public static void Run()
    {
        Console.WriteLine("== Primitive parse overloads ==");
        string[] inputs = { null, "", "42", "-42", "1.25", "1e400", "NaN", "true", "漢", "-9223372036854775808", "18446744073709551615", "1.234,5", "170141183460469231731687303715884105727", "-170141183460469231731687303715884105728", "340282366920938463463374607431768211455", "42\0", "42 \0\0", "42\0 ", "\0" + "42", "42\0x", "-42\0" };
        foreach (string text in inputs)
        {
            foreach (NumberStyles styles in new[] { NumberStyles.Integer, NumberStyles.Any, NumberStyles.HexNumber, NumberStyles.BinaryNumber, NumberStyles.HexNumber | NumberStyles.AllowLeadingSign, (NumberStyles)(-1) })
            {
                Console.WriteLine("input=" + (text ?? "<null>") + " styles=" + (int)styles);
                RunBoolean(text, styles, CultureInfo.InvariantCulture);
                RunChar(text, styles, CultureInfo.InvariantCulture);
                RunSByte(text, styles, CultureInfo.InvariantCulture);
                RunByte(text, styles, CultureInfo.InvariantCulture);
                RunInt16(text, styles, CultureInfo.InvariantCulture);
                RunUInt16(text, styles, CultureInfo.InvariantCulture);
                RunInt32(text, styles, CultureInfo.InvariantCulture);
                RunUInt32(text, styles, CultureInfo.InvariantCulture);
                RunInt64(text, styles, CultureInfo.InvariantCulture);
                RunUInt64(text, styles, CultureInfo.InvariantCulture);
                RunIntPtr(text, styles, CultureInfo.InvariantCulture);
                RunUIntPtr(text, styles, CultureInfo.InvariantCulture);
                RunSingle(text, styles, CultureInfo.InvariantCulture);
                RunDouble(text, styles, CultureInfo.InvariantCulture);
                RunHalf(text, styles, CultureInfo.InvariantCulture);
                RunDecimal(text, styles, CultureInfo.InvariantCulture);
                RunInt128(text, styles, CultureInfo.InvariantCulture);
                RunUInt128(text, styles, CultureInfo.InvariantCulture);
            }
        }
        var symbols = new NumberFormatInfo { NegativeSign = "−", NumberDecimalSeparator = ",", NumberGroupSeparator = "\u202f" };
        foreach (string text in new[] { "−42", "1\u202f234,5" })
        {
            Console.WriteLine("custom input=" + text);
            RunSByte(text, NumberStyles.Any, symbols);
            RunByte(text, NumberStyles.Any, symbols);
            RunInt16(text, NumberStyles.Any, symbols);
            RunUInt16(text, NumberStyles.Any, symbols);
            RunInt32(text, NumberStyles.Any, symbols);
            RunUInt32(text, NumberStyles.Any, symbols);
            RunInt64(text, NumberStyles.Any, symbols);
            RunUInt64(text, NumberStyles.Any, symbols);
            RunIntPtr(text, NumberStyles.Any, symbols);
            RunUIntPtr(text, NumberStyles.Any, symbols);
            RunSingle(text, NumberStyles.Any, symbols);
            RunDouble(text, NumberStyles.Any, symbols);
            RunHalf(text, NumberStyles.Any, symbols);
            RunDecimal(text, NumberStyles.Any, symbols);
            RunInt128(text, NumberStyles.Any, symbols);
            RunUInt128(text, NumberStyles.Any, symbols);
        }
        ConvertValidationSubset.Program.RunDecimalParseFaults();
        Console.WriteLine("Primitive parse overloads end");
    }

    private static string Wide(Int128 value) => (ulong)(value >> 64) + ":" + (ulong)value;
    private static string Wide(UInt128 value) => (ulong)(value >> 64) + ":" + (ulong)value;

    private static void Observe(string label, Func<string> action)
    {
        try { Console.WriteLine(label + "=" + action()); }
        catch (Exception ex) { Console.WriteLine(label + "=" + ex.GetType().Name + ":" + (ex is ArgumentException arg ? arg.ParamName : null) + ":" + ex.Message.Replace("\r", "").Replace("\n", "|")); }
    }

    private static void RunBoolean(string text, NumberStyles styles, IFormatProvider provider)
    {
        Observe("Boolean:Parse:chars", () => System.Boolean.Parse(text.AsSpan()).ToString());
        Observe("Boolean:Parse:string", () => System.Boolean.Parse(text).ToString());
        Observe("Boolean:TryParse:chars/out", () => { bool success = System.Boolean.TryParse(text.AsSpan(), out System.Boolean value2); return success + ":" + value2; });
        Observe("Boolean:TryParse:string/out", () => { bool success = System.Boolean.TryParse(text, out System.Boolean value3); return success + ":" + value3; });
    }

    private static void RunChar(string text, NumberStyles styles, IFormatProvider provider)
    {
        Observe("Char:TryParse:string/out", () => { bool success = System.Char.TryParse(text, out System.Char value0); return success + ":" + value0; });
        Observe("Char:Parse:string", () => System.Char.Parse(text).ToString());
    }

    private static void RunSByte(string text, NumberStyles styles, IFormatProvider provider)
    {
        Observe("SByte:TryParse:utf8/out", () => { bool success = System.SByte.TryParse((text is null ? ReadOnlySpan<byte>.Empty : Encoding.UTF8.GetBytes(text).AsSpan()), out System.SByte value0); return success + ":" + value0; });
        Observe("SByte:TryParse:utf8/styles/provider/out", () => { bool success = System.SByte.TryParse((text is null ? ReadOnlySpan<byte>.Empty : Encoding.UTF8.GetBytes(text).AsSpan()), styles, provider, out System.SByte value1); return success + ":" + value1; });
        Observe("SByte:TryParse:utf8/provider/out", () => { bool success = System.SByte.TryParse((text is null ? ReadOnlySpan<byte>.Empty : Encoding.UTF8.GetBytes(text).AsSpan()), provider, out System.SByte value2); return success + ":" + value2; });
        Observe("SByte:TryParse:chars/out", () => { bool success = System.SByte.TryParse(text.AsSpan(), out System.SByte value3); return success + ":" + value3; });
        Observe("SByte:TryParse:chars/styles/provider/out", () => { bool success = System.SByte.TryParse(text.AsSpan(), styles, provider, out System.SByte value4); return success + ":" + value4; });
        Observe("SByte:TryParse:chars/provider/out", () => { bool success = System.SByte.TryParse(text.AsSpan(), provider, out System.SByte value5); return success + ":" + value5; });
        Observe("SByte:TryParse:string/out", () => { bool success = System.SByte.TryParse(text, out System.SByte value6); return success + ":" + value6; });
        Observe("SByte:TryParse:string/styles/provider/out", () => { bool success = System.SByte.TryParse(text, styles, provider, out System.SByte value7); return success + ":" + value7; });
        Observe("SByte:TryParse:string/provider/out", () => { bool success = System.SByte.TryParse(text, provider, out System.SByte value8); return success + ":" + value8; });
        Observe("SByte:Parse:utf8/styles/provider", () => System.SByte.Parse((text is null ? ReadOnlySpan<byte>.Empty : Encoding.UTF8.GetBytes(text).AsSpan()), styles, provider).ToString());
        Observe("SByte:Parse:utf8/provider", () => System.SByte.Parse((text is null ? ReadOnlySpan<byte>.Empty : Encoding.UTF8.GetBytes(text).AsSpan()), provider).ToString());
        Observe("SByte:Parse:chars/styles/provider", () => System.SByte.Parse(text.AsSpan(), styles, provider).ToString());
        Observe("SByte:Parse:chars/provider", () => System.SByte.Parse(text.AsSpan(), provider).ToString());
        Observe("SByte:Parse:string", () => System.SByte.Parse(text).ToString());
        Observe("SByte:Parse:string/styles", () => System.SByte.Parse(text, styles).ToString());
        Observe("SByte:Parse:string/styles/provider", () => System.SByte.Parse(text, styles, provider).ToString());
        Observe("SByte:Parse:string/provider", () => System.SByte.Parse(text, provider).ToString());
    }

    private static void RunByte(string text, NumberStyles styles, IFormatProvider provider)
    {
        Observe("Byte:TryParse:utf8/out", () => { bool success = System.Byte.TryParse((text is null ? ReadOnlySpan<byte>.Empty : Encoding.UTF8.GetBytes(text).AsSpan()), out System.Byte value0); return success + ":" + value0; });
        Observe("Byte:TryParse:utf8/styles/provider/out", () => { bool success = System.Byte.TryParse((text is null ? ReadOnlySpan<byte>.Empty : Encoding.UTF8.GetBytes(text).AsSpan()), styles, provider, out System.Byte value1); return success + ":" + value1; });
        Observe("Byte:TryParse:utf8/provider/out", () => { bool success = System.Byte.TryParse((text is null ? ReadOnlySpan<byte>.Empty : Encoding.UTF8.GetBytes(text).AsSpan()), provider, out System.Byte value2); return success + ":" + value2; });
        Observe("Byte:TryParse:chars/out", () => { bool success = System.Byte.TryParse(text.AsSpan(), out System.Byte value3); return success + ":" + value3; });
        Observe("Byte:TryParse:chars/styles/provider/out", () => { bool success = System.Byte.TryParse(text.AsSpan(), styles, provider, out System.Byte value4); return success + ":" + value4; });
        Observe("Byte:TryParse:chars/provider/out", () => { bool success = System.Byte.TryParse(text.AsSpan(), provider, out System.Byte value5); return success + ":" + value5; });
        Observe("Byte:TryParse:string/out", () => { bool success = System.Byte.TryParse(text, out System.Byte value6); return success + ":" + value6; });
        Observe("Byte:TryParse:string/styles/provider/out", () => { bool success = System.Byte.TryParse(text, styles, provider, out System.Byte value7); return success + ":" + value7; });
        Observe("Byte:TryParse:string/provider/out", () => { bool success = System.Byte.TryParse(text, provider, out System.Byte value8); return success + ":" + value8; });
        Observe("Byte:Parse:utf8/styles/provider", () => System.Byte.Parse((text is null ? ReadOnlySpan<byte>.Empty : Encoding.UTF8.GetBytes(text).AsSpan()), styles, provider).ToString());
        Observe("Byte:Parse:utf8/provider", () => System.Byte.Parse((text is null ? ReadOnlySpan<byte>.Empty : Encoding.UTF8.GetBytes(text).AsSpan()), provider).ToString());
        Observe("Byte:Parse:chars/styles/provider", () => System.Byte.Parse(text.AsSpan(), styles, provider).ToString());
        Observe("Byte:Parse:chars/provider", () => System.Byte.Parse(text.AsSpan(), provider).ToString());
        Observe("Byte:Parse:string", () => System.Byte.Parse(text).ToString());
        Observe("Byte:Parse:string/styles", () => System.Byte.Parse(text, styles).ToString());
        Observe("Byte:Parse:string/styles/provider", () => System.Byte.Parse(text, styles, provider).ToString());
        Observe("Byte:Parse:string/provider", () => System.Byte.Parse(text, provider).ToString());
    }

    private static void RunInt16(string text, NumberStyles styles, IFormatProvider provider)
    {
        Observe("Int16:TryParse:utf8/out", () => { bool success = System.Int16.TryParse((text is null ? ReadOnlySpan<byte>.Empty : Encoding.UTF8.GetBytes(text).AsSpan()), out System.Int16 value0); return success + ":" + value0; });
        Observe("Int16:TryParse:utf8/styles/provider/out", () => { bool success = System.Int16.TryParse((text is null ? ReadOnlySpan<byte>.Empty : Encoding.UTF8.GetBytes(text).AsSpan()), styles, provider, out System.Int16 value1); return success + ":" + value1; });
        Observe("Int16:TryParse:utf8/provider/out", () => { bool success = System.Int16.TryParse((text is null ? ReadOnlySpan<byte>.Empty : Encoding.UTF8.GetBytes(text).AsSpan()), provider, out System.Int16 value2); return success + ":" + value2; });
        Observe("Int16:TryParse:chars/out", () => { bool success = System.Int16.TryParse(text.AsSpan(), out System.Int16 value3); return success + ":" + value3; });
        Observe("Int16:TryParse:chars/styles/provider/out", () => { bool success = System.Int16.TryParse(text.AsSpan(), styles, provider, out System.Int16 value4); return success + ":" + value4; });
        Observe("Int16:TryParse:chars/provider/out", () => { bool success = System.Int16.TryParse(text.AsSpan(), provider, out System.Int16 value5); return success + ":" + value5; });
        Observe("Int16:TryParse:string/out", () => { bool success = System.Int16.TryParse(text, out System.Int16 value6); return success + ":" + value6; });
        Observe("Int16:TryParse:string/styles/provider/out", () => { bool success = System.Int16.TryParse(text, styles, provider, out System.Int16 value7); return success + ":" + value7; });
        Observe("Int16:TryParse:string/provider/out", () => { bool success = System.Int16.TryParse(text, provider, out System.Int16 value8); return success + ":" + value8; });
        Observe("Int16:Parse:utf8/styles/provider", () => System.Int16.Parse((text is null ? ReadOnlySpan<byte>.Empty : Encoding.UTF8.GetBytes(text).AsSpan()), styles, provider).ToString());
        Observe("Int16:Parse:utf8/provider", () => System.Int16.Parse((text is null ? ReadOnlySpan<byte>.Empty : Encoding.UTF8.GetBytes(text).AsSpan()), provider).ToString());
        Observe("Int16:Parse:chars/styles/provider", () => System.Int16.Parse(text.AsSpan(), styles, provider).ToString());
        Observe("Int16:Parse:chars/provider", () => System.Int16.Parse(text.AsSpan(), provider).ToString());
        Observe("Int16:Parse:string", () => System.Int16.Parse(text).ToString());
        Observe("Int16:Parse:string/styles", () => System.Int16.Parse(text, styles).ToString());
        Observe("Int16:Parse:string/styles/provider", () => System.Int16.Parse(text, styles, provider).ToString());
        Observe("Int16:Parse:string/provider", () => System.Int16.Parse(text, provider).ToString());
    }

    private static void RunUInt16(string text, NumberStyles styles, IFormatProvider provider)
    {
        Observe("UInt16:TryParse:utf8/styles/provider/out", () => { bool success = System.UInt16.TryParse((text is null ? ReadOnlySpan<byte>.Empty : Encoding.UTF8.GetBytes(text).AsSpan()), styles, provider, out System.UInt16 value0); return success + ":" + value0; });
        Observe("UInt16:TryParse:utf8/provider/out", () => { bool success = System.UInt16.TryParse((text is null ? ReadOnlySpan<byte>.Empty : Encoding.UTF8.GetBytes(text).AsSpan()), provider, out System.UInt16 value1); return success + ":" + value1; });
        Observe("UInt16:TryParse:utf8/out", () => { bool success = System.UInt16.TryParse((text is null ? ReadOnlySpan<byte>.Empty : Encoding.UTF8.GetBytes(text).AsSpan()), out System.UInt16 value2); return success + ":" + value2; });
        Observe("UInt16:TryParse:chars/styles/provider/out", () => { bool success = System.UInt16.TryParse(text.AsSpan(), styles, provider, out System.UInt16 value3); return success + ":" + value3; });
        Observe("UInt16:TryParse:chars/provider/out", () => { bool success = System.UInt16.TryParse(text.AsSpan(), provider, out System.UInt16 value4); return success + ":" + value4; });
        Observe("UInt16:TryParse:chars/out", () => { bool success = System.UInt16.TryParse(text.AsSpan(), out System.UInt16 value5); return success + ":" + value5; });
        Observe("UInt16:TryParse:string/styles/provider/out", () => { bool success = System.UInt16.TryParse(text, styles, provider, out System.UInt16 value6); return success + ":" + value6; });
        Observe("UInt16:TryParse:string/provider/out", () => { bool success = System.UInt16.TryParse(text, provider, out System.UInt16 value7); return success + ":" + value7; });
        Observe("UInt16:TryParse:string/out", () => { bool success = System.UInt16.TryParse(text, out System.UInt16 value8); return success + ":" + value8; });
        Observe("UInt16:Parse:utf8/styles/provider", () => System.UInt16.Parse((text is null ? ReadOnlySpan<byte>.Empty : Encoding.UTF8.GetBytes(text).AsSpan()), styles, provider).ToString());
        Observe("UInt16:Parse:utf8/provider", () => System.UInt16.Parse((text is null ? ReadOnlySpan<byte>.Empty : Encoding.UTF8.GetBytes(text).AsSpan()), provider).ToString());
        Observe("UInt16:Parse:chars/styles/provider", () => System.UInt16.Parse(text.AsSpan(), styles, provider).ToString());
        Observe("UInt16:Parse:chars/provider", () => System.UInt16.Parse(text.AsSpan(), provider).ToString());
        Observe("UInt16:Parse:string", () => System.UInt16.Parse(text).ToString());
        Observe("UInt16:Parse:string/styles", () => System.UInt16.Parse(text, styles).ToString());
        Observe("UInt16:Parse:string/styles/provider", () => System.UInt16.Parse(text, styles, provider).ToString());
        Observe("UInt16:Parse:string/provider", () => System.UInt16.Parse(text, provider).ToString());
    }

    private static void RunInt32(string text, NumberStyles styles, IFormatProvider provider)
    {
        Observe("Int32:TryParse:utf8/out", () => { bool success = System.Int32.TryParse((text is null ? ReadOnlySpan<byte>.Empty : Encoding.UTF8.GetBytes(text).AsSpan()), out System.Int32 value0); return success + ":" + value0; });
        Observe("Int32:TryParse:utf8/styles/provider/out", () => { bool success = System.Int32.TryParse((text is null ? ReadOnlySpan<byte>.Empty : Encoding.UTF8.GetBytes(text).AsSpan()), styles, provider, out System.Int32 value1); return success + ":" + value1; });
        Observe("Int32:TryParse:utf8/provider/out", () => { bool success = System.Int32.TryParse((text is null ? ReadOnlySpan<byte>.Empty : Encoding.UTF8.GetBytes(text).AsSpan()), provider, out System.Int32 value2); return success + ":" + value2; });
        Observe("Int32:TryParse:chars/out", () => { bool success = System.Int32.TryParse(text.AsSpan(), out System.Int32 value3); return success + ":" + value3; });
        Observe("Int32:TryParse:chars/styles/provider/out", () => { bool success = System.Int32.TryParse(text.AsSpan(), styles, provider, out System.Int32 value4); return success + ":" + value4; });
        Observe("Int32:TryParse:chars/provider/out", () => { bool success = System.Int32.TryParse(text.AsSpan(), provider, out System.Int32 value5); return success + ":" + value5; });
        Observe("Int32:TryParse:string/out", () => { bool success = System.Int32.TryParse(text, out System.Int32 value6); return success + ":" + value6; });
        Observe("Int32:TryParse:string/styles/provider/out", () => { bool success = System.Int32.TryParse(text, styles, provider, out System.Int32 value7); return success + ":" + value7; });
        Observe("Int32:TryParse:string/provider/out", () => { bool success = System.Int32.TryParse(text, provider, out System.Int32 value8); return success + ":" + value8; });
        Observe("Int32:Parse:utf8/styles/provider", () => System.Int32.Parse((text is null ? ReadOnlySpan<byte>.Empty : Encoding.UTF8.GetBytes(text).AsSpan()), styles, provider).ToString());
        Observe("Int32:Parse:utf8/provider", () => System.Int32.Parse((text is null ? ReadOnlySpan<byte>.Empty : Encoding.UTF8.GetBytes(text).AsSpan()), provider).ToString());
        Observe("Int32:Parse:chars/styles/provider", () => System.Int32.Parse(text.AsSpan(), styles, provider).ToString());
        Observe("Int32:Parse:chars/provider", () => System.Int32.Parse(text.AsSpan(), provider).ToString());
        Observe("Int32:Parse:string", () => System.Int32.Parse(text).ToString());
        Observe("Int32:Parse:string/styles", () => System.Int32.Parse(text, styles).ToString());
        Observe("Int32:Parse:string/styles/provider", () => System.Int32.Parse(text, styles, provider).ToString());
        Observe("Int32:Parse:string/provider", () => System.Int32.Parse(text, provider).ToString());
    }

    private static void RunUInt32(string text, NumberStyles styles, IFormatProvider provider)
    {
        Observe("UInt32:TryParse:utf8/styles/provider/out", () => { bool success = System.UInt32.TryParse((text is null ? ReadOnlySpan<byte>.Empty : Encoding.UTF8.GetBytes(text).AsSpan()), styles, provider, out System.UInt32 value0); return success + ":" + value0; });
        Observe("UInt32:TryParse:utf8/provider/out", () => { bool success = System.UInt32.TryParse((text is null ? ReadOnlySpan<byte>.Empty : Encoding.UTF8.GetBytes(text).AsSpan()), provider, out System.UInt32 value1); return success + ":" + value1; });
        Observe("UInt32:TryParse:utf8/out", () => { bool success = System.UInt32.TryParse((text is null ? ReadOnlySpan<byte>.Empty : Encoding.UTF8.GetBytes(text).AsSpan()), out System.UInt32 value2); return success + ":" + value2; });
        Observe("UInt32:TryParse:chars/styles/provider/out", () => { bool success = System.UInt32.TryParse(text.AsSpan(), styles, provider, out System.UInt32 value3); return success + ":" + value3; });
        Observe("UInt32:TryParse:chars/provider/out", () => { bool success = System.UInt32.TryParse(text.AsSpan(), provider, out System.UInt32 value4); return success + ":" + value4; });
        Observe("UInt32:TryParse:chars/out", () => { bool success = System.UInt32.TryParse(text.AsSpan(), out System.UInt32 value5); return success + ":" + value5; });
        Observe("UInt32:TryParse:string/styles/provider/out", () => { bool success = System.UInt32.TryParse(text, styles, provider, out System.UInt32 value6); return success + ":" + value6; });
        Observe("UInt32:TryParse:string/provider/out", () => { bool success = System.UInt32.TryParse(text, provider, out System.UInt32 value7); return success + ":" + value7; });
        Observe("UInt32:TryParse:string/out", () => { bool success = System.UInt32.TryParse(text, out System.UInt32 value8); return success + ":" + value8; });
        Observe("UInt32:Parse:utf8/styles/provider", () => System.UInt32.Parse((text is null ? ReadOnlySpan<byte>.Empty : Encoding.UTF8.GetBytes(text).AsSpan()), styles, provider).ToString());
        Observe("UInt32:Parse:utf8/provider", () => System.UInt32.Parse((text is null ? ReadOnlySpan<byte>.Empty : Encoding.UTF8.GetBytes(text).AsSpan()), provider).ToString());
        Observe("UInt32:Parse:chars/styles/provider", () => System.UInt32.Parse(text.AsSpan(), styles, provider).ToString());
        Observe("UInt32:Parse:chars/provider", () => System.UInt32.Parse(text.AsSpan(), provider).ToString());
        Observe("UInt32:Parse:string", () => System.UInt32.Parse(text).ToString());
        Observe("UInt32:Parse:string/styles", () => System.UInt32.Parse(text, styles).ToString());
        Observe("UInt32:Parse:string/styles/provider", () => System.UInt32.Parse(text, styles, provider).ToString());
        Observe("UInt32:Parse:string/provider", () => System.UInt32.Parse(text, provider).ToString());
    }

    private static void RunInt64(string text, NumberStyles styles, IFormatProvider provider)
    {
        Observe("Int64:TryParse:utf8/out", () => { bool success = System.Int64.TryParse((text is null ? ReadOnlySpan<byte>.Empty : Encoding.UTF8.GetBytes(text).AsSpan()), out System.Int64 value0); return success + ":" + value0; });
        Observe("Int64:TryParse:utf8/styles/provider/out", () => { bool success = System.Int64.TryParse((text is null ? ReadOnlySpan<byte>.Empty : Encoding.UTF8.GetBytes(text).AsSpan()), styles, provider, out System.Int64 value1); return success + ":" + value1; });
        Observe("Int64:TryParse:utf8/provider/out", () => { bool success = System.Int64.TryParse((text is null ? ReadOnlySpan<byte>.Empty : Encoding.UTF8.GetBytes(text).AsSpan()), provider, out System.Int64 value2); return success + ":" + value2; });
        Observe("Int64:TryParse:chars/out", () => { bool success = System.Int64.TryParse(text.AsSpan(), out System.Int64 value3); return success + ":" + value3; });
        Observe("Int64:TryParse:chars/styles/provider/out", () => { bool success = System.Int64.TryParse(text.AsSpan(), styles, provider, out System.Int64 value4); return success + ":" + value4; });
        Observe("Int64:TryParse:chars/provider/out", () => { bool success = System.Int64.TryParse(text.AsSpan(), provider, out System.Int64 value5); return success + ":" + value5; });
        Observe("Int64:TryParse:string/out", () => { bool success = System.Int64.TryParse(text, out System.Int64 value6); return success + ":" + value6; });
        Observe("Int64:TryParse:string/styles/provider/out", () => { bool success = System.Int64.TryParse(text, styles, provider, out System.Int64 value7); return success + ":" + value7; });
        Observe("Int64:TryParse:string/provider/out", () => { bool success = System.Int64.TryParse(text, provider, out System.Int64 value8); return success + ":" + value8; });
        Observe("Int64:Parse:utf8/styles/provider", () => System.Int64.Parse((text is null ? ReadOnlySpan<byte>.Empty : Encoding.UTF8.GetBytes(text).AsSpan()), styles, provider).ToString());
        Observe("Int64:Parse:utf8/provider", () => System.Int64.Parse((text is null ? ReadOnlySpan<byte>.Empty : Encoding.UTF8.GetBytes(text).AsSpan()), provider).ToString());
        Observe("Int64:Parse:chars/styles/provider", () => System.Int64.Parse(text.AsSpan(), styles, provider).ToString());
        Observe("Int64:Parse:chars/provider", () => System.Int64.Parse(text.AsSpan(), provider).ToString());
        Observe("Int64:Parse:string", () => System.Int64.Parse(text).ToString());
        Observe("Int64:Parse:string/styles", () => System.Int64.Parse(text, styles).ToString());
        Observe("Int64:Parse:string/styles/provider", () => System.Int64.Parse(text, styles, provider).ToString());
        Observe("Int64:Parse:string/provider", () => System.Int64.Parse(text, provider).ToString());
    }

    private static void RunUInt64(string text, NumberStyles styles, IFormatProvider provider)
    {
        Observe("UInt64:TryParse:utf8/styles/provider/out", () => { bool success = System.UInt64.TryParse((text is null ? ReadOnlySpan<byte>.Empty : Encoding.UTF8.GetBytes(text).AsSpan()), styles, provider, out System.UInt64 value0); return success + ":" + value0; });
        Observe("UInt64:TryParse:utf8/provider/out", () => { bool success = System.UInt64.TryParse((text is null ? ReadOnlySpan<byte>.Empty : Encoding.UTF8.GetBytes(text).AsSpan()), provider, out System.UInt64 value1); return success + ":" + value1; });
        Observe("UInt64:TryParse:utf8/out", () => { bool success = System.UInt64.TryParse((text is null ? ReadOnlySpan<byte>.Empty : Encoding.UTF8.GetBytes(text).AsSpan()), out System.UInt64 value2); return success + ":" + value2; });
        Observe("UInt64:TryParse:chars/styles/provider/out", () => { bool success = System.UInt64.TryParse(text.AsSpan(), styles, provider, out System.UInt64 value3); return success + ":" + value3; });
        Observe("UInt64:TryParse:chars/provider/out", () => { bool success = System.UInt64.TryParse(text.AsSpan(), provider, out System.UInt64 value4); return success + ":" + value4; });
        Observe("UInt64:TryParse:chars/out", () => { bool success = System.UInt64.TryParse(text.AsSpan(), out System.UInt64 value5); return success + ":" + value5; });
        Observe("UInt64:TryParse:string/styles/provider/out", () => { bool success = System.UInt64.TryParse(text, styles, provider, out System.UInt64 value6); return success + ":" + value6; });
        Observe("UInt64:TryParse:string/provider/out", () => { bool success = System.UInt64.TryParse(text, provider, out System.UInt64 value7); return success + ":" + value7; });
        Observe("UInt64:TryParse:string/out", () => { bool success = System.UInt64.TryParse(text, out System.UInt64 value8); return success + ":" + value8; });
        Observe("UInt64:Parse:utf8/styles/provider", () => System.UInt64.Parse((text is null ? ReadOnlySpan<byte>.Empty : Encoding.UTF8.GetBytes(text).AsSpan()), styles, provider).ToString());
        Observe("UInt64:Parse:utf8/provider", () => System.UInt64.Parse((text is null ? ReadOnlySpan<byte>.Empty : Encoding.UTF8.GetBytes(text).AsSpan()), provider).ToString());
        Observe("UInt64:Parse:chars/styles/provider", () => System.UInt64.Parse(text.AsSpan(), styles, provider).ToString());
        Observe("UInt64:Parse:chars/provider", () => System.UInt64.Parse(text.AsSpan(), provider).ToString());
        Observe("UInt64:Parse:string", () => System.UInt64.Parse(text).ToString());
        Observe("UInt64:Parse:string/styles", () => System.UInt64.Parse(text, styles).ToString());
        Observe("UInt64:Parse:string/styles/provider", () => System.UInt64.Parse(text, styles, provider).ToString());
        Observe("UInt64:Parse:string/provider", () => System.UInt64.Parse(text, provider).ToString());
    }

    private static void RunIntPtr(string text, NumberStyles styles, IFormatProvider provider)
    {
        Observe("IntPtr:TryParse:utf8/out", () => { bool success = System.IntPtr.TryParse((text is null ? ReadOnlySpan<byte>.Empty : Encoding.UTF8.GetBytes(text).AsSpan()), out System.IntPtr value0); return success + ":" + value0; });
        Observe("IntPtr:TryParse:utf8/styles/provider/out", () => { bool success = System.IntPtr.TryParse((text is null ? ReadOnlySpan<byte>.Empty : Encoding.UTF8.GetBytes(text).AsSpan()), styles, provider, out System.IntPtr value1); return success + ":" + value1; });
        Observe("IntPtr:TryParse:utf8/provider/out", () => { bool success = System.IntPtr.TryParse((text is null ? ReadOnlySpan<byte>.Empty : Encoding.UTF8.GetBytes(text).AsSpan()), provider, out System.IntPtr value2); return success + ":" + value2; });
        Observe("IntPtr:TryParse:chars/out", () => { bool success = System.IntPtr.TryParse(text.AsSpan(), out System.IntPtr value3); return success + ":" + value3; });
        Observe("IntPtr:TryParse:chars/styles/provider/out", () => { bool success = System.IntPtr.TryParse(text.AsSpan(), styles, provider, out System.IntPtr value4); return success + ":" + value4; });
        Observe("IntPtr:TryParse:chars/provider/out", () => { bool success = System.IntPtr.TryParse(text.AsSpan(), provider, out System.IntPtr value5); return success + ":" + value5; });
        Observe("IntPtr:TryParse:string/out", () => { bool success = System.IntPtr.TryParse(text, out System.IntPtr value6); return success + ":" + value6; });
        Observe("IntPtr:TryParse:string/styles/provider/out", () => { bool success = System.IntPtr.TryParse(text, styles, provider, out System.IntPtr value7); return success + ":" + value7; });
        Observe("IntPtr:TryParse:string/provider/out", () => { bool success = System.IntPtr.TryParse(text, provider, out System.IntPtr value8); return success + ":" + value8; });
        Observe("IntPtr:Parse:utf8/styles/provider", () => System.IntPtr.Parse((text is null ? ReadOnlySpan<byte>.Empty : Encoding.UTF8.GetBytes(text).AsSpan()), styles, provider).ToString());
        Observe("IntPtr:Parse:utf8/provider", () => System.IntPtr.Parse((text is null ? ReadOnlySpan<byte>.Empty : Encoding.UTF8.GetBytes(text).AsSpan()), provider).ToString());
        Observe("IntPtr:Parse:chars/styles/provider", () => System.IntPtr.Parse(text.AsSpan(), styles, provider).ToString());
        Observe("IntPtr:Parse:chars/provider", () => System.IntPtr.Parse(text.AsSpan(), provider).ToString());
        Observe("IntPtr:Parse:string", () => System.IntPtr.Parse(text).ToString());
        Observe("IntPtr:Parse:string/styles", () => System.IntPtr.Parse(text, styles).ToString());
        Observe("IntPtr:Parse:string/styles/provider", () => System.IntPtr.Parse(text, styles, provider).ToString());
        Observe("IntPtr:Parse:string/provider", () => System.IntPtr.Parse(text, provider).ToString());
    }

    private static void RunUIntPtr(string text, NumberStyles styles, IFormatProvider provider)
    {
        Observe("UIntPtr:TryParse:utf8/styles/provider/out", () => { bool success = System.UIntPtr.TryParse((text is null ? ReadOnlySpan<byte>.Empty : Encoding.UTF8.GetBytes(text).AsSpan()), styles, provider, out System.UIntPtr value0); return success + ":" + value0; });
        Observe("UIntPtr:TryParse:utf8/provider/out", () => { bool success = System.UIntPtr.TryParse((text is null ? ReadOnlySpan<byte>.Empty : Encoding.UTF8.GetBytes(text).AsSpan()), provider, out System.UIntPtr value1); return success + ":" + value1; });
        Observe("UIntPtr:TryParse:utf8/out", () => { bool success = System.UIntPtr.TryParse((text is null ? ReadOnlySpan<byte>.Empty : Encoding.UTF8.GetBytes(text).AsSpan()), out System.UIntPtr value2); return success + ":" + value2; });
        Observe("UIntPtr:TryParse:chars/styles/provider/out", () => { bool success = System.UIntPtr.TryParse(text.AsSpan(), styles, provider, out System.UIntPtr value3); return success + ":" + value3; });
        Observe("UIntPtr:TryParse:chars/provider/out", () => { bool success = System.UIntPtr.TryParse(text.AsSpan(), provider, out System.UIntPtr value4); return success + ":" + value4; });
        Observe("UIntPtr:TryParse:chars/out", () => { bool success = System.UIntPtr.TryParse(text.AsSpan(), out System.UIntPtr value5); return success + ":" + value5; });
        Observe("UIntPtr:TryParse:string/styles/provider/out", () => { bool success = System.UIntPtr.TryParse(text, styles, provider, out System.UIntPtr value6); return success + ":" + value6; });
        Observe("UIntPtr:TryParse:string/provider/out", () => { bool success = System.UIntPtr.TryParse(text, provider, out System.UIntPtr value7); return success + ":" + value7; });
        Observe("UIntPtr:TryParse:string/out", () => { bool success = System.UIntPtr.TryParse(text, out System.UIntPtr value8); return success + ":" + value8; });
        Observe("UIntPtr:Parse:utf8/styles/provider", () => System.UIntPtr.Parse((text is null ? ReadOnlySpan<byte>.Empty : Encoding.UTF8.GetBytes(text).AsSpan()), styles, provider).ToString());
        Observe("UIntPtr:Parse:utf8/provider", () => System.UIntPtr.Parse((text is null ? ReadOnlySpan<byte>.Empty : Encoding.UTF8.GetBytes(text).AsSpan()), provider).ToString());
        Observe("UIntPtr:Parse:chars/styles/provider", () => System.UIntPtr.Parse(text.AsSpan(), styles, provider).ToString());
        Observe("UIntPtr:Parse:chars/provider", () => System.UIntPtr.Parse(text.AsSpan(), provider).ToString());
        Observe("UIntPtr:Parse:string", () => System.UIntPtr.Parse(text).ToString());
        Observe("UIntPtr:Parse:string/styles", () => System.UIntPtr.Parse(text, styles).ToString());
        Observe("UIntPtr:Parse:string/styles/provider", () => System.UIntPtr.Parse(text, styles, provider).ToString());
        Observe("UIntPtr:Parse:string/provider", () => System.UIntPtr.Parse(text, provider).ToString());
    }

    private static void RunSingle(string text, NumberStyles styles, IFormatProvider provider)
    {
        Observe("Single:TryParse:utf8/out", () => { bool success = System.Single.TryParse((text is null ? ReadOnlySpan<byte>.Empty : Encoding.UTF8.GetBytes(text).AsSpan()), out System.Single value0); return success + ":" + value0; });
        Observe("Single:TryParse:utf8/styles/provider/out", () => { bool success = System.Single.TryParse((text is null ? ReadOnlySpan<byte>.Empty : Encoding.UTF8.GetBytes(text).AsSpan()), styles, provider, out System.Single value1); return success + ":" + value1; });
        Observe("Single:TryParse:utf8/provider/out", () => { bool success = System.Single.TryParse((text is null ? ReadOnlySpan<byte>.Empty : Encoding.UTF8.GetBytes(text).AsSpan()), provider, out System.Single value2); return success + ":" + value2; });
        Observe("Single:TryParse:chars/out", () => { bool success = System.Single.TryParse(text.AsSpan(), out System.Single value3); return success + ":" + value3; });
        Observe("Single:TryParse:chars/styles/provider/out", () => { bool success = System.Single.TryParse(text.AsSpan(), styles, provider, out System.Single value4); return success + ":" + value4; });
        Observe("Single:TryParse:chars/provider/out", () => { bool success = System.Single.TryParse(text.AsSpan(), provider, out System.Single value5); return success + ":" + value5; });
        Observe("Single:TryParse:string/out", () => { bool success = System.Single.TryParse(text, out System.Single value6); return success + ":" + value6; });
        Observe("Single:TryParse:string/styles/provider/out", () => { bool success = System.Single.TryParse(text, styles, provider, out System.Single value7); return success + ":" + value7; });
        Observe("Single:TryParse:string/provider/out", () => { bool success = System.Single.TryParse(text, provider, out System.Single value8); return success + ":" + value8; });
        Observe("Single:Parse:utf8/styles/provider", () => System.Single.Parse((text is null ? ReadOnlySpan<byte>.Empty : Encoding.UTF8.GetBytes(text).AsSpan()), styles, provider).ToString());
        Observe("Single:Parse:utf8/provider", () => System.Single.Parse((text is null ? ReadOnlySpan<byte>.Empty : Encoding.UTF8.GetBytes(text).AsSpan()), provider).ToString());
        Observe("Single:Parse:chars/styles/provider", () => System.Single.Parse(text.AsSpan(), styles, provider).ToString());
        Observe("Single:Parse:chars/provider", () => System.Single.Parse(text.AsSpan(), provider).ToString());
        Observe("Single:Parse:string", () => System.Single.Parse(text).ToString());
        Observe("Single:Parse:string/styles", () => System.Single.Parse(text, styles).ToString());
        Observe("Single:Parse:string/styles/provider", () => System.Single.Parse(text, styles, provider).ToString());
        Observe("Single:Parse:string/provider", () => System.Single.Parse(text, provider).ToString());
    }

    private static void RunDouble(string text, NumberStyles styles, IFormatProvider provider)
    {
        Observe("Double:TryParse:utf8/out", () => { bool success = System.Double.TryParse((text is null ? ReadOnlySpan<byte>.Empty : Encoding.UTF8.GetBytes(text).AsSpan()), out System.Double value0); return success + ":" + value0; });
        Observe("Double:TryParse:utf8/styles/provider/out", () => { bool success = System.Double.TryParse((text is null ? ReadOnlySpan<byte>.Empty : Encoding.UTF8.GetBytes(text).AsSpan()), styles, provider, out System.Double value1); return success + ":" + value1; });
        Observe("Double:TryParse:utf8/provider/out", () => { bool success = System.Double.TryParse((text is null ? ReadOnlySpan<byte>.Empty : Encoding.UTF8.GetBytes(text).AsSpan()), provider, out System.Double value2); return success + ":" + value2; });
        Observe("Double:TryParse:chars/out", () => { bool success = System.Double.TryParse(text.AsSpan(), out System.Double value3); return success + ":" + value3; });
        Observe("Double:TryParse:chars/styles/provider/out", () => { bool success = System.Double.TryParse(text.AsSpan(), styles, provider, out System.Double value4); return success + ":" + value4; });
        Observe("Double:TryParse:chars/provider/out", () => { bool success = System.Double.TryParse(text.AsSpan(), provider, out System.Double value5); return success + ":" + value5; });
        Observe("Double:TryParse:string/out", () => { bool success = System.Double.TryParse(text, out System.Double value6); return success + ":" + value6; });
        Observe("Double:TryParse:string/styles/provider/out", () => { bool success = System.Double.TryParse(text, styles, provider, out System.Double value7); return success + ":" + value7; });
        Observe("Double:TryParse:string/provider/out", () => { bool success = System.Double.TryParse(text, provider, out System.Double value8); return success + ":" + value8; });
        Observe("Double:Parse:utf8/styles/provider", () => System.Double.Parse((text is null ? ReadOnlySpan<byte>.Empty : Encoding.UTF8.GetBytes(text).AsSpan()), styles, provider).ToString());
        Observe("Double:Parse:utf8/provider", () => System.Double.Parse((text is null ? ReadOnlySpan<byte>.Empty : Encoding.UTF8.GetBytes(text).AsSpan()), provider).ToString());
        Observe("Double:Parse:chars/styles/provider", () => System.Double.Parse(text.AsSpan(), styles, provider).ToString());
        Observe("Double:Parse:chars/provider", () => System.Double.Parse(text.AsSpan(), provider).ToString());
        Observe("Double:Parse:string", () => System.Double.Parse(text).ToString());
        Observe("Double:Parse:string/styles", () => System.Double.Parse(text, styles).ToString());
        Observe("Double:Parse:string/styles/provider", () => System.Double.Parse(text, styles, provider).ToString());
        Observe("Double:Parse:string/provider", () => System.Double.Parse(text, provider).ToString());
    }

    private static void RunHalf(string text, NumberStyles styles, IFormatProvider provider)
    {
        Observe("Half:TryParse:utf8/styles/provider/out", () => { bool success = System.Half.TryParse((text is null ? ReadOnlySpan<byte>.Empty : Encoding.UTF8.GetBytes(text).AsSpan()), styles, provider, out System.Half value0); return success + ":" + value0; });
        Observe("Half:TryParse:utf8/out", () => { bool success = System.Half.TryParse((text is null ? ReadOnlySpan<byte>.Empty : Encoding.UTF8.GetBytes(text).AsSpan()), out System.Half value1); return success + ":" + value1; });
        Observe("Half:TryParse:utf8/provider/out", () => { bool success = System.Half.TryParse((text is null ? ReadOnlySpan<byte>.Empty : Encoding.UTF8.GetBytes(text).AsSpan()), provider, out System.Half value2); return success + ":" + value2; });
        Observe("Half:TryParse:chars/styles/provider/out", () => { bool success = System.Half.TryParse(text.AsSpan(), styles, provider, out System.Half value3); return success + ":" + value3; });
        Observe("Half:TryParse:chars/out", () => { bool success = System.Half.TryParse(text.AsSpan(), out System.Half value4); return success + ":" + value4; });
        Observe("Half:TryParse:chars/provider/out", () => { bool success = System.Half.TryParse(text.AsSpan(), provider, out System.Half value5); return success + ":" + value5; });
        Observe("Half:TryParse:string/styles/provider/out", () => { bool success = System.Half.TryParse(text, styles, provider, out System.Half value6); return success + ":" + value6; });
        Observe("Half:TryParse:string/out", () => { bool success = System.Half.TryParse(text, out System.Half value7); return success + ":" + value7; });
        Observe("Half:TryParse:string/provider/out", () => { bool success = System.Half.TryParse(text, provider, out System.Half value8); return success + ":" + value8; });
        Observe("Half:Parse:utf8/styles/provider", () => System.Half.Parse((text is null ? ReadOnlySpan<byte>.Empty : Encoding.UTF8.GetBytes(text).AsSpan()), styles, provider).ToString());
        Observe("Half:Parse:utf8/provider", () => System.Half.Parse((text is null ? ReadOnlySpan<byte>.Empty : Encoding.UTF8.GetBytes(text).AsSpan()), provider).ToString());
        Observe("Half:Parse:chars/styles/provider", () => System.Half.Parse(text.AsSpan(), styles, provider).ToString());
        Observe("Half:Parse:chars/provider", () => System.Half.Parse(text.AsSpan(), provider).ToString());
        Observe("Half:Parse:string", () => System.Half.Parse(text).ToString());
        Observe("Half:Parse:string/styles", () => System.Half.Parse(text, styles).ToString());
        Observe("Half:Parse:string/styles/provider", () => System.Half.Parse(text, styles, provider).ToString());
        Observe("Half:Parse:string/provider", () => System.Half.Parse(text, provider).ToString());
    }

    private static void RunDecimal(string text, NumberStyles styles, IFormatProvider provider)
    {
        Observe("Decimal:TryParse:utf8/out", () => { bool success = System.Decimal.TryParse((text is null ? ReadOnlySpan<byte>.Empty : Encoding.UTF8.GetBytes(text).AsSpan()), out System.Decimal value0); return success + ":" + value0; });
        Observe("Decimal:TryParse:utf8/styles/provider/out", () => { bool success = System.Decimal.TryParse((text is null ? ReadOnlySpan<byte>.Empty : Encoding.UTF8.GetBytes(text).AsSpan()), styles, provider, out System.Decimal value1); return success + ":" + value1; });
        Observe("Decimal:TryParse:utf8/provider/out", () => { bool success = System.Decimal.TryParse((text is null ? ReadOnlySpan<byte>.Empty : Encoding.UTF8.GetBytes(text).AsSpan()), provider, out System.Decimal value2); return success + ":" + value2; });
        Observe("Decimal:TryParse:chars/out", () => { bool success = System.Decimal.TryParse(text.AsSpan(), out System.Decimal value3); return success + ":" + value3; });
        Observe("Decimal:TryParse:chars/styles/provider/out", () => { bool success = System.Decimal.TryParse(text.AsSpan(), styles, provider, out System.Decimal value4); return success + ":" + value4; });
        Observe("Decimal:TryParse:chars/provider/out", () => { bool success = System.Decimal.TryParse(text.AsSpan(), provider, out System.Decimal value5); return success + ":" + value5; });
        Observe("Decimal:TryParse:string/out", () => { bool success = System.Decimal.TryParse(text, out System.Decimal value6); return success + ":" + value6; });
        Observe("Decimal:TryParse:string/styles/provider/out", () => { bool success = System.Decimal.TryParse(text, styles, provider, out System.Decimal value7); return success + ":" + value7; });
        Observe("Decimal:TryParse:string/provider/out", () => { bool success = System.Decimal.TryParse(text, provider, out System.Decimal value8); return success + ":" + value8; });
        Observe("Decimal:Parse:utf8/styles/provider", () => System.Decimal.Parse((text is null ? ReadOnlySpan<byte>.Empty : Encoding.UTF8.GetBytes(text).AsSpan()), styles, provider).ToString());
        Observe("Decimal:Parse:utf8/provider", () => System.Decimal.Parse((text is null ? ReadOnlySpan<byte>.Empty : Encoding.UTF8.GetBytes(text).AsSpan()), provider).ToString());
        Observe("Decimal:Parse:chars/styles/provider", () => System.Decimal.Parse(text.AsSpan(), styles, provider).ToString());
        Observe("Decimal:Parse:chars/provider", () => System.Decimal.Parse(text.AsSpan(), provider).ToString());
        Observe("Decimal:Parse:string", () => System.Decimal.Parse(text).ToString());
        Observe("Decimal:Parse:string/styles", () => System.Decimal.Parse(text, styles).ToString());
        Observe("Decimal:Parse:string/styles/provider", () => System.Decimal.Parse(text, styles, provider).ToString());
        Observe("Decimal:Parse:string/provider", () => System.Decimal.Parse(text, provider).ToString());
    }

    private static void RunInt128(string text, NumberStyles styles, IFormatProvider provider)
    {
        Observe("Int128:TryParse:utf8/styles/provider/out", () => { bool success = System.Int128.TryParse((text is null ? ReadOnlySpan<byte>.Empty : Encoding.UTF8.GetBytes(text).AsSpan()), styles, provider, out System.Int128 value0); return success + ":" + Wide(value0); });
        Observe("Int128:TryParse:utf8/provider/out", () => { bool success = System.Int128.TryParse((text is null ? ReadOnlySpan<byte>.Empty : Encoding.UTF8.GetBytes(text).AsSpan()), provider, out System.Int128 value1); return success + ":" + Wide(value1); });
        Observe("Int128:TryParse:utf8/out", () => { bool success = System.Int128.TryParse((text is null ? ReadOnlySpan<byte>.Empty : Encoding.UTF8.GetBytes(text).AsSpan()), out System.Int128 value2); return success + ":" + Wide(value2); });
        Observe("Int128:TryParse:chars/styles/provider/out", () => { bool success = System.Int128.TryParse(text.AsSpan(), styles, provider, out System.Int128 value3); return success + ":" + Wide(value3); });
        Observe("Int128:TryParse:chars/provider/out", () => { bool success = System.Int128.TryParse(text.AsSpan(), provider, out System.Int128 value4); return success + ":" + Wide(value4); });
        Observe("Int128:TryParse:chars/out", () => { bool success = System.Int128.TryParse(text.AsSpan(), out System.Int128 value5); return success + ":" + Wide(value5); });
        Observe("Int128:TryParse:string/styles/provider/out", () => { bool success = System.Int128.TryParse(text, styles, provider, out System.Int128 value6); return success + ":" + Wide(value6); });
        Observe("Int128:TryParse:string/provider/out", () => { bool success = System.Int128.TryParse(text, provider, out System.Int128 value7); return success + ":" + Wide(value7); });
        Observe("Int128:TryParse:string/out", () => { bool success = System.Int128.TryParse(text, out System.Int128 value8); return success + ":" + Wide(value8); });
        Observe("Int128:Parse:utf8/styles/provider", () => Wide(System.Int128.Parse((text is null ? ReadOnlySpan<byte>.Empty : Encoding.UTF8.GetBytes(text).AsSpan()), styles, provider)));
        Observe("Int128:Parse:utf8/provider", () => Wide(System.Int128.Parse((text is null ? ReadOnlySpan<byte>.Empty : Encoding.UTF8.GetBytes(text).AsSpan()), provider)));
        Observe("Int128:Parse:chars/styles/provider", () => Wide(System.Int128.Parse(text.AsSpan(), styles, provider)));
        Observe("Int128:Parse:chars/provider", () => Wide(System.Int128.Parse(text.AsSpan(), provider)));
        Observe("Int128:Parse:string", () => Wide(System.Int128.Parse(text)));
        Observe("Int128:Parse:string/styles", () => Wide(System.Int128.Parse(text, styles)));
        Observe("Int128:Parse:string/styles/provider", () => Wide(System.Int128.Parse(text, styles, provider)));
        Observe("Int128:Parse:string/provider", () => Wide(System.Int128.Parse(text, provider)));
    }

    private static void RunUInt128(string text, NumberStyles styles, IFormatProvider provider)
    {
        Observe("UInt128:TryParse:utf8/styles/provider/out", () => { bool success = System.UInt128.TryParse((text is null ? ReadOnlySpan<byte>.Empty : Encoding.UTF8.GetBytes(text).AsSpan()), styles, provider, out System.UInt128 value0); return success + ":" + Wide(value0); });
        Observe("UInt128:TryParse:utf8/provider/out", () => { bool success = System.UInt128.TryParse((text is null ? ReadOnlySpan<byte>.Empty : Encoding.UTF8.GetBytes(text).AsSpan()), provider, out System.UInt128 value1); return success + ":" + Wide(value1); });
        Observe("UInt128:TryParse:utf8/out", () => { bool success = System.UInt128.TryParse((text is null ? ReadOnlySpan<byte>.Empty : Encoding.UTF8.GetBytes(text).AsSpan()), out System.UInt128 value2); return success + ":" + Wide(value2); });
        Observe("UInt128:TryParse:chars/styles/provider/out", () => { bool success = System.UInt128.TryParse(text.AsSpan(), styles, provider, out System.UInt128 value3); return success + ":" + Wide(value3); });
        Observe("UInt128:TryParse:chars/provider/out", () => { bool success = System.UInt128.TryParse(text.AsSpan(), provider, out System.UInt128 value4); return success + ":" + Wide(value4); });
        Observe("UInt128:TryParse:chars/out", () => { bool success = System.UInt128.TryParse(text.AsSpan(), out System.UInt128 value5); return success + ":" + Wide(value5); });
        Observe("UInt128:TryParse:string/styles/provider/out", () => { bool success = System.UInt128.TryParse(text, styles, provider, out System.UInt128 value6); return success + ":" + Wide(value6); });
        Observe("UInt128:TryParse:string/provider/out", () => { bool success = System.UInt128.TryParse(text, provider, out System.UInt128 value7); return success + ":" + Wide(value7); });
        Observe("UInt128:TryParse:string/out", () => { bool success = System.UInt128.TryParse(text, out System.UInt128 value8); return success + ":" + Wide(value8); });
        Observe("UInt128:Parse:utf8/styles/provider", () => Wide(System.UInt128.Parse((text is null ? ReadOnlySpan<byte>.Empty : Encoding.UTF8.GetBytes(text).AsSpan()), styles, provider)));
        Observe("UInt128:Parse:utf8/provider", () => Wide(System.UInt128.Parse((text is null ? ReadOnlySpan<byte>.Empty : Encoding.UTF8.GetBytes(text).AsSpan()), provider)));
        Observe("UInt128:Parse:chars/styles/provider", () => Wide(System.UInt128.Parse(text.AsSpan(), styles, provider)));
        Observe("UInt128:Parse:chars/provider", () => Wide(System.UInt128.Parse(text.AsSpan(), provider)));
        Observe("UInt128:Parse:string", () => Wide(System.UInt128.Parse(text)));
        Observe("UInt128:Parse:string/styles", () => Wide(System.UInt128.Parse(text, styles)));
        Observe("UInt128:Parse:string/styles/provider", () => Wide(System.UInt128.Parse(text, styles, provider)));
        Observe("UInt128:Parse:string/provider", () => Wide(System.UInt128.Parse(text, provider)));
    }
}
