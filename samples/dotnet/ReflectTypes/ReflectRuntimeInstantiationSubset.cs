#nullable disable
using System;

// The runtime-instantiation template route
// (Compilation.BuildRuntimeInstantiationTemplates / JudgeRuntimeTemplates on the
// transpiler side, dn2cpp_try_synthesize_generic on the runtime side): a generic
// definition the program typeofs whose type parameter only ever reaches typeof
// positions gets a $CnAny template, so MakeGenericType succeeds at run time for
// ANY argument — value types included — with no closed instantiation anywhere in
// source. Asserted: minting over primitives, an enum, a user struct and classes,
// the virtual body reading typeof(T) through the synthesized rgctx, Activator
// over the synthesized Type (the clone's ctor rows must allocate the CLONE, not
// the template), intern identity (same (def,args), same Type object), the
// GetGenericTypeDefinition and Type.GetType(FullName) round-trips (the latter is
// the dn2cpp_resolve_type_name fallback — the synthesized name must resolve back
// to the same handle), a non-public ctor through
// Activator.CreateInstance(Type, nonPublic), and base-chain synthesis (Sub<T> :
// Tag<T>, identity projection). The negative stays the AOT boundary: a
// definition with a T-typed field is shape-ineligible, so real .NET constructs
// Holder<int> while dn2cpp throws the catchable NotSupportedException naming the
// missing instantiation — the frozen snapshot asserts that message.

namespace ReflectRuntimeInstantiationSubset
{
    abstract class TagBase
    {
        public abstract string Who();
    }

    // typeof-only: T appears in typeof(T) alone, and no closed Tag<X> is ever
    // written — the template is this section's only way to a bool/decimal arg.
    class Tag<T> : TagBase
    {
        private readonly string prefix;
        public Tag() { prefix = "tag"; }
        public override string Who() => prefix + ":" + typeof(T).Name;
    }

    class Sub<T> : Tag<T>
    {
    }

    enum Hue
    {
        Red,
        Green
    }

    struct Pair
    {
        public int X;
        public int Y;
    }

    // The non-public ctor arm: CreateInstance(Type) binds public ctors only, so
    // this mints through CreateInstance(Type, nonPublic: true).
    class Quiet<T> : TagBase
    {
        private Quiet() { }
        public override string Who() => "quiet:" + typeof(T).Name;
    }

    // Shape-ineligible: a T-typed field means a per-argument layout no runtime
    // clone can synthesize.
    class Holder<T> : TagBase
    {
        private readonly T value;
        public Holder() { value = default; }
        public override string Who() => "holder:" + value + ":" + typeof(T).Name;
    }

    class Program
    {
        internal static void Run()
        {
            foreach (Type arg in new[]
                { typeof(bool), typeof(int), typeof(decimal), typeof(Hue),
                  typeof(Pair), typeof(string), typeof(TagBase) })
            {
                Type closed = typeof(Tag<>).MakeGenericType(arg);
                TagBase inst = (TagBase)Activator.CreateInstance(closed);
                Console.WriteLine("mint " + arg.Name + ": " + inst.Who()
                    + " constructed=" + closed.IsConstructedGenericType);
            }

            Type a = typeof(Tag<>).MakeGenericType(typeof(bool));
            Type b = typeof(Tag<>).MakeGenericType(typeof(bool));
            Console.WriteLine("interned=" + ReferenceEquals(a, b)
                + " defRoundTrip=" + (a.GetGenericTypeDefinition() == typeof(Tag<>))
                + " arg=" + a.GetGenericArguments()[0].Name);

            Type resolved = Type.GetType(a.FullName);
            Console.WriteLine("resolve=" + ReferenceEquals(resolved, a));

            Type quiet = typeof(Quiet<>).MakeGenericType(typeof(int));
            TagBase qi = (TagBase)Activator.CreateInstance(quiet, true);
            Console.WriteLine("nonpublic: " + qi.Who());

            Type sub = typeof(Sub<>).MakeGenericType(typeof(int));
            TagBase si = (TagBase)Activator.CreateInstance(sub);
            Console.WriteLine("sub: " + si.Who()
                + " base=" + sub.BaseType.GetGenericArguments()[0].Name);

            try
            {
                Type bad = typeof(Holder<>).MakeGenericType(typeof(int));
                Console.WriteLine("holder<int>: created " + bad.Name);
            }
            catch (NotSupportedException e)
            {
                Console.WriteLine("holder<int>: NotSupportedException: " + e.Message);
            }
        }
    }
}
