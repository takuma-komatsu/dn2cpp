#nullable disable
using System;

namespace JaggedMdArraySubset
{
    // An SZArray whose ELEMENT is an MD array needs a precise array type-info whose
    // elementType is a linkable constant — the emitter's static ti_md_<T> — and the
    // runtime interner must answer that SAME handle for every `new T[,]` of the shape, or
    // GetType()/typeof identity splits in two. The typeof lines pin the mirror case: an MD
    // array whose element is an SZArray.
    internal static class Program
    {
        internal static void Run()
        {
            var a = new int[2][,];
            a[0] = new int[2, 3];
            a[0][1, 2] = 42;
            Console.WriteLine(a[0][1, 2]);                          // 42
            Console.WriteLine(a[1] is null);                        // True
            Console.WriteLine(a.GetType().Name);                    // Int32[,][]
            Console.WriteLine(a.GetType().FullName);                // System.Int32[,][]
            Console.WriteLine(typeof(int[][,]).Name);               // Int32[,][]
            Console.WriteLine(typeof(int[,][]).Name);               // Int32[][,]
            Console.WriteLine(a.GetType() == typeof(int[][,]));     // True
            Console.WriteLine(a[0].GetType() == typeof(int[,]));    // True
            Console.WriteLine(a.GetType().GetElementType().Name);   // Int32[,]
            Console.WriteLine(a.GetType().GetElementType() == a[0].GetType());               // True
            Console.WriteLine(a.GetType().GetElementType().GetElementType() == typeof(int)); // True
            object o = a;
            Console.WriteLine(o is int[][,]);                       // True
            Console.WriteLine(o is string[][,]);                    // False
            Console.WriteLine(o is object[]);                       // True (MD elements are references)
            var cast = (int[][,])o;
            Console.WriteLine(cast[0][1, 2]);                       // 42
            // The outer window is a reference-rep SZArray: Array.Copy shares the
            // inner MD instances (.NET's shallow copy).
            var b2 = new int[2][,];
            Array.Copy(a, b2, 2);
            Console.WriteLine(ReferenceEquals(b2[0], a[0]));        // True
            // Array.CreateInstance over the MD element type must hand back the same
            // runtime identity the emitted handle carries.
            var ci = Array.CreateInstance(typeof(int[,]), 2);
            Console.WriteLine(ci.GetType() == typeof(int[][,]));    // True
            // The other nesting allocated for real: an MD array OF SZArrays, whose
            // interned identity rides the element's precise ti_arr_ handle.
            var m = new int[2, 2][];
            m[1, 1] = new[] { 7, 8, 9 };
            Console.WriteLine(m[1, 1][2]);                          // 9
            Console.WriteLine(m.GetType() == typeof(int[,][]));     // True
            Console.WriteLine(m.GetType().GetElementType() == typeof(int[])); // True
            // Reference-element flavour of the same shape.
            var s = new string[1][,];
            s[0] = new string[1, 1];
            s[0][0, 0] = "x";
            Console.WriteLine(s[0][0, 0]);                          // x
            Console.WriteLine(s.GetType().Name);                    // String[,][]
        }
    }
}
