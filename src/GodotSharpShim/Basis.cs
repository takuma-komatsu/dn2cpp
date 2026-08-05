// Copyright (c) 2014-present Godot Engine contributors (see AUTHORS.md).
// Copyright (c) 2007-2014 Juan Linietsky, Ariel Manzur.
// Licensed under the MIT License.

namespace Godot
{
    /// <summary>Hand-written transform operators for the generated <c>Basis</c> value
    /// type; its fields, constructors and constants come from the binding generator
    /// (<c>GodotShims.g.cs</c>), and the engine dump carries no operator bodies.
    ///
    /// <para>Convention (silent-error risk): Godot's own C# <c>Basis</c> stores three
    /// <em>rows</em> and exposes columns as computed properties. This shim stores the
    /// <em>columns</em> in <c>X</c>/<c>Y</c>/<c>Z</c>, matching GDScript's
    /// <c>basis.x/y/z</c>. The two are transposes:</para>
    /// <code>
    /// Row0 = (X.X, Y.X, Z.X)   Row1 = (X.Y, Y.Y, Z.Y)   Row2 = (X.Z, Y.Z, Z.Z)
    /// </code>
    /// so Godot's row-dotted <c>Basis * Vector3</c> becomes, in column storage:
    /// <code>
    /// result.X = X.X*v.X + Y.X*v.Y + Z.X*v.Z
    /// result.Y = X.Y*v.X + Y.Y*v.Y + Z.Y*v.Z
    /// result.Z = X.Z*v.X + Y.Z*v.Y + Z.Z*v.Z
    /// </code></summary>
    public partial struct Basis
    {
        /// <summary>Transforms (rotates/scales) the <see cref="Vector3"/> by this
        /// basis: the row-dotted matrix-vector product.</summary>
        public static Vector3 operator *(Basis basis, Vector3 v) =>
            new Vector3(
                basis.X.X * v.X + basis.Y.X * v.Y + basis.Z.X * v.Z,
                basis.X.Y * v.X + basis.Y.Y * v.Y + basis.Z.Y * v.Z,
                basis.X.Z * v.X + basis.Y.Z * v.Y + basis.Z.Z * v.Z);

        /// <summary>Composes two bases (matrix product <c>left * right</c>). A column
        /// of a matrix product is the left matrix applied to the corresponding column
        /// of the right matrix, so each result column is <c>left</c> applied (via the
        /// matrix-vector operator above) to <c>right</c>'s column — exploiting our
        /// column storage to stay convention-correct.</summary>
        public static Basis operator *(Basis left, Basis right) =>
            new Basis(left * right.X, left * right.Y, left * right.Z);

        /// <summary>The transpose — our stored columns become the result's rows (and
        /// vice versa). Since we store columns, the transpose's column <c>c</c> is the
        /// tuple of the <c>c</c>-th components of <c>X</c>/<c>Y</c>/<c>Z</c> (matching
        /// the Row0/Row1/Row2 derivation in the type doc).</summary>
        public Basis Transposed() =>
            new Basis(
                new Vector3(X.X, Y.X, Z.X),
                new Vector3(X.Y, Y.Y, Z.Y),
                new Vector3(X.Z, Y.Z, Z.Z));

        /// <summary>The determinant — the scalar triple product of the three column
        /// vectors (<c>det(M)</c> equals the triple product of its columns,
        /// storage-convention-independent).</summary>
        public float Determinant() => X.Dot(Y.Cross(Z));

        /// <summary>The matrix inverse. For a matrix whose columns are <c>X/Y/Z</c>,
        /// the inverse's rows are <c>(Y×Z)/det</c>, <c>(Z×X)/det</c>, <c>(X×Y)/det</c>;
        /// stored back as columns, column <c>j</c> gathers the <c>j</c>-th components
        /// of those three cofactor vectors. (Matches Godot's general 3×3 inverse;
        /// undefined for a singular basis, like Godot's.)</summary>
        public Basis Inverse()
        {
            Vector3 c0 = Y.Cross(Z);
            Vector3 c1 = Z.Cross(X);
            Vector3 c2 = X.Cross(Y);
            float id = 1f / X.Dot(c0);
            return new Basis(
                new Vector3(c0.X, c1.X, c2.X) * id,
                new Vector3(c0.Y, c1.Y, c2.Y) * id,
                new Vector3(c0.Z, c1.Z, c2.Z) * id);
        }

        public Basis(Vector3 axis, float angle)
        {
            Vector3 axisSq = new Vector3(axis.X * axis.X, axis.Y * axis.Y, axis.Z * axis.Z);
            float sin = (float)System.Math.Sin(angle);
            float cos = (float)System.Math.Cos(angle);

            float t = 1.0f - cos;

            float xyzt = axis.X * axis.Y * t;
            float zyxs = axis.Z * sin;
            float yx = xyzt - zyxs;
            float xy = xyzt + zyxs;

            xyzt = axis.X * axis.Z * t;
            zyxs = axis.Y * sin;
            float zx = xyzt + zyxs;
            float xz = xyzt - zyxs;

            xyzt = axis.Y * axis.Z * t;
            zyxs = axis.X * sin;
            float zy = xyzt - zyxs;
            float yz = xyzt + zyxs;

            X = new Vector3(axisSq.X + cos * (1.0f - axisSq.X), xy, xz);
            Y = new Vector3(yx, axisSq.Y + cos * (1.0f - axisSq.Y), yz);
            Z = new Vector3(zx, zy, axisSq.Z + cos * (1.0f - axisSq.Z));
        }
    }
}
