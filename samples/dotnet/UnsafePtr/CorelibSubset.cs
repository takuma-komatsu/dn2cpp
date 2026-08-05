#nullable enable
using System;
using System.Runtime.CompilerServices;

namespace CorelibSubset
{
    // Calls into the REAL System.Private.CoreLib passed with -r: the transpiler
    // resolves the cross-assembly reference, tree-shakes the BCL to the reached
    // methods, and compiles their real IL bodies into the app.
    internal static class Program
    {
        internal static unsafe void __GateEntry()
        {
            int digits = 0;
            int letters = 0;
            int hex = 0;
            string s = "a1b2c3!";
            int i = 0;
            while (i < s.Length)
            {
                char c = s[i];
                // Real corlib leaf bodies. IsAsciiHexDigit reaches HexConverter's
                // 64-bit bit-trick, which needs correct shift/conv width and sign
                // handling to answer 6 rather than a UB-corrupted 7.
                if (char.IsAsciiDigit(c))
                {
                    digits = digits + 1;
                }
                if (char.IsAsciiLetter(c))
                {
                    letters = letters + 1;
                }
                if (char.IsAsciiHexDigit(c))
                {
                    hex = hex + 1;
                }
                i = i + 1;
            }

            Console.WriteLine(digits);
            Console.WriteLine(letters);
            Console.WriteLine(hex);

            // A real InternalCall (no IL body), mapped to a runtime intrinsic;
            // compared against itself to stay deterministic.
            bool sameHash = RuntimeHelpers.GetHashCode(s) == RuntimeHelpers.GetHashCode(s);
            Console.WriteLine(sameHash);

            // Unsafe.SizeOf/Add are [Intrinsic] stubs whose real corlib IL just
            // throws, so they must be emitted inline rather than transpiled.
            int* buf = stackalloc int[4];
            for (int j = 0; j < 4; j++)
            {
                buf[j] = (j + 1) * 10;
            }
            int sum = 0;
            for (int j = 0; j < 4; j++)
            {
                sum = sum + buf[j];
            }
            Console.WriteLine(sum);                  // 100
            Console.WriteLine(Unsafe.SizeOf<int>()); // 4
            ref int third = ref Unsafe.Add(ref buf[0], 2);
            Console.WriteLine(third);                // 30

            // Span<T> over a stackalloc buffer: the ctor, indexer, Slice and
            // Length are real corlib IL. Their bounds checks call ThrowHelper,
            // whose dead-throw closure is intrinsic-mapped to a runtime trap so
            // the exception-construction IL is never pulled in.
            Span<int> span = stackalloc int[5];
            for (int j = 0; j < span.Length; j++)
            {
                span[j] = (j + 1) * (j + 1);         // 1 4 9 16 25
            }
            Console.WriteLine(span.Length);          // 5
            Console.WriteLine(span[2]);              // 9
            Span<int> tail = span.Slice(3);          // {16, 25}
            Console.WriteLine(tail.Length);          // 2
            Console.WriteLine(tail[1]);              // 25

            // The trap must still raise a catchable managed exception.
            bool threw = false;
            try
            {
                int bad = span[10];
                Console.WriteLine(bad);
            }
            catch (IndexOutOfRangeException)
            {
                threw = true;
            }
            Console.WriteLine(threw);                // True
        }
    }
}
