// Copyright (c) 2014-present Godot Engine contributors (see AUTHORS.md).
// Copyright (c) 2007-2014 Juan Linietsky, Ariel Manzur.
// Licensed under the MIT License.

namespace Godot
{
    /// <summary>Hand-written constants for the generated <c>Mathf</c> class. The
    /// functions (<c>Sqrt</c>/<c>Sin</c>/…) are generated from
    /// <c>extension_api.json</c>'s <c>utility_functions</c> and lowered to C++
    /// <c>&lt;cmath&gt;</c>; the constants below aren't in the dump, so they're
    /// hand-written (matching Godot's single-precision <c>Mathf</c>).</summary>
    public static partial class Mathf
    {
        public const float E = 2.7182817f;
        public const float Pi = 3.1415927f;
        public const float Tau = 6.2831855f;
        public const float Sqrt2 = 1.4142135f;
        public const float Inf = float.PositiveInfinity;
        public const float NaN = float.NaN;
        public const float Epsilon = 1e-06f;
    }
}
