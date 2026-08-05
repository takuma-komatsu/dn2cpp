#nullable disable
using System;
using System.Collections.Generic;

namespace ArrayOpsSubset
{
    // Array.Clone() / Array.CopyTo(Array,int) and Array.IndexOf<T> for an own reference
    // type with default (reference) equality.
    //   * Clone():  a shallow copy — a fresh array of the same precise type and length,
    //               with the element block copied verbatim (value elements bitwise,
    //               reference element pointers shared). Returns object.
    //   * CopyTo(dst, index): copies this array's elements into dst starting at index.
    //   * Array.IndexOf<T>(arr, value) for a reference type T that overrides neither
    //               Object.Equals nor IEquatable<T>: pointer comparison — a value-equal
    //               but distinct instance does NOT match (-1); a null search value
    //               matches a null slot; duplicates return the first index.
    // Box / List<T> are sealed reference types with no Equals override — exactly the
    // shape of dn2cpp's own ClassInfo / MethodInfo (List<ClassInfo>.IndexOf reaches
    // Array.IndexOf<ClassInfo>). CoreLib only (no Linq shim). Diffs exact vs real .NET.
    internal static class Program
    {
        // A small sealed reference type with NO Equals/GetHashCode override and no
        // IEquatable<T> — its EqualityComparer<T>.Default is reference equality.
        // Mirrors Dn2Cpp.ClassInfo / Dn2Cpp.MethodInfo.
        private sealed class Box
        {
            public int V;
            public Box(int v) { V = v; }
        }

        internal static void Run()
        {
            CloneValueArray();
            CloneRefArray();
            CloneStringArray();
            CloneDoubleArray();
            CopyToValueArray();
            CopyToRefArray();
            IndexOfRefType();
            IndexOfViaList();
            IndexOfInt();
        }

        private static void CloneValueArray()
        {
            int[] a = { 1, 2, 3 };
            object cObj = a.Clone();
            int[] c = (int[])cObj;
            Console.WriteLine("clone-int: isArr=" + (cObj is int[]) + " len=" + c.Length
                + " sameRef=" + ReferenceEquals(a, c) + " vals=" + c[0] + "," + c[1] + "," + c[2]);
            // mutating the clone must not touch the original (independent buffers)
            c[0] = 99;
            Console.WriteLine("clone-int-indep: orig0=" + a[0] + " clone0=" + c[0]);
        }

        private static void CloneRefArray()
        {
            Box b0 = new Box(10);
            Box b1 = new Box(20);
            Box[] b = { b0, b1 };
            Box[] bc = (Box[])b.Clone();
            // shallow: element references are shared with the original
            Console.WriteLine("clone-ref: len=" + bc.Length + " sameRef=" + ReferenceEquals(b, bc)
                + " elem0Shared=" + ReferenceEquals(b[0], bc[0]) + " elem1Shared=" + ReferenceEquals(b[1], bc[1]));
            bc[0].V = 555; // mutate through the shared element, visible in the original
            Console.WriteLine("clone-ref-shallow: orig0.V=" + b[0].V);
        }

        private static void CloneStringArray()
        {
            string[] s = { "x", "y" };
            string[] sc = (string[])s.Clone();
            Console.WriteLine("clone-str: " + sc[0] + "," + sc[1] + " type=" + sc.GetType().Name);
        }

        private static void CloneDoubleArray()
        {
            double[] d = { 1.5, 2.5, 3.5 };
            double[] dc = (double[])d.Clone();
            Console.WriteLine("clone-dbl: len=" + dc.Length + " vals=" + dc[0] + "," + dc[1] + "," + dc[2]);
        }

        private static void CopyToValueArray()
        {
            int[] src = { 7, 8, 9 };
            int[] dst = new int[6];
            src.CopyTo(dst, 2);
            Console.WriteLine("copyto-int: " + dst[0] + "," + dst[1] + "," + dst[2] + ","
                + dst[3] + "," + dst[4] + "," + dst[5]);
            int[] dst0 = new int[3];
            src.CopyTo(dst0, 0);
            Console.WriteLine("copyto-int0: " + dst0[0] + "," + dst0[1] + "," + dst0[2]);
        }

        private static void CopyToRefArray()
        {
            Box[] b = { new Box(1), new Box(2) };
            Box[] dst = new Box[3];
            b.CopyTo(dst, 1);
            Console.WriteLine("copyto-ref: " + (dst[0] == null ? "null" : "x") + "," + dst[1].V + "," + dst[2].V);
        }

        private static void IndexOfRefType()
        {
            Box o1 = new Box(1);
            Box o2 = new Box(2);
            Box o3 = new Box(1); // value-equal to o1 but a distinct reference
            Box[] arr = { o1, o2 };
            Console.WriteLine("idxref-found: " + Array.IndexOf(arr, o2));        // 1
            Console.WriteLine("idxref-refeq: " + Array.IndexOf(arr, o3));        // -1 (reference equality)
            Console.WriteLine("idxref-missing: " + Array.IndexOf(arr, new Box(9))); // -1
            Box[] arrn = { o1, null, o2 };
            Console.WriteLine("idxref-null: " + Array.IndexOf(arrn, (Box)null)); // 1 (null matches null slot)
            Console.WriteLine("idxref-null-absent: " + Array.IndexOf(arr, (Box)null)); // -1
            Box[] dup = { o1, o2, o1 };
            Console.WriteLine("idxref-dup: " + Array.IndexOf(dup, o1));          // 0 (first index)
        }

        private static void IndexOfViaList()
        {
            // List<T>.IndexOf funnels into Array.IndexOf<T> — the exact dn2cpp gap host
            // shape (List<ClassInfo>.IndexOf / List<MethodInfo>.IndexOf).
            Box o1 = new Box(1);
            Box o2 = new Box(2);
            Box o3 = new Box(1);
            List<Box> list = new List<Box> { o1, o2 };
            Console.WriteLine("idxlist: " + list.IndexOf(o2) + "," + list.IndexOf(o3) + "," + list.Contains(o1));
        }

        private static void IndexOfInt()
        {
            int[] a = { 5, 6, 7, 6 };
            Console.WriteLine("idxint: " + Array.IndexOf(a, 6) + "," + Array.IndexOf(a, 42));
        }
    }
}
