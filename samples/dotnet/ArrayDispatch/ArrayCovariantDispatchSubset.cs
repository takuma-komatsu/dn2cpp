#nullable disable
using System;
using System.Collections.Generic;

// Covariant array -> IEnumerable<Base> interface dispatch. A Derived[] cast at
// runtime to IEnumerable<Base> (out-variant) resolves on the array: dn2cpp_isinst /
// dn2cpp_resolve_interface match the array's own IEnumerable<Derived> map covariantly
// (Derived reference-assignable to Base), the GetEnumerator returns an
// SZArrayEnumerable<Derived>, and IEnumerator<Base>.Current resolves covariantly on it —
// so foreach yields Derived elements seen as Base, with virtual calls dispatching to the
// real runtime type. string[] -> IEnumerable<object> works the same way; int[] (value
// element) is invariant and is NOT IEnumerable<object>. This is the general path the
// GetCustomAttributes<T> covariance point-fix needed. CoreLib only.

namespace ArrayCovariantDispatchSubset
{
    class Animal { public virtual string Speak() { return "?"; } }
    class Dog : Animal { public override string Speak() { return "woof"; } }
    class Cat : Animal { public override string Speak() { return "meow"; } }

    static class Program
    {
        // The array arrives as object, so the cast to IEnumerable<Animal> is a real
        // covariant castclass — the path the array's covariant interface map serves.
        static string SpeakAll(object boxedAnimalArray)
        {
            IEnumerable<Animal> e = (IEnumerable<Animal>)boxedAnimalArray;
            string r = "";
            foreach (Animal a in e)
                r += (r.Length == 0 ? "" : " ") + a.Speak();   // virtual dispatch on runtime type
            return r;
        }

        static int Count<T>(IEnumerable<T> xs)
        {
            int n = 0;
            foreach (T _ in xs)
                n++;
            return n;
        }internal static int Run()
        {
            object dogs = new Dog[] { new Dog(), new Dog() };
            object cats = new Cat[] { new Cat() };

            // Covariant Dog[] / Cat[] -> IEnumerable<Animal>, virtual Speak() through Base.
            Console.WriteLine("dogs=" + SpeakAll(dogs));     // woof woof
            Console.WriteLine("cats=" + SpeakAll(cats));     // meow

            // Covariant `is` predicate: Dog[] is IEnumerable<Animal> (and IEnumerable<Dog>).
            Console.WriteLine("dogIsAnimalEnum=" + (dogs is IEnumerable<Animal>) +
                " dogIsDogEnum=" + (dogs is IEnumerable<Dog>) +
                " dogIsCatEnum=" + (dogs is IEnumerable<Cat>));

            // string[] -> IEnumerable<object> (reference covariance); int[] is invariant.
            object strs = new string[] { "a", "b", "c" };
            object ints = new int[] { 1, 2 };
            Console.WriteLine("strIsObjEnum=" + (strs is IEnumerable<object>) +
                " intIsObjEnum=" + (ints is IEnumerable<object>));
            Console.WriteLine("objcount=" + Count<object>((IEnumerable<object>)strs));   // 3
            return 0;
        }
    }
}
