// Copyright (c) 2014-present Godot Engine contributors (see AUTHORS.md).
// Copyright (c) 2007-2014 Juan Linietsky, Ariel Manzur.
// Licensed under the MIT License.

namespace Godot
{
    /// <summary>Hand-written methods for the generated <c>Quaternion</c> value type
    /// (fields/ctors/operators/constants are generated). Pure-C# basics matching
    /// Godot's formulas.</summary>
    public partial struct Quaternion
    {
        /// <summary>Constructs a rotation of <paramref name="angle"/> radians about
        /// <paramref name="axis"/>. Matches Godot's C# axis-angle constructor verbatim
        /// (the axis is expected normalized; a zero-length axis yields the zero
        /// quaternion).</summary>
        public Quaternion(Vector3 axis, float angle)
        {
            float d = axis.Length();
            if (d == 0f)
            {
                X = 0f;
                Y = 0f;
                Z = 0f;
                W = 0f;
            }
            else
            {
                float half = angle * 0.5f;
                float s = (float)System.Math.Sin(half) / d;
                X = axis.X * s;
                Y = axis.Y * s;
                Z = axis.Z * s;
                W = (float)System.Math.Cos(half);
            }
        }

        /// <summary>Builds a quaternion from Euler angles (radians) in Godot's default
        /// YXZ order. Matches Godot's C# <c>FromEuler</c> verbatim.</summary>
        public static Quaternion FromEuler(Vector3 eulerYXZ)
        {
            float halfA1 = eulerYXZ.Y * 0.5f;
            float halfA2 = eulerYXZ.X * 0.5f;
            float halfA3 = eulerYXZ.Z * 0.5f;
            float sinA1 = (float)System.Math.Sin(halfA1), cosA1 = (float)System.Math.Cos(halfA1);
            float sinA2 = (float)System.Math.Sin(halfA2), cosA2 = (float)System.Math.Cos(halfA2);
            float sinA3 = (float)System.Math.Sin(halfA3), cosA3 = (float)System.Math.Cos(halfA3);
            return new Quaternion(
                (sinA1 * cosA2 * sinA3) + (cosA1 * sinA2 * cosA3),
                (sinA1 * cosA2 * cosA3) - (cosA1 * sinA2 * sinA3),
                (cosA1 * cosA2 * sinA3) - (sinA1 * sinA2 * cosA3),
                (sinA1 * sinA2 * sinA3) + (cosA1 * cosA2 * cosA3));
        }

        public float LengthSquared() => X * X + Y * Y + Z * Z + W * W;
        public float Length() => (float)System.Math.Sqrt(LengthSquared());
        public float Dot(Quaternion with) => X * with.X + Y * with.Y + Z * with.Z + W * with.W;

        public Quaternion Normalized() => this / Length();

        // Inverse of a unit quaternion is its conjugate (matches Godot, which
        // assumes the quaternion is normalized).
        public Quaternion Inverse() => new Quaternion(-X, -Y, -Z, W);

        /// <summary>Spherical linear interpolation between this (unit) quaternion and
        /// <paramref name="to"/>. Matches Godot's C# <c>Slerp</c> verbatim — it takes
        /// the shorter arc (negating <paramref name="to"/> when the dot is negative)
        /// and falls back to linear blend for nearly-parallel quaternions.</summary>
        public Quaternion Slerp(Quaternion to, float weight)
        {
            float cosom = Dot(to);
            Quaternion to1;
            if (cosom < 0f)
            {
                cosom = -cosom;
                to1 = -to;
            }
            else
            {
                to1 = to;
            }

            float scale0, scale1;
            if (1f - cosom > Mathf.Epsilon)
            {
                float omega = (float)System.Math.Acos(cosom);
                float sinom = (float)System.Math.Sin(omega);
                scale0 = (float)System.Math.Sin((1f - weight) * omega) / sinom;
                scale1 = (float)System.Math.Sin(weight * omega) / sinom;
            }
            else
            {
                scale0 = 1f - weight;
                scale1 = weight;
            }

            return new Quaternion(
                (scale0 * X) + (scale1 * to1.X),
                (scale0 * Y) + (scale1 * to1.Y),
                (scale0 * Z) + (scale1 * to1.Z),
                (scale0 * W) + (scale1 * to1.W));
        }

        /// <summary>Quaternion product (composition of rotations). This is the
        /// Hamilton product, NOT component-wise — the generator deliberately does not
        /// synthesize the same-type <c>*</c> for <c>Quaternion</c> (a component-wise
        /// version would compile but silently return garbage). Matches Godot's C#
        /// <c>Quaternion</c> operator verbatim.</summary>
        public static Quaternion operator *(Quaternion left, Quaternion right) =>
            new Quaternion(
                (left.W * right.X) + (left.X * right.W) + (left.Y * right.Z) - (left.Z * right.Y),
                (left.W * right.Y) + (left.Y * right.W) + (left.Z * right.X) - (left.X * right.Z),
                (left.W * right.Z) + (left.Z * right.W) + (left.X * right.Y) - (left.Y * right.X),
                (left.W * right.W) - (left.X * right.X) - (left.Y * right.Y) - (left.Z * right.Z));

        /// <summary>Rotates the <see cref="Vector3"/> by this (unit) quaternion.
        /// Matches Godot's C# <c>Quaternion * Vector3</c> verbatim:
        /// <c>v + ((u × v) * W + u × (u × v)) * 2</c> with <c>u = (X, Y, Z)</c>.</summary>
        public static Vector3 operator *(Quaternion quaternion, Vector3 vector)
        {
            Vector3 u = new Vector3(quaternion.X, quaternion.Y, quaternion.Z);
            Vector3 uv = u.Cross(vector);
            return vector + (((uv * quaternion.W) + u.Cross(uv)) * 2);
        }
    }
}
