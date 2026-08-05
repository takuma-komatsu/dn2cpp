#nullable disable
using System;

// DefaultInterpolatedStringHandler constructed via a value-type `newobj` instead of the
// usual `ldloca + call .ctor` — the shape Roslyn emits for an interpolated string inside
// a `try` block. The handler is the intrinsic Dn2CppISB buffer, so the newobj pushes
// dn2cpp_isb_new(...) and the appends run on its address as in the ldloca path. Every
// interpolation here sits in a try block to force that shape.
namespace InterpolationNewobjSubset;

internal enum Color { Red, Green, Blue }

internal static class Program
{
    static string TryInt(int a) { try { return $"int={a}"; } catch { return "err"; } }
    static string TryStr(string s) { try { return $"[{s}]"; } catch { return "err"; } }
    static string TryFmt(double d) { try { return $"d={d:F2}"; } catch { return "err"; } }
    static string TryAlign(int a) { try { return $"|{a,5}|{a,-5}|"; } catch { return "err"; } }
    static string TryMulti(int a, string b, int c) { try { return $"{a}/{b}/{c}"; } catch { return "err"; } }
    static string TryEnum(Color c) { try { return $"c={c}"; } catch { return "err"; } }
    static string TryBool(bool b) { try { return $"b={b}"; } catch { return "err"; } }
    static string TryHex(int a) { try { return $"x={a:X4}"; } catch { return "err"; } }

    internal static int __GateEntry()
    {
        Console.WriteLine(TryInt(42));
        Console.WriteLine(TryStr("héllo"));
        Console.WriteLine(TryFmt(3.14159));
        Console.WriteLine(TryAlign(7));
        Console.WriteLine(TryMulti(1, "mid", 9));
        Console.WriteLine(TryEnum(Color.Green));
        Console.WriteLine(TryBool(true));
        Console.WriteLine(TryHex(255));
        return 0;
    }
}
