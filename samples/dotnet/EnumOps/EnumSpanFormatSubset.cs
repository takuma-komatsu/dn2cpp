#nullable disable
using System;

namespace EnumSpanFormatSubset
{
    // A plain (non-[Flags]) byte-underlying enum and a [Flags] one, plus an int-
    // underlying [Flags] and a plain int enum — enough to exercise both underlying
    // widths and both branches of Enum.Format's "G" specifier.
    internal enum ByteColor : byte
    {
        Red = 1,
        Green = 2,
        Blue = 4,
    }

    [Flags]
    internal enum BytePerm : byte
    {
        None = 0,
        Read = 1,
        Write = 2,
        Exec = 4,
    }

    [Flags]
    internal enum IntFlags
    {
        A = 1,
        B = 2,
        C = 4,
        D = 8,
    }

    internal enum IntPlain
    {
        X = 10,
        Y = 20,
    }

    // Two reflection-path enum surfaces the fresh Thrive measure reaches, now cut+
    // routed and diffed EXACTLY vs real .NET (corelib_diff_gate):
    //
    //  * Enum.Parse/TryParse(Type, ReadOnlySpan<char>[, bool][, out object]) — the
    //    span overloads (CommandRegistry.TryParseSpanToType / EnumConverter.ConvertFrom
    //    call them directly). The span is realized into a string and reuses the runtime
    //    parse path, so its result matches the string forms exactly.
    //  * Enum.Format(Type, object, string) — the static formatter EnumConverter.ConvertTo
    //    drives. G/g, D/d, X/x, F/f over both a plain and a [Flags] enum, byte and int
    //    underlying: names, flag decomposition (ascending, ", "-joined), the zero-member
    //    name, unaccounted-bit and non-flags-miss decimal fallbacks, and width-sized
    //    uppercase hex ("x" is uppercase too, as .NET does).
    internal static class Program
    {
        private static void SpanTry(string label, string s)
        {
            object v;
            bool ok = Enum.TryParse(typeof(ByteColor), s.AsSpan(), out v);
            Console.WriteLine(label + ": " + ok + " " + (ok ? (int)(ByteColor)v : -1)
                + " null=" + (v == null));
        }

        private static void SpanTryIc(string label, string s)
        {
            object v;
            bool ok = Enum.TryParse(typeof(ByteColor), s.AsSpan(), true, out v);
            Console.WriteLine(label + ": " + ok + " " + (ok ? (int)(ByteColor)v : -1));
        }

        private static void SpanTryPerm(string label, string s)
        {
            object v;
            bool ok = Enum.TryParse(typeof(BytePerm), s.AsSpan(), out v);
            Console.WriteLine(label + ": " + ok + " " + (ok ? (int)(BytePerm)v : -1));
        }

        internal static void __GateEntry()
        {
            // ---- Span Parse / TryParse ----
            SpanTry("span-name", "Green");
            SpanTry("span-miss-case", "green");        // case-sensitive: fail
            SpanTry("span-miss", "Nope");              // unmatched name: fail + null
            SpanTry("span-num", "4");                  // numeric -> Blue
            SpanTryIc("span-ic", "blue");              // ignoreCase: ok
            SpanTryIc("span-ic-miss", "cyan");
            SpanTryPerm("span-flags", "Read, Exec");   // 1|4 = 5

            // Parse(Type, ReadOnlySpan<char>) and Parse(Type, ReadOnlySpan<char>, bool).
            Console.WriteLine("span-parse: " + (int)(ByteColor)Enum.Parse(typeof(ByteColor), "Blue".AsSpan()));
            Console.WriteLine("span-parse-ic: " + (int)(ByteColor)Enum.Parse(typeof(ByteColor), "red".AsSpan(), true));

            // ---- Enum.Format ----
            // Plain byte enum: G names / G-miss decimal / D / X (2 digits) / x uppercase.
            Console.WriteLine("bc-G: " + Enum.Format(typeof(ByteColor), ByteColor.Green, "G"));
            Console.WriteLine("bc-Gmiss: " + Enum.Format(typeof(ByteColor), (ByteColor)9, "G"));
            Console.WriteLine("bc-D: " + Enum.Format(typeof(ByteColor), ByteColor.Blue, "D"));
            Console.WriteLine("bc-X: " + Enum.Format(typeof(ByteColor), ByteColor.Blue, "X"));
            Console.WriteLine("bc-x: " + Enum.Format(typeof(ByteColor), ByteColor.Blue, "x"));
            Console.WriteLine("bc-X255: " + Enum.Format(typeof(ByteColor), (ByteColor)255, "X"));

            // [Flags] byte enum: G decomposes, zero-member name, F always decomposes,
            // unaccounted bits fall back to decimal, D is the raw underlying.
            Console.WriteLine("bp-Gcombo: " + Enum.Format(typeof(BytePerm), BytePerm.Read | BytePerm.Exec, "G"));
            Console.WriteLine("bp-Gnone: " + Enum.Format(typeof(BytePerm), BytePerm.None, "G"));
            Console.WriteLine("bp-F0: " + Enum.Format(typeof(BytePerm), (BytePerm)0, "F"));
            Console.WriteLine("bp-Fall: " + Enum.Format(typeof(BytePerm), BytePerm.Read | BytePerm.Write | BytePerm.Exec, "F"));
            Console.WriteLine("bp-Fleft: " + Enum.Format(typeof(BytePerm), (BytePerm)13, "F"));   // bit 8 unaccounted
            Console.WriteLine("bp-Gleft: " + Enum.Format(typeof(BytePerm), (BytePerm)13, "G"));
            Console.WriteLine("bp-D: " + Enum.Format(typeof(BytePerm), (BytePerm)3, "D"));

            // [Flags] int enum: G decomposes, X zero-pads to 8 digits, D raw.
            Console.WriteLine("if-Gcombo: " + Enum.Format(typeof(IntFlags), IntFlags.A | IntFlags.C, "G"));
            Console.WriteLine("if-Xall: " + Enum.Format(typeof(IntFlags), IntFlags.A | IntFlags.B | IntFlags.C | IntFlags.D, "X"));
            Console.WriteLine("if-D: " + Enum.Format(typeof(IntFlags), IntFlags.B, "D"));

            // Plain int enum: G names / G-miss decimal / X (8 digits) / D.
            Console.WriteLine("ip-G: " + Enum.Format(typeof(IntPlain), IntPlain.X, "G"));
            Console.WriteLine("ip-Gmiss: " + Enum.Format(typeof(IntPlain), (IntPlain)99, "G"));
            Console.WriteLine("ip-X: " + Enum.Format(typeof(IntPlain), IntPlain.Y, "X"));
            Console.WriteLine("ip-D: " + Enum.Format(typeof(IntPlain), IntPlain.Y, "D"));
        }
    }
}
