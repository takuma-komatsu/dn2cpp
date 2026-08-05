// Copyright (c) 2014-present Godot Engine contributors (see AUTHORS.md).
// Copyright (c) 2007-2014 Juan Linietsky, Ariel Manzur.
// Licensed under the MIT License.

namespace Godot
{
    /// <summary>Hand-written members for the generated <c>Vector2i</c> value type.
    /// The fields (<c>X</c>/<c>Y</c>), constructors, arithmetic and equality
    /// operators, and constants come from the binding generator; the engine dump
    /// carries no relational-operator bodies, so they are hand-written here.
    /// Layout stays <c>{X, Y}</c>.</summary>
    public partial struct Vector2i
    {
        // Relational operators — Godot's are LEXICOGRAPHIC (compare X, tie-break on
        // Y), not component-wise. Verbatim from Godot's C# Vector2I.cs. Note the
        // subtlety: the X tie-break path uses strict `<`/`>` even in `<=`/`>=`;
        // only the final dimension (Y) uses the inclusive form.
        public static bool operator <(Vector2i left, Vector2i right)
        {
            if (left.X == right.X)
            {
                return left.Y < right.Y;
            }
            return left.X < right.X;
        }

        public static bool operator >(Vector2i left, Vector2i right)
        {
            if (left.X == right.X)
            {
                return left.Y > right.Y;
            }
            return left.X > right.X;
        }

        public static bool operator <=(Vector2i left, Vector2i right)
        {
            if (left.X == right.X)
            {
                return left.Y <= right.Y;
            }
            return left.X < right.X;
        }

        public static bool operator >=(Vector2i left, Vector2i right)
        {
            if (left.X == right.X)
            {
                return left.Y >= right.Y;
            }
            return left.X > right.X;
        }
    }
}
