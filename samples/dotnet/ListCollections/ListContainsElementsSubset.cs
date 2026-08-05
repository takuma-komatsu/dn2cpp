#nullable enable
using System;
using System.Collections.Generic;

namespace ListContainsElementsSubset
{
    // List<T>.Contains / IndexOf / LastIndexOf / Remove all funnel into the real
    // BCL's Array.IndexOf<T> / Array.LastIndexOf<T>, which the transpiler emits as
    // an inline scan. The element comparison used to be int/string/pointer only, so
    // a List<Vector2>-shaped program — a struct element list, the most ordinary thing
    // in a game — did not transpile at all. The scan now uses the same devirtualized
    // EqualityComparer<T>.Default.Equals as a Dictionary key, so every element type
    // works and a value-equal Derived in a List<Base> is found. Diffed exact vs .NET.
    //
    // The int/string sections live in ListSubset; this one holds the element types
    // that could not run before it.
    internal struct Vec : IEquatable<Vec>
    {
        internal float X;
        internal float Y;

        internal Vec(float x, float y)
        {
            X = x;
            Y = y;
        }

        public bool Equals(Vec other) => X == other.X && Y == other.Y;

        public override bool Equals(object? o) => o is Vec v && Equals(v);

        public override int GetHashCode() => (int)X * 31 + (int)Y;

        public override string ToString() => "<" + X + "," + Y + ">";
    }

    internal class Node
    {
        internal string Name;

        internal Node(string name)
        {
            Name = name;
        }

        public override bool Equals(object? o) => o is Node n && n.Name == Name;

        public override int GetHashCode() => Name.GetHashCode();
    }

    internal class SubNode : Node
    {
        internal SubNode(string name) : base(name)
        {
        }
    }

    internal static class Program
    {
        internal static int __GateEntry()
        {
            // --- struct elements (typed IEquatable<T>.Equals) ---
            List<Vec> vs = new List<Vec>();
            vs.Add(new Vec(1f, 2f));
            vs.Add(new Vec(3f, 4f));
            vs.Add(new Vec(1f, 2f));
            Console.WriteLine(vs.Contains(new Vec(3f, 4f)));      // True
            Console.WriteLine(vs.Contains(new Vec(9f, 9f)));      // False
            Console.WriteLine(vs.IndexOf(new Vec(1f, 2f)));       // 0
            Console.WriteLine(vs.LastIndexOf(new Vec(1f, 2f)));   // 2
            Console.WriteLine(vs.Remove(new Vec(3f, 4f)));        // True
            Console.WriteLine(vs.Count);                          // 2

            // --- double/char/byte elements ---
            List<double> ds = new List<double> { 1.5, 2.5, 1.5 };
            Console.WriteLine(ds.Contains(2.5));         // True
            Console.WriteLine(ds.IndexOf(1.5));          // 0
            Console.WriteLine(ds.LastIndexOf(1.5));      // 2
            Console.WriteLine(ds.Contains(2.6));         // False

            List<char> cs = new List<char> { 'x', 'y', 'z' };
            Console.WriteLine(cs.IndexOf('y'));          // 1
            Console.WriteLine(cs.Contains('q'));         // False

            List<byte> bs = new List<byte> { 3, 4, 5 };
            Console.WriteLine(bs.IndexOf((byte)5));      // 2

            List<long> ls = new List<long> { 10L, 20L };
            Console.WriteLine(ls.Contains(20L));         // True

            // --- enum elements ---
            List<DayOfWeek> days = new List<DayOfWeek> { DayOfWeek.Monday, DayOfWeek.Friday };
            Console.WriteLine(days.Contains(DayOfWeek.Friday));   // True
            Console.WriteLine(days.IndexOf(DayOfWeek.Sunday));    // -1

            // --- reference elements: the Equals override dispatches ---
            List<Node> ns = new List<Node>();
            ns.Add(new Node("a"));
            ns.Add(new SubNode("b"));
            Console.WriteLine(ns.Contains(new Node("b")));    // True (a value-equal SubNode)
            Console.WriteLine(ns.IndexOf(new Node("a")));     // 0
            Console.WriteLine(ns.Remove(new Node("a")));      // True
            Console.WriteLine(ns.Count);                      // 1

            // --- an empty list's backward scan ---
            List<Vec> none = new List<Vec>();
            Console.WriteLine(none.LastIndexOf(new Vec(0f, 0f)));  // -1
            Console.WriteLine(none.IndexOf(new Vec(0f, 0f)));      // -1
            Console.WriteLine(none.Contains(new Vec(0f, 0f)));     // False

            return 0;
        }
    }
}
