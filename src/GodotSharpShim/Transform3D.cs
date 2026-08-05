// Copyright (c) 2014-present Godot Engine contributors (see AUTHORS.md).
// Copyright (c) 2007-2014 Juan Linietsky, Ariel Manzur.
// Licensed under the MIT License.

namespace Godot
{
    /// <summary>Hand-written transform operator for the generated <c>Transform3D</c>
    /// value type. The fields (<c>Basis</c> + <c>Origin</c>), constructors and
    /// constants come from the binding generator (<c>GodotShims.g.cs</c>); the engine
    /// dump carries no operator bodies, so the affine point-transform is hand-written.
    ///
    /// Matches Godot's own C# <c>Transform3D</c> — rotate/scale by the basis, then
    /// translate by the origin (<c>T * v = Basis * v + Origin</c>), reusing the
    /// <see cref="Basis"/> matrix-vector operator.</summary>
    public partial struct Transform3D
    {
        /// <summary>Transforms (multiplies) the <see cref="Vector3"/> by this affine
        /// transform: rotate/scale by <see cref="Basis"/>, then translate by
        /// <see cref="Origin"/>.</summary>
        public static Vector3 operator *(Transform3D transform, Vector3 v) =>
            transform.Basis * v + transform.Origin;

        /// <summary>Composes two affine 3D transforms (matrix product). Matches
        /// Godot's C#: the basis is the product of the two bases, and the new origin
        /// is <c>left</c> applied to <c>right.Origin</c> (the full affine point
        /// transform, including <c>left</c>'s translation).</summary>
        public static Transform3D operator *(Transform3D left, Transform3D right) =>
            new Transform3D(left.Basis * right.Basis, left * right.Origin);
    }
}
