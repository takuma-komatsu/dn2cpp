using System;

namespace EnumParseSpan
{
    // Gate driver: the ReadOnlySpan<char> overloads of the generic Enum.Parse<T> /
    // Enum.TryParse<T>. The string overloads were lowered; the span ones fell into the
    // same arm with a bare cast of the span struct to a string pointer, which the C++
    // compiler rejects — the shape System.Text.Json's EnumConverter<T> reaches for
    // every enum-typed member. Names, numbers, case folding, flags and misses are
    // diffed against real .NET.
    internal enum Color { Red = 1, Green = 2, Blue = 3 }

    [Flags]
    internal enum Perm : byte { None = 0, Read = 1, Write = 2, Exec = 4 }

    internal enum Wide : long { Small = 1, Big = 1L << 40 }

    internal static class Program
    {
        private static void Try<T>(string text, bool ignoreCase) where T : struct, Enum
        {
            ReadOnlySpan<char> span = text.AsSpan();
            bool ok = Enum.TryParse<T>(span, ignoreCase, out T value);
            Console.WriteLine($"TryParse<{typeof(T).Name}>(\"{text}\", {ignoreCase}) = {ok} {value}");
        }

        private static void Parse<T>(string text) where T : struct, Enum
        {
            try
            {
                Console.WriteLine($"Parse<{typeof(T).Name}>(\"{text}\") = {Enum.Parse<T>(text.AsSpan())}");
            }
            catch (ArgumentException)
            {
                Console.WriteLine($"Parse<{typeof(T).Name}>(\"{text}\") threw ArgumentException");
            }
        }

        private static void Main()
        {
            Try<Color>("Green", false);
            Try<Color>("green", false);
            Try<Color>("green", true);
            Try<Color>("2", false);
            Try<Color>("7", false);
            Try<Color>("Purple", true);
            Try<Perm>("Read, Exec", false);
            Try<Perm>("read,write", true);
            Try<Perm>("6", false);
            Try<Wide>("Big", false);
            Try<Wide>("1099511627776", false);
            Try<Color>("", false);
            Parse<Color>("Blue");
            Parse<Color>("blue");
            Parse<Perm>("Write");
            Parse<Wide>("Small");

            // A span that is a slice of a larger buffer, the way a JSON reader hands
            // the value out: length must be honored, not the backing string's.
            ReadOnlySpan<char> sliced = "xxBluexx".AsSpan(2, 4);
            Console.WriteLine(Enum.TryParse<Color>(sliced, false, out Color fromSlice) + " " + fromSlice);
            Console.WriteLine(Enum.TryParse<Color>("Blue", out Color fromString) + " " + fromString);
        }
    }
}
