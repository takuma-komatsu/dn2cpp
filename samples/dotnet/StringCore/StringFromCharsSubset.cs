#nullable enable
using System;
namespace StringFromCharsSubset;


static class Program
{
    private static string _evaluation = "";

    private static char Character(char value)
    {
        _evaluation += "C";
        return value;
    }

    private static char ThrowingCharacter()
    {
        _evaluation += "C";
        throw new InvalidOperationException();
    }

    private static int ThrowingCount()
    {
        _evaluation += "N";
        throw new InvalidOperationException();
    }

    private static ReadOnlySpan<char> SpanValue(char[] value, int start, int length)
    {
        _evaluation += "S";
        return value.AsSpan(start, length);
    }

    private static char[]? ArrayValue(char[]? value)
    {
        _evaluation += "A";
        return value;
    }

    private static int Number(string step, int value)
    {
        _evaluation += step;
        return value;
    }

    private static char[] ThrowingArray()
    {
        _evaluation += "A";
        throw new InvalidOperationException();
    }

    private static int ThrowingLength()
    {
        _evaluation += "L";
        throw new InvalidOperationException();
    }

    private static string Units(string value)
    {
        string result = "";
        foreach (char c in value)
            result += ((int)c).ToString("X4") + " ";
        return result;
    }

    private static void ProbeArray(string label, Func<string> body)
    {
        try
        {
            Console.WriteLine(label + " result=" + Units(body()));
        }
        catch (Exception ex)
        {
            Console.WriteLine(label + " " + ex.GetType().Name);
            if (ex is ArgumentException argument)
            {
                Console.WriteLine(label + " param=" + argument.ParamName);
                Console.WriteLine(label + " message=" + ex.Message.Replace("\r", "").Replace("\n", "|"));
                if (ex is ArgumentOutOfRangeException range)
                    Console.WriteLine(label + " actual=" + (range.ActualValue is null
                        ? "null" : range.ActualValue.GetType().Name + ":" + range.ActualValue));
            }
        }
    }

    internal static void RunEmptySources()
    {
        Console.WriteLine("== empty string char sources ==");
        Console.WriteLine("repeat empties=" + ReferenceEquals(new string('x', 0), string.Empty)
            + ":" + ReferenceEquals(new string('\u0000', 0), string.Empty)
            + ":" + ReferenceEquals(new string('\uD800', 0), string.Empty)
            + ":" + ReferenceEquals(new string('\uDC00', 0), string.Empty));
        char[] chars = { 'A', '\u0000', '\uD800', '\uDC00', 'Z' };
        Console.WriteLine("span ctor empties=" + ReferenceEquals(new string(ReadOnlySpan<char>.Empty), string.Empty)
            + ":" + ReferenceEquals(new string(default(ReadOnlySpan<char>)), string.Empty)
            + ":" + ReferenceEquals(new string(Span<char>.Empty), string.Empty)
            + ":" + ReferenceEquals(new string(default(Span<char>)), string.Empty)
            + ":" + ReferenceEquals(new string(chars.AsSpan(0, 0)), string.Empty)
            + ":" + ReferenceEquals(new string(chars.AsSpan(chars.Length, 0)), string.Empty)
            + ":" + ReferenceEquals(new string("abc".AsSpan(3, 0)), string.Empty));
        Console.WriteLine("span tostring empties=" + ReferenceEquals(ReadOnlySpan<char>.Empty.ToString(), string.Empty)
            + ":" + ReferenceEquals(Span<char>.Empty.ToString(), string.Empty)
            + ":" + ReferenceEquals(chars.AsSpan(chars.Length, 0).ToString(), string.Empty)
            + ":" + ReferenceEquals("abc".AsSpan(3, 0).ToString(), string.Empty));
        string repeated = new string('\uD800', 3);
        string copied = new string(chars.AsSpan(1, 3));
        string spanText = chars.AsSpan(1, 3).ToString();
        Console.WriteLine("repeat units=" + Units(repeated));
        Console.WriteLine("span units=" + Units(copied));
        Console.WriteLine("char source fresh=" + ReferenceEquals(repeated, new string('\uD800', 3))
            + ":" + ReferenceEquals(copied, new string(chars.AsSpan(1, 3)))
            + ":" + ReferenceEquals(spanText, chars.AsSpan(1, 3).ToString()));
        string source = new string('x', 3);
        Console.WriteLine("span string fresh=" + ReferenceEquals(source, new string(source.AsSpan())));
        chars[1] = 'Q';
        GC.Collect();
        Console.WriteLine("span independent=" + Units(copied) + ":" + Units(spanText));
        Console.WriteLine("char source empty after GC=" + ReferenceEquals(new string('x', 0), string.Empty)
            + ":" + ReferenceEquals(new string(chars.AsSpan(chars.Length, 0)), string.Empty)
            + ":" + ReferenceEquals(chars.AsSpan(chars.Length, 0).ToString(), string.Empty));
        ProbeArray("repeat negative", () => new string('x', -1));
        ProbeArray("repeat minimum", () => new string('x', int.MinValue));
        _evaluation = "";
        ProbeArray("repeat evaluated empty", () => new string(Character('x'), Number("N", 0)));
        Console.WriteLine("repeat empty evaluation=" + _evaluation);
        _evaluation = "";
        ProbeArray("repeat evaluated negative", () => new string(Character('x'), Number("N", -1)));
        Console.WriteLine("repeat negative evaluation=" + _evaluation);
        _evaluation = "";
        ProbeArray("repeat throwing count", () => new string(Character('x'), ThrowingCount()));
        Console.WriteLine("repeat throwing count evaluation=" + _evaluation);
        _evaluation = "";
        ProbeArray("repeat throwing char", () => new string(ThrowingCharacter(), Number("N", 0)));
        Console.WriteLine("repeat throwing char evaluation=" + _evaluation);
        _evaluation = "";
        ProbeArray("span evaluated empty", () => new string(SpanValue(chars, chars.Length, 0)));
        Console.WriteLine("span empty evaluation=" + _evaluation);
        Console.WriteLine("empty string char sources end");
    }

    internal static void RunArrays()
    {
        Console.WriteLine("== string array constructors ==");
        char[] chars = { 'A', '\u0000', '\uD800', '\uDC00', 'Z' };
        ProbeArray("whole-utf16", () => new string(chars));
        ProbeArray("slice-utf16", () => new string(chars, 1, 3));
        Console.WriteLine("whole empties=" + ReferenceEquals(new string((char[]?)null), string.Empty)
            + ":" + ReferenceEquals(new string(new char[0]), string.Empty)
            + ":" + ReferenceEquals(new string(Array.Empty<char>()), string.Empty)
            + ":" + ReferenceEquals(new string("".ToCharArray()), string.Empty)
            + ":" + ReferenceEquals(new string("abc".ToCharArray(3, 0)), string.Empty));
        Console.WriteLine("slice empties=" + ReferenceEquals(new string(chars, 5, 0), string.Empty)
            + ":" + ReferenceEquals(new string(chars, 2, 0), string.Empty)
            + ":" + ReferenceEquals(new string(Array.Empty<char>(), 0, 0), string.Empty));
        GC.Collect();
        Console.WriteLine("ctor empty after GC=" + ReferenceEquals(new string(new char[0]), string.Empty)
            + ":" + ReferenceEquals(new string(chars, 5, 0), string.Empty));
        char[] mutable = { 'a', 'b', 'c' };
        string whole = new string(mutable);
        string slice = new string(mutable, 1, 2);
        Console.WriteLine("ctor fresh=" + ReferenceEquals(whole, new string(mutable))
            + ":" + ReferenceEquals(slice, new string(mutable, 1, 2)));
        mutable[1] = 'Q';
        GC.Collect();
        Console.WriteLine("ctor independent=" + whole + ":" + slice + ":" + new string(mutable));

        ProbeArray("null-before-bounds", () => new string((char[])null!, -1, -1));
        ProbeArray("start-before-length", () => new string(mutable, -1, -1));
        ProbeArray("start-minimum", () => new string(mutable, int.MinValue, int.MinValue));
        ProbeArray("length-before-window", () => new string(mutable, int.MaxValue, -1));
        ProbeArray("length-minimum", () => new string(mutable, 0, int.MinValue));
        ProbeArray("start-maximum", () => new string(mutable, int.MaxValue, 0));
        ProbeArray("length-maximum", () => new string(mutable, 0, int.MaxValue));
        ProbeArray("slice-past-end", () => new string(mutable, 4, 0));
        ProbeArray("slice-window", () => new string(mutable, 2, 2));
        ProbeArray("empty-bad-start", () => new string(Array.Empty<char>(), 1, 0));
        ProbeArray("empty-bad-length", () => new string(Array.Empty<char>(), 0, 1));
        ProbeArray("empty-negative-length", () => new string(Array.Empty<char>(), 1, -1));

        _evaluation = "";
        ProbeArray("whole evaluated null", () => new string(ArrayValue(null)));
        Console.WriteLine("whole null evaluation=" + _evaluation);
        _evaluation = "";
        ProbeArray("slice evaluated null", () => new string(ArrayValue(null)!, Number("I", -1), Number("L", -1)));
        Console.WriteLine("slice null evaluation=" + _evaluation);
        _evaluation = "";
        ProbeArray("slice evaluated copy", () => new string(ArrayValue(chars)!, Number("I", 1), Number("L", 3)));
        Console.WriteLine("slice copy evaluation=" + _evaluation);
        _evaluation = "";
        ProbeArray("slice throwing length", () => new string(ArrayValue(null)!, Number("I", -1), ThrowingLength()));
        Console.WriteLine("slice throwing length evaluation=" + _evaluation);
        _evaluation = "";
        ProbeArray("slice throwing array", () => new string(ThrowingArray(), Number("I", -1), Number("L", -1)));
        Console.WriteLine("slice throwing array evaluation=" + _evaluation);
        Console.WriteLine("string array constructors end");
    }

    internal static void Run()
    {
        string s = "hello world";

        // ReadOnlySpan<char>.ToString() -> string (its body is new string(this)).
        Console.WriteLine(s.AsSpan(6).ToString());        // world
        Console.WriteLine(s.AsSpan(0, 5).ToString());     // hello

        // new string(ReadOnlySpan<char>) and from a Span<char>.
        Console.WriteLine(new string(s.AsSpan(0, 5)));    // hello
        Span<char> sc = new char[] { 'S', 'p', 'a', 'n' };
        Console.WriteLine(new string(sc));                // Span

        // new string(char[]) and new string(char[], start, length).
        char[] arr = { 'a', 'b', 'c', 'd', 'e' };
        Console.WriteLine(new string(arr));               // abcde
        Console.WriteLine(new string(arr, 1, 3));         // bcd

        // new string(char, count).
        Console.WriteLine(new string('x', 5));            // xxxxx
        Console.WriteLine(new string('-', 0));            // (empty)

        // The constructed string is independent: mutating the source array afterward
        // must not change it (proves the copy, not an alias).
        char[] m = { 'A', 'B', 'C' };
        string copy = new string(m);
        m[0] = 'Z';
        Console.WriteLine(copy + "/" + new string(m));    // ABC/ZBC

        // Round-trips compose with other string ops.
        Console.WriteLine(new string(s.AsSpan(0, 5)).ToUpper()); // HELLO
        Console.WriteLine(("[" + new string("".AsSpan()) + "]")); // []
    }
}
