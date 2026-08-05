// Pin Nullable<T> *value* semantics: under -r the real System.Nullable`1 struct
// transpiles, so HasValue / Value / GetValueOrDefault / lifted operators / ?? / null
// compare / the special box (→ underlying or null) / ToString all match real .NET. This
// gate locks that behavior and the rarer shapes: Nullable<UserStruct> and GetType() on a
// boxed Nullable. Diffed exact vs real .NET. (Reflection *on* the Nullable type lives in
// ReflectNullableSubset; this is the value side.)
using System;

namespace NullableValueSubset;

public struct Point
{
    public int X;
    public int Y;
    public override string ToString() => "(" + X + "," + Y + ")";
}

public static class Program
{
    public static void __GateEntry()
    {
        int? a = 5;
        int? b = null;

        // HasValue / Value / GetValueOrDefault
        Console.WriteLine(a.HasValue);              // True
        Console.WriteLine(b.HasValue);              // False
        Console.WriteLine(a.Value);                 // 5
        Console.WriteLine(a.GetValueOrDefault());   // 5
        Console.WriteLine(b.GetValueOrDefault());   // 0
        Console.WriteLine(b.GetValueOrDefault(42)); // 42

        // lifted arithmetic / comparison operators
        int? x = 10, y = 3, z = null;
        Console.WriteLine(x + y);     // 13
        Console.WriteLine(x + z);     // (empty — null)
        Console.WriteLine(x * y);     // 30
        Console.WriteLine(x > y);     // True
        Console.WriteLine(x > z);     // False  (any compare with null is false)
        Console.WriteLine(x < z);     // False

        // ?? coalescing
        Console.WriteLine(z ?? -1);   // -1
        Console.WriteLine(x ?? -1);   // 10

        // null comparison (lowers to !HasValue)
        Console.WriteLine(b == null); // True
        Console.WriteLine(a == null); // False
        Console.WriteLine(a != null); // True

        // ToString: null → empty string, value → underlying ToString
        int? p = 99;
        int? q = null;
        Console.WriteLine(p.ToString());     // 99
        Console.WriteLine("[" + q.ToString() + "]"); // []

        // the special box: Nullable<int> boxes to its underlying int (or null)
        int? n = 7;
        object boxed = n;
        Console.WriteLine(boxed.GetType().Name); // Int32
        Console.WriteLine(boxed is int);         // True
        int? nn = null;
        object boxedNull = nn;
        Console.WriteLine(boxedNull == null);    // True

        // Nullable<UserStruct>
        Point? pt = new Point { X = 1, Y = 2 };
        Console.WriteLine(pt.HasValue);              // True
        Console.WriteLine(pt.Value.X);              // 1
        Console.WriteLine(pt);                       // (1,2)  — boxes Point, calls ToString
        Point? pnull = null;
        Console.WriteLine(pnull.HasValue);           // False
        Console.WriteLine(pnull.GetValueOrDefault().X); // 0   — default(Point)

        // GetType() on a boxed Nullable<UserStruct> reports the underlying struct
        object bpt = pt;
        Console.WriteLine(bpt.GetType().Name);       // Point

        // unbox.any: object → Nullable<T> (the inverse of the special box)
        object ob = 123;
        int? fromObj = (int?)ob;
        Console.WriteLine(fromObj.HasValue);         // True
        Console.WriteLine(fromObj.Value);            // 123
        object obNull = null;
        int? fromNull = (int?)obNull;
        Console.WriteLine(fromNull.HasValue);        // False
        Console.WriteLine(fromNull.GetValueOrDefault()); // 0

        // round-trip a Nullable<UserStruct> through object
        object obpt = pt;            // boxes Point
        Point? rt = (Point?)obpt;    // unbox.any back to Point?
        Console.WriteLine(rt.Value); // (1,2)
    }
}
