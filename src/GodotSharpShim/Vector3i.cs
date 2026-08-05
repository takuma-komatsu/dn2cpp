// Copyright (c) 2014-present Godot Engine contributors (see AUTHORS.md).
// Copyright (c) 2007-2014 Juan Linietsky, Ariel Manzur.
// Licensed under the MIT License.

namespace Godot
{
    /// <summary>Hand-written members for the generated <c>Vector3i</c> value type.
    /// The fields (<c>X</c>/<c>Y</c>/<c>Z</c>), constructors, arithmetic and
    /// equality operators, and constants come from the binding generator; the
    /// engine dump carries no relational-operator bodies, so they are hand-written
    /// here. Layout stays <c>{X, Y, Z}</c>.</summary>
    public partial struct Vector3i
    {
        // Relational operators — Godot's are LEXICOGRAPHIC (compare X, then Y, then
        // Z), not component-wise. Verbatim from Godot's C# Vector3I.cs. Subtlety:
        // the X/Y tie-break paths use strict `<`/`>` even in `<=`/`>=`; only the
        // final dimension (Z) uses the inclusive form.
        public static bool operator <(Vector3i left, Vector3i right)
        {
            if (left.X == right.X)
            {
                if (left.Y == right.Y)
                {
                    return left.Z < right.Z;
                }
                return left.Y < right.Y;
            }
            return left.X < right.X;
        }

        public static bool operator >(Vector3i left, Vector3i right)
        {
            if (left.X == right.X)
            {
                if (left.Y == right.Y)
                {
                    return left.Z > right.Z;
                }
                return left.Y > right.Y;
            }
            return left.X > right.X;
        }

        public static bool operator <=(Vector3i left, Vector3i right)
        {
            if (left.X == right.X)
            {
                if (left.Y == right.Y)
                {
                    return left.Z <= right.Z;
                }
                return left.Y < right.Y;
            }
            return left.X < right.X;
        }

        public static bool operator >=(Vector3i left, Vector3i right)
        {
            if (left.X == right.X)
            {
                if (left.Y == right.Y)
                {
                    return left.Z >= right.Z;
                }
                return left.Y > right.Y;
            }
            return left.X > right.X;
        }
    }
}
