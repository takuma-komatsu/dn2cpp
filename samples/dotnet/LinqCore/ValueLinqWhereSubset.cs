#nullable enable
// A LINQ `Where`/`foreach` over a *value-type-element*
// array that is read from a FIELD must wire that array's SZArray interface-dispatch
// table.
//
// This is the exact shape that aborted native dn2cpp mid-transpile.
// `Dn2Cpp.MethodCompiler.BuildExceptionRegions` does
// `regions.Where(r => r.Kind == Catch)` where `regions` is an
// `ImmutableArray<ExceptionRegion>` (a value-type element). `ImmutableArray<T>.Where`
// binds to `System.Linq.ImmutableArrayExtensions.Where`, whose body delegates to the
// shim `Enumerable.Where(source.array, predicate)` — handing the *raw underlying
// `T[]` array* (read from the `array` field via `ldfld`) into the
// `IEnumerable<T>` parameter (array→IEnumerable<T> covariance). The shim's
// `<Where>d__0<T>.MoveNext()` then `callvirt`s `IEnumerable<T>.GetEnumerator()` on
// that array.
//
// The transpiler only threaded a field-read's declared type as the stack entry's
// StaticType when it was a `Class`, so the `T[]` static type was dropped on the
// `ldfld array`. `CoerceTo` therefore never saw `source.array` as an SZArray
// flowing into `IEnumerable<T>`, so it never called `NoteArrayEnumerableElement` —
// the `ExceptionRegion[]` type-info shipped with `interfaceCount == 0`, and the
// runtime `dn2cpp_resolve_interface` trapped (`EntryPointNotFoundException
// (interface dispatch)`, rc=134) when MoveNext dispatched GetEnumerator on it.
//
// Covers BOTH the genuine `ImmutableArray<TStruct>.Where` path and a hand-rolled
// class with a `TStruct[]` field (the minimal `ldfld T[]` → `Where`
// reproduction), each iterated with `foreach` (MoveNext/get_Current/Dispose). The
// native binary's output must match real .NET line-for-line (computed live, not
// hard-coded). Real System.Private.CoreLib (-r) + the real System.Linq ->
// tree-shaken transpile -> run.
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace ValueLinqWhereSubset
{
    // A value-type element, like System.Reflection.Metadata.ExceptionRegion.
    internal readonly struct Region
    {
        public readonly int Kind;
        public readonly int Start;

        public Region(int kind, int start)
        {
            Kind = kind;
            Start = start;
        }
    }

    // A hand-rolled holder whose value-type array lives in a FIELD — reading it via
    // `ldfld` then `.Where(...)` is the minimal form of the dropped-StaticType bug.
    internal sealed class RegionBag
    {
        private readonly Region[] _regions;

        public RegionBag(Region[] regions)
        {
            _regions = regions;
        }

        // `_regions.Where(...)` = `Enumerable.Where((IEnumerable<Region>)ldfld _regions, pred)`.
        public int SumStartsOfKind(int kind)
        {
            int sum = 0;
            foreach (Region r in _regions.Where(x => x.Kind == kind))
                sum += r.Start;
            return sum;
        }
    }

    internal static class Program
    {
        // `regions.Where(...)` over an ImmutableArray<Region> = ImmutableArrayExtensions.Where
        // -> Enumerable.Where(source.array, pred): the exact BuildExceptionRegions shape.
        private static int CountKind(ImmutableArray<Region> regions, int kind)
        {
            int count = 0;
            foreach (Region r in regions.Where(x => x.Kind == kind))
                count++;
            return count;
        }

        internal static int Run()
        {
            var raw = new[]
            {
                new Region(0, 10),
                new Region(1, 20),
                new Region(0, 30),
                new Region(2, 40),
                new Region(1, 50),
            };

            // (1) ImmutableArray<Region>.Where — the genuine BuildExceptionRegions path.
            ImmutableArray<Region> regions = raw.ToImmutableArray();
            Console.WriteLine(CountKind(regions, 0)); // 2
            Console.WriteLine(CountKind(regions, 1)); // 2
            Console.WriteLine(CountKind(regions, 2)); // 1
            Console.WriteLine(CountKind(regions, 9)); // 0

            // (2) class with a Region[] field — minimal ldfld-of-value-array -> shim Where.
            var bag = new RegionBag(raw);
            Console.WriteLine(bag.SumStartsOfKind(0)); // 40  (10 + 30)
            Console.WriteLine(bag.SumStartsOfKind(1)); // 70  (20 + 50)
            Console.WriteLine(bag.SumStartsOfKind(2)); // 40

            return 0;
        }
    }
}
