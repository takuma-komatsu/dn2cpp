#nullable enable
using System;
using System.Collections.Generic;
using System.Reflection;

namespace ReflectBclCtorSubset
{
    // SUBJECT: a closed BCL generic over an app type that appears ONLY as a
    // late-bound construction target — the deserializer shape, where the member's
    // declared type is read off FieldInfo, GetConstructor(Type.EmptyTypes) asked
    // for the default ctor, and that Invoked. The reflection-ctor reachability
    // route must reach the parameterless ctor of every closed BCL specialization
    // an app-module member names, nested type arguments included, or
    // GetConstructor answers null where real .NET constructs the object.
    class CompoundDefinition
    {
        public string Name = "";
        public double Mass;
    }

    class Registry
    {
        // Never assigned a constructed instance anywhere in the program — the
        // ONLY constructions happen through Construct() below.
        public Dictionary<string, CompoundDefinition>? Compounds;
        public List<CompoundDefinition>? Ordered;
        public Dictionary<string, List<CompoundDefinition>>? Grouped;
        public HashSet<CompoundDefinition>? Tagged;
    }

    // The generic-METHOD half: a Deserialize<Dictionary<string, T>>() call names
    // the closed specialization ONLY as a type argument, so the ctor surface must
    // also cover the type arguments of MethodSpecs app bodies instantiate.
    class SaveData
    {
        public int Score;
    }

    static class Deserializer
    {
        // T is only ever a late-bound construction target inside the generic body.
        internal static T Deserialize<T>()
        {
            ConstructorInfo? ci = typeof(T).GetConstructor(Type.EmptyTypes);
            if (ci is null)
                throw new InvalidOperationException($"no default ctor found");
            return (T)ci.Invoke(null)!;
        }
    }

    static class Program
    {
        static object Construct(Type t)
        {
            ConstructorInfo? ci = t.GetConstructor(Type.EmptyTypes);
            if (ci is null)
                throw new InvalidOperationException($"no default ctor found");
            return ci.Invoke(null);
        }

        internal static void Run()
        {
            var reg = new Registry();
            // A fixed name order keeps the output deterministic. The constructed
            // generics' type NAMES are deliberately not printed: dn2cpp's
            // closed-generic reflection names differ from .NET's backtick form.
            foreach (string name in new[] { "Compounds", "Ordered", "Grouped", "Tagged" })
            {
                FieldInfo f = typeof(Registry).GetField(name)!;
                object o = Construct(f.FieldType);
                f.SetValue(reg, o);
                Console.WriteLine($"{name} constructed ok");
            }
            var glucose = new CompoundDefinition { Name = "Glucose", Mass = 180.16 };
            reg.Compounds!["glucose"] = glucose;
            reg.Ordered!.Add(glucose);
            reg.Grouped!["sugars"] = reg.Ordered;
            reg.Tagged!.Add(glucose);
            Console.WriteLine($"compounds={reg.Compounds.Count} ordered={reg.Ordered.Count} " +
                $"grouped={reg.Grouped["sugars"].Count} tagged={reg.Tagged.Count}");
            Console.WriteLine($"glucose mass={reg.Compounds["glucose"].Mass} contains={reg.Tagged.Contains(glucose)}");

            // Generic-method type-argument surface: this specialization is
            // named nowhere else in the program.
            var loaded = Deserializer.Deserialize<Dictionary<string, SaveData>>();
            loaded["s1"] = new SaveData { Score = 42 };
            Console.WriteLine($"deserialized count={loaded.Count} score={loaded["s1"].Score}");
        }
    }
}
