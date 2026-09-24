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
    }

    public interface ILeft : IBase
    {
        string IBase.Pick(int count, string label) => "left";
        string IBase.PickGeneric<T>() => "left";
        string IBase.Unused() => "left";
        string IBase.UnusedGeneric<T>() => "left";
        string IBase.Plain() => "left";
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
