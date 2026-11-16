#nullable disable
using System;
using System.Reflection;

// Nested reflection includes visibility-filtered retained definition identities.

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

    class MetadataOwner<T>
    {
        public class Plain { public static int Uncalled() => 91; }
        public class Generic<U> { public static int Uncalled() => 92; }
        private class Hidden<U> { public static int Uncalled() => 93; }
    }

    class Program
    {
        internal static void RunMetadata()
        {
            Console.WriteLine("== nested metadata ==");
            Type owner = typeof(MetadataOwner<>);
            Type closed = typeof(MetadataOwner<int>);
            const BindingFlags both = BindingFlags.Public | BindingFlags.NonPublic;
            Type plain = owner.GetNestedType("Plain", both);
            Type generic = owner.GetNestedType("Generic`1", both);
            Type hidden = owner.GetNestedType("Hidden`1", both);
            Console.WriteLine("nested definitions=" + (plain != null && plain.IsGenericTypeDefinition)
                + ":" + (generic != null && generic.IsGenericTypeDefinition)
                + ":" + (hidden != null && hidden.IsGenericTypeDefinition));
            Console.WriteLine("nested owners=" + (plain != null && plain.DeclaringType == owner)
                + ":" + (generic != null && generic.DeclaringType == owner)
                + ":" + (hidden != null && hidden.DeclaringType == owner));
            Console.WriteLine("nested closed owner=" + (closed.GetNestedType("Generic`1", both) == generic));
            Console.WriteLine("nested visibility=" + (generic != null && generic.IsNestedPublic)
                + ":" + (hidden != null && hidden.IsNestedPrivate));
            Console.WriteLine("nested arity=" + (plain == null ? -1 : plain.GetGenericArguments().Length)
                + ":" + (generic == null ? -1 : generic.GetGenericArguments().Length));
            Console.WriteLine("nested filters=" + owner.GetNestedTypes(BindingFlags.Public).Length
                + ":" + owner.GetNestedTypes(BindingFlags.NonPublic).Length
                + ":" + closed.GetNestedTypes(both | BindingFlags.DeclaredOnly | BindingFlags.Instance).Length
                + ":" + owner.GetNestedTypes(BindingFlags.Default).Length);
            Console.WriteLine("nested misses=" + (owner.GetNestedType("Generic", both) == null)
                + ":" + (owner.GetNestedType("Hidden`1") == null)
                + ":" + (owner.GetNestedType("Missing", both) == null));
            Console.WriteLine("nested ignorecase=" + (owner.GetNestedType("generic`1", both | BindingFlags.IgnoreCase) == generic));
            Console.WriteLine("nested nongeneric private=" + (typeof(Outer).GetNestedType("Secret", BindingFlags.NonPublic) != null));
            Console.WriteLine("nested members=" + owner.GetMember("Hidden*", MemberTypes.NestedType, both).Length);
            Console.WriteLine("nested metadata end");
        }

        internal static void Run()
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
