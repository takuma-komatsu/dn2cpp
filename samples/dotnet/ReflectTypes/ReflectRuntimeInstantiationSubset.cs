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
// Activator.CreateInstance(Type, nonPublic), base-chain synthesis (Sub<T> :
// Tag<T>, identity projection), and that chain's AOT seam (the synthesized
// Twig<int>'s base is an instantiation the image already carries, so it must
// intern onto the AOT Anchor<int> type-info — BaseType identity,
// IsAssignableFrom and isinst all compare pointers). The negative stays the AOT
// boundary: a
// definition with a T-typed field is shape-ineligible, so real .NET constructs
// Holder<int> while dn2cpp throws the catchable NotSupportedException naming the
// missing instantiation — the frozen snapshot asserts that message. So does a
// definition whose generic virtual override instantiates a generic method over
// the definition's own type parameter: a clone would need that instantiation
// minted per type argument.

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

    // The AOT/synthesized seam: Anchor<int> exists AOT (constructed in Run), so
    // the synthesized Twig<int>'s base must BE that type-info — a duplicate would
    // fail every pointer-comparing walk (is, cast, IsAssignableFrom, BaseType).
    class Anchor<T> : TagBase
    {
        public override string Who() => "anchor:" + typeof(T).Name;
    }

    class Twig<T> : Anchor<T>
    {
    }

    // Ineligible: the override runs Helper<T>, whose method argument is the
    // clone's own type argument.
    class GvmHelperRoot<T>
    {
        public virtual string Tag<U>() => "root";
    }

    class GvmHelperLeaf<T> : GvmHelperRoot<T>
    {
        public override string Tag<U>() => Helper<T>();
        private string Helper<V>() => typeof(V).Name;
    }

    // Shape-ineligible: a T-typed field means a per-argument layout no runtime
    // clone can synthesize.
    class Holder<T> : TagBase
    {
        private readonly T value;
        public Holder() { value = default; }
        public override string Who() => "holder:" + value + ":" + typeof(T).Name;
    }

    // typeof-only apart from MakeList, which nothing invokes: the other methods run
    // only through Invoke on a clone.
    class Reflected<T>
    {
        private readonly string label = "reflected";
        public override string ToString() => label + ":" + typeof(T).Name;
        public string Named() => label + "/" + typeof(T).Name;
        public int Constant() => 7;
        public virtual string Described() => "described:" + typeof(T).Name;
        public object MakeList() => new System.Collections.Generic.List<T>();
    }

    // Only reflection names these methods. Their bodies name T through typeof, a
    // type test, or a call of another such method, static ones included; MakeList
    // names List<T>, which no clone can mint.
    class Called<T>
    {
        private readonly string label = "called";
        public int Constant() => 7;
        public string Kind() => typeof(T).Name;
        public string Twice() => Kind() + "+" + Kind();
        public string Labeled() => label + ":" + Kind();
        public bool Is(object o) => o is T;
        public string ViaStatic() => Describe();
        public static string Describe() => "static:" + typeof(T).Name;
        public static int StaticConstant() => 9;
        public object MakeList() => new System.Collections.Generic.List<T>();
    }

    // Only reflection names Show and StaticShow; they reach the base levels'
    // static methods through the table their level forwards.
    class StaticRoot<T>
    {
        protected static string Name() => typeof(T).Name;
    }

    class StaticMid<T> : StaticRoot<T>
    {
        protected static string MidName() => "mid:" + Name();
    }

    class StaticLeaf<T> : StaticMid<T>
    {
        public string Show() => Name() + "|" + MidName();
        public static string StaticShow() => MidName();
    }

    interface IVisit
    {
        string Visit<U>();
    }

    // Visit is final: a binding through the clone's row runs it directly, one
    // through IVisit takes the case the dispatcher records for the template.
    class Visiting<T> : IVisit
    {
        public string Visit<U>() => typeof(T).Name + "/" + typeof(U).Name;
    }

    // Only reflection names these methods. C# compiles each cast to T to unbox.any.
    class Casting<T>
    {
        public string Cast(object o)
        {
            try
            {
                _ = (T)o;
                return "ok";
            }
            catch (InvalidCastException)
            {
                return "InvalidCastException";
            }
            catch (NullReferenceException)
            {
                return "NullReferenceException";
            }
        }

        public object Boxed(object o) => (T)o;
        public bool IsT(object o) => o is T t;

        public object Kept(object o)
        {
            object kept = "none";
            if (o is T t)
                kept = t;
            return kept;
        }
    }

    class RefCasting<T> where T : class
    {
        public object As(object o) => o as T;
        public bool AsNull(object o) => (o as T) == null;
    }

    interface IBump
    {
        void Bump();
    }

    struct Counter : IBump
    {
        public int N;
        public string Label;

        public void Bump() => N++;
        public override string ToString() => Label + N;
    }

    // Only reflection names these methods. Each reads a T out of its argument, then
    // changes the argument's box and returns the T it read.
    class Snapshot<T>
    {
        public object Bumped(object o)
        {
            T c = (T)o;
            ((IBump)o).Bump();
            return c;
        }

        public object Matched(object o)
        {
            if (o is T t)
            {
                ((IBump)o).Bump();
                return t;
            }
            return "none";
        }

        public object Written(object o)
        {
            T c = (T)o;
            typeof(Counter).GetField("N").SetValue(o, 9);
            return c;
        }
    }

    class Program
    {
        internal static void RunTemplateValues()
        {
            Console.WriteLine("== template values ==");
            foreach (Type arg in new[] { typeof(bool), typeof(string) })
            {
                Type closed = typeof(StaticLeaf<>).MakeGenericType(arg);
                object inst = Activator.CreateInstance(closed);
                Console.WriteLine("static-leaf " + arg.Name + ": show=" + closed.GetMethod("Show").Invoke(inst, null)
                    + " staticShow=" + closed.GetMethod("StaticShow").Invoke(null, null)
                    + " null-bound show=" + NullBound(() =>
                        ((Func<string>)Delegate.CreateDelegate(typeof(Func<string>), null, closed.GetMethod("Show")))()));
            }
            IVisit visited = new Visiting<long>();
            Console.WriteLine("visit aot: " + visited.Visit<int>());
            foreach (Type arg in new[] { typeof(bool), typeof(string) })
            {
                Type closed = typeof(Visiting<>).MakeGenericType(arg);
                object inst = Activator.CreateInstance(closed);
                var viaItf = (Func<string>)Delegate.CreateDelegate(typeof(Func<string>), inst,
                    typeof(IVisit).GetMethod("Visit").MakeGenericMethod(typeof(int)));
                var viaRow = (Func<string>)Delegate.CreateDelegate(typeof(Func<string>), inst,
                    closed.GetMethod("Visit").MakeGenericMethod(typeof(int)));
                Console.WriteLine("visit " + arg.Name + ": " + viaItf() + " " + viaRow()
                    + " equal=" + viaItf.Equals(viaRow) + "/" + viaRow.Equals(viaItf)
                    + " sameHash=" + (viaItf.GetHashCode() == viaRow.GetHashCode()));
            }
            Console.WriteLine("closed casts: " + CastMessage(() => (int?)(object)"s")
                + " | " + CastMessage(() => (int?)(object)DayOfWeek.Friday)
                + " | " + CastMessage(() => (DayOfWeek?)(object)5)
                + " | " + CastMessage(() => (System.Collections.Generic.List<int>)(object)"s"));
            Console.WriteLine("nullable tests: " + IsOf<int?>(5) + " " + IsOf<int?>(DayOfWeek.Friday) + " " + IsOf<int?>(null)
                + " as=" + AsNullableInt(5) + "/" + AsNullableInt(DayOfWeek.Friday).HasValue
                + " assignable=" + typeof(int?).IsAssignableFrom(typeof(int)) + "/" + typeof(int).IsAssignableFrom(typeof(int?))
                + " instance=" + typeof(int?).IsInstanceOfType(5));
            foreach (Type arg in new[] { typeof(int), typeof(string), typeof(int?), typeof(DayOfWeek), typeof(Pair) })
            {
                Type closed = typeof(Casting<>).MakeGenericType(arg);
                object inst = Activator.CreateInstance(closed);
                foreach (object o in new object[] { 5, "s", null, DayOfWeek.Friday, new Pair() })
                    Console.WriteLine("casting " + arg.Name + " " + (o ?? "null")
                        + ": cast=" + closed.GetMethod("Cast").Invoke(inst, new[] { o })
                        + " boxed=" + Described(() => closed.GetMethod("Boxed").Invoke(inst, new[] { o }), o)
                        + " is=" + closed.GetMethod("IsT").Invoke(inst, new[] { o })
                        + " kept=" + Described(() => closed.GetMethod("Kept").Invoke(inst, new[] { o }), o));
                Console.WriteLine("casting " + arg.Name + " null-bound cast: " + NullBound(() =>
                    ((Func<object, string>)Delegate.CreateDelegate(typeof(Func<object, string>), null,
                        closed.GetMethod("Cast")))("s")));
            }
            foreach (Type arg in new[] { typeof(string), typeof(object) })
            {
                Type closed = typeof(RefCasting<>).MakeGenericType(arg);
                object inst = Activator.CreateInstance(closed);
                foreach (object o in new object[] { 5, "s", null })
                    Console.WriteLine("ref-casting " + arg.Name + " " + (o ?? "null")
                        + ": as=" + Described(() => closed.GetMethod("As").Invoke(inst, new[] { o }), o)
                        + " asNull=" + closed.GetMethod("AsNull").Invoke(inst, new[] { o }));
            }
            Console.WriteLine("template values end");
        }

        private static bool IsOf<T>(object o) => o is T;

        private static int? AsNullableInt(object o) => o as int?;

        private static string CastMessage(Func<object> call)
        {
            try
            {
                return "value " + call();
            }
            catch (InvalidCastException ex)
            {
                return ex.Message;
            }
        }

        // The result, its type and whether it is the argument itself, or what the
        // method threw.
        private static string Described(Func<object> call, object arg)
        {
            try
            {
                object r = call();
                return r == null ? "null" : r + ":" + r.GetType().Name + ":" + ReferenceEquals(r, arg);
            }
            catch (System.Reflection.TargetInvocationException ex)
            {
                return ex.InnerException.GetType().Name + "(" + ex.InnerException.Message + ")";
            }
        }

        internal static void RunTemplateCopies()
        {
            Console.WriteLine("== template copies ==");
            foreach (Type arg in new[] { typeof(Counter), typeof(Counter?), typeof(IBump), typeof(object) })
            {
                Type closed = typeof(Snapshot<>).MakeGenericType(arg);
                object inst = Activator.CreateInstance(closed);
                foreach (string method in new[] { "Bumped", "Matched", "Written" })
                {
                    object box = new Counter { Label = "n" };
                    object r = closed.GetMethod(method).Invoke(inst, new[] { box });
                    Console.WriteLine("snapshot " + arg.Name + " " + method + ": result=" + r
                        + " source=" + box + " same=" + ReferenceEquals(r, box));
                }
            }
            Console.WriteLine("template copies end");
        }

        internal static void RunTemplateCalls()
        {
            Console.WriteLine("== template calls ==");
            foreach (Type arg in new[]
                { typeof(bool), typeof(Pair), typeof(string), typeof(System.Collections.Generic.KeyValuePair<string, int>) })
            {
                Type closed = typeof(Called<>).MakeGenericType(arg);
                object inst = Activator.CreateInstance(closed);
                string name = "called " + arg.Name;
                Console.WriteLine(name + ": twice=" + closed.GetMethod("Twice").Invoke(inst, null)
                    + " labeled=" + closed.GetMethod("Labeled").Invoke(inst, null)
                    + " is=" + closed.GetMethod("Is").Invoke(inst, new object[] { "x" })
                    + " viaStatic=" + closed.GetMethod("ViaStatic").Invoke(inst, null)
                    + " describe=" + closed.GetMethod("Describe").Invoke(null, null)
                    + " staticConstant=" + closed.GetMethod("StaticConstant").Invoke(null, null)
                    + " makeList=" + (closed.GetMethod("MakeList") != null));
                Console.WriteLine(name + " static delegate: "
                    + ((Func<string>)Delegate.CreateDelegate(typeof(Func<string>), closed.GetMethod("Describe")))());
                foreach (string method in new[] { "Kind", "Twice", "Labeled", "ViaStatic" })
                    Console.WriteLine(name + " null-bound " + method + ": " + NullBound(() =>
                        ((Func<string>)Delegate.CreateDelegate(typeof(Func<string>), null, closed.GetMethod(method)))()));
                Console.WriteLine(name + " null-bound Constant: " + NullBound(() =>
                    ((Func<int>)Delegate.CreateDelegate(typeof(Func<int>), null, closed.GetMethod("Constant")))()));
                Console.WriteLine(name + " null-bound Is: " + NullBound(() =>
                    ((Func<object, bool>)Delegate.CreateDelegate(typeof(Func<object, bool>), null, closed.GetMethod("Is")))("x")));
            }
            Console.WriteLine("template calls end");
        }

        private static string NullBound(Func<object> call)
        {
            try
            {
                return call().ToString();
            }
            catch (Exception ex)
            {
                return ex.GetType().Name;
            }
        }

        internal static void RunReflectedBodies()
        {
            Console.WriteLine("== reflected template bodies ==");
            foreach (Type arg in new[] { typeof(bool), typeof(Hue), typeof(string) })
            {
                Type closed = typeof(Reflected<>).MakeGenericType(arg);
                object inst = Activator.CreateInstance(closed);
                Console.WriteLine("reflected " + arg.Name + ": " + inst
                    + " named=" + closed.GetMethod("Named").Invoke(inst, null)
                    + " constant=" + closed.GetMethod("Constant").Invoke(inst, null)
                    + " described=" + closed.GetMethod("Described").Invoke(inst, null)
                    + " makeList=" + (closed.GetMethod("MakeList") != null));
            }
            Console.WriteLine("reflected template bodies end");
        }

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

            Anchor<int> anchored = new Anchor<int>();
            Type twig = typeof(Twig<>).MakeGenericType(typeof(int));
            TagBase tw = (TagBase)Activator.CreateInstance(twig);
            Console.WriteLine("seam: " + tw.Who()
                + " baseIsAot=" + (twig.BaseType == typeof(Anchor<int>))
                + " assignable=" + typeof(Anchor<int>).IsAssignableFrom(twig)
                + " isinst=" + (tw is Anchor<int>)
                + " aot=" + anchored.Who());

            GvmHelperRoot<int> root = new GvmHelperRoot<int>();
            try
            {
                GvmHelperRoot<int> leaf = (GvmHelperRoot<int>)Activator.CreateInstance(
                    typeof(GvmHelperLeaf<>).MakeGenericType(typeof(int)));
                Console.WriteLine("gvm-placeholder-helper<int>: " + root.Tag<string>() + "/" + leaf.Tag<string>());
            }
            catch (NotSupportedException e)
            {
                Console.WriteLine("gvm-placeholder-helper<int>: NotSupportedException: " + e.Message);
            }
        }
    }
}
