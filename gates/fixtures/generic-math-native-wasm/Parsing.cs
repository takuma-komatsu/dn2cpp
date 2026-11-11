using System;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;

namespace GenericMathNativeWasm;

internal static partial class Program
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static T ParseChars<T>(ReadOnlySpan<char> text, IFormatProvider provider) where T : ISpanParsable<T> => T.Parse(text, provider);
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool TryChars<T>(ReadOnlySpan<char> text, IFormatProvider provider, out T value) where T : ISpanParsable<T> => T.TryParse(text, provider, out value);
    private delegate T Utf8Parse<T>(ReadOnlySpan<byte> text, IFormatProvider provider);
    private delegate bool Utf8Try<T>(ReadOnlySpan<byte> text, IFormatProvider provider, out T value);
    private delegate T StringParse<T>(string text, IFormatProvider provider);
    private delegate bool StringTry<T>(string text, IFormatProvider provider, out T value);
    private delegate T StyledStringParse<T>(string text, NumberStyles style, IFormatProvider provider);
    private delegate bool StyledStringTry<T>(string text, NumberStyles style, IFormatProvider provider, out T value);

    // Bind concrete public overloads without adding another static-interface contract.
    // Delegates forward the original out address, including its canaries and null faults.
    private static class DirectParser<T>
    {
        internal static Utf8Parse<T> ParseUtf8;
        internal static Utf8Try<T> TryUtf8;
        internal static StringParse<T> ParseString;
        internal static StringTry<T> TryString;
        internal static StyledStringParse<T> ParseStyledString;
        internal static StyledStringTry<T> TryStyledString;
    }

    private static void Bind<T>(Utf8Parse<T> parseUtf8, Utf8Try<T> tryUtf8, StringParse<T> parseString,
        StringTry<T> tryString, StyledStringParse<T> parseStyledString, StyledStringTry<T> tryStyledString)
    {
        DirectParser<T>.ParseUtf8 = parseUtf8;
        DirectParser<T>.TryUtf8 = tryUtf8;
        DirectParser<T>.ParseString = parseString;
        DirectParser<T>.TryString = tryString;
        DirectParser<T>.ParseStyledString = parseStyledString;
        DirectParser<T>.TryStyledString = tryStyledString;
        return;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool TryParse<T>(int route, string text, byte[] bytes, NumberStyles style, IFormatProvider provider, out T value)
        where T : INumberBase<T>
    {
        if (route == 0) return T.TryParse(text.AsSpan(), style, provider, out value);
        if (route == 1) return TryChars(text.AsSpan(), provider, out value);
        if (route == 2) return T.TryParse(new ReadOnlySpan<byte>(bytes), style, provider, out value);
        if (route == 3) return DirectParser<T>.TryUtf8(new ReadOnlySpan<byte>(bytes), provider, out value);
        if (route == 4) return DirectParser<T>.TryStyledString(text, style, provider, out value);
        return DirectParser<T>.TryString(text, provider, out value);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static T Parse<T>(int route, string text, byte[] bytes, NumberStyles style, IFormatProvider provider)
        where T : INumberBase<T>
    {
        if (route == 0) return T.Parse(text.AsSpan(), style, provider);
        if (route == 1) return ParseChars<T>(text.AsSpan(), provider);
        if (route == 2) return T.Parse(new ReadOnlySpan<byte>(bytes), style, provider);
        if (route == 3) return DirectParser<T>.ParseUtf8(new ReadOnlySpan<byte>(bytes), provider);
        if (route == 4) return DirectParser<T>.ParseStyledString(text, style, provider);
        return DirectParser<T>.ParseString(text, provider);
    }

    private static void ParseRoute<T>(string label, int route, string text, byte[] bytes, NumberStyles style,
        IFormatProvider provider, int failure, T expected, T before, T after) where T : unmanaged, IBinaryInteger<T>
    {
        string key = label + ".route" + route;
        T value = before;
        bool ok = TryParse(route, text, bytes, style, provider, out value);
        Check(key + ".try-local", ok == (failure == 0) && value == expected);
        T[] array = { before, after, after };
        ok = TryParse(route, text, bytes, style, provider, out array[1]);
        Check(key + ".try-array", ok == (failure == 0) && array[1] == expected && array[0] == before && array[2] == after);
        Fields<T> fields = new Fields<T> { Before = before, Value = after, After = after };
        ok = TryParse(route, text, bytes, style, provider, out fields.Value);
        Check(key + ".try-field", ok == (failure == 0) && fields.Value == expected && fields.Before == before && fields.After == after);
        int actual = 0;
        value = before;
        try { value = Parse<T>(route, text, bytes, style, provider); }
        catch (FormatException) { actual = 1; }
        catch (OverflowException) { actual = 2; }
        catch (ArgumentNullException error) { actual = error.ParamName == "s" ? 3 : 4; }
        Check(key + ".parse", actual == failure && (failure != 0 || value == expected));
        return;
    }

    private static void ParseCase<T>(string label, string text, NumberStyles style, bool defaults,
        IFormatProvider provider, int failure, T expected, T before, T after) where T : unmanaged, IBinaryInteger<T>
    {
        byte[] bytes = Encoding.UTF8.GetBytes(text);
        for (int route = 0; route < 6; route += defaults ? 1 : 2)
            ParseRoute(label, route, text, bytes, style, provider, failure, expected, before, after);
        return;
    }

    private sealed class ProviderFailure : Exception { }

    private sealed class Provider : IFormatProvider
    {
        internal int Calls;
        internal bool CorrectType = true;
        private readonly bool _throws;
        internal Provider(bool throws) { _throws = throws; }
        public object GetFormat(Type type)
        {
            Calls++;
            CorrectType &= type == typeof(NumberFormatInfo);
            if (_throws) throw new ProviderFailure();
            return CultureInfo.InvariantCulture.NumberFormat;
        }
    }

    private static void ProviderAndNulls<T>(string label, T before, T after) where T : unmanaged, IBinaryInteger<T>
    {
        byte[] valid = { 0x31 }, malformed = { 0xff };
        for (int route = 0; route < 6; route++)
        {
            string key = label + ".provider.route" + route;
            Provider provider = new Provider(false);
            T value = before;
            Check(key + ".try-call", TryParse(route, "1", valid, NumberStyles.Integer, provider, out value)
                && value == T.One && provider.Calls == 1 && provider.CorrectType);
            provider = new Provider(false);
            Check(key + ".parse-call", Parse<T>(route, "1", valid, NumberStyles.Integer, provider) == T.One
                && provider.Calls == 1 && provider.CorrectType);
            provider = new Provider(true);
            bool threw = false;
            value = before;
            try { TryParse(route, "x", malformed, NumberStyles.Integer, provider, out value); }
            catch (ProviderFailure) { threw = true; }
            Check(key + ".try-throw", threw && value == before && provider.Calls == 1 && provider.CorrectType);
            provider = new Provider(true);
            threw = false;
            try { Parse<T>(route, "x", malformed, NumberStyles.Integer, provider); }
            catch (ProviderFailure) { threw = true; }
            Check(key + ".parse-throw", threw && provider.Calls == 1 && provider.CorrectType);
            for (int input = 0; input < 3; input++)
            {
                string text = input == 0 ? "1" : input == 1 ? "x" : "4294967296";
                byte[] bytes = Encoding.UTF8.GetBytes(text);
                provider = new Provider(false);
                bool nullFault = false;
                try { TryParse(route, text, bytes, NumberStyles.Integer, provider, out Unsafe.NullRef<T>()); }
                catch (NullReferenceException) { nullFault = true; }
                Check(key + ".null-out" + input, nullFault && provider.Calls == 1);
            }
            provider = new Provider(true);
            threw = false;
            try { TryParse(route, "1", valid, NumberStyles.Integer, provider, out Unsafe.NullRef<T>()); }
            catch (ProviderFailure) { threw = true; }
            Check(key + ".provider-before-null-out", threw && provider.Calls == 1);
            if ((route & 1) == 0)
            {
                NumberStyles[] invalid = { (NumberStyles)(-1), NumberStyles.HexNumber | NumberStyles.AllowLeadingSign,
                    NumberStyles.BinaryNumber | NumberStyles.AllowDecimalPoint };
                for (int index = 0; index < invalid.Length; index++)
                {
                    provider = new Provider(true);
                    value = before;
                    bool argument = false;
                    try { TryParse(route, null, malformed, invalid[index], provider, out value); }
                    catch (ArgumentException error) { argument = error.ParamName == "style"; }
                    Check(key + ".try-invalid-style" + index, argument && value == before && provider.Calls == 0);
                    provider = new Provider(true);
                    argument = false;
                    try { Parse<T>(route, "x", malformed, invalid[index], provider); }
                    catch (ArgumentException error) { argument = error.ParamName == "style"; }
                    Check(key + ".parse-invalid-style" + index, argument && provider.Calls == 0);
                    provider = new Provider(true);
                    argument = false;
                    try { Parse<T>(route, null, malformed, invalid[index], provider); }
                    catch (ArgumentNullException error) { argument = route == 4 && error.ParamName == "s"; }
                    catch (ArgumentException error) { argument = route != 4 && error.ParamName == "style"; }
                    Check(key + ".parse-null-style-priority" + index, argument && provider.Calls == 0);
                    provider = new Provider(true);
                    argument = false;
                    try { TryParse(route, null, malformed, invalid[index], provider, out Unsafe.NullRef<T>()); }
                    catch (ArgumentException error) { argument = error.ParamName == "style"; }
                    Check(key + ".style-before-null-out" + index, argument && provider.Calls == 0);
                }
            }
            ParseRoute(label + ".null-input", route, null, null, NumberStyles.Integer,
                CultureInfo.InvariantCulture, route >= 4 ? 3 : 1, T.Zero, before, after);
            provider = new Provider(true);
            threw = false;
            value = before;
            bool ok = true;
            try { ok = TryParse(route, null, null, NumberStyles.Integer, provider, out value); }
            catch (ProviderFailure) { threw = true; }
            Check(key + ".null-input-priority", route >= 4
                ? !threw && !ok && value == T.Zero && provider.Calls == 0
                : threw && value == before && provider.Calls == 1);
            provider = new Provider(true);
            threw = false;
            bool nullArgument = false;
            try { Parse<T>(route, null, null, NumberStyles.Integer, provider); }
            catch (ArgumentNullException error) { nullArgument = error.ParamName == "s"; }
            catch (ProviderFailure) { threw = true; }
            Check(key + ".parse-null-input-priority", route >= 4
                ? nullArgument && !threw && provider.Calls == 0
                : !nullArgument && threw && provider.Calls == 1);
            provider = new Provider(true);
            threw = false;
            bool nullOutput = false;
            try { TryParse(route, null, null, NumberStyles.Integer, provider, out Unsafe.NullRef<T>()); }
            catch (NullReferenceException) { nullOutput = true; }
            catch (ProviderFailure) { threw = true; }
            Check(key + ".null-input-output-priority", route >= 4
                ? nullOutput && !threw && provider.Calls == 0
                : !nullOutput && threw && provider.Calls == 1);
        }
        Console.WriteLine(label + " parsing provider/null native32 end");
        return;
    }

    private static void MalformedUtf8<T>(string label, T before, T after) where T : unmanaged, IBinaryInteger<T>
    {
        byte[][] bad = { new byte[] { 0xff }, new byte[] { 0x80 }, new byte[] { 0xc0, 0xaf },
            new byte[] { 0xc2 }, new byte[] { 0xe2, 0x82 }, new byte[] { 0xed, 0xa0, 0x80 },
            new byte[] { 0xf0, 0x80, 0x80, 0x80 }, new byte[] { 0xf4, 0x90, 0x80, 0x80 },
            new byte[] { 0xf1, 0x80, 0x80 }, new byte[] { 0x31, 0xff }, new byte[] { 0xff, 0x31 } };
        NumberFormatInfo format = new NumberFormatInfo { PositiveSign = "\ufffd" };
        for (int input = 0; input < bad.Length; input++)
        for (int route = 2; route < 4; route++)
        {
            ParseRoute(label + ".malformed" + input, route, "", bad[input], NumberStyles.Integer,
                format, 1, T.Zero, before, after);
            bool fault = false;
            try { TryParse(route, "", bad[input], NumberStyles.Integer, format, out Unsafe.NullRef<T>()); }
            catch (NullReferenceException) { fault = true; }
            Check(label + ".malformed" + input + ".null-out" + route, fault);
        }
        ParseCase(label + ".replacement-sign", "\ufffd1", NumberStyles.Integer, true, format, 0, T.One, before, after);
        Console.WriteLine(label + " parsing utf8 native32 end");
        return;
    }

    private static void Parsing<T>(string label, bool signed, T min, T max, T high, T negativeOne,
        T signedMax, T before, T after) where T : unmanaged, IBinaryInteger<T>
    {
        string key = label + ".parse";
        IFormatProvider format = CultureInfo.InvariantCulture;
        ParseCase(key + ".zero", "0", NumberStyles.Integer, true, format, 0, T.Zero, before, after);
        ParseCase(key + ".one", "1", NumberStyles.Integer, true, format, 0, T.One, before, after);
        ParseCase(key + ".negative-zero", "-0", NumberStyles.Integer, true, format, 0, T.Zero, before, after);
        ParseCase(key + ".min", signed ? "-2147483648" : "0", NumberStyles.Integer, true, format, 0, min, before, after);
        ParseCase(key + ".max", signed ? "2147483647" : "4294967295", NumberStyles.Integer, true, format, 0, max, before, after);
        ParseCase(key + ".signed-max", "2147483647", NumberStyles.Integer, true, format, 0, signedMax, before, after);
        ParseCase(key + ".high-bit", "2147483648", NumberStyles.Integer, true, format, signed ? 2 : 0,
            signed ? T.Zero : high, before, after);
        ParseCase(key + ".below", signed ? "-2147483649" : "-1", NumberStyles.Integer, true, format, 2, T.Zero, before, after);
        ParseCase(key + ".above", signed ? "2147483648" : "4294967296", NumberStyles.Integer, true, format, 2, T.Zero, before, after);
        ParseCase(key + ".empty", "", NumberStyles.Integer, true, format, 1, T.Zero, before, after);
        ParseCase(key + ".format", "1x", NumberStyles.Integer, true, format, 1, T.Zero, before, after);
        ParseCase(key + ".whitespace", " \t+1\r\n", NumberStyles.Integer, true, format, 0, T.One, before, after);
        ParseCase(key + ".none", "1", NumberStyles.None, false, format, 0, T.One, before, after);
        ParseCase(key + ".none-whitespace", " 1", NumberStyles.None, false, format, 1, T.Zero, before, after);
        ParseCase(key + ".none-sign", "+1", NumberStyles.None, false, format, 1, T.Zero, before, after);
        ParseCase(key + ".hex-signed-max", "7FFFFFFF", NumberStyles.HexNumber, false, format, 0, signedMax, before, after);
        ParseCase(key + ".hex-high", "80000000", NumberStyles.HexNumber, false, format, 0, high, before, after);
        ParseCase(key + ".hex-ones", "FFFFFFFF", NumberStyles.HexNumber, false, format, 0, signed ? negativeOne : max, before, after);
        ParseCase(key + ".hex-padding", "00000000FFFFFFFF", NumberStyles.HexNumber, false, format, 0, signed ? negativeOne : max, before, after);
        ParseCase(key + ".hex-overflow", "100000000", NumberStyles.HexNumber, false, format, 2, T.Zero, before, after);
        ParseCase(key + ".hex-format", "0x1", NumberStyles.HexNumber, false, format, 1, T.Zero, before, after);
        ParseCase(key + ".binary-high", "10000000000000000000000000000000", NumberStyles.BinaryNumber, false, format, 0, high, before, after);
        ParseCase(key + ".binary-ones", "11111111111111111111111111111111", NumberStyles.BinaryNumber, false, format, 0, signed ? negativeOne : max, before, after);
        ParseCase(key + ".binary-overflow", "100000000000000000000000000000000", NumberStyles.BinaryNumber, false, format, 2, T.Zero, before, after);
        ParseCase(key + ".binary-format", "102", NumberStyles.BinaryNumber, false, format, 1, T.Zero, before, after);
        ParseCase(key + ".decimal-zero", "1.0", NumberStyles.Number, false, format, 0, T.One, before, after);
        ParseCase(key + ".decimal-fraction", "1.5", NumberStyles.Number, false, format, 2, T.Zero, before, after);
        NumberFormatInfo custom = new NumberFormatInfo { PositiveSign = "plus", NegativeSign = "minus" };
        ParseCase(key + ".custom-positive", "plus1", NumberStyles.Integer, true, custom, 0, T.One, before, after);
        ParseCase(key + ".custom-negative", "minus1", NumberStyles.Integer, true, custom, signed ? 0 : 2,
            signed ? negativeOne : T.Zero, before, after);
        ParseCase(key + ".custom-min", signed ? "minus2147483648" : "minus0", NumberStyles.Integer, true, custom, 0, min, before, after);
        custom.PositiveSign = "\u2295";
        custom.NegativeSign = "\u2212";
        ParseCase(key + ".unicode-positive", "\u22951", NumberStyles.Integer, true, custom, 0, T.One, before, after);
        ParseCase(key + ".unicode-negative", "\u22121", NumberStyles.Integer, true, custom, signed ? 0 : 2,
            signed ? negativeOne : T.Zero, before, after);
        Console.WriteLine(label + " parsing values native32 end");
        ProviderAndNulls<T>(label, before, after);
        MalformedUtf8<T>(label, before, after);
        Console.WriteLine(label + " parsing native32 end");
        return;
    }
}
