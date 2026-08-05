#nullable disable
using System;

namespace EnumTryParseTypeSubset
{
    internal enum Color : byte
    {
        Red = 1,
        Green = 2,
        Blue = 4,
    }

    [Flags]
    internal enum Perm
    {
        None = 0,
        Read = 1,
        Write = 2,
        Exec = 4,
    }

    // Non-generic, runtime-Type Enum.TryParse/Parse(Type, …) — the reflection path
    // (Thrive's CommandRegistry uses the non-generic TryParse(Type, …)). The enum
    // type arrives at run time, so the value is parsed off the per-enum runtime
    // table (dn2cpp_enum_try_parse_type / _parse_type), the Type-driven complement
    // of the generic Enum.*<T> inline lowering. Diffed EXACTLY vs real .NET:
    // case-sensitive and ignoreCase name parse, numeric parse (defined + undefined
    // values), [Flags] comma-lists, and the failure contract — TryParse returns
    // false with a null result, Parse throws ArgumentException. The result of a
    // successful parse is a BOXED enum value, unboxed here to its int payload.
    //
    // The ReadOnlySpan<char> overloads of Parse/TryParse — and Enum.Format(Type, …) —
    // are exercised in the sibling EnumSpanFormatSubset section; this one stays on the
    // string forms.
    internal static class Program
    {
        private static void TryColor(string label, string s)
        {
            object v;
            bool ok = Enum.TryParse(typeof(Color), s, out v);
            Console.WriteLine(label + ": " + ok + " " + (ok ? (int)(Color)v : -1)
                + " null=" + (v == null));
        }

        private static void TryColorIc(string label, string s)
        {
            object v;
            bool ok = Enum.TryParse(typeof(Color), s, true, out v);
            Console.WriteLine(label + ": " + ok + " " + (ok ? (int)(Color)v : -1)
                + " null=" + (v == null));
        }

        private static void TryPerm(string label, string s)
        {
            object v;
            bool ok = Enum.TryParse(typeof(Perm), s, out v);
            Console.WriteLine(label + ": " + ok + " " + (ok ? (int)(Perm)v : -1));
        }

        internal static void __GateEntry()
        {
            // TryParse(Type, string, out object): name parse, case-sensitive.
            TryColor("name", "Green");
            TryColor("name-miss-case", "green");   // case-sensitive: fail
            TryColor("miss", "Nope");              // unmatched name: fail + null
            TryColor("null", null);                // null value: fail + null
            TryColor("empty", "");                 // empty: fail + null
            TryColor("ws", "   ");                 // whitespace-only: fail

            // TryParse(Type, string, bool ignoreCase, out object).
            TryColorIc("ic-lower", "green");       // ignoreCase: ok
            TryColorIc("ic-upper", "BLUE");
            TryColorIc("ic-miss", "cyan");         // still a miss

            // Numeric form: a defined value round-trips to its member, an undefined
            // one is returned as-is (both boxed as the enum type).
            TryColor("num-defined", "2");          // -> Green
            TryColor("num-undef", "8");            // undefined value, still parses
            TryColor("num-signed", "+4");          // -> Blue

            // Comma-separated OR (applies regardless of [Flags]).
            TryColor("or", "Red,Blue");            // 1|4 = 5
            TryColor("or-ws", " Red , Green ");    // trimmed tokens -> 1|2 = 3

            // [Flags] enum: named combos and numeric.
            TryPerm("flags-one", "Read");
            TryPerm("flags-combo", "Read, Write, Exec");   // 7
            TryPerm("flags-none", "None");                 // 0
            TryPerm("flags-num", "6");                     // Write|Exec = 6
            TryPerm("flags-miss", "Delete");               // fail

            // Non-generic Parse(Type, string[, ignoreCase]) -> boxed enum, and its
            // ArgumentException failure contract (only the type name is asserted).
            Color b = (Color)Enum.Parse(typeof(Color), "Blue");
            Console.WriteLine("parse: " + (int)b);
            Color r = (Color)Enum.Parse(typeof(Color), "red", true);
            Console.WriteLine("parse-ic: " + (int)r);
            try
            {
                Color bad = (Color)Enum.Parse(typeof(Color), "Nope");
                Console.WriteLine("parse-bad: " + (int)bad);
            }
            catch (ArgumentException e)
            {
                Console.WriteLine("parse-bad: " + e.GetType().Name);
            }
        }
    }
}
