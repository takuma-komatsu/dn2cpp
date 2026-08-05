#nullable enable
using System;
using System.Reflection;

namespace ReflectInvokeSubset
{
    // An internal class whose methods are reflected + invoked. The reflection-invoke
    // reachability route must reach these even when not called directly.
    class Calc
    {
        public int Last;
        public int Add(int a, int b) => a + b;                 // value args -> value
        public static double Scale(double v, double f) => v * f;// static, value -> value
        public string Concat(string a, string b) => a + b;     // ref args -> ref
        public void Store(int v) => Last = v;                  // void return
        public int Describe() => 99;                           // no args -> value
        private int Secret(int x) => x * 7;                    // reflection-only (reach route)
    }

    struct Vec2
    {
        public int X;
        public int Y;
        public int Sum() => X + Y;                             // value-type receiver
    }

    static class Program
    {internal static void Run()
        {
            Calc c = new Calc();
            Type t = typeof(Calc);

            // Instance value-arg method returning a value.
            object? r1 = t.GetMethod("Add")!.Invoke(c, new object[] { 3, 4 });
            Console.WriteLine($"Add => {r1}");

            // Static value-arg method returning a value (null receiver).
            object? r2 = t.GetMethod("Scale")!.Invoke(null, new object[] { 2.5, 4.0 });
            Console.WriteLine($"Scale => {r2}");

            // Reference args returning a reference.
            object? r3 = t.GetMethod("Concat")!.Invoke(c, new object[] { "foo", "bar" });
            Console.WriteLine($"Concat => {r3}");

            // void method: returns null; observe the side effect.
            object? r4 = t.GetMethod("Store")!.Invoke(c, new object[] { 42 });
            Console.WriteLine($"Store => {(r4 is null ? "null" : r4.ToString())} (Last={c.Last})");

            // No-arg instance method (null args).
            object? r5 = t.GetMethod("Describe")!.Invoke(c, null);
            Console.WriteLine($"Describe => {r5}");

            // Private method discovered + invoked only via reflection.
            MethodInfo secret = t.GetMethod("Secret", BindingFlags.NonPublic | BindingFlags.Instance)!;
            object? r6 = secret.Invoke(c, new object[] { 6 });
            Console.WriteLine($"Secret => {r6}");

            // Value-type receiver: invoke on a boxed struct (payload at obj+1).
            object boxed = new Vec2 { X = 3, Y = 4 };
            object? r7 = typeof(Vec2).GetMethod("Sum")!.Invoke(boxed, null);
            Console.WriteLine($"Sum => {r7}");

            // Round-trip the boxed result through a cast.
            int sum = (int)t.GetMethod("Add")!.Invoke(c, new object[] { 10, 20 })!;
            Console.WriteLine($"Add cast => {sum}");
        }
    }
}
