#nullable enable
using System;
using System.Reflection;

namespace ReflectFieldSubset
{
    class Gadget
    {
        public int PubInst;
        private string privInst = "";
        public static double PubStat;
        private static bool privStat;

        // Keep the private members "used" so the compiler doesn't warn/strip.
        public string Touch() => privInst + privStat;
    }

    sealed class SuperGadget : Gadget
    {
        public Gadget? Extra;
    }

    static class Program
    {
        // GetFields() order is unspecified, so sort by name for a stable diff.
        static void Dump(FieldInfo[] fields)
        {
            Array.Sort(fields, (a, b) => string.CompareOrdinal(a.Name, b.Name));
            foreach (FieldInfo f in fields)
                Console.WriteLine($"{f.Name} {f.FieldType.FullName} static={f.IsStatic} public={f.IsPublic} decl={f.DeclaringType!.Name}");
        }

        static void One(FieldInfo? f) =>
            Console.WriteLine(f is null
                ? "null"
                : $"{f.Name} {f.FieldType.FullName} static={f.IsStatic} public={f.IsPublic} decl={f.DeclaringType!.Name}");internal static void Run()
        {
            // GetFields() default = Public | Instance | Static (private excluded).
            Console.WriteLine("== default ==");
            Dump(typeof(Gadget).GetFields());

            // All declared fields of the type (public + private, instance + static).
            Console.WriteLine("== all declared ==");
            Dump(typeof(Gadget).GetFields(
                BindingFlags.Public | BindingFlags.NonPublic |
                BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly));

            // Inherited public fields are included by default (not the inherited statics
            // without FlattenHierarchy — PubStat is inherited static, so it is excluded).
            Console.WriteLine("== inherited default ==");
            Dump(typeof(SuperGadget).GetFields());

            // GetField by name.
            Console.WriteLine("== getfield ==");
            One(typeof(Gadget).GetField("PubInst"));
            One(typeof(Gadget).GetField("PubStat"));
            One(typeof(Gadget).GetField("privInst",
                BindingFlags.NonPublic | BindingFlags.Instance));
            One(typeof(Gadget).GetField("nope"));
            One(typeof(SuperGadget).GetField("Extra"));
        }
    }
}
