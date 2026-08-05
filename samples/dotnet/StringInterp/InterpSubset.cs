#nullable enable
using System;
namespace InterpSubset;


class Program
{
    internal static void __GateEntry()
    {
        // 1. Literal-only interpolation (no holes).
        string lit = $"hello world";
        Console.WriteLine(lit);

        // 2. Single int hole.
        int x = 42;
        string intHole = $"x={x}";
        Console.WriteLine(intHole);

        // 3. Single string hole.
        string name = "Alice";
        string strHole = $"name={name}";
        Console.WriteLine(strHole);

        // 4. Multi-hole mix (int + string).
        int count = 3;
        string item = "apples";
        string mix = $"I have {count} {item} today";
        Console.WriteLine(mix);

        // 5. Alignment: right-align (positive width) and left-align (negative).
        Console.WriteLine($"[{x,5}]");        // "[ 42]"
        Console.WriteLine($"[{name,-8}]");    // "[Alice ]"

        // 6. Integer format specifiers: hex and min-width decimal.
        Console.WriteLine($"{255:X}");        // "FF"
        Console.WriteLine($"{255:x4}");       // "00ff"
        Console.WriteLine($"{42:D5}");        // "00042"
        Console.WriteLine($"{-42:D3}");       // "-042"

        // 7. Double format specifiers: fixed-point and grouped.
        Console.WriteLine($"{3.14159:F2}");       // "3.14"
        Console.WriteLine($"{1234567.891:N2}");   // "1,234,567.89"

        // 8. char hole (must print the character, not its code unit).
        char c = 'Z';
        Console.WriteLine($"ch={c}");         // "ch=Z"

        // 9. long hole with a hex format.
        long big = 255L;
        Console.WriteLine($"{big:X}");        // "FF"

        // 10. Alignment + format combined.
        Console.WriteLine($"[{42,6:D4}]");    // "[ 0042]"

        // 11. Unsigned integer holes — must not be read as a negative int.
        uint u = 3000000000;                  // > int.MaxValue
        Console.WriteLine($"{u}");            // "3000000000"
        Console.WriteLine($"{u:X}");          // "B2D05E00"
        ulong ul = 18000000000000000000;      // > long.MaxValue
        Console.WriteLine($"{ul}");           // "18000000000000000000"

        // 12. Narrow integer holes — hex width follows the declared type.
        byte by = 255;
        Console.WriteLine($"{by:X}");         // "FF"
        short sh = -1;
        Console.WriteLine($"{sh:X}");         // "FFFF"
        sbyte sb = -2;
        Console.WriteLine($"{sb}={sb:X}");    // "-2=FE"
        ushort us = 65535;
        Console.WriteLine($"{us}/{us:X}");    // "65535/FFFF"

        // 13. Enum holes — member name by default, underlying value for ":D",
        // and the numeric fallback for an undefined value.
        Color col = Color.Green;
        Console.WriteLine($"{col}");          // "Green"
        Console.WriteLine($"{col:D}");        // "2"
        Console.WriteLine($"{Color.Blue}");   // "Blue"
        Color undef = (Color)99;
        Console.WriteLine($"{undef}");        // "99"
        Console.WriteLine($"[{Color.Red,-6}]"); // "[Red ]"

        // 14. [Flags] enum holes — combine set bits into a name list.
        Perm p = Perm.Read | Perm.Write;
        Console.WriteLine($"{p}");            // "Read, Write"
        Console.WriteLine($"{Perm.Execute}"); // "Execute"
        Console.WriteLine($"{(Perm)0}");      // "None"
        Console.WriteLine($"{Perm.Read | Perm.Write | Perm.Execute}"); // "Read, Write, Execute"
        Console.WriteLine($"{(Perm)9}");      // "9" (bit 8 undefined -> decimal)
        Console.WriteLine($"{p:D}");          // "3"

        // 15. Enum hex (":X") zero-pads to the underlying type's width, unlike a plain
        // integer hex.
        Console.WriteLine($"{col:X}");        // "00000002"
        Console.WriteLine($"{Color.Blue:x}"); // "00000005"
        // byte-backed enum -> 2 digits; an undefined value still pads to width.
        Console.WriteLine($"{Small.B:X}");    // "0F"
        Console.WriteLine($"{(Small)0xAB:X}");// "AB"
        // [Flags] hex follows the same width rule.
        Console.WriteLine($"{p:X}");          // "00000003"

        // 16. Exponential ("E"/"e") — mantissa digits + a >=3-digit exponent.
        Console.WriteLine($"{1052.0329112756:E}");  // "1.052033E+003"
        Console.WriteLine($"{1052.0329112756:E2}"); // "1.05E+003"
        Console.WriteLine($"{0.000123:e3}");        // "1.230e-004"
        Console.WriteLine($"{42:E2}");              // "4.20E+001" (int hole)

        // The remaining specifiers (P/C below, G/custom further down) are culture-bound
        // in .NET; the transpiler targets InvariantCulture, so the expected values are
        // .NET's InvariantCulture output.

        // 17. Percent ("P"/"p") — value*100, grouped, invariant " %" suffix.
        Console.WriteLine($"{0.1234:P}");           // "12.34 %"
        Console.WriteLine($"{12.5:P1}");            // "1,250.0 %"
        Console.WriteLine($"{1:P0}");               // "100 %" (int hole)

        // 18. Currency ("C"/"c") — invariant "¤n", negatives wrapped "(¤n)".
        Console.WriteLine($"{1234.5:C}");           // "¤1,234.50"
        Console.WriteLine($"{-5:C}");               // "(¤5.00)" (int hole)
        Console.WriteLine($"{1234.5:C0}");          // "¤1,234"

        // 19. Custom numeric patterns: "0" forces a digit, "#" shows a significant
        // one, "." splits sections, "," groups thousands.
        Console.WriteLine($"{7:000}");              // "007"
        Console.WriteLine($"{7.5:00.0}");           // "07.5"
        Console.WriteLine($"{1234567:#,##0}");      // "1,234,567" (int hole)
        Console.WriteLine($"{0:#}");                // "" (no significant digit)
        Console.WriteLine($"{5.567:#.##}");         // "5.57"
        Console.WriteLine($"{1234.5:#,##0.00}");    // "1,234.50"
        Console.WriteLine($"{-1234.5:#,##0.00}");   // "-1,234.50"

        // 20. General ("G"/"g") — significant digits, shortest round-trip when no
        // precision, switching to scientific by .NET's exponent rule.
        Console.WriteLine($"{1234.5:G}");           // "1234.5"
        Console.WriteLine($"{1234.5:G4}");          // "1234"
        Console.WriteLine($"{1234.5:G2}");          // "1.2E+03"
        Console.WriteLine($"{0.00001234:G}");       // "1.234E-05"
        Console.WriteLine($"{0.00001234:g}");       // "1.234e-05" (lowercase)
        Console.WriteLine($"{100000.0:G}");         // "100000"
        Console.WriteLine($"{1e20:G}");             // "1E+20"
        Console.WriteLine($"{3.14159:G3}");         // "3.14"
        Console.WriteLine($"{42:G}");               // "42" (int hole)
    }
}

enum Color { Red, Green = 2, Blue = 5 }

[Flags]
enum Perm { None = 0, Read = 1, Write = 2, Execute = 4 }

enum Small : byte { A = 1, B = 0x0F }
