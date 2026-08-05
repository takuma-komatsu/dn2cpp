using System;

namespace TrimReflectLib
{
    // The library half of the --trim-reflection bucket. Every type here stands in for a
    // FRAMEWORK type as the trim sees one: outside the app module, reached only through the
    // `object`-returning factories below. That the app never NAMES them is what makes them
    // strippable — the app module is kept whole, and a typeof / `is` / cast would keep a
    // library type too (which is what LibKept pins from the other side) — so this bucket
    // needs two assemblies and the app must meet these as `object`.

    // The app DOES name this one, which keeps it: an interface's members live nowhere but
    // its own table, so a stripped interface breaks a GetInterfaces()-then-GetMethods()
    // walk. It carries both a method and a property so the app can reach LibWidget's Twice
    // and Tag through the interface slot WITHOUT naming the concrete type — which is how a
    // member lands in the reflection table of a type that still strips.
    public interface ILibThing
    {
        int Twice(int x);
        string Tag { get; }
    }

    // Stripped: met only as `object`, so its field/method/property tables go and every
    // read of them throws.
    public class LibWidget : ILibThing
    {
        public int Amount;
        private string _tag = "lib";
        public string Tag => _tag;
        public int Twice(int x) => x * 2;
        public LibWidget() { }
        public LibWidget(int a) { Amount = a; }
        public override string ToString() => "LibWidget(" + Amount + "," + _tag + ")";
    }

    // Stripped, and NOT covered by the --reflection-root that names LibWidget: a root keeps
    // exactly the type it names, and this is what proves it keeps no more. The app must
    // reflect over it through enumerations and fields only — reaching a member by name
    // would keep the type and defeat the strip.
    public class LibGadget
    {
        public int Size;
        public int Half() => Size / 2;
        public LibGadget() { }
        public LibGadget(int s) { Size = s; }
        public override string ToString() => "LibGadget(" + Size + ")";
    }

    // Stripped, and rootable only by its ARITY-STRIPPED definition name
    // ("TrimReflectLib.LibBox"): a closed instantiation's full name has its type arguments
    // mangled into it, which is not a name anyone can be expected to guess.
    public class LibBox<T>
    {
        public T Value;
        public T Get() => Value;
        public LibBox() { }
        public LibBox(T v) { Value = v; }
        public override string ToString() => "LibBox(" + Value + ")";
    }

    // A stripped ENUM: enum members live in their own table, not in member metadata, so
    // ToString()/GetNames keep answering while the field/method/property surface goes.
    public enum LibColor
    {
        Red = 1,
        Green = 2,
    }

    // The base of an APP class (TrimReflect.DerivedWidget), kept by the keep-set's
    // base-chain closure rather than by anything naming it: the runtime collectors walk
    // `for (ti = type; ti; ti = ti->base)` and test the stripped bit at every level.
    // Reflecting over the inherited Ping() from the app asserts the closure is still there.
    public class LibBase
    {
        public int Inherited;
        public int Ping() => 1;
    }

    // The positive control: a library type the app names with a typeof. A token keeps a
    // library type's metadata exactly as being in the app module does.
    public class LibKept
    {
        public int Keeper;
        public int Echo(int x) => x;
    }

    public static class Factory
    {
        // Each returns `object`, never the concrete type: the app's IL must not name what
        // the strip probes are about to reflect over.
        public static object Make() => new LibWidget(7);

        // Reaches the parameterless ctor so Activator.CreateInstance(Type) has one to find:
        // an unreached ctor is tree-shaken, which is independent of trimming.
        public static object MakeDefault() => new LibWidget();

        public static object MakeGadget() => new LibGadget(9);
        public static object MakeGadgetDefault() => new LibGadget();

        public static object MakeBox() => new LibBox<int>(5);
        public static object MakeBoxDefault() => new LibBox<int>();

        public static object MakeColor() => LibColor.Green;
    }
}
