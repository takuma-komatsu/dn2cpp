using System;
using System.Text;

// SUBJECT: Activator.CreateInstance<T>(). The generic factory's real body
// reflects, so it is lowered inline to `new T()`: a reference type allocates and
// runs its parameterless ctor, a value type is the zero value plus an explicit
// struct ctor if it has one. Reachability cuts the real body and reaches T's ctor
// and allocated type instead. The `new T()` / new() constraint idiom is the same
// subject — Roslyn lowers it to this call.
namespace ActivatorSubset
{
    // Reference type with the implicit parameterless ctor (field initializer only).
    class Widget { public int X = 42; public override string ToString() => $"Widget(X={X})"; }

    // Reference type with an explicit parameterless ctor.
    class Counter { public int N; public Counter() { N = 7; } public override string ToString() => $"Counter(N={N})"; }

    // Value type — default-initialized.
    struct Point { public int X, Y; public override string ToString() => $"Point({X},{Y})"; }

    // Value type with an explicit parameterless ctor (C# 10+).
    struct WithCtor { public int V; public WithCtor() { V = 99; } public override string ToString() => $"WithCtor(V={V})"; }

    // An object-pool policy's shape: `new T()` where T's ctor is named from nowhere
    // else. For an intrinsic-mapped T the lowering must route to the intrinsic
    // construction, not name a ctor symbol an intrinsic type never transpiles.
    sealed class Policy<T> where T : class, new()
    {
        public T Create() => new T();
    }

    static class Program
    {
        // `new T()` under a new() constraint — Roslyn lowers it to Activator.CreateInstance<T>().
        static T Make<T>() where T : new() => new T();

        internal static void Run()
        {
            // Direct generic Activator.CreateInstance<T>().
            Console.WriteLine("== CreateInstance<T> ==");
            Console.WriteLine(Activator.CreateInstance<Widget>());   // Widget(X=42)
            Console.WriteLine(Activator.CreateInstance<Counter>());  // Counter(N=7)
            Console.WriteLine(Activator.CreateInstance<Point>());    // Point(0,0)
            Console.WriteLine(Activator.CreateInstance<int>());      // 0
            Console.WriteLine(Activator.CreateInstance<WithCtor>()); // WithCtor(V=99)

            // The `new T()` / new() constraint idiom.
            Console.WriteLine("== new T() (new() constraint) ==");
            Console.WriteLine(Make<Widget>());   // Widget(X=42)
            Console.WriteLine(Make<Counter>());  // Counter(N=7)
            Console.WriteLine(Make<Point>());    // Point(0,0)
            Console.WriteLine(Make<WithCtor>()); // WithCtor(V=99)

            // Intrinsic-mapped REFERENCE type T: its ctor is emitted inline, never
            // transpiled, so `new T()` must lower to the intrinsic construction.
            // Reached three ways — direct, the new() idiom, and through a policy.
            Console.WriteLine("== intrinsic reference T ==");
            StringBuilder sb = Activator.CreateInstance<StringBuilder>();
            sb.Append("act");
            Console.WriteLine(sb.ToString());                              // act
            Console.WriteLine(Make<StringBuilder>().Append("mk").ToString());   // mk
            StringBuilder ps = new Policy<StringBuilder>().Create();
            ps.Append("pool");
            Console.WriteLine(ps.ToString());                             // pool
            Console.WriteLine(ps.GetType().Name);                         // StringBuilder

            // Intrinsic-mapped VALUE type T (decimal) — the zero value, never a
            // ctor-symbol call.
            Console.WriteLine("== intrinsic value T ==");
            Console.WriteLine(Activator.CreateInstance<decimal>());       // 0
            Console.WriteLine(Make<decimal>());                           // 0
        }
    }
}
