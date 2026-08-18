using System;
using System.Threading;

#pragma warning disable SYSLIB0054

namespace LegacyThreadVolatile;

internal static class Program
{
    private static int s_i4;
    private static long s_i8;
    private static object? s_ref;
    private static float s_r4;
    private static double s_r8;

    internal static void __GateEntry()
    {
        Console.WriteLine("== legacy thread volatile ==");

        Thread.VolatileWrite(ref s_i4, -7);
        Console.WriteLine(Thread.VolatileRead(ref s_i4));
        Thread.VolatileWrite(ref s_i8, 9_000_000_001L);
        Console.WriteLine(Thread.VolatileRead(ref s_i8));

        object value = new object();
        Thread.VolatileWrite(ref s_ref, value);
        Console.WriteLine(ReferenceEquals(Thread.VolatileRead(ref s_ref), value));

        Thread.VolatileWrite(ref s_r4, 1.25f);
        Console.WriteLine(Thread.VolatileRead(ref s_r4));
        Thread.VolatileWrite(ref s_r8, -2.5);
        Console.WriteLine(Thread.VolatileRead(ref s_r8));
    }
}
