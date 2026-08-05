#nullable enable
using System;
using System.Reflection;

namespace ReflectPropSubset
{
    class Model
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public int ReadOnly => 7;            // get-only
        private int Secret { get; set; }     // non-public
        public static double Scale { get; set; }

        // Keep the private property "used" so it isn't stripped.
        public string Touch() => Secret.ToString();
    }

    sealed class Derived : Model
    {
        public bool Flag { get; set; }
    }

    static class Program
    {
        static string Desc(PropertyInfo p) =>
            $"{p.Name} type={p.PropertyType.Name} read={p.CanRead} write={p.CanWrite} decl={p.DeclaringType!.Name}";

        // GetProperties order is unspecified; sort by name for a stable diff.
        static void Dump(string tag, PropertyInfo[] ps)
        {
            Console.WriteLine($"== {tag} ==");
            int n = ps.Length;
            for (int i = 0; i < n; i++)
                for (int j = i + 1; j < n; j++)
                    if (string.CompareOrdinal(ps[j].Name, ps[i].Name) < 0)
                        (ps[i], ps[j]) = (ps[j], ps[i]);
            foreach (PropertyInfo p in ps)
                Console.WriteLine(Desc(p));
        }

        const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic |
                                 BindingFlags.Instance | BindingFlags.Static |
                                 BindingFlags.DeclaredOnly;internal static void Run()
        {
            Dump("model public", typeof(Model).GetProperties(
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly));
            Dump("model all", typeof(Model).GetProperties(All));
            Dump("derived inherited", typeof(Derived).GetProperties());

            Model m = new Model();
            PropertyInfo idp = typeof(Model).GetProperty("Id")!;
            idp.SetValue(m, 123);
            Console.WriteLine($"Id get => {idp.GetValue(m)}");

            PropertyInfo np = typeof(Model).GetProperty("Name")!;
            np.SetValue(m, "hello");
            Console.WriteLine($"Name get => {np.GetValue(m)}");

            Console.WriteLine($"ReadOnly => {typeof(Model).GetProperty("ReadOnly")!.GetValue(m)}");
            Console.WriteLine($"missing = {(typeof(Model).GetProperty("Nope") is null ? "null" : "found")}");
        }
    }
}
