#nullable disable
using System;

// Per-element array type-info foundation. Each SZArray element type reached via
// `newarr` / `typeof(T[])` gets a per-element Dn2CppTypeInfo (DN2CPP_TF_ARRAY, elementType,
// rank 1), registered under its CLR array name so Type.GetType("T[]") resolves to it and
// Type.GetElementType()/GetArrayRank() report the element type and rank. typeof / array
// allocation / covariance are unchanged.

namespace ReflectArrayTypeSubset
{
    struct Foo { public int X; }
    class Bar { public int Y; }

    static class Program
    {
        static void Dump(string name)
        {
            Type t = Type.GetType(name);
            if (t is null)
            {
                Console.WriteLine(name + " -> null");
                return;
            }
            Type et = t.GetElementType();
            Console.WriteLine(
                name + " IsArray=" + t.IsArray +
                " Name=" + t.Name +
                " FullName=" + t.FullName +
                " Ns=" + t.Namespace +
                " Rank=" + t.GetArrayRank() +
                " Elem=" + (et is null ? "<null>" : et.Name) +
                " ElemEqInt32=" + (et == typeof(int)));
        }internal static int Run()
        {
            // Allocate / reference each element type so its array type-info is reached.
            int[] a = new int[2]; a[0] = 1;
            string[] b = new string[1]; b[0] = "x";
            double[] c = new double[1]; c[0] = 1.0;
            object[] d = new object[1];
            Foo[] e = new Foo[1]; e[0].X = 7;
            Bar[] f = new Bar[1]; f[0] = new Bar();
            int[][] g = new int[1][]; g[0] = a;
            Console.WriteLine("lens " + a.Length + b.Length + c.Length + d.Length + e.Length + f.Length + g.Length);

            Dump("System.Int32[]");
            Dump("System.String[]");
            Dump("System.Double[]");
            Dump("System.Object[]");
            Dump("ReflectArrayTypeSubset.Foo[]");
            Dump("ReflectArrayTypeSubset.Bar[]");
            Dump("System.Int32[][]");

            // GetElementType chain on the jagged type, and GetArrayRank on the inner array.
            Type jag = Type.GetType("System.Int32[][]");
            Type inner = jag.GetElementType();
            Console.WriteLine("jag.Elem=" + inner.Name + " innerRank=" + inner.GetArrayRank() +
                " inner.Elem=" + inner.GetElementType().Name);

            // GetArrayRank on a NON-array is a fault, and a catchable one:
            // real .NET raises ArgumentException ("Must be an array type.",
            // measured on CoreCLR), where the runtime used to reach dn2cpp_fail
            // and abort. A reflection walk over a table of types
            // it does not control asks this question of every row, so one
            // non-array row must not end the process. Type name only — the
            // message is localized.
            //
            // The rows are an SZArray, a jagged array and two non-arrays. A
            // rank-2 `typeof(string[,])` row is deliberately NOT here: it makes
            // the walk raise a NullReferenceException out of dn2cpp_type_require
            // before GetArrayRank can answer anything, so it escapes the
            // ArgumentException handler and ends the run. That is a separate
            // gap — multi-dimensional array Type handles, which nothing in this
            // bucket reflects over — and putting it in this probe would assert
            // the wrong thing about the conversion under test.
            Type[] rows = { typeof(int[]), typeof(int), typeof(int[][]), typeof(string) };
            int ranked = 0, notArray = 0;
            foreach (Type t in rows)
            {
                try
                {
                    ranked += t.GetArrayRank();
                }
                catch (ArgumentException)
                {
                    notArray++;
                }
            }
            Console.WriteLine("rank walk: ranked=" + ranked + " notArray=" + notArray);
            try
            {
                Console.WriteLine("unreachable " + typeof(int).GetArrayRank());
            }
            catch (ArgumentException ae)
            {
                Console.WriteLine("typed catch: " + ae.GetType().Name);
            }
            return 0;
        }
    }
}
