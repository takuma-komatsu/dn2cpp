using System;

// SUBJECT: stackalloc of a non-primitive struct, with element field writes via
// ldflda on a span element (s[i].X = ...).
namespace StackallocStructSubset;

struct Pt { public int X; public int Y; }

class Program
{
    internal static void __GateEntry()
    {
        Span<Pt> s = stackalloc Pt[3];
        for (int i = 0; i < s.Length; i++) { s[i].X = i; s[i].Y = i * 10; }
        int sum = 0;
        for (int i = 0; i < s.Length; i++) sum += s[i].X + s[i].Y;
        Console.WriteLine(sum);          // 33
    }
}
