#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;

// Generic variance on ORDINARY types, not just arrays: an interface dispatch that lands
// on a row the receiver's class implements at a DIFFERENT instantiation of the same
// generic definition.
//
// The runtime has always resolved these (dn2cpp_itf_variant_match walks the receiver's
// interface rows when the exact one misses), but the transpiler's reachability closure
// matched interfaces by reference identity and their methods by SigKey — and variance is
// exactly the case where neither holds: Box<Cat> does not implement ICovariant<Animal>
// by identity, and ICovariant<Animal>::Get is `Get():Animal` while Box<Cat>::Get is
// `Get():Cat`. Nothing reached the body, the slot was emitted null, and the program
// jumped through it. So this section pins every direction of it:
//
//   - user-defined `out T`  (Box<Cat> reached through ICovariant<Animal>)
//   - user-defined `in T`   (AnimalDescriber reached through IContravariant<Cat>)
//   - BCL covariance        (List<Cat> reached through IEnumerable<Animal>)
//   - BCL contravariance    (IComparer<in T>: an IComparer<Animal> sorting a List<Cat>)
//   - a two-parameter def   (IGrouping<out TKey, out TElement> — the old runtime match
//                            was arity-1-only, so a 2-param variant def missed outright)
//   - `is`/`as` through the variant conversion (the isinst side of the same rule)
//
// and the C# 9 COVARIANT RETURN, which is the same root a level down: `override Dog
// Clone()` overriding `virtual Animal Clone()` is compiled as a NEWSLOT virtual plus a
// MethodImpl row, precisely because the runtime's implicit override matching is
// signature-exact and the return type has narrowed. A vtable builder that only does the
// implicit match gives it a slot of its own and a call through the base type keeps
// dispatching the base body — a silently wrong answer, so both receivers are printed.
//
// CoreLib only; deterministic.

namespace GenericVarianceDispatchSubset
{
    internal class Animal
    {
        internal virtual string Name { get { return "animal"; } }
        // C# 9 covariant return: the base declaration.
        internal virtual Animal Clone() { return new Animal(); }
        public override string ToString() { return "Animal(" + Name + ")"; }
    }

    internal sealed class Cat : Animal
    {
        internal override string Name { get { return "cat"; } }
        // The narrowed override. Roslyn emits `newslot virtual instance Cat Clone()`
        // with `.override Animal::Clone` — the row IS the binding.
        internal override Cat Clone() { return new Cat(); }
        internal string Purr() { return "purr"; }
        public override string ToString() { return "Cat(" + Name + ")"; }
    }

    // out T: a producer. Box<Cat> is an ICovariant<Animal>.
    internal interface ICovariant<out T>
    {
        T Get();
    }

    internal sealed class Box<T> : ICovariant<T>
    {
        private readonly T _value;
        internal Box(T value) { _value = value; }
        public T Get() { return _value; }
    }

    // in T: a consumer. IContravariant<Animal> is an IContravariant<Cat>.
    internal interface IContravariant<in T>
    {
        string Describe(T value);
    }

    internal sealed class AnimalDescriber : IContravariant<Animal>
    {
        public string Describe(Animal value) { return "described:" + value.Name; }
    }

    // Both directions on one definition, and two parameters — the shape that used to
    // miss the runtime's arity-1 covariant match outright.
    internal interface ITransform<in TIn, out TOut>
    {
        TOut Apply(TIn value);
    }

    internal sealed class NameOf : ITransform<Animal, string>
    {
        public string Apply(Animal value) { return "name:" + value.Name; }
    }

    // Nested variance: a describer OF describers. IContravariant<IContravariant<Cat>>
    // — a consumer whose consumed type is itself a variant instantiation. Contravariance
    // flips at each level, so two levels net forward: this IS an
    // IContravariant<IContravariant<Animal>>.
    internal sealed class DescribeDescriber : IContravariant<IContravariant<Cat>>
    {
        public string Describe(IContravariant<Cat> value) { return "meta:" + value.Describe(new Cat()); }
    }

    // The other direction of the nesting, built only to prove the cast is one-way:
    // an IContravariant<IContravariant<Animal>> is NOT an IContravariant<IContravariant<Cat>>.
    internal sealed class DescribeAnimalDescriber : IContravariant<IContravariant<Animal>>
    {
        public string Describe(IContravariant<Animal> value) { return "meta2:" + value.Describe(new Animal()); }
    }

    // A contravariant BCL comparer: an IComparer<Animal> is an IComparer<Cat>, so it can
    // sort a List<Cat>.
    internal sealed class ByNameLength : IComparer<Animal>
    {
        public int Compare(Animal x, Animal y)
        {
            int c = x.Name.Length.CompareTo(y.Name.Length);
            return c != 0 ? c : string.CompareOrdinal(x.Name, y.Name);
        }
    }

    internal static class Program
    {
        internal static void Run()
        {
            Console.WriteLine("-- generic variance dispatch --");

            // out T, user-defined. The interface variable is typed at the BASE, the
            // object implements it at the DERIVED — the dispatch resolves the row
            // covariantly and calls Box<Cat>::Get through it.
            ICovariant<Cat> catBox = new Box<Cat>(new Cat());
            ICovariant<Animal> animalBox = catBox;
            Console.WriteLine("covariant get: " + animalBox.Get().Name);
            Console.WriteLine("covariant tostring: " + animalBox.Get());

            // The same through object, so the cast itself (isinst) goes through the
            // variant rule too, not just the dispatch.
            object boxed = new Box<Cat>(new Cat());
            ICovariant<Animal> viaCast = boxed as ICovariant<Animal>;
            Console.WriteLine("covariant as: " + (viaCast is null ? "null" : viaCast.Get().Name));
            Console.WriteLine("covariant is: " + (boxed is ICovariant<Animal>));
            // A Box<Animal> is NOT an ICovariant<Cat> — variance is one-way.
            object animalBoxed = new Box<Animal>(new Animal());
            Console.WriteLine("covariant reverse is: " + (animalBoxed is ICovariant<Cat>));

            // in T, user-defined. The object implements it at the BASE, the interface
            // variable is typed at the DERIVED.
            IContravariant<Animal> describer = new AnimalDescriber();
            IContravariant<Cat> catDescriber = describer;
            Console.WriteLine("contravariant: " + catDescriber.Describe(new Cat()));
            object od = new AnimalDescriber();
            Console.WriteLine("contravariant is: " + (od is IContravariant<Cat>));

            // Both directions on one two-parameter definition.
            ITransform<Animal, string> t = new NameOf();
            ITransform<Cat, object> narrowed = t;
            Console.WriteLine("in/out transform: " + narrowed.Apply(new Cat()));

            // BCL covariance: List<Cat> is an IEnumerable<Animal>, and the enumerator it
            // hands back (a boxed List<Cat>.Enumerator) is resolved as IEnumerator<Animal>
            // by the same rule — Current, MoveNext and Dispose all land on variant rows.
            List<Cat> cats = new List<Cat> { new Cat(), new Cat() };
            IEnumerable<Animal> animals = cats;
            int seen = 0;
            foreach (Animal a in animals)
            {
                seen++;
                Console.WriteLine("bcl covariant elem " + seen + ": " + a.Name);
            }
            Console.WriteLine("bcl covariant count: " + seen);

            // IReadOnlyList<out T> too, and through a cast rather than an assignment.
            IReadOnlyList<Animal> ro = (IReadOnlyList<Animal>)(object)cats;
            Console.WriteLine("bcl readonly[0]: " + ro[0].Name + " of " + ro.Count);

            // BCL contravariance: IComparer<in T>. An IComparer<Animal> sorts a List<Cat>.
            List<Cat> toSort = new List<Cat> { new Cat(), new Cat() };
            toSort.Sort(new ByNameLength());
            Console.WriteLine("bcl contravariant sort: " + toSort.Count + " " + toSort[0].Name);

            // Two-parameter BCL variance: IGrouping<out TKey, out TElement>. The grouping
            // is an IGrouping<string, Cat>; it is read as an IGrouping<object, Animal>.
            var groups = cats.GroupBy(c => c.Name).ToList();
            foreach (IGrouping<string, Cat> g in groups)
            {
                IGrouping<object, Animal> wide = g;
                Console.WriteLine("bcl grouping: " + wide.Key + " x" + wide.Count());
            }

            // C# 9 covariant return. Both receivers: through the BASE type (the vtable
            // slot the .override row binds) and through the DERIVED type (the direct
            // callvirt). Real .NET answers Cat for both; a vtable that missed the
            // MethodImpl row answers Animal for the first and never says so.
            Animal asAnimal = new Cat();
            Animal clonedViaBase = asAnimal.Clone();
            Console.WriteLine("covariant return via base: " + clonedViaBase);
            Console.WriteLine("covariant return via base name: " + clonedViaBase.Name);
            Console.WriteLine("covariant return via base is Cat: " + (clonedViaBase is Cat));

            Cat asCat = new Cat();
            Cat clonedViaDerived = asCat.Clone();
            Console.WriteLine("covariant return via derived: " + clonedViaDerived);
            // The narrowed static type is the whole point of the feature: no cast needed.
            Console.WriteLine("covariant return via derived purr: " + clonedViaDerived.Purr());

            // ---- nested variance ----
            //
            // A variant type ARGUMENT that is itself a variant interface instantiation. Real
            // .NET assigns ICovariant<ICovariant<Cat>> to ICovariant<ICovariant<Animal>>: the
            // outer `out T` moves the argument covariantly, and the argument moves too because
            // it is itself an `out T` interface. dn2cpp's variance check was one-level-deep on
            // both sides — the transpiler reached no body for the nested dispatch (a trap
            // slot) and the runtime cast answered false. Every answer below is pinned against
            // real dotnet by the diff gate.
            Console.WriteLine("-- nested variance --");

            // Covariant-in-covariant, pure form: the argument IS an instantiation of the same
            // definition (Box<ICovariant<Cat>> implements ICovariant<ICovariant<Cat>>). The
            // .Get().Get() chain dispatches through two variant rows, the outer reachable ONLY
            // via the nested rule — before the fix it landed on the trap stub.
            ICovariant<ICovariant<Cat>> nestedCat = new Box<ICovariant<Cat>>(new Box<Cat>(new Cat()));
            ICovariant<ICovariant<Animal>> nestedAnimal = nestedCat;
            Console.WriteLine("nested covariant get.get: " + nestedAnimal.Get().Get().Name);

            object nb = new Box<ICovariant<Cat>>(new Box<Cat>(new Cat()));
            Console.WriteLine("nested is: " + (nb is ICovariant<ICovariant<Animal>>));
            ICovariant<ICovariant<Animal>> nbAs = nb as ICovariant<ICovariant<Animal>>;
            Console.WriteLine("nested as: " + (nbAs is null ? "null" : nbAs.Get().Get().Name));
            // One-way: an ICovariant<ICovariant<Animal>> is NOT an ICovariant<ICovariant<Cat>>.
            object nbRev = new Box<ICovariant<Animal>>(new Box<Animal>(new Animal()));
            Console.WriteLine("nested reverse is: " + (nbRev is ICovariant<ICovariant<Cat>>));

            // The nested argument is a CLASS implementing the inner variant interface: Box<Cat>
            // is assignable to ICovariant<Animal>, so Box<Box<Cat>> is an
            // ICovariant<ICovariant<Animal>>. Exercises the interface-graph arm of the rule
            // (RefAssignable walking `from`'s interface closure / the runtime's interface map).
            object implNested = new Box<Box<Cat>>(new Box<Cat>(new Cat()));
            Console.WriteLine("impl-class nested is: " + (implNested is ICovariant<ICovariant<Animal>>));
            ICovariant<ICovariant<Animal>> implNestedAs = implNested as ICovariant<ICovariant<Animal>>;
            Console.WriteLine("impl-class nested get.get: " + implNestedAs.Get().Get().Name);

            // Contravariant nesting: `in T` flips direction at each level, so two levels net
            // forward — IContravariant<IContravariant<Cat>> IS an
            // IContravariant<IContravariant<Animal>> (real dotnet: True).
            IContravariant<IContravariant<Cat>> dd = new DescribeDescriber();
            IContravariant<IContravariant<Animal>> ddWide = dd;
            IContravariant<Animal> innerDesc = new AnimalDescriber();
            Console.WriteLine("nested contravariant: " + ddWide.Describe(innerDesc));
            object oddd = new DescribeDescriber();
            Console.WriteLine("nested contravariant is: " + (oddd is IContravariant<IContravariant<Animal>>));
            // The reverse is false: an ICon<ICon<Animal>> is not an ICon<ICon<Cat>>.
            object oddAnimal = new DescribeAnimalDescriber();
            Console.WriteLine("nested contravariant reverse is: " + (oddAnimal is IContravariant<IContravariant<Cat>>));

            // BCL nesting: List<List<string>> is an IEnumerable<IEnumerable<string>>, and the
            // inner element widens too — IEnumerable<IEnumerable<string>> is an
            // IEnumerable<IEnumerable<object>>. Both enumerator hops land on variant rows.
            List<List<string>> nestedList = new List<List<string>>
            {
                new List<string> { "a", "b" },
                new List<string> { "c" },
            };
            IEnumerable<IEnumerable<object>> wideSeq = (IEnumerable<IEnumerable<object>>)(object)nestedList;
            int outerRows = 0;
            foreach (IEnumerable<object> innerSeq in wideSeq)
            {
                outerRows++;
                int innerCount = 0;
                foreach (object o in innerSeq)
                    innerCount++;
                Console.WriteLine("bcl nested row " + outerRows + " count " + innerCount);
            }
            Console.WriteLine("bcl nested is: " + ((object)nestedList is IEnumerable<IEnumerable<object>>));
        }
    }
}
