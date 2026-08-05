#nullable disable
using System;
using System.Collections;
using System.Collections.Generic;

// An array's CLR interface set must be answered by `is`/`castclass` INDEPENDENT of whether
// the lazy SZArray dispatch map was wired; tying the two makes `is IList` answer False for
// an array of an element nothing enumerates, which is a silent wrong answer.
//
// The element types tested for MAP-INDEPENDENCE (double, long, Season, Widget) are
// deliberately NEVER used as a collection anywhere in the ArrayDispatch program, so their
// dispatch map stays empty and these tests exercise the flag arms alone — do not add a
// cast, foreach or interface call over them. The cast-AND-call section at the end uses
// separate elements (sbyte / byte) that a visible call site wires, proving the map-free
// type test did not break dispatch.

namespace ArrayInterfaceSetSubset
{
    enum Season : int { Winter, Spring, Summer, Autumn }

    sealed class Widget { }

    static class Program
    {
        static void B(string label, bool v) => Console.WriteLine(label + "=" + (v ? "True" : "False"));

        internal static int Run()
        {
            // Arrays typed as `object` so every test is a runtime type test, never a
            // statically-wrapped boundary. None of these elements is enumerated elsewhere.
            object od = new double[3];
            object ol = new long[4];
            object omd = new double[2, 2];
            object oseason = new Season[2];
            object owidget = new Widget[2];

            Console.WriteLine("-- SZArray non-generic interfaces (un-enumerated) --");
            B("double[] is IList", od is IList);
            B("double[] is ICollection", od is ICollection);
            B("double[] is IEnumerable", od is IEnumerable);
            B("double[] is ICloneable", od is ICloneable);
            B("double[] is IStructuralComparable", od is IStructuralComparable);
            B("double[] is IStructuralEquatable", od is IStructuralEquatable);

            Console.WriteLine("-- SZArray generic collection interfaces (un-enumerated) --");
            B("double[] is IList<double>", od is IList<double>);
            B("double[] is ICollection<double>", od is ICollection<double>);
            B("double[] is IEnumerable<double>", od is IEnumerable<double>);
            B("double[] is IReadOnlyList<double>", od is IReadOnlyList<double>);
            B("double[] is IReadOnlyCollection<double>", od is IReadOnlyCollection<double>);
            B("double[] is IList<long>", od is IList<long>);
            B("double[] is IEnumerable<object>", od is IEnumerable<object>);

            Console.WriteLine("-- generic primitive/enum covariance --");
            B("long[] is IList<long>", ol is IList<long>);
            B("long[] is IList<ulong>", ol is IList<ulong>);   // same-width int equivalence
            B("long[] is IList<int>", ol is IList<int>);       // different width
            B("long[] is IEnumerable<ulong>", ol is IEnumerable<ulong>);
            B("Season[] is IList<int>", oseason is IList<int>);   // enum -> underlying
            B("Season[] is IList<uint>", oseason is IList<uint>);
            B("Season[] is IList<Season>", oseason is IList<Season>);
            B("Season[] is IList<long>", oseason is IList<long>);

            Console.WriteLine("-- reference element covariance --");
            B("Widget[] is IEnumerable<object>", owidget is IEnumerable<object>);
            B("Widget[] is IList<object>", owidget is IList<object>);
            B("Widget[] is ICollection<object>", owidget is ICollection<object>);
            B("Widget[] is IReadOnlyList<object>", owidget is IReadOnlyList<object>);
            B("Widget[] is IEnumerable<Widget>", owidget is IEnumerable<Widget>);

            Console.WriteLine("-- multidim array: non-generic yes, generic no --");
            B("double[,] is IList", omd is IList);
            B("double[,] is ICollection", omd is ICollection);
            B("double[,] is IEnumerable", omd is IEnumerable);
            B("double[,] is ICloneable", omd is ICloneable);
            B("double[,] is IStructuralEquatable", omd is IStructuralEquatable);
            B("double[,] is IList<double>", omd is IList<double>);
            B("double[,] is IEnumerable<double>", omd is IEnumerable<double>);

            Console.WriteLine("-- array-to-array: rank + primitive equivalence --");
            B("(object)long[] is long[]", ol is long[]);
            B("(object)long[] is long[,]", ol is long[,]);     // rank mismatch
            B("(object)double[,] is double[]", omd is double[]);
            B("(object)long[] is ulong[]", ol is ulong[]);     // primitive equivalence
            B("(object)long[] is int[]", ol is int[]);
            B("(object)Season[] is int[]", oseason is int[]);  // enum <-> underlying
            B("(object)Widget[] is object[]", owidget is object[]);

            Console.WriteLine("-- System.Array still holds --");
            B("double[] is Array", od is Array);
            B("double[,] is Array", omd is Array);

            // Cast-AND-CALL on statically-known arrays: the type test is map-free, but a
            // real interface CALL still needs the dispatch map, wired at the visible cast
            // site. sbyte is reached ONLY through a non-generic IList cast here (proving the
            // non-generic wiring), byte through the generic path.
            Console.WriteLine("-- cast and call (map wired at the cast site) --");
            sbyte[] sb = new sbyte[] { 10, 20, 30 };
            IList nl = (IList)sb;
            Console.WriteLine("nonGenCount=" + nl.Count);
            Console.WriteLine("nonGenItem1=" + nl[1]);
            IEnumerable ne = (IEnumerable)sb;
            int nsum = 0;
            foreach (object x in ne)
                nsum += (sbyte)x;
            Console.WriteLine("nonGenSum=" + nsum);

            byte[] by = new byte[] { 1, 2, 3, 4 };
            IList<byte> gl = (IList<byte>)by;
            Console.WriteLine("genCount=" + gl.Count);
            Console.WriteLine("genItem2=" + gl[2]);
            ICollection<byte> gc = (ICollection<byte>)by;
            Console.WriteLine("genColCount=" + gc.Count);
            int gsum = 0;
            foreach (byte v in (IEnumerable<byte>)by)
                gsum += v;
            Console.WriteLine("genSum=" + gsum);
            IReadOnlyList<byte> gr = (IReadOnlyList<byte>)by;
            Console.WriteLine("genRoItem0=" + gr[0]);
            return 0;
        }
    }
}
