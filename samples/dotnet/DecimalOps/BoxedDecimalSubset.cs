using System;
using System.Collections.Generic;

// A boxed System.Decimal routed through the Object virtuals (ToString / Equals /
// GetHashCode). Decimal is an intrinsic value type (Dn2CppDecimal) with no emitted ti_*, so
// boxing names the shared runtime dn2cpp_decimal_type handle and the object_* helpers
// recover the payload. CoreLib only (no Linq shim), which is what keeps this on the plain
// boxing path.
namespace BoxedDecimalSubset;

internal static class Program
{
    internal static void Run()
    {
        // --- boxed ToString (the headline gap) ---
        object a = 12.50m;            // box; scale preserved
        Console.WriteLine(a.ToString());     // 12.50
        Console.WriteLine(a);                // Console.WriteLine(object) -> ToString
        Console.WriteLine("val=" + a);       // string concat with object
        Console.WriteLine(((object)(-3.14m)).ToString());   // negative
        Console.WriteLine(((object)0m).ToString());         // zero
        Console.WriteLine(((object)79228162514264337593543950335m).ToString()); // MaxValue

        // --- boxed in string.Format (no spec -> ToString, with spec -> number path) ---
        decimal d = 1234.5m;
        Console.WriteLine(string.Format("{0}", d));      // 1234.5
        Console.WriteLine(string.Format("{0:F2}", d));   // 1234.50
        Console.WriteLine(string.Format("{0:N2}", d));   // 1,234.50

        // --- boxed Equals: value equality, scale-insensitive ---
        object x = 1.0m;
        object y = 1.00m;            // equal value, different scale
        object z = 2.5m;
        Console.WriteLine(x.Equals(y));   // True
        Console.WriteLine(x.Equals(z));   // False
        Console.WriteLine(((object)(-0.0m)).Equals((object)0.0m)); // True

        // --- boxed GetHashCode: equal values hash equal (collection contract) ---
        Console.WriteLine(x.GetHashCode() == y.GetHashCode());   // True
        var map = new Dictionary<object, string>();
        map[1.0m] = "one";
        Console.WriteLine(map.ContainsKey(1.00m));   // True (equal-by-value key)
        Console.WriteLine(map.ContainsKey(2.0m));    // False

        // The STATICALLY TYPED GetHashCode belongs to the bucket's other half, but it
        // lands here because appending is the only placement that leaves the earlier
        // output an unchanged prefix. Same contract, a different lowering: no boxing, so
        // the transpiler emits the hash at the call site.
        decimal u1 = 1.0m;
        decimal u2 = 1.00m;
        Console.WriteLine(u1.GetHashCode() == u2.GetHashCode());       // True
        Console.WriteLine(u1.GetHashCode() == ((object)u2).GetHashCode()); // True
        Console.WriteLine((-0.0m).GetHashCode() == 0.0m.GetHashCode()); // True
        Console.WriteLine(2.5m.GetHashCode() == u1.GetHashCode());     // False
        var typed = new Dictionary<decimal, string>();
        typed[1.0m] = "one";
        Console.WriteLine(typed.ContainsKey(1.00m));  // True
        Console.WriteLine(typed.ContainsKey(2.0m));   // False
    }
}
