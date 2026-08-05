#nullable enable
// `typeof(T) ==/!= typeof(X)` chains feeding a transpile-time branch fold
// (BranchLiveness' strict window over Compilation.TypeEqualityVerdict), so a
// typeof-chain dispatcher reaches only its live arm per instantiation instead of
// the cross product of every arm's generic callees. The fold only prunes DEAD
// arms: behaviour must stay exactly real .NET's, and a Debug build — where every
// comparison routes through a local and nothing folds — must print the same text.
// Real System.Private.CoreLib (-r), run vs .NET; the gate additionally greps
// (Release only) that no off-diagonal Tag<int,*> body is emitted.
using System;
using System.Collections.Generic;
namespace TypeofFoldSubset;

enum Fruit : byte { Apple, Pear }

class Program
{
    // The cross-product probe: with the fold, Dispatch<T> reaches Tag<T,TM> only
    // on its diagonal — value, reference, enum, array and nested-generic T.
    static string Dispatch<T>()
    {
        if (typeof(T) == typeof(int)) return Tag<T, int>();
        if (typeof(T) == typeof(string)) return Tag<T, string>();
        if (typeof(T) == typeof(Fruit)) return Tag<T, Fruit>();
        if (typeof(T) == typeof(int[])) return Tag<T, int[]>();
        if (typeof(T) == typeof(List<int>)) return Tag<T, List<int>>();
        return "none:" + typeof(T).Name;
    }

    static string Tag<T, TM>() => typeof(T).Name + "=" + typeof(TM).Name;

    // A generic LOCAL function, hoisted by Roslyn to arity 2 and called from
    // every arm at a different TFrom — the direct product the fold exists to kill.
    static string Conv<T>(object v)
    {
        if (typeof(T) == typeof(int)) return As((int)v);
        if (typeof(T) == typeof(double)) return As((double)v);
        if (typeof(T) == typeof(string)) return As((string)v);
        if (typeof(T) == typeof(Fruit)) return As((Fruit)v);
        return "raw:" + v;

        string As<TFrom>(TFrom x) => typeof(T).Name + "<-" + typeof(TFrom).Name + ":" + x!.ToString();
    }

    // op_Inequality folds too (verdict negated).
    static string NotInt<T>() => typeof(T) != typeof(int) ? "not-int:" + typeof(T).Name : "int";

    // The strict window deliberately does not see this: the comparison stays a
    // runtime dn2cpp_type_equals and must still be exact.
    static string ViaLocal<T>()
    {
        bool eq = typeof(T) == typeof(string);
        return "local:" + eq;
    }

    internal static void __GateEntry()
    {
        Console.WriteLine("d int=" + Dispatch<int>());
        Console.WriteLine("d str=" + Dispatch<string>());
        Console.WriteLine("d fruit=" + Dispatch<Fruit>());
        Console.WriteLine("d arr=" + Dispatch<int[]>());
        Console.WriteLine("d list=" + Dispatch<List<int>>());
        Console.WriteLine("d none=" + Dispatch<double>());
        Console.WriteLine("c int=" + Conv<int>(41));
        Console.WriteLine("c dbl=" + Conv<double>(1.5));
        Console.WriteLine("c str=" + Conv<string>("hi"));
        Console.WriteLine("c fruit=" + Conv<Fruit>(Fruit.Pear));
        Console.WriteLine("c raw=" + Conv<byte>((byte)7));
        Console.WriteLine("n int=" + NotInt<int>());
        Console.WriteLine("n str=" + NotInt<string>());
        Console.WriteLine("l str=" + ViaLocal<string>());
        Console.WriteLine("l int=" + ViaLocal<int>());
    }
}
