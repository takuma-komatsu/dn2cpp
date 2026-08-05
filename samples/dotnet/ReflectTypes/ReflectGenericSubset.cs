#nullable disable
using System;
using System.Collections;
using System.Collections.Generic;

// Generic reflection. A closed generic instantiation carries its
// open-definition handle + closed type arguments, so IsGenericType /
// IsGenericTypeDefinition / IsConstructedGenericType / ContainsGenericParameters,
// GetGenericTypeDefinition (a shared handle across closes), GetGenericArguments, and
// MakeGenericType all answer at runtime. Like Unity IL2CPP, MakeGenericType only resolves
// instantiations the AOT reachability analysis already generated — an ungenerated close
// throws NotSupportedException rather than being built at runtime. CoreLib only; diffed
// exact vs real .NET.

namespace ReflectGenericSubset
{
    class Box<T>
    {
        public T Value;
        public Box(T v) { Value = v; }
    }

    // An app-module generic INTERFACE with a reachability-generated close (Holder<int>
    // IHolder<int>), so typeof(IHolder<>) resolves to a NATURAL gendef minted off the
    // closed instantiation — its handle must carry the definition's INTERFACE/ABSTRACT
    // kind bits, not just DN2CPP_TF_GENERICDEF.
    interface IHolder<T> { T Held { get; } }

    // An app-module generic interface NO close of which the program ever instantiates,
    // so typeof(IPairKind<,>) hits the SYNTHETIC gendef pass (no ClassInfo to key off) —
    // that path must stamp the same kind bits.
    interface IPairKind<K, V> { }

    class Holder<T> : IHolder<T>
    {
        public T Held { get; }
        public Holder(T v) { Held = v; }
    }

    class Plain { }

    // Interface-source reflection (relation-only interface tables): an app-module
    // interface pair with inheritance, an abstract carrier of a BCL interface
    // closure, and its concrete leaf that adds nothing of its own.
    interface IItfA { void A(); }
    interface IItfB : IItfA { void B(); }
    class ItfC : IItfB
    {
        public void A() { }
        public void B() { }
    }

    abstract class AbstractRo : IReadOnlyList<int>
    {
        public int this[int index] => index;
        public int Count => 5;
        public IEnumerator<int> GetEnumerator() { yield return 0; }
        IEnumerator IEnumerable.GetEnumerator() { yield return 0; }
    }

    class ConcreteRo : AbstractRo { }

    // Residue coverage for the relation-only interface tables.
    // IStructItf is implemented by a value type the program NEVER boxes — every
    // call on it is a direct/constrained call — so the only reason its interface
    // table exists is reflection.
    interface IStructItf { int Val(); }

    struct NeverBoxedStruct : IStructItf
    {
        public int Val() { return 42; }
    }

    // An interface named by NOTHING but a typeof token: no variable is typed at
    // it, no class implements it, nothing is cast to it. It reaches the emitter
    // through ReferencedTypes only.
    interface IOpaqueItf : IItfA { }

    class Program
    {
        static void Show(string label, Type t)
        {
            Console.WriteLine(label + " Name=" + t.Name
                + " IsGeneric=" + t.IsGenericType
                + " IsDef=" + t.IsGenericTypeDefinition
                + " Constructed=" + t.IsConstructedGenericType
                + " Contains=" + t.ContainsGenericParameters);
        }internal static void Run()
        {
            // Force the closed instantiations to be reachability-generated.
            Box<int> bi = new Box<int>(1);
            Box<string> bs = new Box<string>("x");
            List<int> li = new List<int>();
            li.Add(1);
            Dictionary<string, int> dd = new Dictionary<string, int>();
            dd["a"] = 1;
            Console.WriteLine("seed " + bi.Value + bs.Value + li.Count + dd.Count);

            Type boxInt = typeof(Box<int>);
            Type boxStr = typeof(Box<string>);
            Type listInt = typeof(List<int>);
            Type dict = typeof(Dictionary<string, int>);
            Type plain = typeof(Plain);

            Show("boxInt", boxInt);
            Show("listInt", listInt);
            Show("dict", dict);
            Show("plain", plain);

            // The generic type definition: a shared handle across closes (== compares equal).
            Type def = boxInt.GetGenericTypeDefinition();
            Console.WriteLine("def Name=" + def.Name + " FullName=" + def.FullName
                + " IsDef=" + def.IsGenericTypeDefinition
                + " Contains=" + def.ContainsGenericParameters
                + " IsGeneric=" + def.IsGenericType
                + " Constructed=" + def.IsConstructedGenericType);
            Console.WriteLine("def shared = " + (def == boxStr.GetGenericTypeDefinition()));

            // The closed type arguments.
            Type[] la = listInt.GetGenericArguments();
            Console.WriteLine("listArgs len=" + la.Length + " [0]=" + la[0].Name);
            Type[] da = dict.GetGenericArguments();
            Console.WriteLine("dictArgs len=" + da.Length + " [0]=" + da[0].Name + " [1]=" + da[1].Name);
            Console.WriteLine("plainArgs len=" + plain.GetGenericArguments().Length);

            // MakeGenericType over an AOT-generated instantiation.
            Type made = def.MakeGenericType(typeof(string));
            Console.WriteLine("made Name=" + made.Name + " ==boxStr=" + (made == boxStr));

            // MakeGenericType over an instantiation AOT never generated -> NotSupportedException.
            try
            {
                Type bad = def.MakeGenericType(typeof(double));
                Console.WriteLine("made double Name=" + bad.Name);
            }
            catch (NotSupportedException)
            {
                Console.WriteLine("Box<double> not generated: NotSupportedException");
            }

            // GetGenericTypeDefinition on a non-generic type -> InvalidOperationException.
            try
            {
                plain.GetGenericTypeDefinition();
                Console.WriteLine("plain def: (no throw)");
            }
            catch (InvalidOperationException)
            {
                Console.WriteLine("plain def: InvalidOperationException");
            }

            // Open generic DEFINITIONS taken directly with typeof(Def<>) — the operand
            // carries no type arguments. dn2cpp lowers such an ldtoken to the SAME shared
            // open-definition handle a closed instantiation's GetGenericTypeDefinition()
            // returns, so typeof over one is non-null, reports the backtick FullName,
            // answers IsGenericTypeDefinition, and compares == to a close's definition. Both
            // an app-module definition (Box<>) and a cross-assembly one (IDictionary<,>) —
            // and, right beside them, the shape a JSON serializer's
            // ImplementsGenericDefinition drives: GetInterfaces() + IsGenericType +
            // GetGenericTypeDefinition() == typeof(IDictionary<,>).
            Type openBox = typeof(Box<>);
            Console.WriteLine("openBox FullName=" + openBox.FullName + " IsDef=" + openBox.IsGenericTypeDefinition
                + " IsGeneric=" + openBox.IsGenericType + " Contains=" + openBox.ContainsGenericParameters
                + " Constructed=" + openBox.IsConstructedGenericType);
            Console.WriteLine("openBox == boxInt.GTD: " + (openBox == boxInt.GetGenericTypeDefinition()));
            Type openDict = typeof(Dictionary<,>);
            Console.WriteLine("openDict FullName=" + openDict.FullName
                + " == dict.GTD: " + (openDict == dict.GetGenericTypeDefinition()));
            Type openIDict = typeof(IDictionary<,>);
            Console.WriteLine("openIDict FullName=" + openIDict.FullName
                + " IsDef=" + openIDict.IsGenericTypeDefinition + " distinct=" + (openIDict != openDict));
            Console.WriteLine("Impl(dict, IDictionary<,>)=" + ImplementsGenericDefinition(dd.GetType(), typeof(IDictionary<,>)));
            Console.WriteLine("Impl(list, IList<>)=" + ImplementsGenericDefinition(li.GetType(), typeof(IList<>)));
            Console.WriteLine("Impl(list, IDictionary<,>)=" + ImplementsGenericDefinition(li.GetType(), typeof(IDictionary<,>)));
            Console.WriteLine("Impl(list, IEnumerable<>)=" + ImplementsGenericDefinition(li.GetType(), typeof(IEnumerable<>)));

            // An open generic definition's KIND flags — IsInterface / IsAbstract /
            // IsClass / IsSealed / IsValueType — must answer the same as real .NET. A
            // gendef emit route that stamps only DN2CPP_TF_GENERICDEF makes
            // typeof(IDictionary<,>).IsInterface read false, which trips the
            // `if (!IsInterface || !IsGenericTypeDefinition) throw` front-guard real
            // serializers open with. Cover a NATURAL gendef minted off a closed
            // instantiation —
            // an app interface (IHolder<>), an app class (Box<>), a cross-assembly
            // interface (IDictionary<,>) and class (Dictionary<,>) — and a SYNTHETIC one
            // the program instantiates no close of (app interface IPairKind<,>).
            var holder = new Holder<int>(7);
            IHolder<int> ih = holder;
            Console.WriteLine("holder held=" + ih.Held);
            ShowKind("openHolderItf(nat,app-itf)", typeof(IHolder<>));
            ShowKind("openBoxCls(nat,app-cls)", typeof(Box<>));
            ShowKind("openIDict(nat,x-asm-itf)", typeof(IDictionary<,>));
            ShowKind("openDictCls(nat,x-asm-cls)", typeof(Dictionary<,>));
            ShowKind("openPairKind(syn,app-itf)", typeof(IPairKind<,>));

            // The exact Newtonsoft front-check: a generic interface definition must pass,
            // a generic class definition must be rejected as "not a generic interface
            // definition" — the branch that threw for Thrive.
            Console.WriteLine("front(IDictionary<,>)=" + ImplementsGenericDefinitionGuarded(dd.GetType(), typeof(IDictionary<,>)));
            Console.WriteLine("front(IHolder<>)=" + ImplementsGenericDefinitionGuarded(holder.GetType(), typeof(IHolder<>)));
            Console.WriteLine("front(Box<>)=" + ImplementsGenericDefinitionGuarded(bi.GetType(), typeof(Box<>)));

            RunItfSource();
        }

        // Interface / abstract-class SOURCE reflection. RenderItfTables emits a
        // relation-only interface table (rows with nullptr slots) for interfaces
        // and abstract classes so IsAssignableFrom / GetInterfaces /
        // IsInstanceOfType answer over them. This is the exact question
        // Newtonsoft's DefaultContractResolver.CreateContract asks about an
        // interface-typed member — typeof(IEnumerable).IsAssignableFrom(
        // typeof(IReadOnlyList<int>)) — and a False here classified every such
        // collection as an ObjectContract (blocked Thrive's deserialization).
        static void RunItfSource()
        {
            // BCL interface source, non-generic target (the Newtonsoft test).
            Bl("IEnumerable <- IReadOnlyList<int>", typeof(IEnumerable).IsAssignableFrom(typeof(IReadOnlyList<int>)));
            Bl("IEnumerable <- IList<int>", typeof(IEnumerable).IsAssignableFrom(typeof(IList<int>)));
            Bl("IEnumerable <- IEnumerable<int>", typeof(IEnumerable).IsAssignableFrom(typeof(IEnumerable<int>)));
            Bl("IEnumerable <- IDictionary<string,int>", typeof(IEnumerable).IsAssignableFrom(typeof(IDictionary<string, int>)));
            // BCL interface source, generic interface targets (+ identity).
            Bl("IEnumerable<int> <- IReadOnlyList<int>", typeof(IEnumerable<int>).IsAssignableFrom(typeof(IReadOnlyList<int>)));
            Bl("IReadOnlyCollection<int> <- IReadOnlyList<int>", typeof(IReadOnlyCollection<int>).IsAssignableFrom(typeof(IReadOnlyList<int>)));
            Bl("IReadOnlyList<int> <- IReadOnlyList<int>", typeof(IReadOnlyList<int>).IsAssignableFrom(typeof(IReadOnlyList<int>)));
            Bl("ICollection<int> <- IList<int>", typeof(ICollection<int>).IsAssignableFrom(typeof(IList<int>)));
            Bl("IEnumerable<int> <- IReadOnlyCollection<int>", typeof(IEnumerable<int>).IsAssignableFrom(typeof(IReadOnlyCollection<int>)));
            // Negatives stay negative.
            Bl("IList <- IReadOnlyList<int>", typeof(IList).IsAssignableFrom(typeof(IReadOnlyList<int>)));
            Bl("IReadOnlyList<int> <- IEnumerable<int>", typeof(IReadOnlyList<int>).IsAssignableFrom(typeof(IEnumerable<int>)));
            // App-module interface inheritance.
            var ic = new ItfC();
            ((IItfB)ic).B();
            ((IItfA)ic).A();
            Bl("IItfA <- IItfB", typeof(IItfA).IsAssignableFrom(typeof(IItfB)));
            Bl("IItfB <- IItfA", typeof(IItfB).IsAssignableFrom(typeof(IItfA)));
            // Abstract class source, and its no-op concrete leaf.
            var cr = new ConcreteRo();
            Console.WriteLine("ConcreteRo count=" + ((IReadOnlyList<int>)cr).Count);
            Bl("IReadOnlyList<int> <- AbstractRo", typeof(IReadOnlyList<int>).IsAssignableFrom(typeof(AbstractRo)));
            Bl("IEnumerable <- AbstractRo", typeof(IEnumerable).IsAssignableFrom(typeof(AbstractRo)));
            Bl("IReadOnlyList<int> <- ConcreteRo", typeof(IReadOnlyList<int>).IsAssignableFrom(typeof(ConcreteRo)));
            Bl("AbstractRo <- ConcreteRo", typeof(AbstractRo).IsAssignableFrom(typeof(ConcreteRo)));
            // GetInterfaces() over interface / abstract sources (name-sorted: the
            // table's row order is emit-deterministic but not .NET's order).
            DumpInterfaces("IReadOnlyList<int>", typeof(IReadOnlyList<int>));
            DumpInterfaces("IList<int>", typeof(IList<int>));
            DumpInterfaces("IItfB", typeof(IItfB));
            DumpInterfaces("AbstractRo", typeof(AbstractRo));
            // Newtonsoft's ImplementsGenericDefinition over an INTERFACE source.
            Console.WriteLine("Impl(IReadOnlyList<int>, IEnumerable<>)=" + ImplementsGenericDefinition(typeof(IReadOnlyList<int>), typeof(IEnumerable<>)));
            Console.WriteLine("Impl(IReadOnlyList<int>, IDictionary<,>)=" + ImplementsGenericDefinition(typeof(IReadOnlyList<int>), typeof(IDictionary<,>)));
            Console.WriteLine("Impl(AbstractRo, IReadOnlyCollection<>)=" + ImplementsGenericDefinition(typeof(AbstractRo), typeof(IReadOnlyCollection<>)));
            // IsInstanceOfType with an interface-typed Type object.
            Bl("typeof(IItfA).IsInstanceOfType(ItfC)", typeof(IItfA).IsInstanceOfType(ic));
            Bl("typeof(IReadOnlyList<int>).IsInstanceOfType(ConcreteRo)", typeof(IReadOnlyList<int>).IsInstanceOfType(cr));

            RunItfResidue();
        }

        // Three shapes the relation-only interface tables have to cover, each asserted
        // here against real .NET.
        //
        // (a) A value type the program NEVER BOXES has no dispatch table, so its
        //     relation-only table is the only thing that can answer
        //     typeof(IStructItf).IsAssignableFrom(typeof(NeverBoxedStruct)) — nullptr
        //     slots, no unboxing thunks, because there is no instance to dispatch on.
        // (b) A type reached by a TYPEOF TOKEN ALONE is emitted as an opaque shell, which
        //     needs one too. Only a FRAMEWORK type shows it: every app-module type is an
        //     emit-set seed, which is why IOpaqueItf below answers correctly either way
        //     and ISet<int> / IAsyncEnumerator<int> do not.
        // (c) The shared-generics CANONICAL ALIAS rows — the extra rows an interface table
        //     carries so a shared body dispatching on a canonical handle resolves — must
        //     NOT be handed out by GetInterfaces(); they are marked
        //     (DN2CPP_TF_SHARED_CANON) and skipped by every reader that hands a row
        //     outward as a Type.
        //
        // The canonical-leak scan at the tail is the (c) assertion that generalizes: the
        // alias rows are INTERLEAVED with the real ones in the string and SZArray dispatch
        // maps rather than trailing them, and typeof(string) / typeof(int[]) are the only
        // types in this bucket whose tables come from those maps. Their row COUNTS are not
        // asserted — both are short of .NET's for an unrelated reason (String's
        // IParsable/ISpanParsable and SZArray's IStructuralComparable/IStructuralEquatable/
        // ICloneable are answered by other means and carry no table row) — but the absence
        // of a canonical name in what they report is exact, and matches .NET trivially,
        // since .NET has no such types at all.
        static void RunItfResidue()
        {
            // (a) A value type nothing ever boxes.
            NeverBoxedStruct nb = new NeverBoxedStruct();
            Console.WriteLine("nb.Val=" + nb.Val());
            Bl("IStructItf <- NeverBoxedStruct", typeof(IStructItf).IsAssignableFrom(typeof(NeverBoxedStruct)));
            DumpInterfaces("NeverBoxedStruct", typeof(NeverBoxedStruct));
            // (b) Interfaces named only by a typeof token: an app-module one (which the
            // emit-set seeding already covered) and two framework ones (which it did not).
            Bl("IItfA <- IOpaqueItf", typeof(IItfA).IsAssignableFrom(typeof(IOpaqueItf)));
            DumpInterfaces("IOpaqueItf", typeof(IOpaqueItf));
            DumpInterfaces("ISet<int>", typeof(ISet<int>));
            DumpInterfaces("IAsyncEnumerator<int>", typeof(IAsyncEnumerator<int>));
            Bl("IEnumerable <- ISet<int>", typeof(IEnumerable).IsAssignableFrom(typeof(ISet<int>)));
            Bl("IAsyncDisposable <- IAsyncEnumerator<int>", typeof(IAsyncDisposable).IsAssignableFrom(typeof(IAsyncEnumerator<int>)));
            // (c) A concrete class's reflection-visible interface list, over a BCL generic,
            // a two-argument BCL generic, and a leaf whose rows all come from its abstract
            // base — plus FindInterfaces, the second reader that hands rows outward.
            DumpInterfaces("List<int>", typeof(List<int>));
            DumpInterfaces("Dictionary<string,int>", typeof(Dictionary<string, int>));
            DumpInterfaces("ConcreteRo", typeof(ConcreteRo));
            Console.WriteLine("List<int>.FindInterfaces count="
                + typeof(List<int>).FindInterfaces(delegate (Type m, object c) { return true; }, null).Length);
            // The generalized (c) assertion — including the two types whose tables are the
            // interleaved string / SZArray dispatch maps.
            Type[] scan =
            {
                typeof(List<int>), typeof(Dictionary<string, int>), typeof(ConcreteRo),
                typeof(string), typeof(int[]), typeof(string[]), typeof(IReadOnlyList<int>),
            };
            int leaked = 0;
            foreach (Type t in scan)
                foreach (Type it in t.GetInterfaces())
                    if (StableName(it).Contains("$Cn"))
                    {
                        leaked++;
                        Console.WriteLine("  canonical row leaked: " + StableName(t) + " -> " + StableName(it));
                    }
            Console.WriteLine("canonical rows reported by GetInterfaces() over " + scan.Length + " types = " + leaked);
        }

        static void Bl(string label, bool v)
        {
            Console.WriteLine(label + " = " + v);
        }

        // Stable closed-generic type name (namespace + metadata name + explicit
        // args); Type.FullName's closed form carries assembly version strings.
        static string StableName(Type t)
        {
            string s = t.Namespace is { Length: > 0 } ns ? ns + "." + t.Name : t.Name;
            if (t.IsGenericType && !t.IsGenericTypeDefinition)
            {
                Type[] ga = t.GetGenericArguments();
                string args = "";
                for (int i = 0; i < ga.Length; i++)
                    args += (i > 0 ? "," : "") + StableName(ga[i]);
                s += "<" + args + ">";
            }
            return s;
        }

        static void DumpInterfaces(string label, Type t)
        {
            Type[] itfs = t.GetInterfaces();
            string[] names = new string[itfs.Length];
            for (int i = 0; i < itfs.Length; i++)
                names[i] = StableName(itfs[i]);
            // Ordinal insertion sort: Array.Sort(string[]) compares with the
            // current culture, which need not agree between hosts.
            for (int i = 1; i < names.Length; i++)
            {
                string k = names[i];
                int j = i - 1;
                while (j >= 0 && string.CompareOrdinal(names[j], k) > 0)
                {
                    names[j + 1] = names[j];
                    j--;
                }
                names[j + 1] = k;
            }
            Console.WriteLine(label + ".GetInterfaces() count=" + itfs.Length);
            for (int i = 0; i < names.Length; i++)
                Console.WriteLine("  [" + i + "] " + names[i]);
        }

        static void ShowKind(string label, Type t)
        {
            Console.WriteLine(label + " Full=" + t.FullName
                + " IsItf=" + t.IsInterface
                + " IsAbs=" + t.IsAbstract
                + " IsClass=" + t.IsClass
                + " IsSealed=" + t.IsSealed
                + " IsValue=" + t.IsValueType);
        }

        // Newtonsoft's ReflectionUtils.ImplementsGenericDefinition WITH its front guard:
        // a non-interface or non-definition argument throws ArgumentException rather than
        // scanning. Returns "throw" on that path, else the bool the scan yields.
        static string ImplementsGenericDefinitionGuarded(Type type, Type genericInterfaceDefinition)
        {
            if (!genericInterfaceDefinition.IsInterface || !genericInterfaceDefinition.IsGenericTypeDefinition)
                return "throw";
            return ImplementsGenericDefinition(type, genericInterfaceDefinition).ToString();
        }

        // Newtonsoft's ReflectionUtils.ImplementsGenericDefinition, reduced: does any
        // interface of `type` have `genericDefinition` as its generic type definition?
        static bool ImplementsGenericDefinition(Type type, Type genericDefinition)
        {
            foreach (Type it in type.GetInterfaces())
                if (it.IsGenericType && it.GetGenericTypeDefinition() == genericDefinition)
                    return true;
            return false;
        }
    }
}
