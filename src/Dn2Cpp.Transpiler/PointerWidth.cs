// Two integer readings of one layout at the only two pointer widths that exist, rendered
// as C++ text the compiler re-evaluates for its own target. Nothing here knows a layout
// rule: the representation model (CppEmitter) and the marshalled model
// (Compilation.MarshalLayout) each keep their own walk and their own numbers, and share
// only this arithmetic.

namespace Dn2Cpp;

/// <summary>A modeled byte size rendered as a C++ constant expression.
/// <see cref="Guarded"/> marks the one case left with no expression at all: the model has
/// no 32-bit reading (the narrow walk refused the type), so the emitter must STATE that
/// premise (<c>sizeof(void*) != 8 ||</c>) rather than pin a 64-bit-only number, the same
/// convention the sequential-layout and marshalled-layout asserts follow.</summary>
internal readonly record struct ModeledSize(string Text, bool Guarded, bool UsesPointerWidth)
{
    /// <summary>The size where the slot it fills is <c>int32_t</c>: <c>sizeof</c> is
    /// <c>size_t</c>, so only a width-bearing expression needs the narrowing cast.</summary>
    internal string Int32Expr => UsesPointerWidth ? $"(int32_t)({Text})" : Text;
}

internal static class PointerWidth
{
    // The pointer widths the layout model reads a type at. Every target dn2cpp emits for
    // is one of the two, so a size known at both is known everywhere (see Model).
    // Bytes64 is the model's own width: it alone decides what the transpile refuses.
    internal const int Bytes64 = 8;
    internal const int Bytes32 = 4;

    /// <summary>Renders a size the model read at both pointer widths. The affine form is
    /// preferred where it exists — the line through the two readings is exact at both, and
    /// the text then says WHY the size is right. Where it does not, the two readings are
    /// selected between: a parenthesised ternary on <c>sizeof(void*)</c> is exact at both
    /// widths, and those are the only two, so it is as target-independent as the affine
    /// form and just as legal a constant expression. Only a MISSING 32-bit reading is left
    /// to guard.</summary>
    internal static ModeledSize Model(int size64, int? size32)
    {
        if (size32 is not { } s32)
            return new ModeledSize($"{size64}", true, false);
        if ((size64 - s32) % (Bytes64 - Bytes32) != 0)
            return PerWidth(size64, s32);
        int ptrs = (size64 - s32) / (Bytes64 - Bytes32);
        int rest = size64 - ptrs * Bytes64;
        if (ptrs == 0)
            return new ModeledSize($"{rest}", false, false);
        string term = ptrs is 1 or -1 ? "sizeof(void*)" : $"{Math.Abs(ptrs)} * sizeof(void*)";
        // The constant part is written on whichever side keeps every operand positive:
        // sizeof is size_t, so a negative literal would only come out right by unsigned
        // wraparound.
        if (ptrs > 0)
            return new ModeledSize(rest == 0 ? term : rest > 0 ? $"{rest} + {term}" : $"{term} - {-rest}", false, true);
        if (rest > 0)
            return new ModeledSize($"{rest} - {term}", false, true);
        return PerWidth(size64, s32);
    }

    private static ModeledSize PerWidth(int size64, int size32) =>
        new ModeledSize($"(sizeof(void*) == 8 ? {size64} : {size32})", false, true);
}
