using System;

namespace HotUpdatePatch;

public static class InterpretedConcatSubset
{
    public static string Run()
    {
        Console.WriteLine("== interpreted Concat arrays ==");
        try
        {
            string[] parts = null!;
            Console.WriteLine("null: [" + string.Concat(parts) + "]");
        }
        catch (ArgumentNullException exception)
        {
            Console.WriteLine("null: " + exception.GetType().Name);
        }
        Console.WriteLine("empty: [" + string.Concat(new string[0]) + "]");
        Console.WriteLine("null elements: [" + string.Concat(new string[] { null!, "a", null!, "b", null! }) + "]");
        Console.WriteLine("values: " + string.Concat(new[] { "a", "b", "c", "d", "e" }));
        Console.WriteLine("null scalar: [" + string.Concat((string?)null, "x") + "]");
        return "recovery: ok";
    }
}
