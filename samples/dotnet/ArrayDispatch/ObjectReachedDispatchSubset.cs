#nullable disable
using System;
using System.Collections;
using System.Collections.Generic;

// Collection-interface DISPATCH on an array the emitter never saw as one — the receiver
// reaches the call typed `object` (or comes out of a runtime helper), so no
// statically-visible cast/boundary wired the lazy per-element map. Served by the runtime's
// shared reference-element SZArray fallback dispatch table (registered at init off the
// object-element map) plus the ref-erased wrapper arm for the enumerator it hands back:
//  - non-generic IEnumerable/ICollection/IList on a Pearl[] behind `object` — the
//    non-generic castclass notes nothing (only generic targets and static-array
//    boundaries note), so only the fallback can serve it;
//  - the invariant IList<Base>/generic dispatch over a Derived[] (array covariance —
//    variance matching cannot serve an invariant interface, only the fallback can);
//  - attribute arrays: the runtime materializes them (no newarr site), stamps the
//    typed forms with the filter's precise T[] identity (dn2cpp_find_array_ti), and
//    the static Attribute[] forms retag by their static element — the Thrive
//    Newtonsoft startup-blocker shape (Attribute[] enumerated as IEnumerable<Attribute>).
// Value-element arrays reached this way are now closed too — each noted value element
// gets its per-element map eagerly — and are exercised by
// ObjectReachedValueDispatchSubset, not here. CoreLib only. Diffed exact vs .NET.

namespace ObjectReachedDispatchSubset
{
    [AttributeUsage(AttributeTargets.All, AllowMultiple = false)]
    sealed class MarkerAttribute : Attribute
    {
        public override string ToString() { return "Marker"; }
    }

    class Animal
    {
        public string Name;
        public Animal(string name) { Name = name; }
        public override string ToString() { return "Animal:" + Name; }
    }

    sealed class Cat : Animal
    {
        public Cat(string name) : base(name) { }
        public override string ToString() { return "Cat:" + Name; }
    }

    // A reference element used ONLY behind `object` — no cast or boundary anywhere in
    // the program names Pearl, so its array can never get a per-element map: the
    // non-generic section below genuinely rides the shared fallback table.
    sealed class Pearl
    {
        public string Tag;
        public Pearl(string tag) { Tag = tag; }
        public override string ToString() { return "Pearl:" + Tag; }
    }

    [Marker]
    static class Program
    {
        // The array is created here and immediately forgotten behind `object` — no
        // cast in this method, so nothing notes the element.
        static object MakePearls()
        {
            return new Pearl[] { new Pearl("alpha"), new Pearl("beta"), new Pearl("gamma") };
        }

        static object MakeCats()
        {
            return new Cat[] { new Cat("mia"), new Cat("tom") };
        }

        // Non-generic trio on a Pearl[] reached through object. The non-generic
        // castclass wires no per-element map — this dispatch rides the fallback.
        static void NonGenericOnObject(object o)
        {
            IEnumerable e = (IEnumerable)o;
            string all = "";
            foreach (object x in e)
                all += "[" + x + "]";
            Console.WriteLine("ng-each=" + all);
            ICollection c = (ICollection)o;
            Console.WriteLine("ng-count=" + c.Count);
            IList l = (IList)o;
            object p1 = l[1];
            Console.WriteLine("ng-idx1=" + p1 + " ng-contains=" + l.Contains(p1)
                + " ng-miss=" + l.Contains(new Pearl("delta")) + " ng-indexOf=" + l.IndexOf(p1));
        }

        // Invariant IList<Animal> over a Cat[] (array covariance): the runtime array's
        // own rows are Cat-keyed and variance cannot serve an invariant interface —
        // only the covariant fallback arm can.
        static void InvariantCovariantList(object o)
        {
            IList<Animal> list = (IList<Animal>)o;
            Console.WriteLine("cov-count=" + list.Count + " cov-idx0=" + list[0]);
            IReadOnlyList<Animal> ro = (IReadOnlyList<Animal>)o;
            Console.WriteLine("cov-ro1=" + ro[1]);
            string all = "";
            foreach (Animal a in (IEnumerable<Animal>)o)
                all += "<" + a + ">";
            Console.WriteLine("cov-each=" + all);
        }

        // Attribute arrays are runtime-materialized (no newarr, no boundary): the
        // static Attribute[] forms enumerate as IEnumerable<Attribute> (the Thrive
        // shape), and the typed filter form carries the precise T[] runtime identity.
        static void AttributeArrays()
        {
            Attribute[] attrs = Attribute.GetCustomAttributes(typeof(Program));
            IEnumerable<Attribute> ea = attrs;
            string all = "";
            foreach (Attribute a in ea)
                all += "(" + a + ")";
            Console.WriteLine("attr-each=" + all + " attr-count=" + ((ICollection)attrs).Count);

            object[] typed = typeof(Program).GetCustomAttributes(typeof(MarkerAttribute), false);
            Console.WriteLine("attr-typed=" + (typed is MarkerAttribute[]) + " attr-n=" + typed.Length);
            string tall = "";
            foreach (object a in (IEnumerable)typed)
                tall += "{" + a + "}";
            Console.WriteLine("attr-typed-each=" + tall);
        }

        public static void Run()
        {
            NonGenericOnObject(MakePearls());
            InvariantCovariantList(MakeCats());
            AttributeArrays();
        }
    }
}
