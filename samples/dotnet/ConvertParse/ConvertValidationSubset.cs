using System;
using System.Globalization;
using System.Text;

namespace ConvertValidationSubset;

internal static class Program
{
    private static string Bytes(byte[] values) => BitConverter.ToString(values);
    private static void Fault(string label, Exception ex)
    {
        Console.WriteLine(label + " type=" + ex.GetType().Name);
        Console.WriteLine(label + " param=" + (ex is ArgumentException arg ? arg.ParamName : null));
        Console.WriteLine(label + " message=" + ex.Message.Replace("\r", "").Replace("\n", "|"));
        Console.WriteLine(label + " message units=" + Units(ex.Message));
        object actual = ex is ArgumentOutOfRangeException range ? range.ActualValue : null;
        Console.WriteLine(label + " actual=" + (actual is null ? "null" : actual.GetType().Name + ":" + actual));
    }
    private static void Observe(string label, Func<string> action)
    {
        try { Console.WriteLine(label + " success=" + action()); }
        catch (Exception ex) { Fault(label, ex); }
    }

    private static string Units(string value)
    {
        if (value is null)
            return "null";
        string result = "";
        for (int i = 0; i < value.Length; i++)
            result += (i == 0 ? "" : ",") + (int)value[i];
        return result;
    }

    private static string TryDecode(string text, int capacity, bool chars)
    {
        Span<byte> destination = System.Runtime.InteropServices.MemoryMarshal.CreateSpan(
            ref System.Runtime.CompilerServices.Unsafe.NullRef<byte>(), capacity);
        int count = -7;
        bool success;
        try
        {
            success = chars
                ? Convert.TryFromBase64Chars(text.AsSpan(), destination, out count)
                : Convert.TryFromBase64String(text, destination, out count);
        }
        catch
        {
            Console.WriteLine("null destination out count=" + count);
            throw;
        }
        return success + ":" + count;
    }

    private static string TryEncode(byte[] source, int option)
    {
        return Convert.TryToBase64Chars(source.AsSpan(), default, out int count,
            (Base64FormattingOptions)option) + ":" + count;
    }

    internal static void Run()
    {
        Console.WriteLine("== Conversion validation fields ==");
        byte[][] arrays = { null, Array.Empty<byte>(), new byte[] { 0, 1, 127, 255 } };
        char[][] charArrays = { null, Array.Empty<char>(), "QUJD".ToCharArray(), "!".ToCharArray() };
        int[] bounds = { int.MinValue, -1, 0, 1, 4, int.MaxValue };
        int[] options = { int.MinValue, -1, 0, 1, 2, int.MaxValue };
        for (int a = 0; a < arrays.Length; a++)
        {
            Observe("hex:" + a, () => Convert.ToHexString(arrays[a]));
            Observe("hex lower:" + a, () => Convert.ToHexStringLower(arrays[a]));
            foreach (int option in options)
            {
                Observe("base64:" + a + ":" + option, () => Convert.ToBase64String(arrays[a], (Base64FormattingOptions)option));
                if (arrays[a] is not null)
                    Observe("base64 span:" + a + ":" + option, () => Convert.ToBase64String(arrays[a].AsSpan(), (Base64FormattingOptions)option));
            }
            foreach (int offset in bounds)
                foreach (int length in bounds)
                {
                    string label = a + ":" + offset + ":" + length;
                    Observe("hex range:" + label, () => Convert.ToHexString(arrays[a], offset, length));
                    Observe("hex lower range:" + label, () => Convert.ToHexStringLower(arrays[a], offset, length));
                    foreach (int option in options)
                        Observe("base64 range:" + label + ":" + option, () => Convert.ToBase64String(arrays[a], offset, length, (Base64FormattingOptions)option));
                }
            foreach (int offsetIn in bounds)
                foreach (int length in bounds)
                    foreach (int offsetOut in bounds)
                        foreach (int option in options)
                            for (int d = 0; d < 4; d++)
                            {
                                char[] destination = d == 0 ? null : new char[d == 1 ? 0 : d == 2 ? 4 : 8];
                                if (destination is not null)
                                    Array.Fill(destination, '?');
                                string label = "base64 chars:" + a + ":" + d + ":" + offsetIn + ":" + length + ":" + offsetOut + ":" + option;
                                Observe(label, () => Convert.ToBase64CharArray(arrays[a], offsetIn, length, destination, offsetOut, (Base64FormattingOptions)option).ToString());
                                Console.WriteLine(label + " destination=" + (destination is null ? "null" : new string(destination)));
                            }
        }
        for (int a = 0; a < charArrays.Length; a++)
            foreach (int offset in bounds)
                foreach (int length in bounds)
                    Observe("from chars:" + a + ":" + offset + ":" + length, () => Bytes(Convert.FromBase64CharArray(charArrays[a], offset, length)));

        string[] texts = { null, "", "A", "AAAA", "QUJD", "A===", "!", "  QUJD\r\n", "0", "00", "0F", "0f", "FF", "gg", "true", "FALSE", " true ", "7", "-0", "-1", "+", "0x", "1!", "\0true\0", "\u00a0FALSE\u2003", "\ud800", "a\0b\udc00", "128", "256", "32768", "65536", "2147483648", "4294967295", "4294967296", "9223372036854775808", "18446744073709551615", "18446744073709551616", "-128", "-129", "-2147483648", "-2147483649", "-9223372036854775808", "-9223372036854775809", "FFFF", "FFFFFFFF", "100000000", "FFFFFFFFFFFFFFFF", "10000000000000000", "AAAA!", "2147483648!", "4294967296!", "-1!" };
        for (int i = 0; i < texts.Length; i++)
        {
            string label = i.ToString();
            Observe("from64:" + label, () => Bytes(Convert.FromBase64String(texts[i])));
            Observe("fromhex:" + label, () => Bytes(Convert.FromHexString(texts[i])));
            Observe("bool:" + label, () => Convert.ToBoolean(texts[i]).ToString());
            Observe("bool provider:" + label, () => Convert.ToBoolean(texts[i], CultureInfo.InvariantCulture).ToString());
            Observe("bool object:" + label, () => Convert.ToBoolean((object)texts[i]).ToString());
            Observe("bool parse:" + label, () => bool.Parse(texts[i]).ToString());
            Console.WriteLine("bool try:" + label + " success=" + bool.TryParse(texts[i], out bool parsed) + ":" + parsed);
            foreach (int radix in new[] { -1, 0, 2, 8, 10, 16, 32 })
            {
                Observe("radix i8:" + label + ":" + radix, () => Convert.ToSByte(texts[i], radix).ToString());
                Observe("radix i16:" + label + ":" + radix, () => Convert.ToInt16(texts[i], radix).ToString());
                Observe("radix u16:" + label + ":" + radix, () => Convert.ToUInt16(texts[i], radix).ToString());
                Observe("radix u32:" + label + ":" + radix, () => Convert.ToUInt32(texts[i], radix).ToString());
                Observe("radix i64:" + label + ":" + radix, () => Convert.ToInt64(texts[i], radix).ToString());
                Observe("radix byte:" + label + ":" + radix, () => Convert.ToByte(texts[i], radix).ToString());
                Observe("radix i32:" + label + ":" + radix, () => Convert.ToInt32(texts[i], radix).ToString());
                Observe("radix u64:" + label + ":" + radix, () => Convert.ToUInt64(texts[i], radix).ToString());
            }
            byte[] destination = new byte[4];
            Observe("try64:" + label, () => Convert.TryFromBase64String(texts[i], destination, out int count) + ":" + count);
        }
        for (int i = 0; i < texts.Length; i++)
            foreach (int capacity in new[] { 0, 1, 4 })
            {
                string label = i + ":" + capacity;
                Observe("try null string:" + label, () => TryDecode(texts[i], capacity, false));
                Observe("try null chars:" + label, () => TryDecode(texts[i], capacity, true));
            }
        foreach (byte[] array in arrays)
            foreach (int option in options)
                Observe("try encode:" + (array is null ? "null" : array.Length.ToString()) + ":" + option,
                    () => TryEncode(array, option));
        Console.WriteLine("empty base64 array=" + ReferenceEquals(Convert.ToBase64String(Array.Empty<byte>()), string.Empty));
        Console.WriteLine("empty base64 range=" + ReferenceEquals(Convert.ToBase64String(Array.Empty<byte>(), 0, 0), string.Empty));
        Console.WriteLine("empty base64 span=" + ReferenceEquals(Convert.ToBase64String(ReadOnlySpan<byte>.Empty), string.Empty));
        Console.WriteLine("empty hex array=" + ReferenceEquals(Convert.ToHexString(Array.Empty<byte>()), string.Empty));
        Console.WriteLine("empty hex lower=" + ReferenceEquals(Convert.ToHexStringLower(Array.Empty<byte>()), string.Empty));
        Console.WriteLine("empty hex range=" + ReferenceEquals(Convert.ToHexString(Array.Empty<byte>(), 0, 0), string.Empty));
        Console.WriteLine("empty hex lower range=" + ReferenceEquals(Convert.ToHexStringLower(Array.Empty<byte>(), 0, 0), string.Empty));
        Console.WriteLine("empty hex span=" + ReferenceEquals(Convert.ToHexString(ReadOnlySpan<byte>.Empty), string.Empty));
        Console.WriteLine("empty hex lower span=" + ReferenceEquals(Convert.ToHexStringLower(ReadOnlySpan<byte>.Empty), string.Empty));
        Console.WriteLine("empty from hex=" + ReferenceEquals(Convert.FromHexString(""), Array.Empty<byte>()));
        Console.WriteLine("empty from base64 string=" + ReferenceEquals(Convert.FromBase64String(""), Array.Empty<byte>()));
        Console.WriteLine("empty from base64 chars=" + ReferenceEquals(Convert.FromBase64CharArray(Array.Empty<char>(), 0, 0), Array.Empty<byte>()));
        Console.WriteLine("white from base64 chars=" + ReferenceEquals(Convert.FromBase64CharArray("  ".ToCharArray(), 0, 2), Array.Empty<byte>()));
        Exception[] saved = new Exception[4];
        try { bool.Parse("bad\0\ud800"); } catch (Exception ex) { saved[0] = ex; }
        try { Convert.ToBase64String(Array.Empty<byte>(), (Base64FormattingOptions)int.MaxValue); } catch (Exception ex) { saved[1] = ex; }
        try { Convert.ToHexString(new byte[1], int.MaxValue, 1); } catch (Exception ex) { saved[2] = ex; }
        try { Convert.ToUInt64("-0", 10); } catch (Exception ex) { saved[3] = ex; }
        GC.Collect();
        GC.WaitForPendingFinalizers();
        for (int i = 0; i < saved.Length; i++)
            Fault("conversion after GC:" + i, saved[i]);
        Console.WriteLine("Conversion validation fields end");
    }

    public static void RunSpanConversions()
    {
        Console.WriteLine("== Boolean spans and hex destinations ==");
        string[] texts = { null, "", "true", "FALSE", "\0true\0", "\u00a0FALSE\u2003", "trueX", "tr\0ue", "\ud800" };
        for (int i = 0; i < texts.Length; i++)
        {
            ReadOnlySpan<char> span = texts[i].AsSpan();
            bool parsed = true;
            bool success = bool.TryParse(span, out parsed);
            Console.WriteLine("bool span:" + i + " success=" + success + ":" + parsed);
            string text = texts[i];
            Observe("bool span parse:" + i, () => bool.Parse(text.AsSpan()).ToString());
        }
        ReadOnlySpan<char> slice = "XTrUeY".AsSpan(1, 4);
        Console.WriteLine("bool slice=" + bool.Parse(slice));

        byte[] data = { 0, 0xab, 0xff };
        foreach (int capacity in new[] { 0, 5, 6, 8 })
        {
            foreach (bool lower in new[] { false, true })
            {
                char[] chars = new char[capacity];
                byte[] bytes = new byte[capacity];
                Array.Fill(chars, '?');
                Array.Fill(bytes, (byte)'?');
                int written;
                bool success = lower ? Convert.TryToHexStringLower(data, chars, out written)
                    : Convert.TryToHexString(data, chars, out written);
                Console.WriteLine("hex chars:" + capacity + ":" + lower + "=" + success + ":" + written + ":" + new string(chars));
                success = lower ? Convert.TryToHexStringLower(data, bytes, out written)
                    : Convert.TryToHexString(data, bytes, out written);
                Console.WriteLine("hex bytes:" + capacity + ":" + lower + "=" + success + ":" + written + ":" + Bytes(bytes));
            }
        }
        Console.WriteLine("empty hex chars=" + Convert.TryToHexString(ReadOnlySpan<byte>.Empty, Span<char>.Empty, out int emptyChars) + ":" + emptyChars);
        Console.WriteLine("empty hex bytes=" + Convert.TryToHexStringLower(ReadOnlySpan<byte>.Empty, Span<byte>.Empty, out int emptyBytes) + ":" + emptyBytes);
        Console.WriteLine("Boolean spans and hex destinations end");
    }

    public static void RunHexDecoding()
    {
        Console.WriteLine("== hex decoding overloads ==");
        foreach (string text in new[] { null, "", "0", "00aBff", "0x", "x0", "00x0", "000x", "00a", "é0", "漢0", "0123456789abcdef0123456789abcdef0x" })
        {
            string label = text ?? "<null>";
            byte[] utf8 = text is null ? Array.Empty<byte>() : Encoding.UTF8.GetBytes(text);
            Observe("hex array string:" + label, () => Bytes(Convert.FromHexString(text)));
            Observe("hex array chars:" + label, () => Bytes(Convert.FromHexString(text.AsSpan())));
            Observe("hex array utf8:" + label, () => Bytes(Convert.FromHexString(utf8.AsSpan())));
            foreach (int capacity in new[] { 0, 1, 2, 3, 4, 16 })
            {
                byte[] destination = new byte[capacity];
                int consumed = -1, written = -1;
                Array.Fill(destination, (byte)77);
                Observe("hex decode string:" + label + ":" + capacity, () =>
                    Convert.FromHexString(text, destination.AsSpan(), out consumed, out written) + ":" + consumed + ":" + written);
                Console.WriteLine("hex decoded=" + Bytes(destination));
                Array.Fill(destination, (byte)77);
                var status = Convert.FromHexString(text.AsSpan(), destination.AsSpan(), out consumed, out written);
                Console.WriteLine("hex decode chars:" + label + ":" + capacity + "=" + status + ":" + consumed + ":" + written + ":" + Bytes(destination));
                Array.Fill(destination, (byte)77);
                status = Convert.FromHexString(utf8.AsSpan(), destination.AsSpan(), out consumed, out written);
                Console.WriteLine("hex decode utf8:" + label + ":" + capacity + "=" + status + ":" + consumed + ":" + written + ":" + Bytes(destination));
            }
        }
        Console.WriteLine("empty hex chars array=" + ReferenceEquals(Convert.FromHexString(ReadOnlySpan<char>.Empty), Array.Empty<byte>()));
        Console.WriteLine("empty hex utf8 array=" + ReferenceEquals(Convert.FromHexString(ReadOnlySpan<byte>.Empty), Array.Empty<byte>()));
        Console.WriteLine("hex decoding overloads end");
    }

    public static void RunDecimalParseFaults()
    {
        Console.WriteLine("== decimal parse fault text ==");
        foreach (string text in new[] { "", "漢\0\ud800" })
        {
            Observe("decimal fault string", () => decimal.Parse(text).ToString());
            Observe("decimal fault chars", () => decimal.Parse(text.AsSpan(), CultureInfo.InvariantCulture).ToString());
            Observe("decimal fault utf8", () => decimal.Parse(Encoding.UTF8.GetBytes(text).AsSpan(), CultureInfo.InvariantCulture).ToString());
        }
        Console.WriteLine("decimal parse fault text end");
    }
}
