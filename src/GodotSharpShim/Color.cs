// Copyright (c) 2014-present Godot Engine contributors (see AUTHORS.md).
// Copyright (c) 2007-2014 Juan Linietsky, Ariel Manzur.
// Licensed under the MIT License.

namespace Godot
{
    /// <summary>Hand-written convenience members for the generated <c>Color</c> value
    /// type. The real-storage fields (<c>R</c>/<c>G</c>/<c>B</c>/<c>A</c>), the
    /// 4-component constructor, arithmetic/equality operators and the named-color
    /// constants come from the binding generator (the derived <c>r8</c>/<c>h</c>/
    /// <c>s</c>/<c>v</c>/<c>ok_hsl_*</c> "members" are filtered out via
    /// <c>BuiltinRealMembers</c>). Standard Godot C# commonly uses the 3-argument
    /// constructor (alpha defaults to 1), which has no body in the dump, so it is
    /// hand-written here over the generated fields.</summary>
    public partial struct Color
    {
        public Color(float r, float g, float b)
        {
            R = r;
            G = g;
            B = b;
            A = 1f;
        }

        // Pure-C# methods (matching Godot's formulas) over the generated r/g/b/a.
        public Color Inverted() => new Color(1f - R, 1f - G, 1f - B, A);

        public Color Lerp(Color to, float weight) =>
            new Color(R + weight * (to.R - R), G + weight * (to.G - G),
                B + weight * (to.B - B), A + weight * (to.A - A));

        public Color Lightened(float amount) =>
            new Color(R + (1f - R) * amount, G + (1f - G) * amount, B + (1f - B) * amount, A);

        public Color Darkened(float amount) =>
            new Color(R * (1f - amount), G * (1f - amount), B * (1f - amount), A);

        public Color Clamp(Color min, Color max) =>
            new Color(System.Math.Clamp(R, min.R, max.R), System.Math.Clamp(G, min.G, max.G),
                System.Math.Clamp(B, min.B, max.B), System.Math.Clamp(A, min.A, max.A));

        public float GetLuminance() => 0.2126f * R + 0.7152f * G + 0.0722f * B;
    }
}
