#nullable enable
// The StringBuilder interpolated-string handler enum hole. When `sb` is a
// StringBuilder, the C# compiler lowers
// `sb.Append($"...{value}...")` / `sb.AppendLine($"...")` through the nested
// System.Text.StringBuilder.AppendInterpolatedStringHandler (distinct from the
// top-level DefaultInterpolatedStringHandler that other interpolations use). Its
// real generic AppendFormatted<T> reaches Enum.TryFormatUnconstrained ->
// TryFormatPrimitiveDefault / TryFormatFlagNames / GetEnumInfo (the
// RuntimeType/EnumInfo InternalCall cache) / String.TryCopyTo.
//
// dn2cpp models this handler as the underlying StringBuilder pointer and lowers
// AppendFormatted<T> with the same enum/primitive/value formatting as the
// DefaultInterpolatedStringHandler hole (shared FormatInterpolationHole), then cuts
// the real generic body from reachability so the whole Enum.TryFormat cascade
// collapses. This sample exercises the same handler escape shape dn2cpp's own
// CppEmitter.Emit triggers: a plain enum, a [Flags] enum, numeric specifiers
// (D / X), undefined values, mixed holes (int/string/enum), AppendLine, and
// alignment — diffed exact vs real .NET. CoreLib only (no Linq shim).
using System;
using System.Text;
namespace EnumInterpSubset;


enum Color { Red, Green = 2, Blue = 4 }

[Flags]
enum Perm { None = 0, Read = 1, Write = 2, Exec = 4, All = 7 }

class Program
{internal static void __GateEntry()
    {
        // --- plain enum holes through StringBuilder.Append($"...") ---
        var sb = new StringBuilder();
        Color c = Color.Green;
        sb.Append($"plain={c} ");        // member name (default / "G")
        sb.Append($"plainD={c:D} ");     // numeric specifier -> underlying value
        sb.Append($"plainX={c:X} ");     // hex specifier -> padded hex
        Color undef = (Color)99;
        sb.Append($"undef={undef} ");    // undefined value -> decimal
        Console.WriteLine(sb.ToString());

        // --- [Flags] enum holes ---
        var sbf = new StringBuilder();
        Perm p = Perm.Read | Perm.Write;
        sbf.Append($"flags={p} ");        // "Read, Write"
        sbf.Append($"all={Perm.All} ");   // a named combined value -> "All"
        sbf.Append($"none={Perm.None} "); // the zero member -> "None"
        sbf.Append($"flagsD={p:D} ");     // numeric -> underlying value
        Perm uf = (Perm)64;
        sbf.Append($"undefFlags={uf} ");  // undefined flags -> decimal
        Console.WriteLine(sbf.ToString());

        // --- mixed holes (int / string / enum) + literals in one interpolation ---
        var sbm = new StringBuilder();
        int n = 42;
        string name = "abc";
        sbm.Append($"n={n} name={name} color={Color.Blue}");
        Console.WriteLine(sbm.ToString());

        // --- AppendLine with an interpolation, then a literal Append ---
        var sbl = new StringBuilder();
        sbl.AppendLine($"line color={Color.Red}");
        sbl.Append("tail");
        Console.WriteLine(sbl.ToString());

        // --- alignment (`,N`) on an enum hole ---
        var sba = new StringBuilder();
        sba.Append($"[{Color.Green,10}]");
        sba.Append($"[{Color.Blue,-8}]");
        Console.WriteLine(sba.ToString());

        // --- chained fluent Append over a handler-bearing interpolation ---
        var sbc = new StringBuilder();
        sbc.Append($"a={Color.Red}").Append('|').Append($"b={Perm.Exec}");
        Console.WriteLine(sbc.ToString());
    }
}
