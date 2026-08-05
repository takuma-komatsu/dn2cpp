#nullable enable
using System;
using System.Reflection;

namespace ReflectMethodSubset
{
    class Widget
    {
        public int Add(int a, int b) => a + b;
        public void Greet(string who) { _ = who; }
        public static double Scale(double v, double factor) => v * factor;
        private string Secret() => "x";
        public int Add(int only) => only;             // overload of Add
        public virtual int Describe() => 1;

        // Keep private member "used" so it isn't stripped.
        public string TouchSecret() => Secret();
    }

    sealed class SuperWidget : Widget
    {
        public override int Describe() => 2;          // override -> collapses
        public new void Greet(string who) { _ = who; }// new -> both kept
        public bool Ready(int times) => times > 0;
    }

    static class Program
    {
        static string Params(MethodInfo m)
        {
            ParameterInfo[] ps = m.GetParameters();
            string[] parts = new string[ps.Length];
            for (int i = 0; i < ps.Length; i++)
                parts[i] = ps[i].ParameterType.Name + " " + ps[i].Name + "@" + ps[i].Position;
            return string.Join(",", parts);
        }

        static string Describe(MethodInfo m) =>
            $"{m.Name} ret={m.ReturnType.Name} static={m.IsStatic} " +
            $"public={m.IsPublic} decl={m.DeclaringType!.Name} params=[{Params(m)}]";

        // GetMethods/GetParameters order is unspecified; sort for a stable diff. The
        // object-inherited members (ToString/Equals/GetHashCode/GetType) are opaque to
        // dn2cpp, so exclude them on both sides via DeclaringType != typeof(object).
        static void Dump(string tag, MethodInfo[] ms)
        {
            Console.WriteLine($"== {tag} ==");
            // Filter out object-declared methods.
            int n = 0;
            foreach (MethodInfo m in ms)
                if (m.DeclaringType != typeof(object))
                    n++;
            MethodInfo[] kept = new MethodInfo[n];
            int k = 0;
            foreach (MethodInfo m in ms)
                if (m.DeclaringType != typeof(object))
                    kept[k++] = m;
            // Sort by (Name, parameter count).
            Array.Sort(kept, (a, b) =>
            {
                int c = string.CompareOrdinal(a.Name, b.Name);
                return c != 0 ? c : a.GetParameters().Length - b.GetParameters().Length;
            });
            foreach (MethodInfo m in kept)
                Console.WriteLine(Describe(m));
        }

        static void One(MethodInfo? m) =>
            Console.WriteLine(m is null ? "null" : Describe(m));

        const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic |
                                 BindingFlags.Instance | BindingFlags.Static |
                                 BindingFlags.DeclaredOnly;internal static void Run()
        {
            // Public declared methods of Widget (default flags + DeclaredOnly).
            Dump("widget public", typeof(Widget).GetMethods(
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static |
                BindingFlags.DeclaredOnly));

            // All declared (public + private, instance + static).
            Dump("widget all", typeof(Widget).GetMethods(All));

            // Inherited view of SuperWidget (no DeclaredOnly): override collapses to
            // SuperWidget.Describe, `new` keeps both Greet, inherited Add(s) show.
            Dump("super inherited", typeof(SuperWidget).GetMethods(
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static));

            // GetMethod by name.
            Console.WriteLine("== getmethod ==");
            One(typeof(Widget).GetMethod("Scale"));
            One(typeof(Widget).GetMethod("Greet"));
            One(typeof(Widget).GetMethod("Secret",
                BindingFlags.NonPublic | BindingFlags.Instance));
            One(typeof(Widget).GetMethod("Nope"));
            One(typeof(SuperWidget).GetMethod("Ready"));
            One(typeof(SuperWidget).GetMethod("Describe"));   // override -> SuperWidget
        }
    }
}
