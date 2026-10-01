using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Intrinsics;
using System.Text;

namespace ArrayDefaultOrderSubset
{
    // The DEFAULT order of an element with no IComparable<T> of its own, and what a sort
    // or search reports when a comparison throws. Comparer<T>.Default is ObjectComparer<T>
    // for such a T — the boxed non-generic IComparable order, which throws
    // ArgumentException for a T that has none — and NullableComparer<U> for U?; a public
    // CompareTo(T) without the interface is not an order. Sort and BinarySearch compare
    // nothing below two elements (one for a search), and report what a comparison throws
    // as InvalidOperationException around it — or, for an IndexOutOfRangeException from a
    // sort, the ArgumentException naming the comparer .NET's ArraySortHelper names. A
    // boxed framework struct whose type argument is a reference type holding a SIMD vector
    // still compares that argument by its own Equals.
    internal struct Plain
    {
        public int V;
        public Plain(int v) { V = v; }
        public override string ToString() => "P" + V;
    }

    internal struct Boxed : IComparable
    {
        public int V;
        public Boxed(int v) { V = v; }
        public int CompareTo(object o) => V.CompareTo(((Boxed)o).V);
        public override string ToString() => "B" + V;
    }

    // A typed CompareTo without IComparable<T> is not the default order.
    internal struct TypedOnly
    {
        public int V;
        public TypedOnly(int v) { V = v; }
        public int CompareTo(TypedOnly o) => V.CompareTo(o.V);
        public override string ToString() => "T" + V;
    }

    // Orders by CompareTo(object); the reversed typed overload is not the default order.
    internal struct BoxedAndTyped : IComparable
    {
        public int V;
        public BoxedAndTyped(int v) { V = v; }
        public int CompareTo(object o) => V.CompareTo(((BoxedAndTyped)o).V);
        public int CompareTo(BoxedAndTyped o) => o.V.CompareTo(V);
        public override string ToString() => "BT" + V;
    }

    internal sealed class PlainRef
    {
        public int V;
        public PlainRef(int v) { V = v; }
        public override string ToString() => "p" + V;
    }

    internal sealed class BoxedRef : IComparable
    {
        public int V;
        public BoxedRef(int v) { V = v; }
        public int CompareTo(object o) => o is BoxedRef r ? V.CompareTo(r.V) : 1;
        public override string ToString() => "b" + V;
    }

    internal class Base : IComparable<Base>
    {
        public int V;
        public int CompareTo(Base o) => V.CompareTo(o.V);
    }

    // IComparable<Base> reaches IComparable<Derived> through contravariance.
    internal sealed class Derived : Base
    {
        public Derived(int v) { V = v; }
        public override string ToString() => "d" + V;
    }

    internal interface IOutput<out T> { T Get(); }
    internal class Animal { }
    internal sealed class Dog : Animal { }

    internal sealed class NestedOrder : IOutput<Dog>, IComparable<IOutput<Animal>>
    {
        public int V;
        public NestedOrder(int v) { V = v; }
        Dog IOutput<Dog>.Get() => new Dog();
        public int CompareTo(IOutput<Animal> other) => other is NestedOrder order ? V.CompareTo(order.V) : 1;
        public override string ToString() => "n" + V;
    }

    // IComparable<int> is not an order of the class itself.
    internal sealed class OtherArg : IComparable<int>
    {
        public int V;
        public OtherArg(int v) { V = v; }
        public int CompareTo(int o) => V.CompareTo(o);
        public override string ToString() => "o" + V;
    }

    internal struct TypedFault : IComparable<TypedFault>
    {
        public int CompareTo(TypedFault o) => throw new IndexOutOfRangeException("typed");
    }

    internal struct BoxedFault : IComparable
    {
        public int CompareTo(object o) => throw new IndexOutOfRangeException("boxed");
    }

    internal sealed class FaultComparer : IComparer<int>
    {
        public int Compare(int a, int b) => throw new IndexOutOfRangeException("comparer");
    }

    internal sealed class FaultObjectComparer : IComparer
    {
        public int Compare(object a, object b) => throw new IndexOutOfRangeException("object comparer");
    }

    internal sealed class NamedFaultComparer : IComparer<int>, IComparer
    {
        public string Name;
        public bool ThrowName;
        public int NameCalls;
        public int Compare(int a, int b) => throw new IndexOutOfRangeException("named fault");
        public int Compare(object a, object b) => Compare((int)a, (int)b);
        public override string ToString()
        {
            NameCalls++;
            GC.Collect();
            if (ThrowName)
                throw new FormatException("name fault");
            return Name;
        }
    }

    internal static class Program
    {
        private static string Units(string text)
        {
            if (text is null)
                return "null";
            var result = new StringBuilder();
            foreach (char c in text)
                result.Append(((int)c).ToString("X4"));
            return result.ToString();
        }

        private static string FaultUnits(Action action)
        {
            try
            {
                action();
                return "unexpected";
            }
            catch (Exception ex)
            {
                return ex.GetType().Name + "/" + Units(ex.Message) + "/"
                    + Units((ex as ArgumentException)?.ParamName) + "/"
                    + (ex.InnerException is null ? "null" : ex.InnerException.GetType().Name);
            }
        }

        private static void NamedFaults()
        {
            Console.WriteLine("== comparer message UTF-16 ==");
            foreach (string name in new string[] { null, "", "ascii", "\0\uD800\uDC00\uD801x\uDC01" })
            {
                var comparer = new NamedFaultComparer { Name = name };
                Console.WriteLine("named array " + Units(name) + "="
                    + FaultUnits(() => Array.Sort(new[] { 2, 1 }, comparer)));
                Console.WriteLine("named span " + Units(name) + "="
                    + FaultUnits(() => new[] { 2, 1 }.AsSpan().Sort(comparer)));
                Console.WriteLine("named System.Array " + Units(name) + "="
                    + FaultUnits(() => Array.Sort((Array)new[] { 2, 1 }, comparer)));
                Console.WriteLine("named calls=" + comparer.NameCalls);
            }
            var throwing = new NamedFaultComparer { ThrowName = true };
            Console.WriteLine("throwing name sort="
                + FaultUnits(() => Array.Sort(new[] { 2, 1 }, throwing)));
            Console.WriteLine("throwing name search="
                + FaultUnits(() => Array.BinarySearch(new[] { 1, 2 }, 1, throwing)));
            Console.WriteLine("throwing name calls=" + throwing.NameCalls);
        }

        private static string Join<T>(IEnumerable<T> xs)
        {
            var sb = new StringBuilder();
            foreach (var x in xs)
            {
                if (sb.Length > 0)
                    sb.Append(',');
                sb.Append(x is null ? "null" : x.ToString());
            }
            return sb.ToString();
        }

        private static void Try(string label, Func<string> f)
        {
            string r;
            try
            {
                r = f();
            }
            catch (Exception e)
            {
                r = e.GetType().Name + ": " + e.Message;
                if (e.InnerException is { } i)
                    r += " <- " + i.GetType().Name + ": " + i.Message;
            }
            Console.WriteLine(label + " => " + r);
        }

        private static void Orders<T>(string n, Func<int, T> mk)
        {
            Try(n + " sort 0", () => { var a = new T[0]; Array.Sort(a); return "ok"; });
            Try(n + " sort 1", () => { var a = new[] { mk(5) }; Array.Sort(a); return Join(a); });
            Try(n + " sort 3", () => { var a = new[] { mk(3), mk(1), mk(2) }; Array.Sort(a); return Join(a); });
            Try(n + " sort null comparer", () => { var a = new[] { mk(3), mk(1), mk(2) }; Array.Sort(a, (IComparer<T>)null); return Join(a); });
            Try(n + " sort range 1", () => { var a = new[] { mk(3), mk(1), mk(2) }; Array.Sort(a, 1, 1); return Join(a); });
            Try(n + " sort pair", () => { var a = new[] { mk(3), mk(1), mk(2) }; var b = new[] { "c", "a", "b" }; Array.Sort(a, b); return Join(b); });
            Try(n + " sort pair 1", () => { var a = new[] { mk(3) }; var b = new[] { "c" }; Array.Sort(a, b); return Join(b); });
            Try(n + " sort System.Array", () => { var a = new[] { mk(3), mk(1), mk(2) }; Array.Sort((Array)a); return Join(a); });
            Try(n + " span sort", () => { var a = new[] { mk(3), mk(1), mk(2) }; a.AsSpan().Sort(); return Join(a); });
            Try(n + " span sort 1", () => { var a = new[] { mk(3) }; a.AsSpan().Sort(); return Join(a); });
            Try(n + " list sort", () => { var l = new List<T> { mk(3), mk(1), mk(2) }; l.Sort(); return Join(l); });
            Try(n + " list sort 1", () => { var l = new List<T> { mk(4) }; l.Sort(); return Join(l); });
            Try(n + " search 0", () => Array.BinarySearch(new T[0], mk(1)).ToString());
            Try(n + " search 1", () => Array.BinarySearch(new[] { mk(1) }, mk(1)).ToString());
            Try(n + " search 3", () => Array.BinarySearch(new[] { mk(1), mk(2), mk(3) }, mk(3)).ToString());
            Try(n + " search null comparer", () => Array.BinarySearch(new[] { mk(1), mk(2), mk(3) }, mk(2), null).ToString());
            Try(n + " list search", () => new List<T> { mk(1), mk(2), mk(3) }.BinarySearch(mk(2)).ToString());
            Try(n + " default comparer", () => Comparer<T>.Default.GetType().Name);
            Try(n + " default compare", () => Comparer<T>.Default.Compare(mk(1), mk(3)).ToString());
            Try(n + " default interface compare", () => { IComparer<T> c = Comparer<T>.Default; return c.Compare(mk(3), mk(1)).ToString(); });
        }

        internal static void Run()
        {
            Console.WriteLine("== default order without IComparable<T> ==");
            Orders("plain struct", v => new Plain(v));
            Orders("IComparable struct", v => new Boxed(v));
            Orders("typed-only struct", v => new TypedOnly(v));
            Orders("IComparable and typed struct", v => new BoxedAndTyped(v));
            Orders("plain class", v => new PlainRef(v));
            Orders("IComparable class", v => new BoxedRef(v));
            Orders("contravariant class", v => new Derived(v));
            Orders("nested contravariant class", v => new NestedOrder(v));
            Orders("other-argument class", v => new OtherArg(v));
            Orders("nullable int", v => v == 2 ? null : (int?)v);
            Orders("nullable plain struct", v => (Plain?)new Plain(v));

            Console.WriteLine("== comparison faults in sorts and searches ==");
            Try("typed sort", () => { Array.Sort(new[] { new TypedFault(), new TypedFault() }); return "ok"; });
            Try("typed pair sort", () => { Array.Sort(new[] { new TypedFault(), new TypedFault() }, new[] { 1, 2 }); return "ok"; });
            Try("typed list sort", () => { new List<TypedFault> { new TypedFault(), new TypedFault() }.Sort(); return "ok"; });
            Try("typed search", () => Array.BinarySearch(new[] { new TypedFault(), new TypedFault() }, new TypedFault()).ToString());
            Try("boxed sort", () => { Array.Sort(new[] { new BoxedFault(), new BoxedFault() }); return "ok"; });
            Try("boxed pair sort", () => { Array.Sort(new[] { new BoxedFault(), new BoxedFault() }, new[] { 1, 2 }); return "ok"; });
            Try("boxed span sort", () => { new[] { new BoxedFault(), new BoxedFault() }.AsSpan().Sort(); return "ok"; });
            Try("comparer sort", () => { Array.Sort(new[] { 1, 2 }, new FaultComparer()); return "ok"; });
            Try("comparer pair sort", () => { Array.Sort(new[] { 1, 2 }, new[] { 1, 2 }, new FaultComparer()); return "ok"; });
            Try("comparer span sort", () => { new[] { 1, 2 }.AsSpan().Sort(new FaultComparer()); return "ok"; });
            Try("comparer list sort", () => { new List<int> { 1, 2 }.Sort(new FaultComparer()); return "ok"; });
            Try("comparer search", () => Array.BinarySearch(new[] { 1, 2 }, 1, new FaultComparer()).ToString());
            Try("comparison sort", () => { Array.Sort(new[] { 1, 2 }, (a, b) => throw new IndexOutOfRangeException("c")); return "ok"; });
            Try("comparison list sort", () => { new List<int> { 1, 2 }.Sort((a, b) => throw new IndexOutOfRangeException("c")); return "ok"; });
            Try("comparison span sort", () => { new[] { 1, 2 }.AsSpan().Sort((a, b) => throw new IndexOutOfRangeException("c")); return "ok"; });
            Try("comparison format sort", () => { Array.Sort(new[] { 3, 1, 2 }, (a, b) => throw new FormatException("f")); return "ok"; });
            Try("comparison format list sort", () => { new List<int> { 3, 1, 2 }.Sort((a, b) => throw new FormatException("f")); return "ok"; });
            Try("object sort", () => { Array.Sort(new object[] { new PlainRef(1), new PlainRef(2) }); return "ok"; });
            Try("System.Array sort", () => { Array.Sort((Array)new object[] { new BoxedFault(), new BoxedFault() }); return "ok"; });
            Try("System.Array comparer sort", () => { Array.Sort((Array)new object[] { 1, 2 }, new FaultObjectComparer()); return "ok"; });
            Try("System.Array search", () => Array.BinarySearch((Array)new object[] { new BoxedFault(), new BoxedFault() }, new BoxedFault()).ToString());
            Try("System.Array mismatched search", () => Array.BinarySearch((Array)new object[] { 1, 2, 3 }, "x").ToString());
            Try("ArrayList sort", () => { new ArrayList { new BoxedFault(), new BoxedFault() }.Sort(); return "ok"; });

            Console.WriteLine("== boxed tuple over a list of vectors ==");
            var vl = new List<Vector128<float>> { Vector128.Create(1f) };
            object bt = (vl, 1);
            Console.WriteLine(bt.Equals((vl, 1)));
            Console.WriteLine(bt.Equals((new List<Vector128<float>>(), 1)));
            Console.WriteLine(bt.Equals((vl, 2)));
            Console.WriteLine(bt.GetHashCode() == ((object)(vl, 1)).GetHashCode());
            NamedFaults();
            Console.WriteLine("default order end");
        }
    }
}
