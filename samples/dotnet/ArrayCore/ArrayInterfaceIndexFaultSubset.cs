using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

// An out-of-range index on an array, taken through its COLLECTION INTERFACES.
//
// The sibling ArrayIndexFaultSubset covers the direct `arr[i]` / `md[i,j]` routes, whose
// subject is dn2cpp_bounds_check and whose answer is IndexOutOfRangeException. This
// section covers the routes that go through the array's interface map instead, where real
// .NET answers with a DIFFERENT exception — and the difference is not cosmetic:
// IndexOutOfRangeException is not an ArgumentException, so `catch
// (ArgumentOutOfRangeException)`, which is the clause the documented behaviour leads a
// caller to write, does not catch it and the program aborts instead of handling it.
//
// Measured against real .NET, and the split is per-route rather than per-array:
//
//   arr[bad]                      -> IndexOutOfRangeException
//   ((IList<T>)arr)[bad]          -> ArgumentOutOfRangeException, ParamName "index"
//   ((IList<T>)arr)[bad] = v      -> ArgumentOutOfRangeException  (the SETTER too)
//   ((IReadOnlyList<T>)arr)[bad]  -> ArgumentOutOfRangeException
//   ((IList)arr)[bad]             -> IndexOutOfRangeException     (NON-generic: unchanged)
//
// The last row is the negative control and is the reason the fix is in the GENERIC
// indexer of Dn2Cpp.Runtime.SZArrayEnumerable<T> alone: the non-generic IList indexer is a
// separate explicit implementation and must keep faulting through the raw array. A fix
// applied to "the array's interface indexer" as a whole would have made that row wrong.
//
// The message and ParamName are asserted, not just the type, because they are reachable
// exactly: the wrapper is managed C# and ArgumentOutOfRangeException is constructed by its
// real BCL ctor, so ParamName has storage and ArgumentException.get_Message appends the
// "(Parameter 'index')" suffix itself. This bucket diffs live against real .NET, so every
// line below is checked against the oracle on every run.
namespace ArrayInterfaceIndexFaultSubset;

internal static class Program
{
    private static void T(string label, Action a)
    {
        try
        {
            a();
            Console.WriteLine(label + " -> no-throw");
        }
        catch (Exception e)
        {
            string pn = e is ArgumentException ae ? (ae.ParamName ?? "<null>") : "-";
            Console.WriteLine(label + " -> " + e.GetType().Name + " param=" + pn);
        }
    }

    internal static void Run()
    {
        Console.WriteLine("== array index faults through the collection interfaces ==");

        int[] arr = { 1, 2, 3 };

        // The direct route, restated here as the contrast the whole section is about.
        T("direct-hi   ", () => { int _ = arr[10]; });
        T("direct-neg  ", () => { int _ = arr[-1]; });
        T("direct-count", () => { int _ = arr[3]; });   // exactly Length: still out of range

        // IList<T>: getter AND setter, high and negative.
        IList<int> il = arr;
        T("ilist-get-hi ", () => { int _ = il[10]; });
        T("ilist-get-neg", () => { int _ = il[-1]; });
        T("ilist-set-hi ", () => { il[10] = 9; });
        T("ilist-set-neg", () => { il[-1] = 9; });
        T("ilist-count  ", () => { int _ = il[3]; });

        // IReadOnlyList<T> — a different interface, the same implicit member behind it.
        IReadOnlyList<int> irol = arr;
        T("irol-get-hi ", () => { int _ = irol[10]; });
        T("irol-get-neg", () => { int _ = irol[-1]; });

        // The NEGATIVE CONTROL: the non-generic IList indexer must still fault through the
        // raw array. If this ever reads ArgumentOutOfRangeException, the fix has leaked out
        // of the generic member it belongs to.
        IList ngl = arr;
        T("nongen-get-hi ", () => { object? _ = ngl[10]; });
        T("nongen-get-neg", () => { object? _ = ngl[-1]; });

        // A reference-element array, so the fix is not accidentally specific to the i4 rep.
        IList<string> sl = new[] { "a", "b" };
        T("ref-get-hi ", () => { string _ = sl[10]; });
        T("ref-set-hi ", () => { sl[10] = "z"; });

        // An EMPTY array: every index is out of range, and index 0 is the case a
        // `< Length` check written as `<= Length` would let through.
        IList<int> empty = Array.Empty<int>();
        T("empty-ilist ", () => { int _ = empty[0]; });
        T("empty-direct", () => { int _ = Array.Empty<int>()[0]; });

        // ReadOnlyCollection<T> over an array forwards to the same IList<T> indexer.
        var roc = new ReadOnlyCollection<int>(arr);
        T("roc-hi ", () => { int _ = roc[10]; });
        T("roc-neg", () => { int _ = roc[-1]; });

        // The message, once, verbatim — the half a type check cannot see.
        try { int _ = il[10]; }
        catch (ArgumentOutOfRangeException e) { Console.WriteLine("msg " + e.Message); }

        // And the point of the whole exercise: the documented clause actually catches.
        try { int _ = il[10]; Console.WriteLine("catch-generic no-throw"); }
        catch (ArgumentOutOfRangeException) { Console.WriteLine("catch-generic caught by ArgumentOutOfRangeException"); }
        catch (Exception e) { Console.WriteLine("catch-generic ESCAPED as " + e.GetType().Name); }

        try { int _ = arr[10]; Console.WriteLine("catch-direct no-throw"); }
        catch (IndexOutOfRangeException) { Console.WriteLine("catch-direct caught by IndexOutOfRangeException"); }
        catch (Exception e) { Console.WriteLine("catch-direct ESCAPED as " + e.GetType().Name); }

        // The in-range reads still work — a bounds check that was off by one would show
        // here as a throw on a perfectly good index.
        Console.WriteLine("inrange " + il[0] + il[2] + " " + irol[1] + " " + roc[2] + " " + sl[1]);
    }
}
