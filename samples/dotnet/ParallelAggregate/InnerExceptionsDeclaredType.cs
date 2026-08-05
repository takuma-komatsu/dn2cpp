#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace InnerExceptionsDeclaredType;

// AggregateException.InnerExceptions read through its DECLARED type: the getter returns a
// real ReadOnlyCollection<Exception>, so `var inner = ae.InnerExceptions` dispatches a
// ReadOnlyCollection vtable and not an array's. Every other section in the bucket upcasts
// to IReadOnlyList first, which is the route an array-shaped answer also served.
// Deliberately NOT printed: inner.GetType(), whose mangled closed-generic name is the
// tree-wide FullName/ToString carve-out documented at dn2cpp_type_name.
static class Program
{
    private static AggregateException Two()
        => new AggregateException(new InvalidOperationException("alpha"), new FormatException("beta"));

    internal static void __GateEntry()
    {
        Console.WriteLine("== InnerExceptions via the declared type ==");

        // No upcast anywhere.
        var inner = Two().InnerExceptions;
        Console.WriteLine("count " + inner.Count);
        Console.WriteLine("idx " + inner[0].Message + "," + inner[1].Message);

        // foreach binds ReadOnlyCollection's own enumerator, not the array's.
        var seen = new List<string>();
        foreach (Exception e in inner)
            seen.Add(e.GetType().Name);
        Console.WriteLine("foreach " + string.Join(",", seen));

        // The rest of the concrete surface, each a distinct slot.
        Console.WriteLine("contains " + inner.Contains(inner[0]));
        Console.WriteLine("indexof " + inner.IndexOf(inner[1]));
        Exception[] copy = new Exception[inner.Count];
        inner.CopyTo(copy, 0);
        Console.WriteLine("copyto " + copy[0].Message + "," + copy[1].Message);

        // Out of range must throw rather than read past the array, and the type is printed
        // to pin that the wrapper neither swallows nor re-wraps on the way out. The
        // underlying routes are covered by ArrayCore's ArrayInterfaceIndexFaultSubset.
        try { Console.WriteLine("oob " + inner[5].Message); }
        catch (Exception e) { Console.WriteLine("oob " + e.GetType().Name); }

        // It really is the wrapper, and it still satisfies the interfaces the other
        // sections upcast to.
        ReadOnlyCollection<Exception> asRoc = inner;
        Console.WriteLine("declared " + asRoc.Count);
        Console.WriteLine("is irol " + (inner is IReadOnlyList<Exception>)
            + " ilist " + (inner is IList<Exception>)
            + " ienum " + (inner is IEnumerable<Exception>));
        // Real .NET returns a ReadOnlyCollection, never an array.
        Console.WriteLine("is array " + (inner is Exception[]));

        IReadOnlyList<Exception> viaItf = inner;
        Console.WriteLine("itf " + viaItf.Count + " " + viaItf[0].Message);
        // A second pass by index rather than by enumerator, so the two readers of the same
        // slots have to agree. Not LINQ: this bucket does not reference System.Linq.
        var byIndex = new List<string>();
        for (int i = 0; i < inner.Count; i++)
            byIndex.Add(inner[i].GetType().Name);
        Console.WriteLine("byindex " + string.Join("|", byIndex));

        // The neighbouring member reading the same runtime array, so a change to the
        // wrapper cannot quietly move it. Flatten() is not probed: it has no intrinsic
        // mapping and refuses at transpile time, which would make the bucket untranspilable.
        var agg = Two();
        Console.WriteLine("single " + agg.InnerException!.GetType().Name);

        // Real .NET's InnerExceptions is a stored field, so two reads hand back the SAME
        // object: the wrapper has to be memoized.
        var agg2 = Two();
        Console.WriteLine("refeq " + ReferenceEquals(agg2.InnerExceptions, agg2.InnerExceptions));

        var one = new AggregateException(new InvalidOperationException("solo"));
        Console.WriteLine("one " + one.InnerExceptions.Count + " " + one.InnerExceptions[0].Message);
        var none = new AggregateException();
        Console.WriteLine("none " + none.InnerExceptions.Count);
    }
}
