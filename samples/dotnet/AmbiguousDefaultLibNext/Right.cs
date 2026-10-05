using System.Collections.Generic;

namespace AmbiguousDefaultLib
{
    // IRight now overrides every IBase method but Plain, and IBoxRight
    // overrides Take, so a class implementing both sides of either pair has no
    // most specific body for them.
    public interface IRight : IBase
    {
        string IBase.Pick(int count, string label) => "right";
        string IBase.PickGeneric<T>() => "right";
        string IBase.Unused() => "right";
        string IBase.UnusedGeneric<T>() => "right";
        string IBase.Find(First.Key key) => "right";
        int IBase.Find(Second.Key key) => 2;
    }

    public interface IBoxRight<T> : IBox<T>
    {
        string IBox<T>.Take(T item, List<T> items) => "right";
        string IBox<T>.Select<U>(T item, U value) => "right";
        string IBox<T>.Pair<X, Y>() => "right";
    }

    public interface IDuoRight<TFirst, TSecond> : IDuo<TFirst, TSecond>
    {
        string IDuo<TFirst, TSecond>.Single<V>(TFirst first, TSecond second, V value) => "right";
        string IDuo<TFirst, TSecond>.Pair<X, Y>() => "right";
        string IDuo<TFirst, TSecond>.Triple<X, Y, Z>() => "right";
    }
}
