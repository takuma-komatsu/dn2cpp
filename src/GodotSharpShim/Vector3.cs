// Copyright (c) 2014-present Godot Engine contributors (see AUTHORS.md).
// Copyright (c) 2007-2014 Juan Linietsky, Ariel Manzur.
// Licensed under the MIT License.

namespace Godot
{
    /// <summary>Hand-written pure-C# methods for the generated <c>Vector3</c> value
    /// type. The fields (<c>X</c>/<c>Y</c>/<c>Z</c>), constructors, arithmetic and
    /// equality operators, and constants come from the binding generator
    /// (<c>GodotShims.g.cs</c>, from <c>extension_api.json</c>'s
    /// <c>builtin_classes</c>); the engine carries no method bodies in the dump, so
    /// the common math is hand-written here (the same split as the hand-written
    /// <c>Vector2</c> methods), reusing the generated operators/ctors. Layout stays
    /// <c>{X, Y, Z}</c> exactly as generated.</summary>
    public partial struct Vector3
    {
        public float LengthSquared() => X * X + Y * Y + Z * Z;
        public float Length() => (float)System.Math.Sqrt(X * X + Y * Y + Z * Z);
        public float Dot(Vector3 with) => X * with.X + Y * with.Y + Z * with.Z;

        public Vector3 Cross(Vector3 with) =>
            new Vector3(Y * with.Z - Z * with.Y, Z * with.X - X * with.Z, X * with.Y - Y * with.X);

        public float DistanceSquaredTo(Vector3 to) => (to - this).LengthSquared();
        public float DistanceTo(Vector3 to) => (to - this).Length();

        public Vector3 Abs() =>
            new Vector3(System.Math.Abs(X), System.Math.Abs(Y), System.Math.Abs(Z));

        public Vector3 Normalized()
        {
            float len = Length();
            return len == 0 ? Zero : this / len;
        }

        public Vector3 Sign() =>
            new Vector3(System.Math.Sign(X), System.Math.Sign(Y), System.Math.Sign(Z));

        public Vector3 Clamp(Vector3 min, Vector3 max) =>
            new Vector3(System.Math.Clamp(X, min.X, max.X), System.Math.Clamp(Y, min.Y, max.Y),
                System.Math.Clamp(Z, min.Z, max.Z));

        // Linear interpolation (unclamped, matching Godot).
        public Vector3 Lerp(Vector3 to, float weight) =>
            new Vector3(X + (to.X - X) * weight, Y + (to.Y - Y) * weight, Z + (to.Z - Z) * weight);

        public Vector3 MoveToward(Vector3 to, float delta)
        {
            Vector3 vd = to - this;
            float len = vd.Length();
            return len <= delta || len < 1e-6f ? to : this + vd / len * delta;
        }

        public Vector3 DirectionTo(Vector3 to) => (to - this).Normalized();

        public Vector3 LimitLength(float length)
        {
            float l = Length();
            return l > 0f && length < l ? this / l * length : this;
        }

        public Vector3 Project(Vector3 onto) => onto * (Dot(onto) / onto.LengthSquared());

        // Reflect/slide/bounce expect a normalized `normal` (as in Godot).
        public Vector3 Slide(Vector3 normal) => this - normal * Dot(normal);
        public Vector3 Reflect(Vector3 normal) => normal * (2f * Dot(normal)) - this;
        public Vector3 Bounce(Vector3 normal) => -Reflect(normal);

        public Vector3 Floor() =>
            new Vector3((float)System.Math.Floor(X), (float)System.Math.Floor(Y), (float)System.Math.Floor(Z));
        public Vector3 Ceil() =>
            new Vector3((float)System.Math.Ceiling(X), (float)System.Math.Ceiling(Y), (float)System.Math.Ceiling(Z));
        public Vector3 Round() =>
            new Vector3((float)System.Math.Round(X), (float)System.Math.Round(Y), (float)System.Math.Round(Z));

        public Vector3 Min(Vector3 with) =>
            new Vector3(System.Math.Min(X, with.X), System.Math.Min(Y, with.Y), System.Math.Min(Z, with.Z));
        public Vector3 Max(Vector3 with) =>
            new Vector3(System.Math.Max(X, with.X), System.Math.Max(Y, with.Y), System.Math.Max(Z, with.Z));

        // Relational operators — Godot's are LEXICOGRAPHIC (compare X, then Y, then
        // Z), not component-wise. Verbatim from Godot's C# Vector3.cs. Subtlety: the
        // X/Y tie-break paths use strict `<`/`>` even in `<=`/`>=`; only the final
        // dimension (Z) uses the inclusive form.
        public static bool operator <(Vector3 left, Vector3 right)
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

        public static bool operator >(Vector3 left, Vector3 right)
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

        public static bool operator <=(Vector3 left, Vector3 right)
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

        public static bool operator >=(Vector3 left, Vector3 right)
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

        public Vector3 Rotated(Vector3 axis, float angle)
        {
            return new Basis(axis, angle) * this;
        }

        public float AngleTo(Vector3 to)
        {
            return (float)System.Math.Atan2(Cross(to).Length(), Dot(to));
        }

        public Vector3 Slerp(Vector3 to, float weight)
        {
            float startLengthSquared = LengthSquared();
            float endLengthSquared = to.LengthSquared();
            if (startLengthSquared == 0f || endLengthSquared == 0f)
            {
                return Lerp(to, weight);
            }
            Vector3 axis = Cross(to);
            float axisLengthSquared = axis.LengthSquared();
            if (axisLengthSquared == 0f)
            {
                return Lerp(to, weight);
            }
            axis /= (float)System.Math.Sqrt(axisLengthSquared);
            float startLength = (float)System.Math.Sqrt(startLengthSquared);
            float resultLength = startLength + (((float)System.Math.Sqrt(endLengthSquared) - startLength) * weight);
            float angle = AngleTo(to);
            return Rotated(axis, angle * weight) * (resultLength / startLength);
        }

        public Vector3 Snapped(Vector3 step) =>
            new Vector3(
                step.X != 0f ? (float)System.Math.Floor((X / step.X) + 0.5f) * step.X : X,
                step.Y != 0f ? (float)System.Math.Floor((Y / step.Y) + 0.5f) * step.Y : Y,
                step.Z != 0f ? (float)System.Math.Floor((Z / step.Z) + 0.5f) * step.Z : Z
            );

        public Vector3 Snapped(float step) =>
            new Vector3(
                step != 0f ? (float)System.Math.Floor((X / step) + 0.5f) * step : X,
                step != 0f ? (float)System.Math.Floor((Y / step) + 0.5f) * step : Y,
                step != 0f ? (float)System.Math.Floor((Z / step) + 0.5f) * step : Z
            );

        public Vector3 Inverse() => new Vector3(1f / X, 1f / Y, 1f / Z);
    }
}
