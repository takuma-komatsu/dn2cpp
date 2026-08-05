namespace ConvOpLib
{
    // A small value type defined in a SEPARATE assembly, so the user-side
    // conversions bind through a cross-assembly MemberRef (the path that drops
    // the return type during overload resolution). Mirrors the Godot shim's
    // Variant, whose Variant->Vector3 / Variant->int conversions overload on
    // return type alone.
    public struct Point
    {
        public int X;
        public int Y;
    }

    public struct Cell
    {
        public int Raw;

        // The int-returning conversion is declared FIRST — the trap. A
        // params-only resolver bound (Point)cell to this overload, then cast the
        // int to a Point* -> garbage/segfault. The fix disambiguates by return
        // type so each cast picks its own overload.
        public static implicit operator int(Cell c) => c.Raw;

        public static implicit operator Point(Cell c)
        {
            Point p;
            p.X = c.Raw;
            p.Y = c.Raw * 2;
            return p;
        }

        public static implicit operator string(Cell c) => "cell:" + c.Raw.ToString();

        // Conversion operators also overload on return type for op_Explicit.
        public static explicit operator long(Cell c) => (long)c.Raw * 1000L;

        public static explicit operator double(Cell c) => c.Raw + 0.25;
    }
}
