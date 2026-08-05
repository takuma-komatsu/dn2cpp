#nullable disable
using System;

// Precise array covariance on the per-element array type-infos.
// dn2cpp_isinst decides an array-to-array cast by element assignability: a reference-
// element array is covariant (string[] is an object[], Dog[] is an Animal[]); a value-
// element array is invariant (int[] is not object[]). A valid covariant up/down-cast
// succeeds; the `is` predicate answers covariance precisely; and the Span<T> array-
// covariance guard rejects a Span<object> over a string[] while accepting one over a
// genuine object[] — all matching real .NET. (Catching an InvalidCastException from a
// hard cast is a separate, unmodeled feature, so invalid downcasts are probed via `is`.)

namespace ArrayCovarianceSubset
{
    class Animal { public virtual string N() { return "animal"; } }
    class Dog : Animal { public override string N() { return "dog"; } }

    static class Program
    {internal static int Run()
        {
            // Covariant upcast (implicit reference conversion): string[] -> object[].
            string[] sa = new string[] { "a", "b" };
            object[] oa = sa;
            Console.WriteLine("upcast " + oa.Length + " " + (string)oa[0]);

            // Explicit covariant cast Dog[] -> Animal[], then a virtual call through it.
            Dog[] dogs = new Dog[] { new Dog() };
            Animal[] animals = (Animal[])dogs;
            Console.WriteLine("covcast " + animals[0].N());

            // `is` covariance: string[] IS object[] (covariant); a value int[] is NOT.
            object o1 = new string[1];
            object o2 = new int[1];
            Console.WriteLine("strIsObjArr=" + (o1 is object[]) + " intIsObjArr=" + (o2 is object[]) +
                " strIsStrArr=" + (o1 is string[]) + " intIsIntArr=" + (o2 is int[]));

            // Valid downcast: an object[] holding a real string[] -> string[].
            object boxed = sa;
            string[] back = (string[])boxed;
            Console.WriteLine("downcast " + back[1]);

            // Invalid downcast, probed via `is` (a genuine object[] is not a string[]).
            object genuine = new object[] { new object() };
            Console.WriteLine("badDowncast=" + (genuine is string[]));

            // Span<T> covariance guard: a Span<object> over a string[] (arriving typed as
            // object[]) is rejected at construction — the array's exact type != object[].
            object[] cov = new string[] { "x", "y", "z" };
            try { Span<object> sp = cov; Console.WriteLine("span ok " + sp.Length); }
            catch (Exception) { Console.WriteLine("span rejected"); }

            // A Span<object> over a genuine object[] is fine.
            object[] real = new object[] { "p", "q" };
            Span<object> sp2 = real;
            Console.WriteLine("span ok " + sp2.Length);
            return 0;
        }
    }
}
