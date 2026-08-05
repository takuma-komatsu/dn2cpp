#nullable disable
using System;

namespace ToStringSubset
{
    internal class Point
    {
        public int X;
        public int Y;
        public override string ToString() => "(" + X + "," + Y + ")";
    }

    // A derived type that does NOT override ToString: it inherits Point's.
    internal sealed class Pixel : Point
    {
    }

    // No ToString override: formats as the type name (default Object.ToString).
    internal sealed class Blank
    {
    }

    // an overridden ToString is dispatched wherever Object.ToString is used
    // (Concat / interpolation / Console / string.Join / an explicit call), not just
    // direct calls. The type-info carries the override's function pointer, so even a
    // statically-`object` reference dispatches it.
    internal static class Program
    {
        internal static void __GateEntry()
        {
            var p = new Point { X = 3, Y = 4 };

            Console.WriteLine(p.ToString());      // (3,4)
            Console.WriteLine("p=" + p);          // p=(3,4)
            Console.WriteLine($"point {p}!");     // point (3,4)!
            Console.WriteLine(p);                 // (3,4) (Console.WriteLine(object))

            object o = p;
            Console.WriteLine(o.ToString());      // (3,4) (static object)

            // Inherited override.
            var px = new Pixel { X = 1, Y = 2 };
            Console.WriteLine("px=" + px);        // px=(1,2)

            // Concat of several overriding objects via Concat(object[]).
            Console.WriteLine("" + new Point { X = 0, Y = 0 } + new Point { X = 5, Y = 6 }); // (0,0)(5,6)

            // No override: type name.
            Console.WriteLine("b=" + new Blank()); // b=ToStringSubset.Blank
        }
    }
}
