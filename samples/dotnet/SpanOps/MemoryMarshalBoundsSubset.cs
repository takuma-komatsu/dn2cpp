using System;
using System.Runtime.InteropServices;

namespace SpanOps;

internal static class MemoryMarshalBoundsSubset
{
    internal static void Run()
    {
        Console.WriteLine("== MemoryMarshal bounds ==");
        foreach (int length in new[] { 0, 3 })
        {
            byte[] bytes = { 11, 22, 33, 44 };
            try
            {
                MemoryMarshal.Read<int>(bytes.AsSpan(0, length));
                Console.WriteLine("read missed");
            }
            catch (ArgumentOutOfRangeException ex)
            {
                Console.WriteLine($"read {length}={ex.ParamName}:{ex.Message}");
            }
            try
            {
                MemoryMarshal.Write(bytes.AsSpan(0, length), 123456789);
                Console.WriteLine("write missed");
            }
            catch (ArgumentOutOfRangeException ex)
            {
                Console.WriteLine($"write {length}={ex.ParamName}:{ex.Message}");
            }
            Console.WriteLine($"unchanged={bytes[0]},{bytes[1]},{bytes[2]},{bytes[3]}");
        }
        byte[] exact = new byte[sizeof(int)];
        MemoryMarshal.Write(exact, 123456789);
        Console.WriteLine("exact=" + MemoryMarshal.Read<int>(exact));
        Console.WriteLine("MemoryMarshal bounds end");
    }
}
