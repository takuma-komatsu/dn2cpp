using System;
using System.Collections.Generic;
using System.Runtime.Intrinsics;

namespace BoxedComparableSubset;

// The UNCONSTRAINED (IComparable<T>)box.CompareTo forms: a boxed value cast to
// IComparable<T> (castclass), or assigned straight to it (box to interface, no
// castclass), then compared. Devirtualizing only the `constrained.` shape misses both —
// a boxed primitive's type-info carries no IComparable<T> interface map, so the castclass
// throws and the callvirt has no mapping. Both must lower to a typed three-way compare.

internal static class Program
{
    internal static void Run()
    {
        // Cast form: (IComparable<int>)boxedInt.
        object o = 5;
        IComparable<int> ci = (IComparable<int>)o;
        Console.WriteLine(ci.CompareTo(3) + " " + ci.CompareTo(5) + " " + ci.CompareTo(9));

        // Box-to-interface form (no castclass): IComparable<int> = 7.
        IComparable<int> ci2 = 7;
        Console.WriteLine(ci2.CompareTo(10));

        // double via the cast form.
        object od = 2.5;
        IComparable<double> cd = (IComparable<double>)od;
        Console.WriteLine(cd.CompareTo(2.5) + " " + cd.CompareTo(1.0));

        // long via the box-to-interface form.
        IComparable<long> cl = 100L;
        Console.WriteLine(cl.CompareTo(50L) + " " + cl.CompareTo(100L));

        // string — the "box" is identity (string is already a reference). Ordinal
        // compare (the project's string-ordering model); Math.Sign normalises the
        // magnitude so it matches.NET's culture-aware sign for these ASCII words.
        object os = "mango";
        IComparable<string> cs = (IComparable<string>)os;
        Console.WriteLine(Math.Sign(cs.CompareTo("apple")) + " " + Math.Sign(cs.CompareTo("mango")) + " " + Math.Sign(cs.CompareTo("zebra")));

        // Comparer<object>.Default -> ObjectComparer<object> order: null-handling plus
        // non-generic System.IComparable dispatch on the boxed values (dn2cpp_object_compare).
        // dn2cpp synthesizes a GenericComparer<object> for Comparer<T>.Default; its Compare's
        // `((IComparable<object>)x).CompareTo(y)` (constrained on object) devirtualizes here.
        // Matches real .NET (which returns ObjectComparer<object>). Math.Sign normalises magnitude.
        var oc = Comparer<object>.Default;
        Console.WriteLine(Math.Sign(oc.Compare(3, 5)) + " " + oc.Compare(7, 7) + " " + Math.Sign(oc.Compare(9, 2)));
        Console.WriteLine(oc.Compare(null, 1) + " " + oc.Compare(1, null) + " " + oc.Compare(null, null));
        Console.WriteLine(Math.Sign(oc.Compare("b", "a")));

        // object[] sorted through the same boxed order (the non-generic IComparable path).
        object[] arr = { 5, 1, 3, 2, 4 };
        Array.Sort(arr);
        Console.WriteLine(string.Join(",", arr));

        // Comparer<Vector128<byte>>.Default: Vector128<T> does NOT implement IComparable, so
        // real .NET's default comparer is ObjectComparer<T> whose Compare boxes and dispatches
        // non-generic System.IComparable — which the box does not implement, so it throws
        // (ArgumentException). dn2cpp reproduces the throw as a catchable
        // PlatformNotSupportedException; both sides throw, so only "it throws" is asserted (the
        // exception subtype intentionally differs — the comparer faults where real .NET's does).
        try
        {
            Comparer<Vector128<byte>>.Default.Compare(Vector128.Create((byte)1), Vector128.Create((byte)2));
            Console.WriteLine("v128 NOTHROW");
        }
        catch (Exception)
        {
            Console.WriteLine("v128 throws");
        }
    }
}
