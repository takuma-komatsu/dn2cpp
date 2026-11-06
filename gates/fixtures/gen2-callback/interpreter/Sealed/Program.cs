using System;

namespace Gen2CallbackSealedPatch;

internal sealed class Derived
{
    public Derived() { }
}

internal static class Program
{
    private static void Main()
    {
        _ = new Derived();
        Console.WriteLine("gen2-sealed-unexpected-execution");
    }
}
