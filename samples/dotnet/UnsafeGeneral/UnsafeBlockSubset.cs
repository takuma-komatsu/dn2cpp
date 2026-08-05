using System;
using System.Runtime.CompilerServices;

// Unsafe.CopyBlock / InitBlock and the Unaligned variants over typed buffers;
// the transpiler maps these non-generic block ops to memset/memmove.
namespace UnsafeBlockSubset;

struct Vec2 { public float X; public float Y; }   // 8 bytes

class Program
{
    internal static unsafe void __GateEntry()
    {
        Vec2* zs = stackalloc Vec2[3];
        zs[0] = new Vec2 { X = 1f, Y = 2f };
        Unsafe.InitBlock(zs, 0, (uint)(sizeof(Vec2) * 3));
        Console.WriteLine($"{zs[0].X},{zs[0].Y}");                 // 0,0

        Vec2[] src = { new Vec2 { X = 1.5f, Y = 2.5f }, new Vec2 { X = 3.5f, Y = 4.5f } };
        Vec2[] dst = new Vec2[2];
        Unsafe.CopyBlock(ref Unsafe.As<Vec2, byte>(ref dst[0]),
                         ref Unsafe.As<Vec2, byte>(ref src[0]),
                         (uint)(sizeof(Vec2) * 2));
        Console.WriteLine($"{dst[0].X},{dst[0].Y},{dst[1].X},{dst[1].Y}"); // 1.5,2.5,3.5,4.5

        // Unaligned variants, at a deliberately odd offset.
        byte* u = stackalloc byte[6];
        Unsafe.InitBlockUnaligned(u + 1, 9, 4);
        byte* u2 = stackalloc byte[4];
        Unsafe.CopyBlockUnaligned(u2, u + 1, 4);
        Console.WriteLine($"{u2[0]},{u2[1]},{u2[2]},{u2[3]}");     // 9,9,9,9
    }
}
