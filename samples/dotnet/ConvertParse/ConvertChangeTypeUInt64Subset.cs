#nullable disable
// (reflection rebuild): Convert.ChangeType's TypeCode.UInt64 arm. A new boxed
// dn2cpp_uint64_type lets a runtime-Type / TypeCode driven conversion target System.UInt64,
// the one integer carve-out left by /. The key wrinkle is *unsigned* handling:
// a UInt64 whose value exceeds Int64.MaxValue must parse from / format to its unsigned
// decimal (not the signed int64 fallback). An enum target still throws, matching real.NET.
// Diffed exact vs real.NET.
using System;

namespace ConvertChangeTypeUInt64Subset;

public static class Program
{
    internal static void __GateEntry()
    {
        // numeric source -> ulong, then ToString (boxed -> unsigned format)
        object a = Convert.ChangeType(42, typeof(ulong));
        Console.WriteLine(a);                 // 42
        Console.WriteLine(a.GetType().Name);  // UInt64

        // string source -> ulong, value > Int64.MaxValue (needs unsigned parse)
        object big = Convert.ChangeType("18446744073709551615", typeof(ulong));
        Console.WriteLine(big);               // 18446744073709551615
        Console.WriteLine((ulong)big);        // 18446744073709551615 (unbox round-trip)

        // ulong source > Int64.MaxValue -> string (needs unsigned format)
        ulong max = ulong.MaxValue;
        object s = Convert.ChangeType(max, typeof(string));
        Console.WriteLine(s);                 // 18446744073709551615

        // ulong -> narrower / floating targets
        ulong mid = 5000000000UL;             // > uint.MaxValue
        Console.WriteLine(Convert.ChangeType(mid, typeof(double)));  // 5000000000
        Console.WriteLine(Convert.ChangeType(100UL, typeof(int)));   // 100

        // the (object, TypeCode) overload — TypeCode.UInt64 == 12
        object viaCode = Convert.ChangeType("250", TypeCode.UInt64);
        Console.WriteLine(viaCode);                 // 250
        Console.WriteLine(viaCode.GetType().Name);  // UInt64

        // identity: ulong -> ulong
        object id = Convert.ChangeType(max, typeof(ulong));
        Console.WriteLine((ulong)id == max);  // True

        // typeof / reflection on the ulong type
        Console.WriteLine(typeof(ulong).Name);                 // UInt64
        Console.WriteLine(Type.GetType("System.UInt64").Name); // UInt64

        // format-with-spec on a boxed ulong (unsigned hole formatting)
        Console.WriteLine(string.Format("{0:X}", max));   // FFFFFFFFFFFFFFFF
        Console.WriteLine(string.Format("{0:N0}", mid));  // 5,000,000,000
    }
}
