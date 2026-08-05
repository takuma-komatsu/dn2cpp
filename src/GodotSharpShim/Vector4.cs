// Copyright (c) 2014-present Godot Engine contributors (see AUTHORS.md).
// Copyright (c) 2007-2014 Juan Linietsky, Ariel Manzur.
// Licensed under the MIT License.

namespace Godot
{
    /// <summary>Hand-written members for the generated <c>Vector4</c> value type.
    /// The fields (<c>X</c>/<c>Y</c>/<c>Z</c>/<c>W</c>), constructors, arithmetic
    /// and equality operators, and constants come from the binding generator
    /// (<c>GodotShims.g.cs</c>, from <c>extension_api.json</c>'s
    /// <c>builtin_classes</c>); the engine dump carries no relational-operator
    /// bodies, so they are hand-written here. Layout stays <c>{X, Y, Z, W}</c>.</summary>
    public partial struct Vector4
    {
        // Relational operators — Godot's are LEXICOGRAPHIC (compare X, then Y, then
        // Z, then W), not component-wise. Verbatim from Godot's C# Vector4.cs.
        // Subtlety: the X/Y/Z tie-break paths use strict `<`/`>` even in `<=`/`>=`;
        // only the final dimension (W) uses the inclusive form.
        public static bool operator <(Vector4 left, Vector4 right)
        {
            if (left.X == right.X)
            {
                if (left.Y == right.Y)
                {
                    if (left.Z == right.Z)
                    {
                        return left.W < right.W;
                    }
                    return left.Z < right.Z;
                }
                return left.Y < right.Y;
            }
            return left.X < right.X;
        }

        public static bool operator >(Vector4 left, Vector4 right)
        {
            if (left.X == right.X)
            {
                if (left.Y == right.Y)
                {
                    if (left.Z == right.Z)
                    {
                        return left.W > right.W;
                    }
                    return left.Z > right.Z;
                }
                return left.Y > right.Y;
            }
            return left.X > right.X;
        }

        public static bool operator <=(Vector4 left, Vector4 right)
        {
            if (left.X == right.X)
            {
                if (left.Y == right.Y)
                {
                    if (left.Z == right.Z)
                    {
                        return left.W <= right.W;
                    }
                    return left.Z < right.Z;
                }
                return left.Y < right.Y;
            }
            return left.X < right.X;
        }

        public static bool operator >=(Vector4 left, Vector4 right)
        {
            if (left.X == right.X)
            {
                if (left.Y == right.Y)
                {
                    if (left.Z == right.Z)
                    {
                        return left.W >= right.W;
                    }
                    return left.Z > right.Z;
                }
                return left.Y > right.Y;
            }
            return left.X > right.X;
        }
    }
}
