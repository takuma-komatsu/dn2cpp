using System;

// SUBJECT: variable-size stackalloc — a runtime length operand to localloc,
// sized from a static field rather than a compile-time constant.
namespace StackallocVarSubset;

class Program
{
    static int N = 5;

    internal static void __GateEntry()
    {
        Span<int> s = stackalloc int[N];
        for (int i = 0; i < s.Length; i++) s[i] = i;
        int sum = 0;
        foreach (int v in s) sum += v;
        Console.WriteLine(s.Length);     // 5
        Console.WriteLine(sum);          // 10
    }
}
