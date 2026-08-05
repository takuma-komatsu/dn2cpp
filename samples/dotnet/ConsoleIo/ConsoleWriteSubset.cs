#nullable disable
using System;

namespace ConsoleWriteSubset
{
    // Console.Write (no trailing newline) for string/char/int/long/double/
    // bool/object, plus the matching char/object WriteLine overloads. Write and
    // WriteLine share per-type formatting (the WriteLine variants append '\n').
    internal static class Program
    {
        internal static void __GateEntry()
        {
            Console.Write("a");
            Console.Write('b');
            Console.Write(1);
            Console.Write(true);
            Console.WriteLine();        // ab1True

            Console.Write(2.5);
            Console.Write(' ');
            Console.Write(100L);
            Console.WriteLine();        // 2.5 100

            Console.WriteLine('X');     // X

            object o = 42;
            Console.WriteLine(o);       // 42

            Console.Write("no-newline-");
            Console.WriteLine("end");   // no-newline-end
        }
    }
}
