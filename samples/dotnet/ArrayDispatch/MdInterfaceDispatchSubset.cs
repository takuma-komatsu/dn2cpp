#nullable disable
using System;
using System.Collections;
using System.Linq;

// Interface DISPATCH on a MULTI-DIMENSIONAL (rank >= 2) array. An MD array's type-info is
// runtime-interned (dn2cpp_mdarr_ti) and carries no per-element interface rows; dispatch is
// served by the ONE shared six-interface table the init prologue installs
// (dn2cpp_array_set_md_fallback_interfaces), whose thunks wrap the receiver into the
// non-generic Dn2Cpp.Runtime.MDArrayEnumerable. Every receiver here is therefore reached as
// a plain `object` and cast at run time — nothing element-specific is wired, and one table
// serves int, string, a user struct and every rank.
//
//  - IEnumerable over int[2,3] / string[2,3] / int[2,2,2]: rightmost-fastest (flat
//    data) order, via foreach and via LINQ Cast<int>().Sum() (which first probes
//    `is IEnumerable<int>` — an MD array implements NO generic collection interface,
//    so a wrong True there would dispatch a generic row this table cannot carry);
//  - the enumerator protocol: Current before MoveNext / after exhaustion / after
//    Reset (exception type + message), Reset restarting the walk, and the
//    enumerator NOT being IDisposable (matching System.ArrayEnumerator);
//  - ICollection: Count = flat count, IsSynchronized, SyncRoot reference-identical
//    to the ARRAY (not the per-dispatch wrapper), CopyTo -> RankException;
//  - IList: IsFixedSize/IsReadOnly, indexer get+set / Add / Insert / Remove /
//    RemoveAt / Contains / IndexOf all throwing real .NET's exact type AND message
//    — and Clear(), the one mutator real .NET serves, genuinely zeroing every
//    element in place (contents printed after);
//  - ICloneable.Clone: same type/rank/lengths, values copied, storage independent;
//  - IStructuralEquatable/IStructuralComparable in real .NET's measured guard ORDER:
//    the reference-equal / null / non-array / length-mismatch arguments answer
//    WITHOUT throwing; only the element walk's first GetValue(int) throws on a
//    rank>=2 receiver;
//  - a struct element WITHOUT Equals/GetHashCode: enumerating and unboxing proves
//    no element equality support is ever demanded (IndexOf/Contains throw
//    RankException before any equality question — the property that lets one
//    non-generic wrapper serve elements the equality builders cannot answer);
//  - foreach over a System.Array-typed variable: the Array.GetEnumerator intrinsic
//    resolves the same non-generic IEnumerable row off the MD type-info.
// Output is ints, bools and exception texts only (no double formatting).
// CoreLib + System.Linq. Diffed exact vs .NET.

namespace MdInterfaceDispatchSubset
{
    // Deliberately no Equals/GetHashCode/ToString: an element type the equality
    // builders cannot serve, enumerable through the MD table regardless.
    struct Vane
    {
        public int X;
        public int Y;
        public Vane(int x, int y) { X = x; Y = y; }
    }

    // A comparer for the structural pair whose answers the receiver never consults
    // (the throw fires first); user-defined because the receiver only needs SOME
    // IComparer/IEqualityComparer instance.
    class PlainComparer : IComparer, IEqualityComparer
    {
        public int Compare(object x, object y) { return 0; }
        public new bool Equals(object x, object y) { return true; }
        public int GetHashCode(object obj) { return 0; }
    }

    static class Program
    {
        static void Probe(string tag, Action a)
        {
            try
            {
                a();
                Console.WriteLine(tag + "=ok");
            }
            catch (Exception ex)
            {
                Console.WriteLine(tag + "=" + ex.GetType().Name + ": " + ex.Message);
            }
        }

        // Created here and immediately forgotten behind `object` — every section
        // below re-discovers the interfaces by runtime cast.
        static object MakeGrid()
        {
            int[,] a = new int[2, 3];
            for (int i = 0; i < 2; i++)
                for (int j = 0; j < 3; j++)
                    a[i, j] = i * 10 + j;
            return a;
        }

        static void EnumerationSection(object o)
        {
            string all = "";
            foreach (object v in (IEnumerable)o)
                all += "[" + v + "]";
            Console.WriteLine("md-each=" + all);
            string cast = "";
            foreach (int v in ((IEnumerable)o).Cast<int>())
                cast += "." + v;
            Console.WriteLine("md-cast=" + cast + " md-sum=" + ((IEnumerable)o).Cast<int>().Sum());

            object s = new string[2, 3] { { "a", "b", "c" }, { "d", "e", "f" } };
            string sall = "";
            foreach (object v in (IEnumerable)s)
                sall += (string)v;
            Console.WriteLine("md-str=" + sall);

            int[,,] cube = new int[2, 2, 2];
            int k = 0;
            for (int i = 0; i < 2; i++)
                for (int j = 0; j < 2; j++)
                    for (int m = 0; m < 2; m++)
                        cube[i, j, m] = k++;
            string call3 = "";
            foreach (object v in (IEnumerable)(object)cube)
                call3 += v;
            Console.WriteLine("md-3d=" + call3);
        }

        static void EnumeratorProtocolSection(object o)
        {
            IEnumerator e = ((IEnumerable)o).GetEnumerator();
            Probe("md-curBefore", () => { object x = e.Current; });
            int moved = 0;
            while (e.MoveNext())
                moved++;
            Console.WriteLine("md-moved=" + moved);
            Probe("md-curAfter", () => { object x = e.Current; });
            e.Reset();
            Probe("md-curAfterReset", () => { object x = e.Current; });
            e.MoveNext();
            Console.WriteLine("md-first=" + e.Current + " md-disposable=" + (e is IDisposable));
        }

        static void CollectionSection(object o)
        {
            ICollection c = (ICollection)o;
            Console.WriteLine("md-count=" + c.Count + " md-sync=" + c.IsSynchronized
                + " md-syncRoot=" + ReferenceEquals(c.SyncRoot, o));
            Probe("md-copyTo", () => c.CopyTo(new int[6], 0));
            Probe("md-copyToObj", () => c.CopyTo(new object[6], 0));
        }

        static void ListSection(object o)
        {
            IList l = (IList)o;
            Console.WriteLine("md-fixed=" + l.IsFixedSize + " md-ro=" + l.IsReadOnly);
            Probe("md-get", () => { object x = l[0]; });
            Probe("md-set", () => l[0] = 7);
            Probe("md-add", () => l.Add(7));
            Probe("md-insert", () => l.Insert(0, 7));
            Probe("md-remove", () => l.Remove(7));
            Probe("md-removeAt", () => l.RemoveAt(0));
            Probe("md-contains", () => l.Contains(7));
            Probe("md-indexOf", () => l.IndexOf(7));
            // The one mutator real .NET serves: Clear zeroes every element in place.
            int[,] a = (int[,])o;
            a[0, 0] = 42;
            l.Clear();
            string after = "";
            for (int i = 0; i < 2; i++)
                for (int j = 0; j < 3; j++)
                    after += a[i, j];
            Console.WriteLine("md-cleared=" + after);
        }

        static void CloneSection(object o)
        {
            int[,] a = (int[,])o;
            object c = ((ICloneable)o).Clone();
            int[,] cc = (int[,])c;
            Console.WriteLine("md-cloneType=" + c.GetType() + " md-cloneDims="
                + cc.GetLength(0) + "x" + cc.GetLength(1) + " md-cloneV=" + cc[1, 2]);
            a[1, 2] = -1; // independence: the original's write must not show in the clone
            Console.WriteLine("md-cloneKept=" + cc[1, 2] + " md-orig=" + a[1, 2]);
        }

        static void StructuralSection(object o)
        {
            PlainComparer pc = new PlainComparer();
            IStructuralEquatable se = (IStructuralEquatable)o;
            Console.WriteLine("md-structSelf=" + se.Equals(o, pc)
                + " md-structNull=" + se.Equals(null, pc)
                + " md-structLen=" + se.Equals(new int[5], pc));
            Probe("md-structEq", () => se.Equals(new int[2, 3], pc));
            Probe("md-structHash", () => se.GetHashCode(pc));
            IStructuralComparable sc = (IStructuralComparable)o;
            Console.WriteLine("md-structCmpNull=" + sc.CompareTo(null, pc));
            Probe("md-structCmp", () => sc.CompareTo(o, pc));
            Probe("md-structCmpLen", () => sc.CompareTo(new int[5], pc));
        }

        static void StructElementSection()
        {
            Vane[,] v = new Vane[2, 2];
            for (int i = 0; i < 2; i++)
                for (int j = 0; j < 2; j++)
                    v[i, j] = new Vane(i, j);
            object o = v;
            string all = "";
            foreach (object b in (IEnumerable)o)
            {
                Vane w = (Vane)b;
                all += "(" + w.X + "," + w.Y + ")";
            }
            Console.WriteLine("md-vane=" + all + " md-vaneCount=" + ((ICollection)o).Count);
            Probe("md-vaneIndexOf", () => ((IList)o).IndexOf(new Vane(1, 1)));
        }

        static void ArrayTypedSection(object o)
        {
            // foreach over a System.Array-typed variable lowers to the
            // Array.GetEnumerator intrinsic, which resolves the same non-generic
            // IEnumerable row off the MD array's runtime-interned type-info.
            Array a = (Array)o;
            string all = "";
            foreach (object v in a)
                all += "|" + v;
            Console.WriteLine("md-arrEach=" + all);
        }

        public static void Run()
        {
            // CloneSection writes into the shared grid and ListSection zeroes its
            // own, so the mutating sections run last / on fresh grids.
            object o = MakeGrid();
            EnumerationSection(o);
            EnumeratorProtocolSection(o);
            CollectionSection(o);
            StructuralSection(o);
            CloneSection(o);
            ListSection(MakeGrid());
            StructElementSection();
            ArrayTypedSection(MakeGrid());
        }
    }
}
