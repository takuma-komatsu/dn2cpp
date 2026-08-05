#nullable enable
using System;

// Field access where the receiver is a by-value struct on the IL stack (a struct
// value, not a managed pointer or heap reference): reading a field of a struct
// returned by value, nested struct-value field chains, sub-word fields read out of
// a struct value (int32-promoted on load), a struct value reached through an `in`
// parameter, and value-copy semantics. This is the struct-value field-access
// surface that the stfld / ldflda struct-value paths extend. CoreLib only; diffed
// exact vs real .NET.
namespace StructValueFieldSubset;

struct Leaf { public int N; public byte Flag; public short S; }
struct Node { public Leaf L; public int Tag; }

class Program
{
    static Node Make(int n) =>
        new Node { L = new Leaf { N = n, Flag = (byte)(n & 0xFF), S = (short)(-n) }, Tag = n * 2 };

    static int SumIn(in Node nd) => nd.L.N + nd.Tag;

    internal static void __GateEntry()
    {
        // field of a struct returned by value (ldfld on a struct value).
        Console.WriteLine(Make(5).Tag);                 // 10
        // nested struct-value field chain (ldfld; ldfld).
        Console.WriteLine(Make(7).L.N);                 // 7
        // sub-word fields read out of a struct value (int32-promote on load).
        Console.WriteLine(Make(300).L.Flag);            // 300 & 0xFF = 44
        Console.WriteLine(Make(9).L.S);                 // -9
        // struct-value fields used in arithmetic.
        Console.WriteLine(Make(4).L.N + Make(6).Tag);   // 4 + 12 = 16
        // in-parameter (readonly managed pointer) field chain.
        Console.WriteLine(SumIn(Make(11)));             // 11 + 22 = 33
        // value-copy semantics: mutating the copy leaves the original intact.
        Node a = Make(2);
        Node b = a;
        b.Tag = 99;
        Console.WriteLine($"{a.Tag},{b.Tag}");          // 4,99
    }
}
