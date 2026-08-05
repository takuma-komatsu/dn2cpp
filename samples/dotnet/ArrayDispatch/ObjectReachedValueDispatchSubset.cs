#nullable disable
using System;
using System.Collections;
using System.Collections.Generic;

// Collection-interface DISPATCH on a VALUE-element array the
// emitter never saw as one — the receiver reaches the call typed `object`, so no
// statically-visible cast/boundary wired the lazy per-element map, and the shared
// reference-element fallback table cannot serve it (value layouts differ per element).
// Served by the per-element map every noted value element now gets EAGERLY
// (the value loop in Compilation.ExpandArrayEnumerableMaps): the exact rows on
// ti_arr_<E> answer the non-generic trio and the generic collection interfaces with
// no covariance/erasure arm involved.
//
// The element types here (ushort / the file-local Tide enum / the file-local Knot
// struct) are named by NO other file in this bucket, and this file writes NO cast or
// generic-interface cast that names them — that absence is itself part of the assert:
// nothing anywhere can have wired these maps lazily, so a green run proves the eager
// wiring. The one generic section reaches ICollection<ushort>/IList<ushort> purely
// through reflection (typeof notes no array-enumerable element).
//
//  - non-generic IEnumerable/ICollection/IList on a ushort[] behind `object`:
//    foreach, Count (both interfaces), indexer get + SET (write-through re-read),
//    Contains/IndexOf on a boxed element, a wrong-type Contains probe;
//  - a short-backed enum element: non-generic foreach prints the boxed enum names;
//  - a struct element: foreach through the ToString override, plus IndexOf/Contains
//    against field-equal but DISTINCT boxed instances — the non-generic IndexOf
//    unboxes and compares with EqualityComparer<Knot>.Default, i.e. the synthesized
//    value equality, never reference identity;
//  - generic interfaces with no static site at all: ICollection<ushort>.Count and
//    IList<ushort>.get_Item invoked via reflection on the object-typed array.
// Output is ints, enum names and Knot's own ToString only (no double formatting).
// Intrinsic value elements (Vector128<T> etc. — no emitted ti_) keep the loud abort.
// CoreLib only. Diffed exact vs .NET.

namespace ObjectReachedValueDispatchSubset
{
    enum Tide : short { Ebb, Neap, Spring, Surge }

    struct Knot
    {
        public int A;
        public int B;
        public Knot(int a, int b) { A = a; B = b; }
        public override string ToString() { return "Knot(" + A + "," + B + ")"; }
    }

    static class Program
    {
        // Each array is created here and immediately forgotten behind `object` — no
        // cast anywhere in this file names the element, so nothing wires lazily.
        static object MakeDepths()
        {
            return new ushort[] { 11, 22, 33, 44 };
        }

        static object MakeTides()
        {
            return new Tide[] { Tide.Neap, Tide.Ebb, Tide.Surge };
        }

        static object MakeKnots()
        {
            return new Knot[] { new Knot(1, 2), new Knot(3, 4), new Knot(5, 6) };
        }

        // Non-generic trio on a ushort[] reached through object: only the eagerly
        // wired per-element map can answer (the fallback table rejects value elements).
        static void NonGenericTrio(object o)
        {
            string all = "";
            foreach (object v in (IEnumerable)o)
                all += "[" + v + "]";
            Console.WriteLine("val-each=" + all);
            IList l = (IList)o;
            Console.WriteLine("val-count=" + l.Count);
            object v1 = l[1];
            l[2] = v1;   // write-through: the setter unboxes into the backing array
            Console.WriteLine("val-idx1=" + v1 + " val-idx2=" + l[2]
                + " val-contains=" + l.Contains(v1) + " val-miss=" + l.Contains("nope")
                + " val-indexOf=" + l.IndexOf(v1));
            Console.WriteLine("val-icount=" + ((ICollection)o).Count);
        }

        // A short-backed enum element: the boxed elements print their names.
        static void EnumSection(object o)
        {
            string all = "";
            foreach (object t in (IEnumerable)o)
                all += "(" + t + ")";
            Console.WriteLine("tide-each=" + all + " tide-count=" + ((ICollection)o).Count);
        }

        // A struct element: IndexOf/Contains take field-equal but DISTINCT boxed
        // instances, so a hit proves the synthesized value equality.
        static void StructSection(object o)
        {
            string all = "";
            foreach (object k in (IEnumerable)o)
                all += "<" + k + ">";
            Console.WriteLine("knot-each=" + all);
            IList l = (IList)o;
            Console.WriteLine("knot-indexOf=" + l.IndexOf(new Knot(3, 4))
                + " knot-contains=" + l.Contains(new Knot(5, 6))
                + " knot-miss=" + l.Contains(new Knot(9, 9)));
        }

        // The generic interfaces with NO static site: typeof notes no array-enumerable
        // element, so these rows too exist only because the wiring is eager.
        static void GenericNoSite(object o)
        {
            object count = typeof(ICollection<ushort>).GetProperty("Count").GetValue(o);
            object item = typeof(IList<ushort>).GetMethod("get_Item").Invoke(o, new object[] { 1 });
            Console.WriteLine("gen-count=" + count + " gen-item1=" + item);
        }

        public static void Run()
        {
            NonGenericTrio(MakeDepths());
            EnumSection(MakeTides());
            StructSection(MakeKnots());
            GenericNoSite(MakeDepths());
        }
    }
}
