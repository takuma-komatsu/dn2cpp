#nullable disable
using System;
using System.Collections.Generic;

// an array carries a real IEnumerable<T> interface-dispatch map, so a *runtime*
// cast (IEnumerable<T>)obj — where the static type is object/Array and the boundary
// wrap can't see the T[] — resolves on the array itself: the cast succeeds and foreach /
// GetEnumerator dispatch through the array's GetEnumerator (which wraps it into the
// SZArrayEnumerable<T> enumerator). Exact element type only here; covariance is.
// CoreLib only (no LINQ shim) — proves the auto-referenced support shim is enough.

namespace ArrayInterfaceDispatchSubset
{
    static class Program
    {
        // The array arrives typed as object, so the cast is a real castclass IEnumerable<T>
        // (not a statically-wrapped boundary) — this is the path the array's own map serves.
        static int SumViaRuntimeCast(object boxedIntArray)
        {
            IEnumerable<int> e = (IEnumerable<int>)boxedIntArray;
            int s = 0;
            foreach (int x in e)
                s += x;
            return s;
        }

        static string JoinViaRuntimeCast(object boxedStringArray)
        {
            IEnumerable<string> e = (IEnumerable<string>)boxedStringArray;
            string r = "";
            foreach (string w in e)
                r += (r.Length == 0 ? "" : ",") + w;
            return r;
        }internal static int Run()
        {
            object ints = new int[] { 3, 4, 5, 6 };
            object strs = new string[] { "a", "b", "c" };

            Console.WriteLine("sum=" + SumViaRuntimeCast(ints));      // 18
            Console.WriteLine("join=" + JoinViaRuntimeCast(strs));    // a,b,c

            // `is IEnumerable<T>` on a boxed array (runtime type test through the map).
            Console.WriteLine("intIsEnum=" + (ints is IEnumerable<int>) +
                " strIsEnum=" + (strs is IEnumerable<string>) +
                " intIsStrEnum=" + (ints is IEnumerable<string>));

            // A second, independent foreach over the same array enumerates from the start
            // (each GetEnumerator hands back a fresh cursor — array semantics).
            int again = 0;
            foreach (int x in (IEnumerable<int>)ints)
                again += x;
            Console.WriteLine("again=" + again);                     // 18
            return 0;
        }
    }
}
