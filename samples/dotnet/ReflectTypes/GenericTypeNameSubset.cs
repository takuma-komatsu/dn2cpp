#nullable disable
using System;
using System.Collections.Generic;

// A closed generic's CLR NAME surface. Type.Name, ToString(), FullName and
// AssemblyQualifiedName are composed at run time from the type-info's genericDef +
// genericArgs; the emitted ti->name stays the dn2cpp-mangled instantiation, because
// --trim-reflection's refusal tells the user to pass exactly it to --reflection-root
// (gates/expected/trim-reflection-trimmed.txt pins the pair).
//
// The section's real subject is therefore twofold, and only the first half is about
// names. The second is that the composed spellings RESOLVE BACK: no registry key
// carries them, so Type.GetType(FullName) and the AssemblyQualifiedName round-trip —
// a serializer's $type path — work only through the runtime's structural fallback.
// Losing that round-trip is silent — the suite stays green — so the round-trip lines are
// the ones worth keeping if anyone ever prunes this section.
//
// One line diverges from real .NET and is marked at its site: the ungenerated-close
// lookup (the AOT boundary).

namespace GenericTypeNameSubset
{
    class Box<T>
    {
        public T Value;
        public Box(T v) { Value = v; }
    }

    class Program
    {
        static void Show(string label, Type t)
        {
            Console.WriteLine(label + " Name=" + t.Name + " Namespace=" + t.Namespace);
            Console.WriteLine(label + " ToString=" + t.ToString());
            Console.WriteLine(label + " FullName=" + t.FullName);
            Console.WriteLine(label + " AQN=" + t.AssemblyQualifiedName);
            Console.WriteLine(label + " roundtrip full=" + (Type.GetType(t.FullName) == t)
                + " aqn=" + (Type.GetType(t.AssemblyQualifiedName) == t)
                + " tostring=" + (Type.GetType(t.ToString()) == t));
        }

        internal static void Run()
        {
            // Force the closed instantiations to be reachability-generated.
            Box<int> bi = new Box<int>(7);
            List<int> li = new List<int>();
            li.Add(1);
            Dictionary<string, List<int>> nested = new Dictionary<string, List<int>>();
            nested["a"] = li;
            KeyValuePair<string, int> kvp = new KeyValuePair<string, int>("k", 1);
            int? ni = 5;
            Console.WriteLine("seed " + bi.Value + li.Count + nested.Count + kvp.Key + ni.Value);

            Show("box", typeof(Box<int>));
            Show("list", typeof(List<int>));
            Show("nullable", typeof(int?));
            Show("kvp", typeof(KeyValuePair<string, int>));
            // A type ARGUMENT that is itself a closed generic: the composition recurses,
            // and so does the resolution that reads it back.
            Show("nested", typeof(Dictionary<string, List<int>>));
            // Non-generic control: these four names are ti->name verbatim, unchanged.
            Show("plain", typeof(string));

            // The open definition. All three are exact: ToString appends the DECLARED
            // parameter names (List`1[T]) and FullName never does.
            Type def = typeof(List<>);
            Console.WriteLine("def Name=" + def.Name + " FullName=" + def.FullName
                + " ToString=" + def.ToString());

            // Object.ToString()'s default arm is GetType().ToString(), so it composes too
            // (Box<T> declares no override).
            Console.WriteLine("boxinstance=" + bi.ToString());
            object boxed = new Box<string>("s");
            Console.WriteLine("boxedinstance=" + boxed.ToString());

            // The CLR spelling resolves even though nothing ever wrote it out: real .NET
            // accepts it, and no registry key carries it — the structural fallback does.
            Console.WriteLine("byclrname=" + (Type.GetType("System.Collections.Generic.List`1[System.Int32]") == typeof(List<int>)));
            // A close AOT never generated stays a miss, the same AOT boundary
            // MakeGenericType reports. Box<T> is closed at int and string here and nowhere
            // else, which no BCL definition can be relied on for in a bucket this size.
            // DIVERGES: real .NET constructs the type and answers False.
            Console.WriteLine("bymissingclose=" + (Type.GetType("GenericTypeNameSubset.Box`1[System.Double]") is null));
        }
    }
}
