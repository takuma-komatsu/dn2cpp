#nullable enable
// SUBJECT: the AmbiguousMatchException message and HResult of each ambiguous
// reflection lookup. A member lookup names its first match as
// "'{DeclaringType} {member}'" — a method or constructor by its signature, a property
// by its type and index parameters, an interface after the empty DeclaringType of a
// top-level type — and a single-attribute getter names the first attribute's type. The
// overload beside the Object Equals row names the emitted overload, which precedes the
// row; Object's own rows name the instance Equals. GetInterfaceSubset's fixtures carry
// the interface and attribute cases.
using System;
using System.Reflection;

namespace AmbiguousMatchMessageSubset
{
    class Overloads
    {
        public void M(int x) { }
        public void M(string s) { }
        public int this[int i] => i;
        public int this[string s] => s.Length;
        public bool Equals(Overloads other) => ReferenceEquals(this, other);
    }

    class Ctors
    {
        public Ctors(string s) { }
        public Ctors(int[] a) { }
    }

    static class Program
    {
        static void PM(string label, Func<object?> f)
        {
            try
            {
                object? r = f();
                Console.WriteLine($"{label}: {(r is null ? "null" : r.GetType().Name)}");
            }
            catch (AmbiguousMatchException e)
            {
                Console.WriteLine($"{label}: {e.HResult:X8} {e.Message}");
            }
        }

        internal static void Run()
        {
            Console.WriteLine("== ambiguous match messages ==");
            PM("GetMethod overloads", () => typeof(Overloads).GetMethod("M"));
            PM("GetMethod beside object row", () => typeof(Overloads).GetMethod("Equals"));
            PM("GetMethod object rows", () => typeof(object).GetMethod("Equals"));
            PM("GetProperty indexers", () => typeof(Overloads).GetProperty("Item"));
            PM("GetInterface two namespaces", () => typeof(GetInterfaceSubset.TwoNs).GetInterface("IFoo"));
            MethodInfo m = typeof(GetInterfaceSubset.Attributed).GetMethod("M")!;
            PM("member attribute", () => Attribute.GetCustomAttribute(m, typeof(GetInterfaceSubset.BaseAttr)));
            PM("assembly attribute",
                () => typeof(Program).Assembly.GetCustomAttribute(typeof(GetInterfaceSubset.BaseAttr)));
            PM("CreateInstance null argument", () => Activator.CreateInstance(typeof(Ctors), new object?[] { null }));
        }
    }
}
