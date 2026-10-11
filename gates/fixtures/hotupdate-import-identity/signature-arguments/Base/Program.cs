using System;
using System.Globalization;
using Dn2Cpp.Runtime;

namespace SignatureArgumentImports;

public sealed class Leaf
{
}

public class Cell<T>
{
    public static int Value;
    private readonly int _value;

    public Cell(int value) => _value = value;
    public int Read() => _value;
}

public static unsafe class Surface
{
    public static void Reset()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Cell<delegate*<int>[]>.Value = 11;
        Cell<delegate*<uint>[]>.Value = 23;
        Cell<delegate*<Leaf>[]>.Value = 37;
        Cell<global::Gfn0_5_Int32EndArr>.Value = 59;
    }

    public static int Read<T>() => Cell<T>.Value;

    public static void Write<T>(int value) => Cell<T>.Value = value;

    public static void Root()
    {
        if (Read<delegate*<int>[]>() != 11 || Read<delegate*<uint>[]>() != 23)
            throw new InvalidOperationException("Closed generic signature identities collided.");
        Write<delegate*<int>[]>(11);
        Write<delegate*<uint>[]>(23);
        if (Read<delegate*<Leaf>[]>() != 37 || Read<global::Gfn0_5_Int32EndArr>() != 59)
            throw new InvalidOperationException("Named signature arguments collided.");
        Write<delegate*<Leaf>[]>(37);
        Write<global::Gfn0_5_Int32EndArr>(59);
        if (new Cell<delegate*<int>[]>(31).Read() != 31
            || new Cell<delegate*<uint>[]>(47).Read() != 47)
            throw new InvalidOperationException("Closed generic signature imports failed.");
        if (new Cell<delegate*<Leaf>[]>(61).Read() != 61
            || new Cell<global::Gfn0_5_Int32EndArr>(73).Read() != 73)
            throw new InvalidOperationException("Named signature imports failed.");
    }
}

internal static class Program
{
    private static void Main(string[] args)
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Surface.Reset();
        Surface.Root();
        Console.WriteLine("signature argument base ready");
        if (args.Length != 0)
        {
            try { HotUpdate.Run(args[0]); }
            catch (Exception exception) { Console.WriteLine("signature update rejected: " + exception.Message); }
        }
    }
}
