#nullable enable
using System;
namespace StringCreateSubset;


class Program
{internal static void Run()
    {
        // Fill the writable span from a char state.
        string xs = string.Create(5, 'x', static (span, c) =>
        {
            for (int i = 0; i < span.Length; i++)
                span[i] = c;
        });
        Console.WriteLine(xs);

        // State carries data read inside; index reads + writes.
        string label = "AB";
        string mirror = string.Create(4, label, static (sp, st) =>
        {
            sp[0] = st[0];
            sp[1] = st[1];
            sp[2] = st[1];
            sp[3] = st[0];
        });
        Console.WriteLine(mirror);

        // Integer state used to compute each char.
        string digits = string.Create(6, 7, static (sp, start) =>
        {
            for (int i = 0; i < sp.Length; i++)
                sp[i] = (char)('0' + (start + i) % 10);
        });
        Console.WriteLine(digits);
    }
}
