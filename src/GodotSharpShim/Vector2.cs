// Copyright (c) 2014-present Godot Engine contributors (see AUTHORS.md).
// Copyright (c) 2007-2014 Juan Linietsky, Ariel Manzur.
// Licensed under the MIT License.

namespace Godot
{
    /// <summary>Hand-written members for the generated <c>Vector2</c> value type.
    /// The fields (<c>X</c>/<c>Y</c>), constructors, arithmetic/equality operators
    /// and constants are generated from <c>extension_api.json</c>'s
    /// <c>builtin_classes</c> (the layout stays <c>{X, Y}</c> exactly as the engine
    /// marshalling requires); the engine dump carries no method bodies, so
    /// the common math is hand-written here, plus the left-scalar <c>*</c> overload
    /// the C# binding offers but the dump doesn't list.</summary>
    public partial struct Vector2
    {
        // Scalar-on-the-left multiply: the C# binding offers it, but the engine
        // dump only lists `Vector2 * scalar`, so it isn't generated.
        public static Vector2 operator *(float s, Vector2 a) => new Vector2(a.X * s, a.Y * s);

        public float LengthSquared() => X * X + Y * Y;
        public float Length() => (float)System.Math.Sqrt(X * X + Y * Y);
        public float Dot(Vector2 with) => X * with.X + Y * with.Y;
        public float DistanceSquaredTo(Vector2 to) => (to - this).LengthSquared();
        public float DistanceTo(Vector2 to) => (to - this).Length();

        public Vector2 Normalized()
        {
            float len = Length();
            return len == 0 ? Zero : new Vector2(X / len, Y / len);
        }

        // The 2D "cross product" (z-component / determinant), a float in Godot.
        public float Cross(Vector2 with) => X * with.Y - Y * with.X;

        public float Angle() => (float)System.Math.Atan2(Y, X);
        public float AngleTo(Vector2 to) => (float)System.Math.Atan2(Cross(to), Dot(to));
        public float AngleToPoint(Vector2 to) => (to - this).Angle();

        public Vector2 Abs() => new Vector2(System.Math.Abs(X), System.Math.Abs(Y));
        public Vector2 Sign() => new Vector2(System.Math.Sign(X), System.Math.Sign(Y));

        public Vector2 Clamp(Vector2 min, Vector2 max) =>
            new Vector2(System.Math.Clamp(X, min.X, max.X), System.Math.Clamp(Y, min.Y, max.Y));

        // Linear interpolation (unclamped, matching Godot).
        public Vector2 Lerp(Vector2 to, float weight) =>
            new Vector2(X + (to.X - X) * weight, Y + (to.Y - Y) * weight);

        public Vector2 Rotated(float angle)
        {
            float cos = (float)System.Math.Cos(angle);
            float sin = (float)System.Math.Sin(angle);
            return new Vector2(X * cos - Y * sin, X * sin + Y * cos);
        }

        public Vector2 MoveToward(Vector2 to, float delta)
        {
            Vector2 vd = to - this;
            float len = vd.Length();
            return len <= delta || len < 1e-6f ? to : this + vd / len * delta;
        }

        public Vector2 DirectionTo(Vector2 to) => (to - this).Normalized();

        public Vector2 LimitLength(float length)
        {
            float l = Length();
            return l > 0f && length < l ? this / l * length : this;
        }

        public Vector2 Project(Vector2 onto) => onto * (Dot(onto) / onto.LengthSquared());

        // Reflect/slide/bounce expect a normalized `normal` (as in Godot).
        public Vector2 Slide(Vector2 normal) => this - normal * Dot(normal);
        public Vector2 Reflect(Vector2 normal) => normal * (2f * Dot(normal)) - this;
        public Vector2 Bounce(Vector2 normal) => -Reflect(normal);

        public Vector2 Floor() => new Vector2((float)System.Math.Floor(X), (float)System.Math.Floor(Y));
        public Vector2 Ceil() => new Vector2((float)System.Math.Ceiling(X), (float)System.Math.Ceiling(Y));
        public Vector2 Round() => new Vector2((float)System.Math.Round(X), (float)System.Math.Round(Y));

        public Vector2 Min(Vector2 with) => new Vector2(System.Math.Min(X, with.X), System.Math.Min(Y, with.Y));
        public Vector2 Max(Vector2 with) => new Vector2(System.Math.Max(X, with.X), System.Math.Max(Y, with.Y));

        // Relational operators — Godot's are LEXICOGRAPHIC (compare X, tie-break on
        // Y), not component-wise. Verbatim from Godot's C# Vector2.cs. Note the
        // subtlety: the X tie-break path uses strict `<`/`>` even in `<=`/`>=`;
        // only the final dimension (Y) uses the inclusive form.
        public static bool operator <(Vector2 left, Vector2 right)
        {
            if (left.X == right.X)
            {
                return left.Y < right.Y;
            }
            return left.X < right.X;
        }

        public static bool operator >(Vector2 left, Vector2 right)
        {
            if (left.X == right.X)
            {
                return left.Y > right.Y;
            }
            return left.X > right.X;
        }

        public static bool operator <=(Vector2 left, Vector2 right)
        {
            if (left.X == right.X)
            {
                return left.Y <= right.Y;
            }
            return left.X < right.X;
        }

        public static bool operator >=(Vector2 left, Vector2 right)
        {
            if (left.X == right.X)
            {
                return left.Y >= right.Y;
            }
            return left.X > right.X;
        }

        public Vector2 Slerp(Vector2 to, float weight)
        {
            float startLengthSquared = LengthSquared();
            float endLengthSquared = to.LengthSquared();
            if (startLengthSquared == 0f || endLengthSquared == 0f)
            {
                return Lerp(to, weight);
            }
            float startLength = (float)System.Math.Sqrt(startLengthSquared);
            float resultLength = startLength + (((float)System.Math.Sqrt(endLengthSquared) - startLength) * weight);
            float angle = AngleTo(to);
            return Rotated(angle * weight) * (resultLength / startLength);
        }

        public Vector2 Snapped(Vector2 step) =>
            new Vector2(
                step.X != 0f ? (float)System.Math.Floor((X / step.X) + 0.5f) * step.X : X,
                step.Y != 0f ? (float)System.Math.Floor((Y / step.Y) + 0.5f) * step.Y : Y
            );

        public Vector2 Snapped(float step) =>
            new Vector2(
                step != 0f ? (float)System.Math.Floor((X / step) + 0.5f) * step : X,
                step != 0f ? (float)System.Math.Floor((Y / step) + 0.5f) * step : Y
            );

        public Vector2 Inverse() => new Vector2(1f / X, 1f / Y);
    }
}
