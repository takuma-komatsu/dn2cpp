#nullable disable
using System;
using System.Collections;
using System.Collections.Generic;

// An array carries its full CLR SZArray interface-dispatch map on its
// per-element type-info (ti_arr_<T>). A *runtime* cast — the static type is object/Array,
// so the boundary wrap can't see the T[] — to IList<T> / ICollection<T> /
// IReadOnlyList<T> / IReadOnlyCollection<T> (and the non-generic IList / ICollection /
// IEnumerable) now resolves on the array itself: get_Count / the indexer (get + the
// write-through setter) / Contains / IndexOf / CopyTo dispatch through thunks that wrap
// the array into a fresh SZArrayEnumerable<T> and forward to it. The covariant
// IReadOnlyList<object> over a string[] (out T) resolves the same way. Size-changing
// members would throw NotSupportedException (array semantics) but are not exercised here.
// CoreLib only (the auto-referenced support shim suffices — no LINQ -r). Diffed exact vs .NET.

namespace ArrayRuntimeCollectionDispatchSubset
{
    static class Program
    {
        // Generic IList<int> via a runtime cast (the array arrives typed as object).
        static void GenericIntList(object boxed)
        {
            IList<int> list = (IList<int>)boxed;
            Console.WriteLine("count=" + list.Count);
            Console.WriteLine("idx2=" + list[2]);
            list[2] = 99;                       // write-through to the backing array
            Console.WriteLine("after=" + list[2]);
            Console.WriteLine("contains40=" + list.Contains(40) + " contains7=" + list.Contains(7));
            Console.WriteLine("indexOf50=" + list.IndexOf(50) + " indexOf7=" + list.IndexOf(7));
            int[] dest = new int[7];
            list.CopyTo(dest, 1);
            string s = "";
            for (int k = 0; k < dest.Length; k++)
                s += (k == 0 ? "" : ",") + dest[k];
            Console.WriteLine("copy=" + s);
        }

        // Generic ICollection<string> + IReadOnlyList<string> via runtime casts.
        static void GenericStringColl(object boxed)
        {
            ICollection<string> c = (ICollection<string>)boxed;
            Console.WriteLine("scount=" + c.Count + " sBeta=" + c.Contains("beta") + " sZeta=" + c.Contains("zeta"));
            IReadOnlyList<string> r = (IReadOnlyList<string>)boxed;
            Console.WriteLine("sidx1=" + r[1] + " srcount=" + r.Count);
        }

        // Covariant IReadOnlyList<object> over a string[] (out T) — resolved on the array.
        static void CovariantReadOnly(object boxed)
        {
            IReadOnlyList<object> r = (IReadOnlyList<object>)boxed;
            Console.WriteLine("covcount=" + r.Count + " cov0=" + r[0] + " cov2=" + r[2]);
        }

        // Non-generic IList over an int[] (the object boundary boxes/unboxes int). The
        // array CLR quirk: IsFixedSize is true but IsReadOnly is false (elements writable).
        static void NonGenericIntList(object boxed)
        {
            IList list = (IList)boxed;
            Console.WriteLine("ngcount=" + list.Count + " ngfixed=" + list.IsFixedSize + " ngro=" + list.IsReadOnly);
            object e = list[1];
            Console.WriteLine("ngidx1=" + e);
            Console.WriteLine("ng20=" + list.Contains(20) + " ngIdx40=" + list.IndexOf(40));
        }

        // Non-generic IList over a string[].
        static void NonGenericStringList(object boxed)
        {
            IList list = (IList)boxed;
            Console.WriteLine("ngsidx0=" + list[0] + " ngsIdxGamma=" + list.IndexOf("gamma") + " ngsCount=" + list.Count);
        }

        // The core win: the value behind a collection-interface variable is the
        // *raw array*, not a compile-time wrapper — so GetType() is exact (a boundary
        // wrap would report the SZArrayEnumerable wrapper type instead, which is wrong).
        static void Identity(object boxed)
        {
            IList<int> list = (IList<int>)boxed;
            IEnumerable<int> seq = (IEnumerable<int>)boxed;
            Console.WriteLine("idlist=" + list.GetType().Name + " idseq=" + seq.GetType().Name);
        }internal static int Run()
        {
            GenericIntList(new int[] { 10, 20, 30, 40, 50 });
            GenericStringColl(new string[] { "alpha", "beta", "gamma" });
            CovariantReadOnly(new string[] { "alpha", "beta", "gamma" });
            NonGenericIntList(new int[] { 10, 20, 30, 40, 50 });
            NonGenericStringList(new string[] { "alpha", "beta", "gamma" });
            Identity(new int[] { 1, 2, 3 });
            return 0;
        }
    }
}
