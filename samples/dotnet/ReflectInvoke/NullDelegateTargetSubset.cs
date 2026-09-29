#nullable disable
using System;
using System.Runtime.CompilerServices;

namespace NullDelegateTargetSubset;

internal sealed class Receiver
{
    internal int Value() => 7;
}

internal static class Program
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Receiver NullReceiver() => null;

    internal static void Run()
    {
        Func<int> valid = new Receiver().Value;
        Console.WriteLine("null-delegate-real=" + valid());
        try
        {
            Func<int> invalid = NullReceiver().Value;
            Console.WriteLine("null-delegate-bound=" + invalid());
        }
        catch (Exception ex)
        {
            Console.WriteLine("null-delegate-bound=" + ex.GetType().Name);
        }
    }
}
