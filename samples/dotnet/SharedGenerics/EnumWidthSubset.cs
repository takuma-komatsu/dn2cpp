#nullable enable
// Width split of the canonical enum groups: byte-underlying enums group under the
// byte placeholder, since their packed storage width differs from int, and
// 8-byte-underlying enums are excluded from sharing entirely. Real
// System.Private.CoreLib (-r), run vs .NET.
using System;
using System.Collections.Generic;
namespace EnumWidthSubset;

enum Small : byte { A = 1, B = 2, C = 250 }
enum Tiny : byte { X = 3, Y = 4 }
enum Big : long { P = 1099511627776, Q = -5 }

class Program
{
    internal static void __GateEntry()
    {
        var smalls = new Dictionary<Small, string> { { Small.A, "a" }, { Small.C, "c" } };
        Console.WriteLine("small count=" + smalls.Count + " c=" + smalls[Small.C]);
        Console.WriteLine("small has b=" + smalls.ContainsKey(Small.B));
        foreach (var kv in smalls)
            Console.WriteLine("small kv " + kv.Key + "=" + kv.Value + " raw=" + (byte)kv.Key);

        var tinies = new List<Tiny> { Tiny.X, Tiny.Y, Tiny.X };
        Console.WriteLine("tiny count=" + tinies.Count + " [2]=" + tinies[2]);
        Tiny[] tarr = tinies.ToArray();
        Console.WriteLine("tiny arr type=" + (tarr.GetType() == typeof(Tiny[])));
        Console.WriteLine("tiny arr type byte=" + (tarr.GetType() == typeof(byte[])));

        var bigs = new Dictionary<Big, string> { { Big.P, "p" }, { Big.Q, "q" } };
        Console.WriteLine("big count=" + bigs.Count + " p=" + bigs[Big.P]);
        Console.WriteLine("big q raw=" + (long)Big.Q + " p raw=" + (long)Big.P);
        var bigList = new List<Big> { Big.Q, Big.P };
        Console.WriteLine("big list [0]=" + bigList[0] + " count=" + bigList.Count);
    }
}
