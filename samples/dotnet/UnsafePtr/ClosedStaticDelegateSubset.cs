using System;
using System.Collections.Generic;
using System.Linq;

namespace ClosedStaticDelegateSubset
{
    // A delegate CLOSED over a static's first argument. The target slot carries
    // that argument, so Invoke takes one argument fewer than the static — hence
    // two adapter shapes keyed together with the method
    // (Compilation.DelegateAdapter); the open one alone shifts every argument by
    // one. The bound argument is always a reference type, which is not a gap: C#
    // refuses the method-group form over a value-type receiver (CS1113) and real
    // .NET rejects the reflection form too (ReflectInvoke's ReflectDelegateSubset
    // asserts that), so the transpiler fails loudly rather than modelling it.

    internal enum Ctx { None = 0, Menu = 1, Game = 2 }

    internal interface IShape
    {
        int Sides { get; }
    }

    internal class Poly : IShape
    {
        public virtual int Sides => 4;
    }

    internal sealed class Tri : Poly
    {
        public override int Sides => 3;
    }

    internal static class Util
    {
        // Generic extension over an interface, receiver an array: the bound argument
        // is interface-typed, and with an enum element the body is shared canonical.
        public static bool Contains<T>(this IReadOnlyList<T> list, T value)
        {
            for (int i = 0; i < list.Count; ++i)
                if (EqualityComparer<T>.Default.Equals(list[i], value))
                    return true;
            return false;
        }

        // Bound argument IS the type parameter — a canonical placeholder in a
        // shared body.
        public static string Describe<T>(this T item, int n)
            where T : class
            => item is null ? "null#" + n : item.ToString() + "#" + n;

        // One static reached BOTH ways: closed over its first parameter, and open.
        public static string Tag(this string a, int b)
            => a + ":" + b;

        public static void Log(this string a, int b)
            => Console.WriteLine("log " + a + " " + b);

        public static void Ping(this string a)
            => Console.WriteLine("ping " + a);

        // Bound-argument kinds: interface, base class, array, object.
        public static int SideCount(this IShape s)
            => s.Sides;

        public static int Doubled(this Poly p)
            => p.Sides * 2;

        public static int Total(this int[] xs)
        {
            int t = 0;
            foreach (int x in xs)
                t += x;
            return t;
        }

        public static string Kind(this object o)
            => o is null ? "none" : o.GetType().Name;

        // Proves the target slot carries the real object, not a copy.
        public static void Push(this List<int> into, int v)
            => into.Add(v);
    }

    internal static class Program
    {
        internal static void __GateEntry()
        {
            Console.WriteLine("== closed static delegates ==");

            // 1. Extension-method group over an array, handed to LINQ as a
            // Func<T, bool>: an enum element type (shared body) and a reference one.
            Ctx[] contexts = new[] { Ctx.Menu, Ctx.Game };
            Ctx[] disallow = new[] { Ctx.Game };
            Ctx[] allowed = new[] { Ctx.None };
            Console.WriteLine($"enum-any-hit => {disallow.Any(contexts.Contains)}");
            Console.WriteLine($"enum-any-miss => {allowed.Any(contexts.Contains)}");
            string[] names = new[] { "a", "b", "c" };
            Console.WriteLine($"str-any => {new[] { "b", "z" }.Any(names.Contains)}");
            Console.WriteLine($"str-where => {string.Join(",", new[] { "c", "z", "a" }.Where(names.Contains))}");
            Console.WriteLine($"str-count => {new[] { "a", "z", "c" }.Count(names.Contains)}");

            Func<Ctx, bool> known = contexts.Contains;
            Console.WriteLine($"direct => {known(Ctx.Game)}|{known(Ctx.None)}");

            // 2. The SAME static in both shapes, in one program.
            Func<string, int, string> openTag = Util.Tag;
            Func<int, string> closedTag = "hi".Tag;
            Console.WriteLine($"open => {openTag("a", 1)}");
            Console.WriteLine($"closed => {closedTag(2)}");
            Console.WriteLine($"open-again => {openTag("b", 3)}");

            // 3. Bound-argument kinds, including a null target.
            IShape tri = new Tri();
            Func<int> sides = tri.SideCount;
            Console.WriteLine($"itf => {sides()}");
            Poly poly = new Tri();
            Func<int> doubled = poly.Doubled;
            Console.WriteLine($"base-derived => {doubled()}");
            Func<int> total = new[] { 3, 4, 5 }.Total;
            Console.WriteLine($"array => {total()}");
            Func<string> kind = ((object)tri).Kind;
            Console.WriteLine($"object => {kind()}");
            string nil = null;
            Func<string> nilKind = ((object)nil).Kind;
            Console.WriteLine($"null-target => {nilKind()}");
            Func<int, string> nilTag = nil.Tag;
            Console.WriteLine($"null-target-str => {nilTag(9)}");

            // 4. void return, and the 0-argument closed form.
            Action<int> logTo = "v".Log;
            logTo(7);
            Action ping = "solo".Ping;
            ping();

            // 5. A generic static whose bound argument IS the type parameter.
            Func<int, string> descStr = "gen".Describe;
            Console.WriteLine($"generic-bound => {descStr(1)}");
            Func<int, string> descObj = tri.Describe;
            Console.WriteLine($"generic-bound-itf => {descObj(2)}");

            // 6. Multicast: each node keeps its own bound target.
            Action<int> chained = "left".Log;
            chained += "right".Log;
            chained(0);

            // 7. Target is the bound argument, and two bindings of the same
            // (static, target) pair compare equal. Delegate.Method is deliberately
            // NOT asserted: an IL-constructed delegate carries no metadata
            // back-reference here, so it reports null where .NET reports a MethodInfo.
            Console.WriteLine($"target => {ReferenceEquals(known.Target, contexts)}");
            Func<Ctx, bool> known2 = contexts.Contains;
            Console.WriteLine($"equal => {known == known2}");
            Func<Ctx, bool> other = disallow.Contains;
            Console.WriteLine($"not-equal => {known == other}");

            // 8. The bound target is the object itself, not a copy.
            List<int> sink = new List<int>();
            Action<int> push = sink.Push;
            push(11);
            push(22);
            Console.WriteLine($"mutates-target => {string.Join(",", sink)}");
        }
    }
}
