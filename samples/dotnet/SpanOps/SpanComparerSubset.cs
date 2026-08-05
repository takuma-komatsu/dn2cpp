#nullable enable
using System;
using System.Collections.Generic;

// The trailing IEqualityComparer<T> of the net9+ MemoryExtensions scan overloads.
// A genuine comparer is HONORED: the emitted scan hoists a
// dn2cpp_try_resolve_interface probe out of the loop and dispatches every element
// comparison through the comparer's Equals slot, the runtime-null arm falling back
// to the element type's default equality. Both halves are pinned against .NET.
//
// Three operands MEAN "default equality" and must scan as if no comparer were
// written: the null literal (the omitted optional argument), an explicit
// EqualityComparer<T>.Default (whose intrinsic value IS the null sentinel — both
// settled statically, costing no probe), and a null that only a runtime value
// proves, arriving through a variable, which takes the probe's null arm.
//
// The rows using a real comparer are chosen so honoring it is observable — mod-10
// answers True / a real index where default equality says False / -1 — and the
// throwing comparer proves the scan enters the comparer's own Equals. The
// receivers cover a class comparer, a BOXED STRUCT comparer (dispatched through
// the interface table's unboxing thunks), a reference element type (pointer-typed
// operands), and a non-IEquatable struct element, which binds the comparer-taking
// overload even where no comparer is written.
namespace SpanComparerSubset;

// Custom equality that visibly differs from default: congruence mod 10.
sealed class Mod10Comparer : IEqualityComparer<int>
{
    public bool Equals(int a, int b) => ((a % 10) + 10) % 10 == ((b % 10) + 10) % 10;
    public int GetHashCode(int v) => ((v % 10) + 10) % 10;
}

// The same equality as a STRUCT: boxed at the call site, dispatched through an
// unboxing-thunk interface slot.
struct Mod10StructComparer : IEqualityComparer<int>
{
    public bool Equals(int a, int b) => ((a % 10) + 10) % 10 == ((b % 10) + 10) % 10;
    public int GetHashCode(int v) => ((v % 10) + 10) % 10;
}

// Reaching Equals is the observable: proves the scan enters the comparer object.
sealed class ThrowingComparer : IEqualityComparer<int>
{
    public bool Equals(int a, int b) => throw new PlatformNotSupportedException("custom comparer reached");
    public int GetHashCode(int v) => v;
}

// A reference-element comparer: equality by string length.
sealed class LengthComparer : IEqualityComparer<string>
{
    public bool Equals(string? x, string? y) => (x?.Length ?? -1) == (y?.Length ?? -1);
    public int GetHashCode(string s) => s.Length;
}

// No IEquatable<T>, only an Equals(object) override: this does not bind the
// constrained scan overload, so the compiler picks the comparer-taking one with a
// null literal even where no comparer is written.
struct Plain
{
    public int V;
    public override bool Equals(object? o) => o is Plain p && p.V == V;
    public override int GetHashCode() => V;
}

sealed class PlainModComparer : IEqualityComparer<Plain>
{
    public bool Equals(Plain a, Plain b) => a.V % 10 == b.V % 10;
    public int GetHashCode(Plain p) => p.V % 10;
}

class Program
{
    // Each scan sits behind a method so the comparer arrives as an ARGUMENT — an
    // ldarg, not an ldnull — the shape Enumerable's array fast paths forward, and
    // the one the runtime probe exists for: a null here is null only at run time.
    private static string Contains(int[] arr, int v, IEqualityComparer<int>? cmp)
    {
        try { return ((ReadOnlySpan<int>)arr).Contains(v, cmp).ToString(); }
        catch (PlatformNotSupportedException) { return "PlatformNotSupportedException"; }
    }

    private static string IndexOf(int[] arr, int v, IEqualityComparer<int>? cmp)
    {
        try { return ((ReadOnlySpan<int>)arr).IndexOf(v, cmp).ToString(); }
        catch (PlatformNotSupportedException) { return "PlatformNotSupportedException"; }
    }

    private static string LastIndexOf(int[] arr, int v, IEqualityComparer<int>? cmp)
    {
        try { return ((ReadOnlySpan<int>)arr).LastIndexOf(v, cmp).ToString(); }
        catch (PlatformNotSupportedException) { return "PlatformNotSupportedException"; }
    }

    private static string SequenceEqual(int[] arr, int[] other, IEqualityComparer<int>? cmp)
    {
        try { return ((ReadOnlySpan<int>)arr).SequenceEqual(other, cmp).ToString(); }
        catch (PlatformNotSupportedException) { return "PlatformNotSupportedException"; }
    }

    private static string StartsWith(int[] arr, int[] prefix, IEqualityComparer<int>? cmp)
    {
        try { return ((ReadOnlySpan<int>)arr).StartsWith(prefix, cmp).ToString(); }
        catch (PlatformNotSupportedException) { return "PlatformNotSupportedException"; }
    }

    private static string EndsWith(int[] arr, int[] suffix, IEqualityComparer<int>? cmp)
    {
        try { return ((ReadOnlySpan<int>)arr).EndsWith(suffix, cmp).ToString(); }
        catch (PlatformNotSupportedException) { return "PlatformNotSupportedException"; }
    }

    private static string PlainContains(Plain[] arr, Plain v, IEqualityComparer<Plain>? cmp)
    {
        try { return ((ReadOnlySpan<Plain>)arr).Contains(v, cmp).ToString(); }
        catch (PlatformNotSupportedException) { return "PlatformNotSupportedException"; }
    }

    // Inside a GENERIC method: a reference-element instantiation may compile once
    // for the canonical group, so the dispatch must probe through the canonical
    // handle and resolve via the alias row on the real receiver. A value-element
    // instantiation stays concrete; both are called below.
    private static string GenericContains<T>(T[] arr, T v, IEqualityComparer<T>? cmp)
    {
        try { return ((ReadOnlySpan<T>)arr).Contains(v, cmp).ToString(); }
        catch (PlatformNotSupportedException) { return "PlatformNotSupportedException"; }
    }

    internal static void __GateEntry()
    {
        int[] a = { 3, 1, 4, 1, 5 };
        int[] same = { 3, 1, 4, 1, 5 };
        int[] head = { 3, 1 };
        int[] tail = { 1, 5 };
        IEqualityComparer<int>? none = null;
        IEqualityComparer<int> deflt = EqualityComparer<int>.Default;

        // --- the null literal: the omitted optional argument, settled statically ---
        Console.WriteLine("-- null literal --");
        Console.WriteLine(((ReadOnlySpan<int>)a).Contains(4, null));            // True
        Console.WriteLine(((ReadOnlySpan<int>)a).IndexOf(1, null));             // 1
        Console.WriteLine(((ReadOnlySpan<int>)a).LastIndexOf(1, null));         // 3
        Console.WriteLine(((ReadOnlySpan<int>)a).SequenceEqual(same, null));    // True
        Console.WriteLine(((ReadOnlySpan<int>)a).StartsWith(head, null));       // True
        Console.WriteLine(((ReadOnlySpan<int>)a).EndsWith(tail, null));         // True

        // --- EqualityComparer<T>.Default: the intrinsic's null sentinel, also static ---
        Console.WriteLine("-- EqualityComparer<T>.Default --");
        Console.WriteLine(((ReadOnlySpan<int>)a).Contains(4, EqualityComparer<int>.Default));         // True
        Console.WriteLine(((ReadOnlySpan<int>)a).IndexOf(9, EqualityComparer<int>.Default));          // -1
        Console.WriteLine(((ReadOnlySpan<int>)a).SequenceEqual(same, EqualityComparer<int>.Default)); // True

        // --- a null arriving through a variable: the probe's runtime null arm ---
        Console.WriteLine("-- null through a variable --");
        Console.WriteLine(Contains(a, 4, none));          // True
        Console.WriteLine(Contains(a, 9, none));          // False
        Console.WriteLine(IndexOf(a, 1, none));           // 1
        Console.WriteLine(LastIndexOf(a, 1, none));       // 3
        Console.WriteLine(SequenceEqual(a, same, none));  // True
        Console.WriteLine(StartsWith(a, head, none));     // True
        Console.WriteLine(EndsWith(a, tail, none));       // True

        // --- the default comparer through a variable: same, cleared at run time ---
        Console.WriteLine("-- EqualityComparer<T>.Default through a variable --");
        Console.WriteLine(Contains(a, 4, deflt));         // True
        Console.WriteLine(IndexOf(a, 5, deflt));          // 4
        Console.WriteLine(SequenceEqual(a, same, deflt)); // True

        // --- a custom comparer: every row differs from default equality ---
        Console.WriteLine("-- custom comparer (honored) --");
        IEqualityComparer<int> mod = new Mod10Comparer();
        Console.WriteLine(Contains(a, 14, mod));                                // True  (4 = 14 mod 10)
        Console.WriteLine(Contains(a, 12, mod));                                // False
        Console.WriteLine(IndexOf(a, 11, mod));                                 // 1
        Console.WriteLine(LastIndexOf(a, 21, mod));                             // 3
        Console.WriteLine(SequenceEqual(a, new[] { 13, 11, 14, 21, 25 }, mod)); // True
        Console.WriteLine(StartsWith(a, new[] { 13, 21 }, mod));                // True
        Console.WriteLine(StartsWith(a, new[] { 13, 22 }, mod));                // False
        Console.WriteLine(EndsWith(a, new[] { 31, 45 }, mod));                  // True
        // Inline sites: the comparer as a direct newobj argument, and the
        // membership overloads' comparer forms.
        Console.WriteLine(((ReadOnlySpan<int>)a).IndexOf(14, new Mod10Comparer()));  // 2
        Console.WriteLine(((ReadOnlySpan<int>)a).IndexOfAny((ReadOnlySpan<int>)new[] { 12, 15 }, new Mod10Comparer()));     // 4
        Console.WriteLine(((ReadOnlySpan<int>)a).LastIndexOfAny((ReadOnlySpan<int>)new[] { 11, 13 }, new Mod10Comparer())); // 3
        Console.WriteLine(((ReadOnlySpan<int>)a).IndexOfAnyExcept(13, new Mod10Comparer()));                                // 1

        // --- a boxed struct comparer: dispatched through the unboxing thunk ---
        Console.WriteLine("-- boxed struct comparer --");
        Console.WriteLine(Contains(a, 24, new Mod10StructComparer()));        // True
        Console.WriteLine(SequenceEqual(a, same, new Mod10StructComparer())); // True

        // --- a reference element type: pointer-typed operands through the slot ---
        Console.WriteLine("-- reference element (string) --");
        string[] words = { "a", "bb", "ccc" };
        Console.WriteLine(((ReadOnlySpan<string>)words).Contains("xyz", new LengthComparer())); // True
        Console.WriteLine(((ReadOnlySpan<string>)words).IndexOf("zz", new LengthComparer()));   // 1
        Console.WriteLine(((ReadOnlySpan<string>)words).Contains("bb", null));                  // True

        // --- the scan inside a generic method: the shared-generics canonical path ---
        Console.WriteLine("-- generic method (canonical shared body) --");
        Console.WriteLine(GenericContains(words, "zz", new LengthComparer())); // True
        Console.WriteLine(GenericContains(words, "zzzz", new LengthComparer())); // False
        Console.WriteLine(GenericContains(words, "bb", null));                 // True
        Console.WriteLine(GenericContains(a, 14, mod));                        // True
        Console.WriteLine(GenericContains(a, 14, none));                       // False

        // --- the throwing comparer: proves the comparer's own Equals runs ---
        Console.WriteLine("-- throwing comparer (dispatch observable) --");
        Console.WriteLine(Contains(a, 4, new ThrowingComparer()));         // PlatformNotSupportedException
        Console.WriteLine(SequenceEqual(a, same, new ThrowingComparer())); // PlatformNotSupportedException

        // --- the element that binds the comparer overload with none written ---
        Console.WriteLine("-- non-IEquatable struct element --");
        Plain[] ps = { new Plain { V = 7 }, new Plain { V = 8 } };
        Console.WriteLine(((ReadOnlySpan<Plain>)ps).Contains(new Plain { V = 8 }));         // True
        Console.WriteLine(PlainContains(ps, new Plain { V = 8 }, null));                    // True
        Console.WriteLine(PlainContains(ps, new Plain { V = 18 }, new PlainModComparer())); // True
        Console.WriteLine(PlainContains(ps, new Plain { V = 19 }, new PlainModComparer())); // False
    }
}
