// SUBJECT: the box of a Nullable<T> is a box of T, so what the box dispatches is T's.
// A struct boxed only through a Nullable<T> of it, directly or inside a generic
// method, answers its interface members, a generic interface's included, and two
// such boxes of equal structs compare and hash by value; an empty Nullable<T> boxes
// to null.
using System;

namespace NullableBoxDispatchSubset
{
    interface IShow
    {
        string Show();
    }

    interface IValue<T>
    {
        T Value();
    }

    struct Shown : IShow
    {
        public int V;

        public string Show() => "shown:" + V;
    }

    struct GenericShown : IShow
    {
        public int V;

        public string Show() => "generic:" + V;
    }

    struct Valued : IValue<int>
    {
        public int V;

        public int Value() => V * 2;
    }

    struct Plain
    {
        public int Number;
        public string Text;
    }

    internal static class Program
    {
        private static string ShowBoxed<T>(T? value) where T : struct
        {
            object boxed = value;
            return boxed is null ? "null" : ((IShow)boxed).Show();
        }

        internal static void Run()
        {
            Console.WriteLine("== boxed Nullable<T> dispatch ==");
            Shown? shown = new Shown { V = 3 };
            object direct = shown;
            Console.WriteLine("direct: " + direct.GetType().Name + " " + ((IShow)direct).Show());
            Console.WriteLine("generic: " + ShowBoxed<GenericShown>(new GenericShown { V = 4 })
                + " " + ShowBoxed<GenericShown>(null));
            Valued? valued = new Valued { V = 5 };
            object generic = valued;
            Console.WriteLine("generic interface: " + ((IValue<int>)generic).Value());
            Plain? first = new Plain { Number = 1, Text = "one" };
            Plain? second = new Plain { Number = 1, Text = "one" };
            object a = first, b = second;
            Console.WriteLine("equality: " + a.Equals(b) + "/" + object.Equals(a, b) + "/"
                + (a.GetHashCode() == b.GetHashCode()));
            Shown? none = null;
            object empty = none;
            Console.WriteLine("empty: " + (empty is null));
        }
    }
}
