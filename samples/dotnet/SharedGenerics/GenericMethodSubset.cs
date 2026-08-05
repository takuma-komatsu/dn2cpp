#nullable enable
using System;

namespace GenericMethodSubset
{
    // Generic methods declared on a *generic* type, e.g. Container<T>.Map<U>. The
    // call site is a MethodSpec over a MemberRef with a TypeSpec parent, so the
    // method is instantiated in a context binding the class type args AND the
    // method type args. Also exercises generic-interface dispatch.
    //
    // `#nullable enable` is file-local (the bucket's csproj disables it) and is
    // what the `default!` below needs.
    internal interface IBox<T>
    {
        T Get();
        void Set(T v);
    }

    internal sealed class Box<T> : IBox<T>
    {
        private T _v = default!;
        public T Get() => _v;
        public void Set(T v) => _v = v;
    }

    internal sealed class Container<T>
    {
        private readonly T[] _items = new T[8];
        private int _n;
        public void Add(T x) => _items[_n++] = x;

        // Generic method on the generic type: T from the class, U from the method.
        public U Map<U>(Func<T, U> f, int i) => f(_items[i]);
    }

    // Two class type parameters plus a method type parameter.
    internal sealed class Pair<K, V>
    {
        private readonly K _k;
        private readonly V _v;
        public Pair(K k, V v) { _k = k; _v = v; }
        public R Combine<R>(Func<K, V, R> f) => f(_k, _v);
    }

    // Static-virtual *generic* interface dispatch: a static-abstract method with
    // its own type parameter, invoked through `constrained. call` on a generic
    // type parameter. The closed callee binds the method type arg while the
    // struct's body stays an open template, so the dispatch must re-open the
    // interface template to find and instantiate that body.
    internal interface ISelector<T>
    {
        static abstract U Select<U>(T a, T b, bool first, Func<T, U> conv);
    }

    internal struct FirstSelector<T> : ISelector<T>
    {
        public static U Select<U>(T a, T b, bool first, Func<T, U> conv) => conv(first ? a : b);
    }

    internal static class Dispatch
    {
        public static U Run<TSel, T, U>(T a, T b, bool first, Func<T, U> conv)
            where TSel : ISelector<T>
            => TSel.Select<U>(a, b, first, conv);
    }

    internal static class Program
    {
        private static int ViaIface(IBox<int> b)
        {
            b.Set(42);
            return b.Get();
        }

        internal static void __GateEntry()
        {
            Console.WriteLine(ViaIface(new Box<int>()));        // 42

            var c = new Container<int>();
            c.Add(10);
            c.Add(20);
            Console.WriteLine(c.Map(x => x * 3, 1));            // Map<int>:  60
            Console.WriteLine(c.Map(x => x > 15, 0));           // Map<bool>: False
            Console.WriteLine(c.Map(x => x > 15, 1));           // Map<bool>: True

            var p = new Pair<int, int>(3, 4);
            Console.WriteLine(p.Combine((a, b) => a * a + b * b)); // Combine<int>: 25
            Console.WriteLine(p.Combine((a, b) => a < b));         // Combine<bool>: True

            // Closed at three (T, U) shapes, including a reference-type T.
            Console.WriteLine(Dispatch.Run<FirstSelector<int>, int, int>(7, 9, true, x => x * 2));   // 14
            Console.WriteLine(Dispatch.Run<FirstSelector<int>, int, bool>(7, 9, false, x => x > 8)); // True
            Console.WriteLine(Dispatch.Run<FirstSelector<string>, string, int>("ab", "c", true, s => s.Length)); // 2
        }
    }
}
