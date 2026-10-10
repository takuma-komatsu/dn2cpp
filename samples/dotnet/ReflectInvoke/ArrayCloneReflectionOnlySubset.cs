using System;

namespace ArrayCloneReflectionOnlySubset;

static class Program
{
    internal static void Run()
    {
        int[] source = { 2, 5, 8 };
        ICloneable value = source;
        var copy = (int[])typeof(ICloneable).GetMethod(nameof(ICloneable.Clone)).Invoke(value, null);
        copy[0] = 99;
        Console.WriteLine("array clone reflected: " + !ReferenceEquals(source, copy) + "/"
            + (source.GetType() == copy.GetType()) + "/" + copy.Length + "/"
            + source[0] + "/" + copy[2]);
    }
}
