#nullable enable
// A value-type (struct) with value equality used as a HashSet/Dictionary
// key. EqualityComparer<T>.Default for a struct key devirtualizes to the struct's
// own GetHashCode / IEquatable<T>.Equals(T) override (no boxing); those overrides
// are reached at the comparer-dispatch site (use-site gated, so a struct merely
// boxed elsewhere doesn't drag its equality in). Also fixes a direct
// `structValue.GetHashCode` — a `constrained. callvirt object::GetHashCode`
// now calls the override on the struct pointer instead of reading the raw struct
// as a boxed-object header (a latent crash). Covers a hand-written struct and
// a record struct.
//
// Distinct from `ValueStructKeySubset` next door, which covers the struct that
// overrides NEITHER Equals(object) nor GetHashCode (the synthesized field walk);
// here the struct supplies both, and the point is that the comparer reaches them.
//
// Former standalone gate: build-and-run-struct-key-subset.sh. Its sample declared
// `Coord`, `Cell` and `Program` in the GLOBAL namespace; nothing here prints or
// reflects over a type name (no typeof/nameof/GetType, and the record struct's
// synthesized ToString is never called), so the namespace is a free rename.
using System;
using System.Collections.Generic;

namespace StructKeySubset
{
    internal struct Coord : IEquatable<Coord>
    {
        public int X, Y;
        public Coord(int x, int y) { X = x; Y = y; }
        public bool Equals(Coord o) => X == o.X && Y == o.Y;
        public override bool Equals(object? o) => o is Coord c && Equals(c);
        public override int GetHashCode() => X * 397 + Y;
    }

    internal readonly record struct Cell(int Row, int Col);

    internal static class Program
    {
        internal static void __GateEntry()
        {
            var set = new HashSet<Coord>();
            set.Add(new Coord(1, 2)); set.Add(new Coord(1, 2)); set.Add(new Coord(3, 4));
            Console.WriteLine("setcount=" + set.Count);
            Console.WriteLine("contains=" + set.Contains(new Coord(3, 4)));

            var dict = new Dictionary<Coord, string>();
            dict[new Coord(1, 2)] = "a"; dict[new Coord(1, 2)] = "b"; dict[new Coord(5, 6)] = "c";
            Console.WriteLine("dictcount=" + dict.Count);
            Console.WriteLine("lookup=" + dict[new Coord(1, 2)]);
            Console.WriteLine("hasheq=" + (new Coord(1, 2).GetHashCode() == new Coord(1, 2).GetHashCode()));

            // record struct as key (auto GetHashCode + IEquatable)
            var rset = new HashSet<Cell>();
            rset.Add(new Cell(0, 0)); rset.Add(new Cell(0, 0)); rset.Add(new Cell(1, 1));
            Console.WriteLine("recsetcount=" + rset.Count);
            var rdict = new Dictionary<Cell, int>();
            rdict[new Cell(2, 3)] = 10; rdict[new Cell(2, 3)] = 20;
            Console.WriteLine("recdictcount=" + rdict.Count + " val=" + rdict[new Cell(2, 3)]);
            Console.WriteLine("receq=" + (new Cell(1, 1) == new Cell(1, 1)));
        }
    }
}
