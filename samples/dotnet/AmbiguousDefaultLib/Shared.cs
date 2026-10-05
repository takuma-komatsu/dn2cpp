using System.Collections.Generic;

namespace AmbiguousDefaultLib
{
    // Both library versions compile this file; only Right.cs differs.
    public interface IBase
    {
        string Pick(int count, string label) => "base";
        string PickGeneric<T>() => "base";
        string Unused() => "base";
        string UnusedGeneric<T>() => "base";
        string Plain() => "base";
        // The ambiguity message spells a nested type by its own name, so both
        // overloads read Find(Key) while their return types differ in ABI.
        string Find(First.Key key) => "base";
        int Find(Second.Key key) => 0;
    }

    public interface ILeft : IBase
    {
        string IBase.Pick(int count, string label) => "left";
        string IBase.PickGeneric<T>() => "left";
        string IBase.Unused() => "left";
        string IBase.UnusedGeneric<T>() => "left";
        string IBase.Plain() => "left";
        string IBase.Find(First.Key key) => "left";
        int IBase.Find(Second.Key key) => 1;
    }

    public sealed class First
    {
        public sealed class Key
        {
        }
    }

    public sealed class Second
    {
        public sealed class Key
        {
        }
    }

    public interface IBox<T>
    {
        string Take(T item, List<T> items) => "base";
        string Select<U>(T item, U value) => "base";
        string Pair<X, Y>() => "base";
        static virtual string StaticSelect<U>() => "base";
        static abstract string StaticAbstract<U>();
    }

    public interface IBoxLeft<T> : IBox<T>
    {
        string IBox<T>.Take(T item, List<T> items) => "left";
        string IBox<T>.Select<U>(T item, U value) => "left";
        string IBox<T>.Pair<X, Y>() => "left";
        static string IBox<T>.StaticSelect<U>() => "left";
        static string IBox<T>.StaticAbstract<U>() => "left";
    }

    public interface IDuo<TFirst, TSecond>
    {
        string Single<V>(TFirst first, TSecond second, V value) => "base";
        string Pair<X, Y>() => "base";
        string Triple<X, Y, Z>() => "base";
    }

    public interface IDuoLeft<TFirst, TSecond> : IDuo<TFirst, TSecond>
    {
        string IDuo<TFirst, TSecond>.Single<V>(TFirst first, TSecond second, V value) => "left";
        string IDuo<TFirst, TSecond>.Pair<X, Y>() => "left";
        string IDuo<TFirst, TSecond>.Triple<X, Y, Z>() => "left";
    }
    public interface IStaticBase
    {
        static virtual string Default() => "base";
        static virtual string DefaultGeneric<U>() => "base";
        static abstract string Abstract();
        static abstract string AbstractGeneric<U>();
    }

    public interface IStaticLeft : IStaticBase
    {
        static string IStaticBase.Default() => "left";
        static string IStaticBase.DefaultGeneric<U>() => "left";
        static string IStaticBase.Abstract() => "left";
        static string IStaticBase.AbstractGeneric<U>() => "left";
    }
    public interface IStaticResolved : IStaticLeft, IStaticRight
    {
        static string IStaticBase.Default() => "specific";
        static string IStaticBase.DefaultGeneric<U>() => "specific";
        static string IStaticBase.Abstract() => "specific";
        static string IStaticBase.AbstractGeneric<U>() => "specific";
    }
}
