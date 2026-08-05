using System;
using System.Runtime.InteropServices;

// MemoryMarshal span construction over arbitrary element types: GetReference,
// GetArrayDataReference, CreateSpan, CreateReadOnlySpan. Each ref must alias the
// original storage, so every arm writes through it.
namespace MarshalShapeSubset;

struct Cell { public int Key; public int Val; }

class Program
{
    internal static void __GateEntry()
    {
        double[] xs = { 1.5, 2.5, 3.5 };
        ref double r0 = ref MemoryMarshal.GetReference(xs.AsSpan());
        r0 = 9.5;
        Console.WriteLine(xs[0]);                              // 9.5

        Cell[] cells = { new Cell { Key = 1, Val = 2 }, new Cell { Key = 3, Val = 4 } };
        ref Cell c0 = ref MemoryMarshal.GetArrayDataReference(cells);
        c0.Val = 99;
        Console.WriteLine($"{cells[0].Key},{cells[0].Val}");  // 1,99

        long[] ls = { 100, 200, 300 };
        ref long l0 = ref MemoryMarshal.GetArrayDataReference(ls);
        Console.WriteLine(l0);                                // 100

        Cell cell = new Cell { Key = 5, Val = 6 };
        Span<Cell> cs = MemoryMarshal.CreateSpan(ref cell, 1);
        cs[0].Key = 50;
        Console.WriteLine($"{cell.Key},{cell.Val}");          // 50,6

        double d = 7.25;
        ReadOnlySpan<double> rds = MemoryMarshal.CreateReadOnlySpan(ref d, 1);
        Console.WriteLine($"{rds.Length}:{rds[0]}");          // 1:7.25
    }
}
