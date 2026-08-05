#nullable enable
using System;
using System.IO;

// The System.IO.Path overloads that are NOT lowered to a dn2cpp_path_* helper, and so
// transpile from their real BCL bodies. Every one of these used to be a hard transpile
// failure: the intercept was routed by method NAME (so the reachability edge to the real
// body was cut) but the lowering matched on the SIGNATURE (so an unmodeled overload found
// no lowering and had nothing left to fall back on). They are the same pure-lexical
// operations as the intercepted string forms, and diff exact vs real .NET.
//
// Deliberately no GetFullPath(string, string) here: it reaches Interop.Sys.GetCwd, which
// needs the net10-pinned CoreLib. That one is asserted in the FileReal bucket.
namespace PathSpanSubset;

internal static class Program
{
    static void S(string label, string v) => Console.WriteLine($"{label}=[{v}]");

    internal static void __GateEntry()
    {
        // The ReadOnlySpan<char> overloads of the five ops whose string forms are
        // intercepted. .AsSpan() picks the span overload unambiguously; these return a
        // ReadOnlySpan<char> (empty, never null) rather than a string.
        S("spanFn", Path.GetFileName("/a/b/c.txt".AsSpan()).ToString());
        S("spanFnTrail", Path.GetFileName("/a/b/".AsSpan()).ToString());
        S("spanDn", Path.GetDirectoryName("/a/b/c.txt".AsSpan()).ToString());
        S("spanDnRoot", Path.GetDirectoryName("/".AsSpan()).ToString());
        S("spanDnRel", Path.GetDirectoryName("a/b".AsSpan()).ToString());
        S("spanExt", Path.GetExtension("/a/b.tar.gz".AsSpan()).ToString());
        S("spanExtNone", Path.GetExtension("/a/b".AsSpan()).ToString());
        S("spanExtHidden", Path.GetExtension("/a/.hidden".AsSpan()).ToString());
        S("spanFnwe", Path.GetFileNameWithoutExtension("/a/b.txt".AsSpan()).ToString());
        S("spanFnweMulti", Path.GetFileNameWithoutExtension("b.tar.gz".AsSpan()).ToString());
        S("spanRootedRel", Path.IsPathRooted("a/b".AsSpan()).ToString());
        S("spanRootedAbs", Path.IsPathRooted("/a/b".AsSpan()).ToString());

        // Combine past the fixed arity-2/3/4 helpers. These are TWO different IL tokens
        // and two different fall-throughs, so both are asserted: an explicit string[]
        // binds Combine(string[]), while a 5-argument call site binds the .NET 9+
        // Combine(params ReadOnlySpan<string>) under C# 13 params-collection rules —
        // which is exactly the overload a fix that only taught the intrinsic about
        // string[] would still miss.
        S("combArr5", Path.Combine(new[] { "a", "b", "c", "d", "e.txt" }));
        S("combSpan5", Path.Combine("a", "b", "c", "d", "e.txt"));
        // params Combine with a rooted mid-element: the rooted element restarts the join,
        // discarding everything to its left.
        S("combArrMidRoot", Path.Combine(new[] { "a", "b", "/c", "d" }));
        S("combSpanMidRoot", Path.Combine("a", "b", "/c", "d", "e"));
        // Empty elements are dropped, and a trailing separator is not doubled.
        S("combArrEmpty", Path.Combine(new[] { "a", "", "b", "c", "d" }));
        S("combSpanSep", Path.Combine("a/", "b", "c/", "d", "e"));
    }
}
