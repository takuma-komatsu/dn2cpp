#nullable enable
using System;
using System.Threading.Tasks;

namespace ReflectActivatorGenericSubset
{
    // SUBJECT: Activator.CreateInstance<T>() and Lazy<T>'s default-ctor path when
    // T has no public parameterless ctor. The generic factory reflects, so the
    // miss is a RUN-TIME MissingMethodException, never a compile-time reject;
    // Lazy<T> catches that type and re-raises MissingMemberException. Exception
    // types are printed, not messages.
    interface IFace { }

    abstract class Abs { }

    class NoCtor
    {
        private readonly int _x;
        public NoCtor(int x) { _x = x; }
        public override string ToString() => $"NoCtor({_x})";
    }

    // Only a NON-PUBLIC parameterless ctor: the factory binds public ones only, so
    // visibility rather than mere presence decides. The public member keeps the
    // ctor row alive through tree-shaking.
    class PrivateCtorOnly
    {
        public int V;
        private PrivateCtorOnly() { V = 5; }
        public override string ToString() => $"PrivateCtorOnly({V})";
    }

    class Good
    {
        public int V = 42;
        public override string ToString() => $"Good({V})";
    }

    struct SVal
    {
        public int X;
        public override string ToString() => $"SVal({X})";
    }

    // An enum is a value type with an implicit default(T), so the factory returns 0
    // and never throws — at any underlying width, the long case included.
    enum EInt { A = 1, B = 2 }

    enum ELong : long { X = 1, Y = 2 }

    static class Program
    {
        private static void ShowT<T>(string label, Func<T> f)
        {
            try
            {
                Console.WriteLine($"{label} => {f()}");
            }
            catch (Exception e)
            {
                Console.WriteLine($"{label} => threw {e.GetType().Name}");
            }
        }

        private static void ShowLazy<T>(string label, Func<Lazy<T>> make)
        {
            try
            {
                Lazy<T> lz = make();
                Console.WriteLine($"{label} => {lz.Value}");
            }
            catch (Exception e)
            {
                Console.WriteLine($"{label} => threw {e.GetType().Name}");
            }
        }

        internal static void Run()
        {
            Console.WriteLine("== CreateInstance<T> not-instantiable ==");
            ShowT("iface", () => Activator.CreateInstance<IFace>());
            ShowT("abstract", () => Activator.CreateInstance<Abs>());
            ShowT("no parameterless", () => Activator.CreateInstance<NoCtor>());
            // Only a non-public parameterless ctor: uninstantiable via the factory.
            ShowT("non-public ctor", () => Activator.CreateInstance<PrivateCtorOnly>());
            // An intrinsic-mapped type whose ctors are all internal: the
            // instantiability verdict must run BEFORE the intrinsic arm, landing on
            // the run-time throw rather than a transpile-time abort.
            ShowT("intrinsic no-pub-ctor", () => Activator.CreateInstance<Task<int>>());

            Console.WriteLine("== CreateInstance<T> ok ==");
            ShowT("class ok", () => Activator.CreateInstance<Good>());
            ShowT("struct ok", () => Activator.CreateInstance<SVal>());
            ShowT("enum int", () => Activator.CreateInstance<EInt>());
            ShowT("enum long", () => Activator.CreateInstance<ELong>());
            // Lazy<TEnum> exercises the same factory through CreateViaDefaultConstructor.
            ShowLazy("lazy enum int", () => new Lazy<EInt>());
            ShowLazy("lazy enum long", () => new Lazy<ELong>());

            Console.WriteLine("== Lazy<T> default-ctor ==");
            ShowLazy("lazy iface", () => new Lazy<IFace>());
            ShowLazy("lazy no-ctor", () => new Lazy<NoCtor>());
            ShowLazy("lazy non-public ctor", () => new Lazy<PrivateCtorOnly>());
            ShowLazy("lazy intrinsic no-pub-ctor", () => new Lazy<Task<int>>());
            ShowLazy("lazy ok", () => new Lazy<Good>());
            // A value-factory Lazy<T> never runs the default-ctor path, yet
            // CreateViaDefaultConstructor stays statically reachable.
            ShowLazy("lazy factory", () => new Lazy<NoCtor>(() => new NoCtor(7)));
        }
    }
}
