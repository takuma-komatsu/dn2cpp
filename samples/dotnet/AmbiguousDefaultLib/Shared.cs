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
    }

    public interface IBoxLeft<T> : IBox<T>
    {
        string IBox<T>.Take(T item, List<T> items) => "left";
    }
}
