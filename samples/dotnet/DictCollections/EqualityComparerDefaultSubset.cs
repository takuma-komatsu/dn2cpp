using System;
using System.Collections.Generic;

namespace EqualityComparerDefaultSubset;

class Program
{
    internal static void __GateEntry()
    {
        var comparer = EqualityComparer<int>.Default;
        Console.WriteLine("default nonnull=" + (comparer is not null));
        Console.WriteLine("default stable=" + ReferenceEquals(comparer, EqualityComparer<int>.Default));
        Console.WriteLine("default distinct=" + !ReferenceEquals(comparer, EqualityComparer<string>.Default));
        Console.WriteLine("default type stable=" + (comparer.GetType() == EqualityComparer<int>.Default.GetType()));
        Console.WriteLine("default type distinct=" + (comparer.GetType() != EqualityComparer<string>.Default.GetType()));
        Console.WriteLine("default concrete=" + (comparer.GetType() != typeof(EqualityComparer<int>)));
        Console.WriteLine("default base=" + (comparer.GetType().BaseType == typeof(EqualityComparer<int>))
            + " sealed=" + comparer.GetType().IsSealed);
        object boxed = comparer;
        Console.WriteLine("default interface=" + (boxed is IEqualityComparer<int>));
        Console.WriteLine("default cross interface=" + ((object)EqualityComparer<string>.Default is IEqualityComparer<int>));
        IEqualityComparer<int> throughInterface = (IEqualityComparer<int>)boxed;
        Console.WriteLine("default interface ops=" +
            (throughInterface.Equals(7, 7) && !throughInterface.Equals(7, 8)
                && throughInterface.GetHashCode(7) == comparer.GetHashCode(7)));
        Console.WriteLine("default nongeneric interface=" +
            (boxed is System.Collections.IEqualityComparer));
        var throughNongeneric = (System.Collections.IEqualityComparer)boxed;
        Console.WriteLine("default nongeneric ops=" +
            (throughNongeneric.Equals(7, 7) && !throughNongeneric.Equals(7, 8)
                && throughNongeneric.GetHashCode(7) == comparer.GetHashCode(7)));
        Console.WriteLine("default equal=" + (comparer?.Equals(7, 7) == true));
        EqualityComparer<int> missing = null;
        try { missing.Equals(1, 1); }
        catch (NullReferenceException) { Console.WriteLine("null receiver=NRE"); }
    }
}
