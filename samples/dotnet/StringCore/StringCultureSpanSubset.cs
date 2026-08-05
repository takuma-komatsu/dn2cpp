#nullable disable
using System;
using System.Globalization;

namespace StringCultureSpanSubset
{
    // Culture-tier StringComparison over the MemoryExtensions span overloads — the
    // CompareInfo funnels (IsPrefix/IsSuffix/IndexOf/Compare) under the
    // invariant-globalization fold. Inputs stay ASCII, where ICU's invariant collation
    // and invariant-mode comparison agree; mixed-case ORDERING diverges (ICU tertiary
    // weights order 'a' before 'A'), so CompareTo only orders distinct letters.
    internal static class Program
    {
        internal static void Run()
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

            ReadOnlySpan<char> hay = "Hello World".AsSpan();

            // IsPrefix — MemoryExtensions.StartsWith culture arms.
            Console.WriteLine(hay.StartsWith("Hello".AsSpan(), StringComparison.InvariantCulture));
            Console.WriteLine(hay.StartsWith("hello".AsSpan(), StringComparison.InvariantCulture));
            Console.WriteLine(hay.StartsWith("hello".AsSpan(), StringComparison.InvariantCultureIgnoreCase));
            Console.WriteLine(hay.StartsWith("World".AsSpan(), StringComparison.CurrentCulture));
            Console.WriteLine(hay.StartsWith("".AsSpan(), StringComparison.CurrentCulture));

            // IsSuffix — MemoryExtensions.EndsWith culture arms.
            Console.WriteLine(hay.EndsWith("World".AsSpan(), StringComparison.InvariantCulture));
            Console.WriteLine(hay.EndsWith("world".AsSpan(), StringComparison.CurrentCultureIgnoreCase));
            Console.WriteLine(hay.EndsWith("Hello".AsSpan(), StringComparison.InvariantCultureIgnoreCase));

            // IndexOf — MemoryExtensions.IndexOf culture arms.
            Console.WriteLine(hay.IndexOf("World".AsSpan(), StringComparison.InvariantCulture));
            Console.WriteLine(hay.IndexOf("world".AsSpan(), StringComparison.InvariantCulture));
            Console.WriteLine(hay.IndexOf("WORLD".AsSpan(), StringComparison.CurrentCultureIgnoreCase));
            Console.WriteLine(hay.IndexOf("xyz".AsSpan(), StringComparison.CurrentCulture));

            // Compare — MemoryExtensions.Equals / CompareTo culture arms.
            Console.WriteLine(hay.Equals("Hello World".AsSpan(), StringComparison.InvariantCulture));
            Console.WriteLine(hay.Equals("hello world".AsSpan(), StringComparison.InvariantCulture));
            Console.WriteLine(hay.Equals("HELLO WORLD".AsSpan(), StringComparison.InvariantCultureIgnoreCase));
            Console.WriteLine(Math.Sign("apple".AsSpan().CompareTo("banana".AsSpan(), StringComparison.InvariantCulture)));
            Console.WriteLine(Math.Sign("same".AsSpan().CompareTo("same".AsSpan(), StringComparison.CurrentCulture)));
            Console.WriteLine(Math.Sign("zoo".AsSpan().CompareTo("app".AsSpan(), StringComparison.InvariantCultureIgnoreCase)));
        }
    }
}
