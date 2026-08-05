#nullable disable
using System;
using System.Reflection;

// Assembly-level custom attributes. Assembly.GetCustomAttributes([attrType,] inherit)
// materializes the attributes applied with [assembly: ...] — including Type and Type[]
// constructor arguments (each Type materializes as its runtime Type handle) plus named
// properties — and Assembly identity: typeof(X).Assembly equals GetEntryAssembly() for
// an app type, and GetName().Name reports the assembly simple name. Real .NET also
// reports compiler/BCL assembly attributes (TargetFramework, ...), so the enumeration
// filters to this section's namespace; attribute order is unspecified, so results are
// sorted by type name before printing. Beta is referenced ONLY through the attribute's
// Type[] argument, proving typeof-arg keep-alive.

[assembly: ReflectAsmAttrSubset.AsmMeta("x", 3)]
[assembly: ReflectAsmAttrSubset.AsmTypes(typeof(ReflectAsmAttrSubset.Alpha),
    new[] { typeof(ReflectAsmAttrSubset.Alpha), typeof(ReflectAsmAttrSubset.Beta) },
    Label = "tagged")]

namespace ReflectAsmAttrSubset
{
    [AttributeUsage(AttributeTargets.Assembly)]
    sealed class AsmMetaAttribute : Attribute
    {
        public string Name { get; }
        public int Order { get; }
        public AsmMetaAttribute(string name, int order) { Name = name; Order = order; }
    }

    [AttributeUsage(AttributeTargets.Assembly)]
    sealed class AsmTypesAttribute : Attribute
    {
        public Type Main { get; }
        public Type[] Types { get; }
        public string Label { get; set; } = "";
        public AsmTypesAttribute(Type main, Type[] types) { Main = main; Types = types; }
    }

    class Alpha { }
    class Beta { }

    static class Program
    {
        internal static void Run()
        {
            Assembly asm = Assembly.GetEntryAssembly();

            object[] attrs = asm.GetCustomAttributes(false);
            int mine = 0;
            foreach (var a in attrs)
                if (a.GetType().Namespace == "ReflectAsmAttrSubset")
                    mine++;
            object[] ours = new object[mine];
            int k = 0;
            foreach (var a in attrs)
                if (a.GetType().Namespace == "ReflectAsmAttrSubset")
                    ours[k++] = a;
            Array.Sort(ours, (x, y) => string.CompareOrdinal(x.GetType().Name, y.GetType().Name));

            Console.WriteLine("== assembly attrs ==");
            foreach (var a in ours)
            {
                if (a is AsmMetaAttribute m)
                    Console.WriteLine($"  AsmMeta Name={m.Name} Order={m.Order}");
                else if (a is AsmTypesAttribute t)
                {
                    string names = "";
                    foreach (var ty in t.Types)
                        names = names.Length == 0 ? ty.Name : names + "," + ty.Name;
                    Console.WriteLine($"  AsmTypes Main={t.Main.Name} Types={names} Label={t.Label}");
                }
                else
                    Console.WriteLine($"  ?{a.GetType().Name}");
            }

            Console.WriteLine($"typed-count {asm.GetCustomAttributes(typeof(AsmMetaAttribute), false).Length}");
            Console.WriteLine($"entry-identity {typeof(Alpha).Assembly == Assembly.GetEntryAssembly()}");
            Console.WriteLine($"asm-name {asm.GetName().Name}");
        }
    }
}
