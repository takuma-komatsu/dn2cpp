using System;

namespace MathGenericClampBounds;

internal static class Program
{
    internal static void Run()
    {
        Console.WriteLine("== generic clamp bounds ==");
        try { _ = Int128.Clamp((Int128)1, (Int128)5, (Int128)2); Console.WriteLine("unexpected"); }
        catch (ArgumentException ex) { Console.WriteLine("Int128=" + ex.Message); }
        try { Console.WriteLine(Int128.Clamp((Int128)5, (Int128)2, (Int128)10) == (Int128)5); }
        catch (ArgumentException ex) { Console.WriteLine("valid=" + ex.Message); }
    }
}
