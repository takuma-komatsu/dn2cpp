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
    }

    public interface IBoxRight<T> : IBox<T>
    {
        string IBox<T>.Take(T item, List<T> items) => "right";
    }
}
