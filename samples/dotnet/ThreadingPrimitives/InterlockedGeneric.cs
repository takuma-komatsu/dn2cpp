using System;
using System.Threading;

// Generic Interlocked.Exchange<T>/CompareExchange<T>, forced into the MethodSpec form
// through helper wrappers so overload resolution cannot fall back to the non-generic
// surface. T must dispatch at its DECLARED storage width — the packed-struct and array
// probes fail visibly if a wider atomic clobbers the neighboring bytes.
namespace InterlockedGeneric;

internal enum IntEnum { Zero = 0, One = 1, Two = 2 }
internal enum ByteEnum : byte { A = 10, B = 20 }
internal enum LongEnum : long { Small = 1, Large = 123456789 }

internal static class Program
{
    private struct BoolPack
    {
        public byte Pre;
        public bool Flag;   // 1-byte storage between two live neighbors
        public byte Post;
    }

    private struct LongEnumPack
    {
        public LongEnum Value;  // real 8-byte storage width in a struct field
        public long Guard;
    }

    private static bool s_staticFlag;

    private static T Xchg<T>(ref T l, T v) => Interlocked.Exchange(ref l, v);
    private static T Cas<T>(ref T l, T v, T c) => Interlocked.CompareExchange(ref l, v, c);

    internal static void __GateEntry()
    {
        // ---- bool local ----
        bool b = false;
        Console.WriteLine(Xchg(ref b, true));                       // False (old)
        Console.WriteLine(Cas(ref b, false, true));                 // True (hit)
        Console.WriteLine(b);                                       // False

        // ---- bool packed between byte fields: neighbors must survive ----
        var bp = new BoolPack { Pre = 0x11, Flag = false, Post = 0x5A };
        Console.WriteLine(Xchg(ref bp.Flag, true));                 // False
        Console.WriteLine(bp.Flag);                                 // True
        Console.WriteLine(Cas(ref bp.Flag, false, true));           // True (hit)
        Console.WriteLine(bp.Flag);                                 // False
        Console.WriteLine(bp.Pre);                                  // 17
        Console.WriteLine(bp.Post);                                 // 90

        // ---- bool[] element ----
        bool[] flags = new bool[3];
        Console.WriteLine(Xchg(ref flags[1], true));                // False
        Console.WriteLine(flags[0]);                                // False
        Console.WriteLine(flags[1]);                                // True
        Console.WriteLine(flags[2]);                                // False

        // ---- static bool ----
        Console.WriteLine(Xchg(ref s_staticFlag, true));            // False
        Console.WriteLine(s_staticFlag);                            // True

        // ---- char ----
        char c = 'a';
        Console.WriteLine(Xchg(ref c, 'Z'));                        // a
        Console.WriteLine(Cas(ref c, 'q', 'Z'));                    // Z (hit)
        Console.WriteLine(c);                                       // q

        // ---- sub-word integers through the generic form ----
        byte by = 250;
        Console.WriteLine(Xchg(ref by, (byte)5));                   // 250
        Console.WriteLine(by);                                      // 5
        sbyte sb = -100;
        Console.WriteLine(Cas(ref sb, (sbyte)100, (sbyte)-100));    // -100 (hit)
        Console.WriteLine(sb);                                      // 100
        short sh = -30000;
        Console.WriteLine(Xchg(ref sh, (short)30000));              // -30000
        Console.WriteLine(sh);                                      // 30000
        ushort ush = 60000;
        Console.WriteLine(Cas(ref ush, (ushort)1, (ushort)60000));  // 60000 (hit)
        Console.WriteLine(ush);                                     // 1

        // ---- enums: int-, byte- and long-underlying (printed numerically) ----
        IntEnum ie = IntEnum.One;
        Console.WriteLine((int)Xchg(ref ie, IntEnum.Two));          // 1
        Console.WriteLine((int)Cas(ref ie, IntEnum.Zero, IntEnum.Two)); // 2 (hit)
        Console.WriteLine((int)ie);                                 // 0
        ByteEnum be = ByteEnum.A;
        Console.WriteLine((byte)Xchg(ref be, ByteEnum.B));          // 10
        Console.WriteLine((byte)be);                                // 20
        // A long-underlying enum dispatches at the full 8-byte width, in a struct field
        // (real int64 storage). Member values stay within int32 range: plain enum loads go
        // through the transpiler's enum-as-int32 stack model, which truncates wider ones.
        var ep = new LongEnumPack { Value = LongEnum.Large, Guard = -7 };
        Console.WriteLine((long)Xchg(ref ep.Value, LongEnum.Small));    // 123456789
        Console.WriteLine((long)Cas(ref ep.Value, LongEnum.Large, LongEnum.Small)); // 1 (hit)
        Console.WriteLine((long)ep.Value);                          // 123456789
        Console.WriteLine(ep.Guard);                                // -7

        // ---- float / double through the generic form ----
        float f = 1.25f;
        Console.WriteLine(Xchg(ref f, -2.5f));                      // 1.25
        Console.WriteLine(Cas(ref f, 10f, -2.5f));                  // -2.5 (hit)
        Console.WriteLine(f);                                       // 10
        double d = 6.25;
        Console.WriteLine(Xchg(ref d, -12.5));                      // 6.25
        Console.WriteLine(d);                                       // -12.5

        // ---- string (reference T) ----
        string s = "first";
        Console.WriteLine(Xchg(ref s, "second"));                   // first
        Console.WriteLine(Cas(ref s, "third", "second"));           // second (hit)
        Console.WriteLine(s);                                       // third
    }
}
