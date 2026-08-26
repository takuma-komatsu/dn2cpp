using System.Reflection.Metadata;
using System.Text;
using SRME = System.Reflection.Metadata.Ecma335.MetadataTokens;

namespace Dn2Cpp;

internal sealed partial class MethodCompiler
{
    /// <summary>Maps System.Math/MathF static functions — and the System.Double /
    /// System.Single static generic-math surface, which shares this map — to C++
    /// &lt;cmath&gt; or simple expressions. Returns false (having popped nothing) if
    /// the (name, arity) is unmapped, so the caller's switch keeps flowing.</summary>
    private static bool IsMidpointRounding(TypeDesc t) =>
        (t.Kind == TypeKind.External && t.ExternalName == "System.MidpointRounding")
        || (t.Kind == TypeKind.Class && t.Class?.FullName == "System.MidpointRounding");

    // Static bit-counting / rotate operations exposed as static methods on the
    // primitive integer types (uint.TrailingZeroCount, ulong.PopCount, …) — the
    // IBinaryInteger<TSelf> / IBinaryNumber<TSelf> intrinsics — mapped straight to
    // clang builtins. These types are s_intrinsicTypes members, so
    // Compilation.ResolveCallTarget already cuts their BCL bodies. The result type
    // follows the signature (TSelf, e.g. uint for uint.PopCount), not the int the
    // underlying BitOperations return.
    private bool TryEmitIntegerBitOpIntrinsic(string declType, string name, MethodSignature<TypeDesc> sig)
    {
        int width = declType switch
        {
            "System.Int32" or "System.UInt32" => 32,
            "System.Int64" or "System.UInt64" => 64,
            _ => 0,
        };
        if (width == 0)
            return false;

        string uct = width == 64 ? "uint64_t" : "uint32_t";
        string retCt = CppTypes.Of(sig.ReturnType);
        StackKind retKind = CppTypes.KindOf(sig.ReturnType);

        // Single-operand counts: delegate to the shared builtin emitter. The result
        // type follows the signature (TSelf, e.g. uint for uint.PopCount). The
        // signed types' Log2 raises ArgumentOutOfRangeException on a negative
        // input, matching int.Log2/long.Log2 (BitOperations.Log2 never routes
        // here — its declaring type doesn't match above).
        if (name is "TrailingZeroCount" or "LeadingZeroCount" or "Log2" or "PopCount"
                && sig.ParameterTypes.Length == 1)
        {
            EmitBitCountOp(name, Pop(), width, retCt, retKind,
                signedLog2Throws: declType is "System.Int32" or "System.Int64");
            return true;
        }

        // RotateLeft/RotateRight(value, rotateAmount). Mask the amount to the bit
        // width so amount 0 (and multiples of the width) stay well-defined — a
        // shift by the full width is UB in C++. Both value and the masked amount
        // appear twice, so spill each.
        if (name is "RotateLeft" or "RotateRight" && sig.ParameterTypes.Length == 2)
        {
            var off = Pop();
            var v = Pop();
            string vt = NewTemp(uct);
            Emit($"{vt} = ({uct})({v.Expr});");
            string rt = NewTemp("int32_t");
            Emit($"{rt} = (int32_t)({off.Expr}) & {width - 1};");
            string expr = name == "RotateLeft"
                ? $"(({vt} << {rt}) | ({vt} >> (({width} - {rt}) & {width - 1})))"
                : $"(({vt} >> {rt}) | ({vt} << (({width} - {rt}) & {width - 1})))";
            Push(retKind, retCt, $"({retCt})({expr})");
            return true;
        }

        return false;
    }

    // Emits a single-operand bit-count op (TrailingZeroCount, LeadingZeroCount,
    // Log2, PopCount) as the matching clang builtin over an already-popped operand
    // of the given bit width, pushing the count as retCt/retKind. .NET defines the
    // zero input — *ZeroCount return the bit width and Log2 returns 0 — whereas
    // __builtin_ctz/clz(0) are UB, so the zero case is an explicit guard; the
    // operand is spilled because it appears several times in it. A sub-word width
    // (8/16 — the generic-math TSelf forms) narrows the operand out of its
    // int32-promoted slot before the 32-bit builtins run (a sign-extended sbyte must
    // count 8 stored bits, not 32) and rebases LeadingZeroCount from the builtin's
    // 32-bit position. signedLog2Throws is the SIGNED types' Log2 contract: a
    // negative input raises ArgumentOutOfRangeException, checked at the operand's
    // real storage width. BitOperations.Log2 (no throw — zero and the sign bit are
    // plain data) and the unsigned types pass false.
    private void EmitBitCountOp(string name, StackEntry v, int width, string retCt, StackKind retKind,
        bool signedLog2Throws = false)
    {
        string uct = width == 64 ? "uint64_t" : "uint32_t";
        string ll = width == 64 ? "ll" : "";
        int builtinWidth = width == 64 ? 64 : 32;
        string narrowed = width switch { 8 => "(uint8_t)", 16 => "(uint16_t)", _ => "" };
        string lzAdjust = builtinWidth == width ? "" : $" - {builtinWidth - width}";
        string t = NewTemp(uct);
        Emit($"{t} = ({uct}){narrowed}({v.Expr});");
        if (name == "Log2" && signedLog2Throws)
        {
            string sct = width switch { 8 => "int8_t", 16 => "int16_t", 64 => "int64_t", _ => "int32_t" };
            string snarrow = width switch { 8 => "(uint8_t)", 16 => "(uint16_t)", _ => "" };
            Emit($"if (({sct}){snarrow}{t} < 0) dn2cpp_throw_argument_out_of_range();");
        }
        string expr = name switch
        {
            "TrailingZeroCount" => $"({t} == 0 ? {width} : __builtin_ctz{ll}({t}))",
            "LeadingZeroCount" => $"({t} == 0 ? {width} : __builtin_clz{ll}({t}){lzAdjust})",
            "Log2" => $"({t} == 0 ? 0 : ({builtinWidth - 1} - __builtin_clz{ll}({t})))",
            "PopCount" => $"__builtin_popcount{ll}({t})",
            _ => throw new InvalidOperationException(),
        };
        Push(retKind, retCt, $"({retCt})({expr})");
    }

    // The correctly-rounded Pi in the overload's precision (float.Pi / double.Pi)
    // — the *Pi angle family and the degree conversions divide/multiply by this
    // fkind-typed constant so every step stays in the overload's precision.
    private static string FloatPi(bool isFloat) =>
        isFloat ? "3.14159265f" : "3.141592653589793";

    private bool TryMathIntrinsic(string declType, string name, MethodSignature<TypeDesc> sig)
    {
        bool isFloat = declType is "System.MathF" or "System.Single";
        string fkind = isFloat ? "float" : "double";
        var ret = sig.ReturnType;

        // Math.ThrowNegateTwosCompOverflow — the internal Abs/CopySign guard helper
        // (`throw new OverflowException(SR.Overflow_NegateTwosCompNum)`), lowered to
        // the runtime's managed OverflowException trap.
        if (declType == "System.Math" && name == "ThrowNegateTwosCompOverflow"
            && sig.ParameterTypes.Length == 0)
        {
            Emit("dn2cpp_overflow();");
            return true;
        }

        // The IFloatingPointConstants properties (double.Pi / E / Tau and the
        // float forms — properties, unlike the const Math.PI/E/Tau fields, so
        // they arrive as static getter calls): the correctly-rounded constants
        // in the overload's precision.
        if (sig.ParameterTypes.Length == 0)
        {
            string? fconst = name switch
            {
                "get_Pi" => FloatPi(isFloat),
                "get_E" => isFloat ? "2.71828183f" : "2.718281828459045",
                "get_Tau" => isFloat ? "6.28318531f" : "6.283185307179586",
                _ => null,
            };
            if (fconst is not null)
            {
                Push(StackKind.R8, "double", $"(double){fconst}");
                return true;
            }
        }

        // Math.DivRem — integer quotient+remainder. The modern form returns a
        // (Quotient, Remainder) ValueTuple<T,T>; the obsolete 3-arg form returns the
        // quotient and writes the remainder to an out param. Operands are evaluated
        // once into temps; integer / and % already match .NET truncation/unsigned
        // semantics for the closed operand type — but NOT its fault semantics, so
        // both go through the same guards the IL div/rem lowering takes
        // (DivideByZeroException on a zero divisor, OverflowException on MinValue/-1).
        if (name == "DivRem" && sig.ParameterTypes is [{ Kind: TypeKind.Primitive } d0, _, ..])
        {
            string ct = CppTypes.Of(d0);
            if (sig.ParameterTypes.Length == 2)
            {
                var b = Pop();
                var a = Pop();
                string at = NewTemp(ct), bt = NewTemp(ct);
                Emit($"{at} = {Cast(a, ct)};");
                Emit($"{bt} = {Cast(b, ct)};");
                string retCt = CppTypes.Of(sig.ReturnType);
                // The tuple fields hold the operand's REAL storage width (a
                // sub-word overload's fields are uint8_t/int16_t/…, while the
                // operands ride int32-promoted), so narrow each element into
                // the field type — the int32 quotient/remainder of in-range
                // sub-word operands always fits.
                string est = CppTypes.StorageOf(d0);
                Push(StackKind.Struct, retCt,
                    $"{retCt}{{ ({est}){DivRemGuard(ct, rem: false)}({at}, {bt}), "
                        + $"({est}){DivRemGuard(ct, rem: true)}({at}, {bt}) }}");
                return true;
            }
            if (sig.ParameterTypes.Length == 3)
            {
                var rem = Pop();
                var b = Pop();
                var a = Pop();
                string at = NewTemp(ct), bt = NewTemp(ct);
                Emit($"{at} = {Cast(a, ct)};");
                Emit($"{bt} = {Cast(b, ct)};");
                Emit($"*({ct}*)({rem.Expr}) = ({ct}){DivRemGuard(ct, rem: true)}({at}, {bt});");
                Push(CppTypes.KindOf(d0), ct, $"({ct}){DivRemGuard(ct, rem: false)}({at}, {bt})");
                return true;
            }
        }
        // Math.BigMul — the full-width product. The 32-bit overloads widen and
        // multiply inline (each operand is re-narrowed first: the IL stack may
        // carry a differently-signed expression in the int32 slot). The 64-bit
        // overloads need the 128-bit product, which lives in a runtime helper:
        // the (a, b, out low) forms return the high half and store the low one
        // like DivRem stores its remainder, and the Int128/UInt128-returning
        // forms aggregate the helper's two halves in the struct's declaration
        // order (_lower, _upper — both uint64 fields, little-endian layout).
        // Must sit before the integer block: the (int,int)->long overload's
        // integral return type would otherwise route the name there and miss.
        if (name == "BigMul" && sig.ParameterTypes is [{ Kind: TypeKind.Primitive } m0, ..])
        {
            if (sig.ParameterTypes.Length == 2
                && m0.Primitive is PrimitiveTypeCode.Int32 or PrimitiveTypeCode.UInt32)
            {
                bool signed32 = m0.Primitive == PrimitiveTypeCode.Int32;
                string wide = signed32 ? "int64_t" : "uint64_t";
                string narrow = signed32 ? "int32_t" : "uint32_t";
                var b = Pop();
                var a = Pop();
                Push(CppTypes.KindOf(ret), wide,
                    $"(({wide})({narrow})({a.Expr}) * ({wide})({narrow})({b.Expr}))");
                return true;
            }
            if (m0.Primitive is PrimitiveTypeCode.Int64 or PrimitiveTypeCode.UInt64)
            {
                bool signed64 = m0.Primitive == PrimitiveTypeCode.Int64;
                string ct = signed64 ? "int64_t" : "uint64_t";
                string helper = signed64 ? "dn2cpp_math_bigmul_i64" : "dn2cpp_math_bigmul_u64";
                if (sig.ParameterTypes.Length == 3)
                {
                    var lowPtr = Pop();
                    var b = Pop();
                    var a = Pop();
                    string hi = NewTemp(ct);
                    Emit($"{hi} = {helper}({Cast(a, ct)}, {Cast(b, ct)}, ({ct}*)({lowPtr.Expr}));");
                    Push(CppTypes.KindOf(ret), ct, hi);
                    return true;
                }
                if (sig.ParameterTypes.Length == 2)
                {
                    var b = Pop();
                    var a = Pop();
                    string lo = NewTemp(ct), hi = NewTemp(ct);
                    Emit($"{hi} = {helper}({Cast(a, ct)}, {Cast(b, ct)}, &{lo});");
                    string retCt = CppTypes.Of(ret);
                    Push(StackKind.Struct, retCt,
                        $"{retCt}{{ (uint64_t)({lo}), (uint64_t)({hi}) }}");
                    return true;
                }
            }
        }
        // Math/MathF.SinCos — returns a (Sin, Cos) ValueTuple, built like DivRem's.
        // The operand is spilled once: it feeds both calls and the stack expr may
        // have side effects.
        if (name == "SinCos" && sig.ParameterTypes.Length == 1)
        {
            var a = Pop();
            string t = NewTemp(fkind);
            Emit($"{t} = ({fkind})({a.Expr});");
            string retCt = CppTypes.Of(ret);
            Push(StackKind.Struct, retCt,
                $"{retCt}{{ ({fkind})std::sin({t}), ({fkind})std::cos({t}) }}");
            return true;
        }
        // Double/Single.SinCosPi — a (SinPi, CosPi) ValueTuple built like
        // SinCos's, but filled by the runtime helper from one argument
        // reduction (the BCL's managed AOCL port; see the SinPi arm below).
        if (name == "SinCosPi" && sig.ParameterTypes.Length == 1)
        {
            var a = Pop();
            string sinT = NewTemp(fkind), cosT = NewTemp(fkind);
            Emit($"dn2cpp_math_sincospi{(isFloat ? "_f" : "")}(({fkind})({a.Expr}), &{sinT}, &{cosT});");
            string retCt = CppTypes.Of(ret);
            Push(StackKind.Struct, retCt, $"{retCt}{{ {sinT}, {cosT} }}");
            return true;
        }
        // Math/MathF.ILogB — the unbiased base-2 exponent as an int32. Routed to
        // a runtime helper, not std::ilogb, whose zero/NaN return values are
        // platform-defined where .NET pins int.MinValue / int.MaxValue. Must sit
        // before the integer Abs/Min/Max block: the int32 return type would
        // otherwise route the name there and miss.
        if (name == "ILogB" && sig.ParameterTypes.Length == 1)
        {
            var iv = Pop();
            Push(StackKind.I4, "int32_t",
                $"dn2cpp_math_ilogb{(isFloat ? "_f" : "")}(({fkind})({iv.Expr}))");
            return true;
        }

        // Integer Abs/Min/Max/Clamp keep the overload's integral stack kind — for
        // every integral primitive, not just Int32/Int64. The unsigned overloads
        // (uint/ulong Min/Max/Clamp) and the sub-word ones (byte/sbyte/short/ushort)
        // must take this arm too: routing them through std::fmin/fmax would carry an
        // R8 stack kind (stack-kind merge failures at use sites) and lose precision
        // above 2^53. The per-operand Cast to the parameter's C++ type below
        // preserves signedness.
        bool intResult = ret.Kind == TypeKind.Primitive && IsIntegerPrimitive(ret.Primitive);

        // The Math.* decimal overloads (Round/Abs/Truncate/Floor/Ceiling/Min/Max/
        // Sign) route to the System.Decimal runtime helpers.
        if (!isFloat && sig.ParameterTypes.Any(IsDecimal))
        {
            if (name == "Sign" && sig.ParameterTypes.Length == 1)
            {
                var a = Pop();
                Push(StackKind.I4, "int32_t", $"dn2cpp_decimal_cmp({DecVal(a)}, dn2cpp_decimal_from_i4(0))");
                return true;
            }
            return TryEmitDecimalIntrinsic(name, sig);
        }

        // Sign always returns int (-1 / 0 / +1), whatever the numeric input. The
        // operand is spilled once — it appears twice in the three-way (plus the
        // NaN guard) and the stack expr may have side effects. A float/double
        // NaN has no sign: .NET throws ArithmeticException, so guard before the
        // comparison (which would otherwise quietly answer 0).
        if (name == "Sign" && sig.ParameterTypes.Length == 1)
        {
            string sct = CppTypes.Of(sig.ParameterTypes[0]);
            var a = Pop();
            string t = NewTemp(sct);
            Emit($"{t} = {Cast(a, sct)};");
            if (sig.ParameterTypes[0] is { Kind: TypeKind.Primitive,
                    Primitive: PrimitiveTypeCode.Single or PrimitiveTypeCode.Double })
                Emit($"if (std::isnan({t})) dn2cpp_throw_arithmetic();");
            Push(StackKind.I4, "int32_t", $"(({t}) > 0 ? 1 : (({t}) < 0 ? -1 : 0))");
            return true;
        }

        // Integer Abs/Min/Max/Clamp keep integer stack types. Every operand is cast to
        // the overload's parameter type first: the IL stack may carry a differently-signed
        // C++ expression for an int32 slot, and C++'s usual arithmetic conversions would
        // otherwise compare -1 > u as unsigned and pick the wrong operand.
        if (!isFloat && intResult)
        {
            string mct = CppTypes.Of(sig.ParameterTypes[0]);
            switch (name, sig.ParameterTypes.Length)
            {
                case ("Clamp", 3):
                {
                    // The runtime template raises ArgumentException when min > max
                    // like .NET, and evaluates each operand once. The explicit casts
                    // deduce T as the overload's parameter type.
                    var hi = Pop(); var lo = Pop(); var v = Pop();
                    Push(CppTypes.KindOf(sig.ParameterTypes[0]), mct,
                        $"dn2cpp_math_clamp(({mct})({v.Expr}), ({mct})({lo.Expr}), ({mct})({hi.Expr}))");
                    return true;
                }
                case ("Abs", 1):
                {
                    // Abs(MinValue) has no positive counterpart: .NET throws
                    // OverflowException — and -MinValue is UB in C++ anyway. The
                    // operand narrows to the overload's real storage width (sub-word
                    // values ride the stack as int32) so the template's
                    // numeric_limits MinValue check matches the declared type.
                    var a = Pop();
                    string act = CppTypes.StorageOf(sig.ParameterTypes[0]);
                    Push(CppTypes.KindOf(sig.ParameterTypes[0]), mct,
                        $"({mct})dn2cpp_math_abs_int(({act})({a.Expr}))");
                    return true;
                }
                case ("Min", 2):
                {
                    var b = Pop(); var a = Pop();
                    string ax = Cast(a, mct), bx = Cast(b, mct);
                    Push(CppTypes.KindOf(sig.ParameterTypes[0]), mct, $"(({ax}) < ({bx}) ? ({ax}) : ({bx}))");
                    return true;
                }
                case ("Max", 2):
                {
                    var b = Pop(); var a = Pop();
                    string ax = Cast(a, mct), bx = Cast(b, mct);
                    Push(CppTypes.KindOf(sig.ParameterTypes[0]), mct, $"(({ax}) > ({bx}) ? ({ax}) : ({bx}))");
                    return true;
                }
            }
            return false;
        }

        // Floating-point functions (stack kind R8 throughout).
        string? unary = name switch
        {
            "Sqrt" => "std::sqrt", "Cbrt" => "std::cbrt", "Abs" => "std::fabs",
            "Floor" => "std::floor",
            "Ceiling" => "std::ceil", "Round" when sig.ParameterTypes.Length == 1 => "std::nearbyint",
            "Truncate" => "std::trunc", "Sin" => "std::sin",
            "Cos" => "std::cos", "Tan" => "std::tan",
            "Asin" => "std::asin", "Acos" => "std::acos", "Atan" => "std::atan",
            "Sinh" => "std::sinh", "Cosh" => "std::cosh", "Tanh" => "std::tanh",
            "Asinh" => "std::asinh", "Acosh" => "std::acosh", "Atanh" => "std::atanh",
            "Exp" => "std::exp",
            "Log10" => "std::log10", "Log2" => "std::log2",
            "Log" when sig.ParameterTypes.Length == 1 => "std::log",
            _ => null,
        };
        if (unary is not null && sig.ParameterTypes.Length == 1)
        {
            var a = Pop();
            Push(StackKind.R8, "double", $"(double){unary}(({fkind})({a.Expr}))");
            return true;
        }
        // The Double/Single-only exp/log derivatives and the *Pi angle family.
        // The BCL computes every one of these as a composition of the base libm
        // functions — ExpM1(x) = Exp(x) - 1, Exp2(x) = Pow(2, x), AsinPi(x) =
        // Asin(x) / Pi, DegreesToRadians(d) = (d * Pi) / 180 — never via the
        // specialized libm entry points (expm1/exp2/log1p/...), whose different
        // algorithms drift in the last ulp. The same composition here over the
        // same libm bit-matches .NET; every step stays in the overload's
        // precision. Placeholders: {0} the fkind-cast operand, {1} fkind,
        // {2} the fkind-typed Pi.
        string? composed = name switch
        {
            "ExpM1" => "std::exp({0}) - ({1})1",
            "Exp2" => "std::pow(({1})2, {0})",
            "Exp2M1" => "std::pow(({1})2, {0}) - ({1})1",
            "Exp10" => "std::pow(({1})10, {0})",
            "Exp10M1" => "std::pow(({1})10, {0}) - ({1})1",
            "LogP1" => "std::log({0} + ({1})1)",
            "Log2P1" => "std::log2({0} + ({1})1)",
            "Log10P1" => "std::log10({0} + ({1})1)",
            "AsinPi" => "std::asin({0}) / {2}",
            "AcosPi" => "std::acos({0}) / {2}",
            "AtanPi" => "std::atan({0}) / {2}",
            "DegreesToRadians" => "({0} * {2}) / ({1})180",
            "RadiansToDegrees" => "({0} * ({1})180) / {2}",
            _ => null,
        };
        if (composed is not null && sig.ParameterTypes.Length == 1)
        {
            var ca = Pop();
            Push(StackKind.R8, "double", "(double)(" + string.Format(
                composed, $"(({fkind})({ca.Expr}))", fkind, FloatPi(isFloat)) + ")");
            return true;
        }
        // SinPi/CosPi/TanPi are NOT compositions: the BCL computes sin/cos/tan
        // of pi*x with its own managed AOCL-derived reduction over the
        // fractional turn position (exact signed zeros/infinities at the
        // half-turn positions, no libm counterpart in the last ulp), ported
        // bit-for-bit to the runtime helpers.
        string? piTrig = name switch
        {
            "SinPi" => "dn2cpp_math_sinpi",
            "CosPi" => "dn2cpp_math_cospi",
            "TanPi" => "dn2cpp_math_tanpi",
            _ => null,
        };
        if (piTrig is not null && sig.ParameterTypes.Length == 1)
        {
            var pa = Pop();
            Push(StackKind.R8, "double",
                $"(double){piTrig}{(isFloat ? "_f" : "")}(({fkind})({pa.Expr}))");
            return true;
        }
        // RootN(x, int n) — the BCL's managed algorithm in a runtime helper
        // (n == 2/3 route to sqrt/cbrt, the general case is the sign-copied
        // pow — double-precision even for float, like Single.cs — and n == 0
        // answers NaN: no exception paths). The n operand stays an int32.
        if (name == "RootN" && sig.ParameterTypes.Length == 2)
        {
            var rn = Pop();
            var rx = Pop();
            Push(StackKind.R8, "double",
                $"(double)dn2cpp_math_rootn{(isFloat ? "_f" : "")}(({fkind})({rx.Expr}), {Cast(rn, "int32_t")})");
            return true;
        }
        // Atan2Pi(y, x) = Atan2(y, x) / Pi — the two-operand member of the same
        // *Pi family (atan2 settles the quadrant, the division rescales to
        // half-turns).
        if (name == "Atan2Pi" && sig.ParameterTypes.Length == 2)
        {
            var px = Pop(); var py = Pop();
            Push(StackKind.R8, "double",
                $"(double)(std::atan2(({fkind})({py.Expr}), ({fkind})({px.Expr})) / {FloatPi(isFloat)})");
            return true;
        }
        // Lerp(v1, v2, t) = MultiplyAddEstimate(v1, 1 - t, v2 * t): with
        // MultiplyAddEstimate the fused fma (as real .NET computes on arm64),
        // the whole composition is deterministic. t feeds both factor terms,
        // so it is spilled once.
        if (name == "Lerp" && sig.ParameterTypes.Length == 3)
        {
            var lt = Pop(); var lv2 = Pop(); var lv1 = Pop();
            string tt = NewTemp(fkind);
            Emit($"{tt} = ({fkind})({lt.Expr});");
            Push(StackKind.R8, "double",
                $"(double)std::fma(({fkind})({lv1.Expr}), ({fkind})1 - {tt}, ({fkind})({lv2.Expr}) * {tt})");
            return true;
        }
        // ReciprocalEstimate / ReciprocalSqrtEstimate: .NET documents the result as a
        // platform-dependent approximation of 1/x and 1/sqrt(x), so the exact division
        // is a conforming (maximally precise) estimate. The IEEE special cases
        // (±0 -> ±infinity, ±infinity -> ±0, NaN and negative sqrt inputs -> NaN) fall
        // out of the division/sqrt identically on both sides.
        if (name is "ReciprocalEstimate" or "ReciprocalSqrtEstimate"
            && sig.ParameterTypes.Length == 1)
        {
            var re = Pop();
            string expr = name == "ReciprocalEstimate"
                ? $"(({fkind})1 / ({fkind})({re.Expr}))"
                : $"(({fkind})1 / std::sqrt(({fkind})({re.Expr})))";
            Push(StackKind.R8, "double", $"(double){expr}");
            return true;
        }
        // Round(value, MidpointRounding mode): the mode (enum -> int on the stack)
        // selects the rounding at runtime; an invalid mode raises the BCL's
        // catchable ArgumentException inside the helper.
        if (name == "Round" && sig.ParameterTypes is [_, { } modeParam] && IsMidpointRounding(modeParam))
        {
            var mode = Pop();
            var v = Pop();
            Push(StackKind.R8, "double",
                $"(double)dn2cpp_math_round_mode(({fkind})({v.Expr}), {Cast(mode, "int32_t")})");
            return true;
        }
        // Round(value, int digits) and Round(value, int digits, MidpointRounding):
        // the BCL's scale/round/unscale lives in the runtime helper (digit table,
        // the 1e16/1e8f passthrough limit, and the digits/mode validation); the
        // digitless-mode default is ToEven (0). The _f helper keeps MathF
        // arithmetic in float end to end.
        if (name == "Round"
            && sig.ParameterTypes is [_, { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Int32 }, ..]
            && (sig.ParameterTypes.Length == 2
                || (sig.ParameterTypes.Length == 3 && IsMidpointRounding(sig.ParameterTypes[2]))))
        {
            string modeExpr = "0";
            if (sig.ParameterTypes.Length == 3)
                modeExpr = Cast(Pop(), "int32_t");
            var dg = Pop();
            var rv = Pop();
            Push(StackKind.R8, "double",
                $"(double)dn2cpp_math_round_digits{(isFloat ? "_f" : "")}"
                + $"(({fkind})({rv.Expr}), {Cast(dg, "int32_t")}, {modeExpr})");
            return true;
        }
        // Min/Max take the runtime templates, not std::fmin/fmax: .NET follows
        // IEEE 754:2019 minimum/maximum (a NaN operand propagates; -0.0 orders
        // below +0.0). The (fkind) casts on both operands deduce T=float for
        // MathF so single-precision stays in float.
        string? binary = name switch
        {
            "Pow" => "std::pow", "Atan2" => "std::atan2",
            "Min" => "dn2cpp_math_min", "Max" => "dn2cpp_math_max",
            // CopySign and IEEERemainder are IEEE-exact, so <cmath> matches the
            // BCL bit-for-bit (std::remainder is the same ties-to-even exact
            // remainder .NET's managed IEEERemainder computes, including the
            // sign-of-zero rule); Ieee754Remainder is Math.IEEERemainder under
            // its generic-math name. MaxMagnitude/MinMagnitude take the runtime
            // templates that port the BCL's NaN and equal-magnitude handling.
            "CopySign" => "std::copysign",
            "IEEERemainder" or "Ieee754Remainder" => "std::remainder",
            // Hypot is NOT std::hypot: Double.Hypot is the BCL's managed AOCL
            // head/tail algorithm and float.Hypot the exact double sum of
            // squares under one sqrt — both ported to runtime helpers that
            // match real .NET bit-for-bit (std::hypot drifts in the last ulp).
            "Hypot" => isFloat ? "dn2cpp_math_hypot_f" : "dn2cpp_math_hypot",
            "MaxMagnitude" => "dn2cpp_math_maxmag", "MinMagnitude" => "dn2cpp_math_minmag",
            // The *Number variants DROP a NaN operand (the other operand wins;
            // NaN only when both are) — the runtime templates port the BCL's
            // exact comparison chains. The *Native variants are the plain
            // comparison ternary: .NET leaves their NaN and ±0 behavior
            // platform-defined (arm64 hardware answers fmax/fmin), so only the
            // unambiguous finite results are contractual.
            "MaxNumber" => "dn2cpp_math_maxnum", "MinNumber" => "dn2cpp_math_minnum",
            "MaxMagnitudeNumber" => "dn2cpp_math_maxmagnum",
            "MinMagnitudeNumber" => "dn2cpp_math_minmagnum",
            "MaxNative" => "dn2cpp_math_maxnative", "MinNative" => "dn2cpp_math_minnative",
            _ => null,
        };
        if (binary is not null && sig.ParameterTypes.Length == 2)
        {
            var b = Pop(); var a = Pop();
            Push(StackKind.R8, "double", $"(double){binary}(({fkind})({a.Expr}), ({fkind})({b.Expr}))");
            return true;
        }
        // FusedMultiplyAdd(x, y, z) = x*y + z with a single rounding — std::fma
        // is the same IEEE operation. MultiplyAddEstimate is documented as a
        // platform-dependent estimate between fused and mul+add; real .NET on
        // arm64 always fuses, and std::fma reproduces that exactly. All three
        // operands stay in the overload's precision so single-precision never
        // rounds through double.
        if (name is "FusedMultiplyAdd" or "MultiplyAddEstimate" && sig.ParameterTypes.Length == 3)
        {
            var fz = Pop(); var fy = Pop(); var fx = Pop();
            Push(StackKind.R8, "double",
                $"(double)std::fma(({fkind})({fx.Expr}), ({fkind})({fy.Expr}), ({fkind})({fz.Expr}))");
            return true;
        }
        // ScaleB(x, int n) = x * 2^n, exactly — std::scalbn is the same musl
        // algorithm the BCL ports. The exponent stays an int32, not a (fkind).
        if (name == "ScaleB" && sig.ParameterTypes.Length == 2)
        {
            var sn = Pop(); var sx = Pop();
            Push(StackKind.R8, "double",
                $"(double)std::scalbn(({fkind})({sx.Expr}), {Cast(sn, "int32_t")})");
            return true;
        }
        // BitIncrement/BitDecrement — the adjacent representable value toward
        // ±infinity. std::nextafter matches the BCL exactly, including the
        // special cases (-0 increments to +Epsilon, NaN propagates, the
        // infinity moved away from steps to Min/MaxValue).
        if (name is "BitIncrement" or "BitDecrement" && sig.ParameterTypes.Length == 1)
        {
            var bv = Pop();
            string toward = name == "BitIncrement"
                ? $"std::numeric_limits<{fkind}>::infinity()"
                : $"-std::numeric_limits<{fkind}>::infinity()";
            Push(StackKind.R8, "double",
                $"(double)std::nextafter(({fkind})({bv.Expr}), {toward})");
            return true;
        }
        // Log(value, newBase) = log(value)/log(newBase), like the BCL.
        if (name == "Log" && sig.ParameterTypes.Length == 2)
        {
            var nb = Pop(); var v = Pop();
            Push(StackKind.R8, "double",
                $"(double)(std::log(({fkind})({v.Expr})) / std::log(({fkind})({nb.Expr})))");
            return true;
        }
        // Floating-point Clamp(value, min, max) — the same runtime template as
        // the integer overloads: min > max raises ArgumentException, each operand
        // is evaluated once, and a NaN value passes through unchanged like .NET.
        // ClampNative validates min > max identically but composes the Native
        // ternaries (MinNative(MaxNative(v, min), max)), so its NaN/±0 results
        // are the platform-defined ones .NET documents.
        if (name is "Clamp" or "ClampNative" && sig.ParameterTypes.Length == 3)
        {
            string clampFn = name == "Clamp" ? "dn2cpp_math_clamp" : "dn2cpp_math_clampnative";
            var hi = Pop(); var lo = Pop(); var v = Pop();
            Push(StackKind.R8, "double",
                $"(double){clampFn}(({fkind})({v.Expr}), ({fkind})({lo.Expr}), ({fkind})({hi.Expr}))");
            return true;
        }
        return false;
    }

    /// <summary>Pops the receiver of a primitive instance call and yields its
    /// value: a managed pointer (from ldloca) is dereferenced; a value is used
    /// directly.</summary>
    private string DerefReceiver(string cppType)
    {
        var r = Pop();
        return r.Kind == StackKind.Ptr ? $"(*({cppType}*)({r.Expr}))" : $"({cppType})({r.Expr})";
    }

    /// <summary>The real C++ storage width of a sub-word declaring type, used to
    /// deref a managed-pointer receiver at exactly its element width (a byte*/short*/
    /// char16_t* into packed storage — an array element, a span ref, or
    /// a value type's narrowed sub-word field — must read one element, not an int32
    /// that splices in adjacent bytes). A value receiver keeps the int32 slot
    /// (DerefReceiver casts it), so this only affects the Ptr-receiver case.</summary>
    private static string SubWordCppType(string declType) => declType switch
    {
        "System.Byte" => "uint8_t",
        "System.SByte" => "int8_t",
        "System.Int16" => "int16_t",
        "System.UInt16" => "uint16_t",
        "System.Char" => "char16_t",
        "System.Boolean" => "uint8_t",
        _ => "int32_t",
    };

    /// <summary>Finds the closed <c>IComparer&lt;T&gt;</c> interface a comparer class
    /// implements (transitively through bases / interface inheritance), or null. Used by
    /// span.Sort(TComparer) — the comparer arrives as its concrete class, so dispatch goes
    /// through the IComparer interface it carries.</summary>
    private ClassInfo? FindComparerInterface(ClassInfo c)
    {
        for (var b = c; b is not null; b = b.BaseClass)
            foreach (var i in b.Interfaces)
            {
                if (Comp.GenericDefFullName(i) == "System.Collections.Generic.IComparer")
                    return i;
                if (FindComparerInterface(i) is { } nested)
                    return nested;
            }
        return null;
    }

    /// <summary>Spills a popped span operand to a value temp (named <c>spanCt</c>): a
    /// by-value span is cast; a span arriving by address (ldloca) is dereferenced. The
    /// MemoryExtensions scan loops then read its <c>.f__reference</c>/<c>.f__length</c>
    ///.</summary>
    private string SpanValue(StackEntry e, string spanCt)
    {
        string t = NewTemp(spanCt);
        Emit($"{t} = {(e.Kind == StackKind.Ptr ? $"*({spanCt}*)({e.Expr})" : Cast(e, spanCt))};");
        return t;
    }

    /// <summary>The <c>dn2cpp_search_values_*</c> element suffix for a SearchValues
    /// element type — <c>"u8"</c> for byte-width, <c>"u16"</c> for char-width — or null
    /// for any other element (only byte/char SearchValues are modeled).</summary>
    private static string? SearchValuesSuffix(TypeDesc? elem) => elem switch
    {
        { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Byte or PrimitiveTypeCode.SByte } => "u8",
        { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Char or PrimitiveTypeCode.UInt16
            or PrimitiveTypeCode.Int16 } => "u16",
        _ => null,
    };

    /// <summary>Decode the closed signature (return + parameter types, method type
    /// args substituted) of a MethodSpec call. Used by the span-shaping MemoryMarshal
    /// intrinsics to recover the result Span/ReadOnlySpan type and the input
    /// span's struct type.</summary>
    private MethodSignature<TypeDesc> MethodSpecSig(MethodSpecificationHandle msh, TypeDesc[] methodArgs)
    {
        var ctx = new GenericContext(System.Array.Empty<TypeDesc>(), methodArgs);
        var ms = Reader.GetMethodSpecification(msh);
        return ms.Method.Kind == HandleKind.MethodDefinition
            ? Reader.GetMethodDefinition((MethodDefinitionHandle)ms.Method).DecodeSignature(Comp.SigProvider, ctx)
            : Reader.GetMemberReference((MemberReferenceHandle)ms.Method).DecodeMethodSignature(Comp.SigProvider, ctx);
    }

    /// <summary>Formats one interpolated-string hole value <paramref name="val"/> of
    /// static type <paramref name="t"/> into a <c>Dn2CppString*</c> C++ expression,
    /// honoring the <c>:fmt</c> specifier carried by <paramref name="formatExpr"/>
    /// (a <c>Dn2CppString*</c> or <c>nullptr</c>). Shared by the
    /// DefaultInterpolatedStringHandler and StringBuilder.AppendInterpolatedStringHandler
    /// generic <c>AppendFormatted&lt;T&gt;</c> paths so enum / primitive / value-type /
    /// reference holes format identically regardless of which handler the C# compiler
    /// chose. Emits temps for the enum lookup as needed.</summary>
    private string FormatInterpolationHole(TypeDesc t, StackEntry val, string formatExpr)
    {
        if (t.IsString)
            return Cast(val, "Dn2CppString*");
        if (t.Kind == TypeKind.Primitive && t.Primitive == PrimitiveTypeCode.Boolean)
            return $"dn2cpp_bool_to_string({val.Expr})";
        if (t.Kind == TypeKind.Primitive && t.Primitive == PrimitiveTypeCode.Char)
            return $"dn2cpp_char_to_string((char16_t)({val.Expr}))";
        // A float hole formats at FLOAT precision (real .NET's AppendFormatted<float>
        // routes to float.TryFormat — shortest round-trip text for the float value,
        // "3.4028235E+38", not the widened double's "3.4028234663852886E+38").
        if (t.Kind == TypeKind.Primitive && t.Primitive == PrimitiveTypeCode.Single)
            return $"dn2cpp_format_r4((float)({val.Expr}), {formatExpr})";
        if (t.Kind == TypeKind.Primitive && t.Primitive == PrimitiveTypeCode.Double)
            return $"dn2cpp_format_r8((double)({val.Expr}), {formatExpr})";
        if (t.Kind == TypeKind.Primitive && IntegralHoleInfo(t.Primitive) is (int byteWidth, bool isSigned))
        {
            // Format by the hole's declared CLR type so signedness and hex
            // width match .NET (a `uint` is not a negative `int`; a `short`
            // -1 is "FFFF", not "FFFFFFFF"). A native-int hole (byteWidth
            // 0) truncates/extends through intptr_t/uintptr_t and passes
            // sizeof(intptr_t), so the same output is right on 32-bit targets.
            string w = ByteWidthExpr(byteWidth);
            if (isSigned)
            {
                string sc = byteWidth == 0 ? "(intptr_t)" : "";
                return $"dn2cpp_format_int((int64_t){sc}({val.Expr}), {w}, {formatExpr})";
            }
            string uc = byteWidth switch { 0 => "uintptr_t", 1 => "uint8_t", 2 => "uint16_t", 4 => "uint32_t", _ => "uint64_t" };
            return $"dn2cpp_format_uint((uint64_t)({uc})({val.Expr}), {w}, {formatExpr})";
        }
        if (t.Kind == TypeKind.Class && t.Class!.IsEnum)
        {
            // Enum hole: print the member name (default/"G"/"F"), or the
            // underlying value for a numeric specifier ("D"/"X"). A [Flags]
            // enum decomposes a combined value into "A, B"; a plain enum maps
            // a single value through a switch. Either way an undefined value
            // falls back to the decimal. The value is hoisted so the lookup and
            // the fallback share one evaluation, and it is read at the enum's
            // MODEL width — a long/ulong-underlying enum's members do not
            // survive int32.
            bool wide = IsWideEnum(t.Class);
            string cty = wide ? "int64_t" : "int32_t";
            string vtmp = NewTemp(cty);
            string ntmp = NewTemp("Dn2CppString*");
            Emit($"{vtmp} = ({cty})({val.Expr});");
            Emit($"{ntmp} = nullptr;");
            var members = EnumMembersModel(t.Class);
            if (members.Count > 0 && IsFlagsEnum(t.Class))
            {
                // Sort by descending unsigned value for greedy flag matching.
                var ordered = new List<(long value, string name)>(members);
                ordered.Sort((a, b) => EnumFlagKey(b.value, wide).CompareTo(EnumFlagKey(a.value, wide)));
                string varr = NewLocalArray(cty, ordered.Count);
                string narr = NewLocalArray("Dn2CppString*", ordered.Count);
                for (int i = 0; i < ordered.Count; i++)
                {
                    Emit($"{varr}[{i}] = {EnumMemberLit(ordered[i].value, wide)};");
                    Emit($"{narr}[{i}] = {Literals.GetOrAdd(ordered[i].name)};");
                }
                string flagsFn = wide ? "dn2cpp_enum_flags_to_string64" : "dn2cpp_enum_flags_to_string";
                Emit($"if (!dn2cpp_fmt_numeric({formatExpr})) {ntmp} = {flagsFn}({vtmp}, {varr}, {narr}, {ordered.Count});");
            }
            else if (members.Count > 0)
            {
                Emit($"if (!dn2cpp_fmt_numeric({formatExpr})) switch ({vtmp}) {{");
                foreach (var (value, memberName) in members)
                    Emit($"    case {EnumMemberLit(value, wide)}: {ntmp} = {Literals.GetOrAdd(memberName)}; break;");
                Emit("}");
            }
            var (ew, es) = EnumUnderlyingInfo(t.Class);
            Emit($"if ({ntmp} == nullptr) {ntmp} = dn2cpp_format_enum((int64_t){vtmp}, {ByteWidthExpr(ew)}, {(es ? 1 : 0)}, {formatExpr});");
            return ntmp;
        }
        if (CppTypes.KindOf(t) == StackKind.Ref)
        {
            // Reference-type hole: format via Object.ToString, which dispatches
            // an overridden ToString (or the type name otherwise).
            return $"dn2cpp_object_tostring({Cast(val, "Dn2CppObject*")})";
        }
        if (IsDecimal(t))
        {
            // Intrinsic decimal hole — Decimal.ToString's real body is cut, so
            // route to the runtime formatter (the same lowering the ToString
            // intrinsic picks), honoring a `:fmt` specifier when present.
            string dv = Cast(val, "Dn2CppDecimal");
            return formatExpr == "nullptr"
                ? $"dn2cpp_decimal_to_string({dv})"
                : $"dn2cpp_decimal_format({dv}, {formatExpr})";
        }
        if (IsTimeSpan(t))
        {
            // Intrinsic value-type hole — the `:fmt` specifier IS honored:
            // route to the runtime invariant formatter, not the untranspilable BCL
            // ToString. AppendFormatted<T> passes the struct by value.
            return $"dn2cpp_timespan_format({Cast(val, "Dn2CppTimeSpan")}, {formatExpr})";
        }
        if (IsDateTime(t))
            return $"dn2cpp_datetime_format({Cast(val, "Dn2CppDateTime")}, {formatExpr})";
        // The rest of the date family, which must precede the value-type arm below:
        // that arm would direct-call their real ToString(string) body, which
        // reachability cuts for an intrinsic type, so the named-symbol backstop fails
        // the transpile of a plain $"{dto:O}".
        if (IsDateTimeOffset(t))
            return $"dn2cpp_datetimeoffset_format({Cast(val, "Dn2CppDateTimeOffset")}, {formatExpr})";
        if (IsDateOnly(t))
            return $"dn2cpp_dateonly_format({Cast(val, "Dn2CppDateOnly")}, {formatExpr})";
        if (IsTimeOnly(t))
            return $"dn2cpp_timeonly_format({Cast(val, "Dn2CppTimeOnly")}, {formatExpr})";
        if (IsDefaultNameExternalIntrinsic(t))
        {
            string ti = TypeInfoExpr(t)
                ?? throw new NotSupportedException(
                    $"{Method.DeclaringClass.FullName}.{Method.Name}: interpolation of {t} has no runtime type-info");
            return $"((void)({val.Expr}), dn2cpp_type_tostring({ti}))";
        }
        if (t is { Kind: TypeKind.Class,
                Class: { IntrinsicCppName: not null } intrinsic })
        {
            // ValueType/Object.ToString on an intrinsic with no declaration depends only
            // on exact CLR identity. This also covers intrinsic reference types represented
            // by-value in C++ (memory-map handles), which cannot use object-header dispatch.
            // A declared intrinsic override stays unhandled here so an unmodeled custom
            // formatter remains a loud transpile failure.
            if (!_c.DeclaresIntrinsicToStringOverride(intrinsic))
            {
                string ti = TypeInfoExpr(t)
                    ?? throw new NotSupportedException(
                        $"{Method.DeclaringClass.FullName}.{Method.Name}: interpolation of {t} has no emitted type-info");
                return $"((void)({val.Expr}), dn2cpp_type_tostring({ti}))";
            }
        }
        if (t is { Kind: TypeKind.Class, Class: { IsValueType: true } vh })
        {
            // Value-type hole (e.g. $"{tuple}") — AppendFormatted<T> passes the
            // struct by value (no box). Spill it and call its ToString override
            // on the address (the scan reached the impl). A `:fmt` specifier is
            // threaded into the struct's ToString(string) overload when it has
            // one (the IFormattable route the real handler takes — Guid);
            // otherwise it is ignored (matches the BCL default for a struct
            // without IFormattable).
            string ct = CppTypes.Of(t);
            if (formatExpr != "nullptr" && Compilation.EffectiveToStringFormat(vh) is { } vhTsF)
            {
                string tmpF = NewTemp(ct);
                Emit($"{tmpF} = {Cast(val, ct)};");
                return $"{DirectCallSym(vhTsF)}({ArgsWithRgctx($"&{tmpF}, {formatExpr}", vhTsF)})";
            }
            if (Compilation.EffectiveToString(vh) is { } vhTs)
            {
                string tmp = NewTemp(ct);
                Emit($"{tmp} = {Cast(val, ct)};");
                return $"{DirectCallSym(vhTs)}({ArgsWithRgctx($"&{tmp}", vhTs)})";
            }
        }
        throw new NotSupportedException(
            $"{Method.DeclaringClass.FullName}.{Method.Name}: interpolated string " +
            $"hole of type '{t}' is not supported yet");
    }
}
