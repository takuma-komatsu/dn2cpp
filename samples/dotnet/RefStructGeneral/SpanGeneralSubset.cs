using System;

// SUBJECT: Span<T>/ReadOnlySpan<T> through their real CoreLib IL — indexer
// (get_Item -> ref T), Length, Slice, over stackalloc / a heap array / a string
// literal. The general ref-field model, not the bulk-method intrinsics.
namespace SpanGeneralSubset;

class Program
{
    static int Sum(Span<int> s)
    {
        int acc = 0;
        for (int i = 0; i < s.Length; i++) acc += s[i];
        s[0] = 100;
        return acc;
    }

    internal static void __GateEntry()
    {
        Span<int> s = stackalloc int[3];
        s[0] = 1; s[1] = 2; s[2] = 3;
        Span<int> sl = s.Slice(1, 2);
        Console.WriteLine(Sum(s));       // 6
        Console.WriteLine(sl.Length);    // 2
        Console.WriteLine(sl[0]);        // 2
        Console.WriteLine(s[0]);         // 100

        int[] a = { 1, 2, 3, 4 };
        Span<int> hs = a;
        hs[1] = 20;
        Console.WriteLine(a[1]);         // 20

        ReadOnlySpan<char> r = "hello";
        int n = 0;
        for (int i = 0; i < r.Length; i++) if (r[i] == 'l') n++;
        Console.WriteLine(n);            // 2
    }
}
