using System;
using System.Threading;

// Non-generic float/double Interlocked.Exchange/CompareExchange. The CAS compares the BIT
// PATTERN, not IEEE equality: a -0.0 comparand must not match a +0.0 location, and a
// bit-identical NaN comparand must match and swap.
namespace InterlockedFloatDouble;

internal static class Program
{
    private static float s_f = 1.5f;
    private static double s_d = 2.5;

    internal static void __GateEntry()
    {
        // ---- float Exchange / CAS hit / CAS miss (returns the original) ----
        Console.WriteLine(Interlocked.Exchange(ref s_f, 3.25f));                 // 1.5
        Console.WriteLine(Interlocked.CompareExchange(ref s_f, -4.75f, 3.25f));  // 3.25 (hit)
        Console.WriteLine(Interlocked.CompareExchange(ref s_f, 9f, 3.25f));      // -4.75 (miss)
        Console.WriteLine(s_f);                                                  // -4.75

        // ---- double Exchange / CAS hit / CAS miss ----
        Console.WriteLine(Interlocked.Exchange(ref s_d, 6.125));                 // 2.5
        Console.WriteLine(Interlocked.CompareExchange(ref s_d, -8.5, 6.125));    // 6.125 (hit)
        Console.WriteLine(Interlocked.CompareExchange(ref s_d, 11.0, 6.125));    // -8.5 (miss)
        Console.WriteLine(s_d);                                                  // -8.5

        // ---- bitwise comparison: -0.0 comparand vs +0.0 location -> NO swap ----
        double dz = 0.0;
        double dzOld = Interlocked.CompareExchange(ref dz, 42.0, -0.0);
        Console.WriteLine(dzOld);                    // 0
        Console.WriteLine(dz);                       // 0 (unchanged: sign bits differ)
        Console.WriteLine(1.0 / dz > 0);             // True (still +0.0, not -0.0 or 42)
        float fz = 0.0f;
        float fzOld = Interlocked.CompareExchange(ref fz, 42.0f, -0.0f);
        Console.WriteLine(fzOld);                    // 0
        Console.WriteLine(1.0f / fz > 0);            // True (unchanged +0.0)

        // ---- and the mirror: a -0.0 location matches a -0.0 comparand ----
        double dn = -0.0;
        Console.WriteLine(Interlocked.CompareExchange(ref dn, 7.5, -0.0) == 0.0); // True (old is -0.0)
        Console.WriteLine(dn);                       // 7.5 (swapped: bit-identical)

        // ---- bitwise comparison: a bit-identical NaN comparand DOES swap ----
        double dNan = double.NaN;
        double dNanOld = Interlocked.CompareExchange(ref dNan, 12.5, double.NaN);
        Console.WriteLine(double.IsNaN(dNanOld));    // True (original was NaN)
        Console.WriteLine(dNan);                     // 12.5 (swapped though NaN != NaN)
        float fNan = float.NaN;
        float fNanOld = Interlocked.CompareExchange(ref fNan, 0.5f, float.NaN);
        Console.WriteLine(float.IsNaN(fNanOld));     // True
        Console.WriteLine(fNan);                     // 0.5

        // ---- Exchange moves NaN in and out losslessly ----
        double dx = 3.5;
        Console.WriteLine(Interlocked.Exchange(ref dx, double.NaN));             // 3.5
        Console.WriteLine(double.IsNaN(Interlocked.Exchange(ref dx, 1.25)));     // True
        Console.WriteLine(dx);                                                   // 1.25
    }
}
