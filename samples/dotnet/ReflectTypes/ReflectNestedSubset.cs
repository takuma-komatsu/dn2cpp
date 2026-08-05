#nullable disable
using System;

// (reflection R5 completion): Type.GetNestedTypes / GetNestedType(name). Each
// type-info gains a table of its *public* nested types (emitted, non-generic) — the
// default BindingFlags.Public set. Non-public nested types are a carve-out. CoreLib only;
// diffed exact vs real.NET.

namespace ReflectNestedSubset
{
    class Outer
    {
        public class Inner { public int X; }
        public struct Point { public int A; }
        public enum Kind { One, Two }
        private class Secret { public int S; }
        public class Mid { public class Leaf { public int Y; } }

        // Touch each nested type so it is reachability-emitted (the private Secret too,
        // to prove the visibility filter excludes it rather than it just being stripped).
        public static int Use()
        {
            Inner i = new Inner(); i.X = 1;
            Point p = new Point(); p.A = 2;
            Kind k = Kind.Two;
            Secret s = new Secret(); s.S = 3;
            Mid.Leaf l = new Mid.Leaf(); l.Y = 4;
            Mid m = new Mid(); // roots Mid itself so GetNestedTypes reports it
            return i.X + p.A + (int)k + s.S + l.Y + (m != null ? 0 : 1);
        }
    }

    class Program
    {internal static void Run()
        {
            Console.WriteLine("use=" + Outer.Use());
            // Reference the nested enum as a type so its type-info is emitted.
            Console.WriteLine("kind=" + typeof(Outer.Kind).Name);

            Type[] nt = typeof(Outer).GetNestedTypes();
            string[] names = new string[nt.Length];
            for (int i = 0; i < nt.Length; i++) names[i] = nt[i].Name;
            Array.Sort(names, StringComparer.Ordinal);
            Console.WriteLine("nested=" + names.Length + " [" + string.Join(",", names) + "]");

            Console.WriteLine("Inner=" + (typeof(Outer).GetNestedType("Inner") == null ? "<null>" : typeof(Outer).GetNestedType("Inner").Name));
            Console.WriteLine("Point=" + (typeof(Outer).GetNestedType("Point") == null ? "<null>" : typeof(Outer).GetNestedType("Point").Name));
            // Private nested -> not in the public set -> null (matches default BindingFlags).
            Console.WriteLine("Secret=" + (typeof(Outer).GetNestedType("Secret") == null ? "<null>" : "found"));
            Console.WriteLine("Missing=" + (typeof(Outer).GetNestedType("Missing") == null ? "<null>" : "found"));

            // A nested type's reflection names match real .NET — FullName is
            // the '+'-joined declaring chain, Name the bare simple name, Namespace
            // the declaring chain's namespace.
            Console.WriteLine("Inner.Name=" + typeof(Outer.Inner).Name);
            Console.WriteLine("Inner.FullName=" + typeof(Outer.Inner).FullName);
            Console.WriteLine("Inner.Namespace=" + typeof(Outer.Inner).Namespace);
            Console.WriteLine("Kind.FullName=" + typeof(Outer.Kind).FullName);
            Console.WriteLine("Leaf.FullName=" + typeof(Outer.Mid.Leaf).FullName);
            Console.WriteLine("Leaf.Name=" + typeof(Outer.Mid.Leaf).Name);
            Console.WriteLine("GetNestedType.FullName=" + typeof(Outer).GetNestedType("Inner").FullName);
            // Type.GetType round-trips the CLR '+' syntax to the same handle.
            Type rt = Type.GetType("ReflectNestedSubset.Outer+Inner");
            Console.WriteLine("GetType(+)=" + (rt == null ? "<null>" : rt.FullName)
                + " same=" + (rt == typeof(Outer.Inner)));
            // The bare metadata simple name is NOT a reflection name (matches .NET).
            Console.WriteLine("GetType(bare)=" + (Type.GetType("Inner") == null ? "<null>" : "found"));
            // A nested-element array reads the '+' name before "[]".
            Console.WriteLine("InnerArr.FullName=" + typeof(Outer.Inner[]).FullName);
        }
    }
}
