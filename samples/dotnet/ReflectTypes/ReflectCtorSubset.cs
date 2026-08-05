#nullable enable
using System;
using System.Reflection;

namespace ReflectCtorSubset
{
    // Constructed only via reflection — the reflection-ctor reachability route
    // must reach these ctors (and allocate the type) even with no direct `new Box(...)`.
    class Box
    {
        public int A;
        public string S;
        public Box() { A = -1; S = "default"; }
        public Box(int a) { A = a; S = "one-arg"; }
        public Box(int a, string s) { A = a; S = s; }
    }

    struct Pt
    {
        public int X;
        public int Y;
        public Pt(int x, int y) { X = x; Y = y; }
    }

    static class Program
    {internal static void Run()
        {
            Type tb = typeof(Box);

            // Enumerate constructors (order unspecified -> sort by arg count).
            ConstructorInfo[] ctors = tb.GetConstructors();
            int n = ctors.Length;
            for (int i = 0; i < n; i++)
                for (int j = i + 1; j < n; j++)
                    if (ctors[j].GetParameters().Length < ctors[i].GetParameters().Length)
                        (ctors[i], ctors[j]) = (ctors[j], ctors[i]);
            Console.WriteLine($"ctor count = {n}");
            foreach (ConstructorInfo ci in ctors)
            {
                ParameterInfo[] ps = ci.GetParameters();
                string[] parts = new string[ps.Length];
                for (int i = 0; i < ps.Length; i++)
                    parts[i] = ps[i].ParameterType.Name;
                Console.WriteLine($"ctor({string.Join(",", parts)}) public={ci.IsPublic}");
            }

            // GetConstructor(Type[]) + ConstructorInfo.Invoke(object[]).
            ConstructorInfo two = tb.GetConstructor(new Type[] { typeof(int), typeof(string) })!;
            Box b2 = (Box)two.Invoke(new object[] { 7, "hi" });
            Console.WriteLine($"two-arg => A={b2.A} S={b2.S}");

            ConstructorInfo one = tb.GetConstructor(new Type[] { typeof(int) })!;
            Box b1 = (Box)one.Invoke(new object[] { 42 });
            Console.WriteLine($"one-arg => A={b1.A} S={b1.S}");

            // Parameterless ctor via reflection + via Activator.CreateInstance(Type).
            ConstructorInfo zero = tb.GetConstructor(Type.EmptyTypes)!;
            Box b0 = (Box)zero.Invoke(null);
            Console.WriteLine($"zero-arg => A={b0.A} S={b0.S}");

            Box ba = (Box)Activator.CreateInstance(tb)!;
            Console.WriteLine($"activator class => A={ba.A} S={ba.S}");

            // Missing match -> null.
            Console.WriteLine($"no bool ctor = {(tb.GetConstructor(new Type[] { typeof(bool) }) is null ? "null" : "found")}");

            // Value-type ctor via reflection (boxed result).
            ConstructorInfo pc = typeof(Pt).GetConstructor(new Type[] { typeof(int), typeof(int) })!;
            Pt p = (Pt)pc.Invoke(new object[] { 3, 4 });
            Console.WriteLine($"pt ctor => ({p.X},{p.Y})");

            // Activator on a struct with no explicit parameterless ctor -> zero-init.
            Pt pz = (Pt)Activator.CreateInstance(typeof(Pt))!;
            Console.WriteLine($"activator struct => ({pz.X},{pz.Y})");
        }
    }
}
