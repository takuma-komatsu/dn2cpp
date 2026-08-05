// Copyright (c) 2014-present Godot Engine contributors (see AUTHORS.md).
// Copyright (c) 2007-2014 Juan Linietsky, Ariel Manzur.
// Licensed under the MIT License.

namespace Godot
{
    /// <summary>Hand-written transform operators for the generated <c>Transform2D</c>
    /// value type. The fields (<c>X</c>/<c>Y</c> — the basis columns — and
    /// <c>Origin</c>), constructors and constants come from the binding generator
    /// (<c>GodotShims.g.cs</c>); the engine dump carries no method/operator bodies, so
    /// the affine transform math is hand-written here. Godot's own <c>Transform2D</c>
    /// stores <c>X</c>/<c>Y</c>/<c>Origin</c> identically, so the formulas translate
    /// directly:
    /// <code>
    /// // Tdotx(v) = this[0,0]*v[0] + this[1,0]*v[1] = X.X*v.X + Y.X*v.Y
    /// // Tdoty(v) = this[0,1]*v[0] + this[1,1]*v[1] = X.Y*v.X + Y.Y*v.Y
    /// // T * v    = (Tdotx(v), Tdoty(v)) + Origin
    /// </code></summary>
    public partial struct Transform2D
    {
        private readonly float Tdotx(Vector2 v) => X.X * v.X + Y.X * v.Y;
        private readonly float Tdoty(Vector2 v) => X.Y * v.X + Y.Y * v.Y;

        /// <summary>Transforms (multiplies) the <see cref="Vector2"/> by this
        /// affine transform: rotate/scale by the basis columns, then translate by
        /// <see cref="Origin"/>.</summary>
        public static Vector2 operator *(Transform2D transform, Vector2 v) =>
            new Vector2(transform.Tdotx(v), transform.Tdoty(v)) + transform.Origin;

        /// <summary>Composes two affine transforms (matrix product). Matches Godot's
        /// C#: the new origin is <c>left</c> applied to <c>right.Origin</c> (the full
        /// affine point transform), and each new basis column is <c>left</c>'s linear
        /// part (<c>Tdotx</c>/<c>Tdoty</c>, no translation) applied to <c>right</c>'s
        /// column.</summary>
        public static Transform2D operator *(Transform2D left, Transform2D right) =>
            new Transform2D(
                new Vector2(left.Tdotx(right.X), left.Tdoty(right.X)),
                new Vector2(left.Tdotx(right.Y), left.Tdoty(right.Y)),
                left * right.Origin);
    }
}
