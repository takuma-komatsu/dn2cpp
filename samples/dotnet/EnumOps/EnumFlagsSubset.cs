#nullable disable
using System;

namespace EnumFlagsSubset
{
    [Flags]
    internal enum Access
    {
        None = 0,
        Read = 1,
        Write = 2,
        Execute = 4,
    }

    // Enum.HasFlag is emitted as an inline bit test ((value & flag) == flag)
    // rather than dispatching the real body (which reaches GetMethodTable /
    // InternalCall). The receiver and flag arrive as boxed Enum values.
    internal static class Program
    {
        // A non-constant flag forces a real Enum.HasFlag call (Roslyn inlines a
        // constant-flag HasFlag itself).
        private static bool Has(Access a, Access flag) => a.HasFlag(flag);

        internal static void __GateEntry()
        {
            Access a = Access.Read | Access.Execute;

            Console.WriteLine(Has(a, Access.Read));    // True
            Console.WriteLine(Has(a, Access.Write));   // False
            Console.WriteLine(Has(a, Access.Execute)); // True
            Console.WriteLine(Has(a, Access.None));    // True (HasFlag(None) is always true)

            // Constant flag (Roslyn may inline this directly).
            Console.WriteLine(a.HasFlag(Access.Read | Access.Execute)); // True
            Console.WriteLine(a.HasFlag(Access.Read | Access.Write));   // False

            Console.WriteLine((int)a); // 5
        }
    }
}
