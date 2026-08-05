using System.Reflection.Metadata;
using System.Text;
using SRME = System.Reflection.Metadata.Ecma335.MetadataTokens;

namespace Dn2Cpp;

internal sealed partial class MethodCompiler
{
    private static bool IsDecimal(TypeDesc t) =>
        t is { Kind: TypeKind.Class, Class.FullName: "System.Decimal" };

    private static bool IsGCHandle(TypeDesc t) =>
        t is { Kind: TypeKind.Class, Class.FullName: "System.Runtime.InteropServices.GCHandle" };

    /// <summary>A decimal value/pointer operand as a Dn2CppDecimal rvalue (deref a
    /// managed pointer; pass a struct value through).</summary>
    private string DecVal(StackEntry e) =>
        e.Kind == StackKind.Ptr ? $"(*(Dn2CppDecimal*)({e.Expr}))" : e.Expr;

    /// <summary>Builds the C++ expression for a decimal value from a single numeric
    /// scalar argument (the widening ctors and to-decimal conversions).</summary>
    private string DecimalFromScalar(StackEntry a, TypeDesc t)
    {
        if (IsDecimal(t)) return DecVal(a);
        if (t.Kind != TypeKind.Primitive)
            throw new NotSupportedException($"{Method.DeclaringClass.FullName}.{Method.Name}: decimal from {t} is not supported");
        return t.Primitive switch
        {
            PrimitiveTypeCode.Int32 or PrimitiveTypeCode.Int16 or PrimitiveTypeCode.SByte
                => $"dn2cpp_decimal_from_i4((int32_t)({a.Expr}))",
            PrimitiveTypeCode.UInt32 or PrimitiveTypeCode.UInt16 or PrimitiveTypeCode.Byte or PrimitiveTypeCode.Char
                => $"dn2cpp_decimal_from_u4((uint32_t)({a.Expr}))",
            PrimitiveTypeCode.Int64 => $"dn2cpp_decimal_from_i8((int64_t)({a.Expr}))",
            PrimitiveTypeCode.UInt64 => $"dn2cpp_decimal_from_u8((uint64_t)({a.Expr}))",
            PrimitiveTypeCode.Single => $"dn2cpp_decimal_from_float((float)({a.Expr}))",
            PrimitiveTypeCode.Double => $"dn2cpp_decimal_from_double((double)({a.Expr}))",
            _ => throw new NotSupportedException(
                $"{Method.DeclaringClass.FullName}.{Method.Name}: decimal from {t.Primitive} is not supported"),
        };
    }

    /// <summary>Pops a Decimal ctor's args (pushed left-to-right) and returns the
    /// C++ expression constructing the Dn2CppDecimal — the (lo,mid,hi,isNeg,scale)
    /// bit-pattern ctor and the numeric-widening single-arg ctors.</summary>
    private string EmitDecimalCtorExpr(MethodSignature<TypeDesc> sig)
    {
        var ps = sig.ParameterTypes;
        var args = new StackEntry[ps.Length];
        for (int i = ps.Length - 1; i >= 0; i--)
            args[i] = Pop();
        if (ps.Length == 5)
            return $"dn2cpp_decimal_from_parts((int32_t)({args[0].Expr}), (int32_t)({args[1].Expr}), " +
                   $"(int32_t)({args[2].Expr}), (int32_t)({args[3].Expr}), (int32_t)({args[4].Expr}))";
        if (ps.Length == 1)
            return DecimalFromScalar(args[0], ps[0]);
        throw new NotSupportedException(
            $"{Method.DeclaringClass.FullName}.{Method.Name}: decimal ctor with {ps.Length} args is not supported");
    }

    /// <summary>Builds the C++ expression converting a decimal rvalue *out* of decimal
    /// to the target primitive type (with the narrowing cast for the sub-32-bit
    /// integers), typed at the target's natural width — not stack-widened.</summary>
    private string DecimalToScalarExpr(string v, TypeDesc t)
    {
        return t.Primitive switch
        {
            PrimitiveTypeCode.Double => $"dn2cpp_decimal_to_double({v})",
            PrimitiveTypeCode.Single => $"dn2cpp_decimal_to_float({v})",
            PrimitiveTypeCode.Int64 => $"dn2cpp_decimal_to_i8({v})",
            PrimitiveTypeCode.UInt64 => $"dn2cpp_decimal_to_u8({v})",
            PrimitiveTypeCode.Int32 or PrimitiveTypeCode.Int16 or PrimitiveTypeCode.SByte
                => $"(int32_t)dn2cpp_decimal_to_i8({v})",
            PrimitiveTypeCode.UInt32 or PrimitiveTypeCode.UInt16 or PrimitiveTypeCode.Byte or PrimitiveTypeCode.Char
                => $"(uint32_t)dn2cpp_decimal_to_u8({v})",
            _ => throw new NotSupportedException(
                $"{Method.DeclaringClass.FullName}.{Method.Name}: decimal conversion to {t.Primitive} is not supported"),
        };
    }

    /// <summary>Pushes a decimal-returning result of a numeric conversion *out* of
    /// decimal (op_Explicit / Convert) per the target primitive type.</summary>
    private void PushDecimalToScalar(string v, TypeDesc ret)
    {
        switch (ret.Primitive)
        {
            case PrimitiveTypeCode.Double: Push(StackKind.R8, "double", DecimalToScalarExpr(v, ret)); break;
            case PrimitiveTypeCode.Single: Push(StackKind.R8, "double", $"(double){DecimalToScalarExpr(v, ret)}"); break;
            case PrimitiveTypeCode.Int64: Push(StackKind.I8, "int64_t", DecimalToScalarExpr(v, ret)); break;
            case PrimitiveTypeCode.UInt64: Push(StackKind.I8, "int64_t", $"(int64_t){DecimalToScalarExpr(v, ret)}"); break;
            case PrimitiveTypeCode.Int32 or PrimitiveTypeCode.Int16 or PrimitiveTypeCode.SByte:
                Push(StackKind.I4, "int32_t", DecimalToScalarExpr(v, ret)); break;
            case PrimitiveTypeCode.UInt32 or PrimitiveTypeCode.UInt16 or PrimitiveTypeCode.Byte or PrimitiveTypeCode.Char:
                Push(StackKind.I4, "int32_t", $"(int32_t){DecimalToScalarExpr(v, ret)}"); break;
            default:
                throw new NotSupportedException(
                    $"{Method.DeclaringClass.FullName}.{Method.Name}: decimal conversion to {ret.Primitive} is not supported");
        }
    }

    /// <summary>Lowers a generic-math TryConvertFrom/To{Checked,Truncating,Saturating} with
    /// the intrinsic decimal on at least one side and a primitive (or decimal) on the other
    /// — the INumberBase&lt;T&gt; static-abstract conversions. Deliberate divergence: the
    /// checked/saturating range semantics collapse to a truncating approximation (the same
    /// overflow carve-out as the primitive generic-math lowering) and the boolean result is
    /// always success. Returns false for any other member or parameter shape.</summary>
    private bool TryEmitDecimalTryConvert(string name, MethodSignature<TypeDesc> sig)
    {
        if (name is not ("TryConvertFromChecked" or "TryConvertFromTruncating" or "TryConvertFromSaturating"
                or "TryConvertToChecked" or "TryConvertToTruncating" or "TryConvertToSaturating")
            || sig.ParameterTypes is not [{ } src, { Kind: TypeKind.ByRef, Element: { } dst }]
            || !(IsDecimal(src) || IsDecimal(dst))
            || !(IsDecimal(src) || src.Kind == TypeKind.Primitive)
            || !(IsDecimal(dst) || dst.Kind == TypeKind.Primitive))
            return false;
        var outPtr = Pop(); // out result (managed pointer, top of stack)
        var val = Pop();    // value
        if (IsDecimal(dst))
            Emit($"*({Cast(outPtr, "Dn2CppDecimal*")}) = {DecimalFromScalar(val, src)};");
        else
        {
            // Storage width plus the promoted-slot fixup: the byref is the caller's, so it
            // is as likely a packed element or a field as a slot (MethodCompiler.Call.cs's
            // all-primitive arm states the convention).
            string dstSt = CppTypes.StorageOf(dst);
            Emit($"*({Cast(outPtr, dstSt + "*")}) = ({dstSt}){DecimalToScalarExpr(DecVal(val), dst)};");
            NoteByRefSlotFixup(outPtr, sig.ParameterTypes[1]);
            EmitByRefSlotFixups();
        }
        Push(StackKind.I4, "int32_t", "1");
        return true;
    }

    /// <summary>Lowers a System.Decimal member (ctor/operator/conversion/ToString/
    /// static) to its Dn2CppDecimal runtime helper. Returns false if unmapped so the
    /// caller reports a clear unsupported-member error.</summary>
    private bool TryEmitDecimalIntrinsic(string name, MethodSignature<TypeDesc> sig)
    {
        var ps = sig.ParameterTypes;

        //.ctor — the ldloca+call (address-init) form. The newobj expression form is
        // handled in TranslateNewobj. Args are above the `this` pointer on the stack.
        if (name == ".ctor")
        {
            string expr = EmitDecimalCtorExpr(sig);
            var addr = Pop();
            Emit($"*({Cast(addr, "Dn2CppDecimal*")}) = {expr};");
            return true;
        }

        // Binary arithmetic operators / static equivalents (decimal, decimal) -> decimal.
        string? binOp = (name, ps.Length) switch
        {
            ("op_Addition", 2) or ("Add", 2) => "dn2cpp_decimal_add",
            ("op_Subtraction", 2) or ("Subtract", 2) => "dn2cpp_decimal_sub",
            ("op_Multiply", 2) or ("Multiply", 2) => "dn2cpp_decimal_mul",
            ("op_Division", 2) or ("Divide", 2) => "dn2cpp_decimal_div",
            ("op_Modulus", 2) or ("Remainder", 2) => "dn2cpp_decimal_rem",
            _ => null,
        };
        if (binOp is not null && IsDecimal(ps[0]) && IsDecimal(ps[1]))
        {
            var b = Pop(); var a = Pop();
            Push(StackKind.Struct, "Dn2CppDecimal", $"{binOp}({DecVal(a)}, {DecVal(b)})");
            return true;
        }

        // Unary operators.
        switch (name)
        {
            case "op_UnaryNegation" or "Negate" when ps.Length == 1:
            { var a = Pop(); Push(StackKind.Struct, "Dn2CppDecimal", $"dn2cpp_decimal_neg({DecVal(a)})"); return true; }
            case "op_UnaryPlus" when ps.Length == 1:
            { var a = Pop(); Push(StackKind.Struct, "Dn2CppDecimal", DecVal(a)); return true; }
            case "op_Increment" when ps.Length == 1:
            { var a = Pop(); Push(StackKind.Struct, "Dn2CppDecimal", $"dn2cpp_decimal_add({DecVal(a)}, dn2cpp_decimal_from_i4(1))"); return true; }
            case "op_Decrement" when ps.Length == 1:
            { var a = Pop(); Push(StackKind.Struct, "Dn2CppDecimal", $"dn2cpp_decimal_sub({DecVal(a)}, dn2cpp_decimal_from_i4(1))"); return true; }
        }

        // Comparison operators (decimal, decimal) -> bool.
        string? cmpOp = name switch
        {
            "op_Equality" => "== 0", "op_Inequality" => "!= 0",
            "op_LessThan" => "< 0", "op_GreaterThan" => "> 0",
            "op_LessThanOrEqual" => "<= 0", "op_GreaterThanOrEqual" => ">= 0",
            _ => null,
        };
        if (cmpOp is not null && ps.Length == 2 && IsDecimal(ps[0]) && IsDecimal(ps[1]))
        {
            var b = Pop(); var a = Pop();
            Push(StackKind.I4, "int32_t", $"(dn2cpp_decimal_cmp({DecVal(a)}, {DecVal(b)}) {cmpOp} ? 1 : 0)");
            return true;
        }

        // Conversions: op_Implicit/op_Explicit either into or out of decimal.
        if ((name == "op_Implicit" || name == "op_Explicit") && ps.Length == 1)
        {
            if (IsDecimal(sig.ReturnType))   // numeric -> decimal
            {
                var a = Pop();
                Push(StackKind.Struct, "Dn2CppDecimal", DecimalFromScalar(a, ps[0]));
                return true;
            }
            if (IsDecimal(ps[0]))            // decimal -> numeric
            {
                var a = Pop();
                PushDecimalToScalar(DecVal(a), sig.ReturnType);
                return true;
            }
        }

        // Static rounding / conversion helpers (decimal -> decimal).
        switch (name)
        {
            case "Truncate" when ps.Length == 1:
            { var a = Pop(); Push(StackKind.Struct, "Dn2CppDecimal", $"dn2cpp_decimal_truncate({DecVal(a)})"); return true; }
            case "Floor" when ps.Length == 1:
            { var a = Pop(); Push(StackKind.Struct, "Dn2CppDecimal", $"dn2cpp_decimal_floor({DecVal(a)})"); return true; }
            case "Ceiling" when ps.Length == 1:
            { var a = Pop(); Push(StackKind.Struct, "Dn2CppDecimal", $"dn2cpp_decimal_ceiling({DecVal(a)})"); return true; }
            case "Abs" when ps.Length == 1:
            { var a = Pop(); Push(StackKind.Struct, "Dn2CppDecimal", $"dn2cpp_decimal_abs({DecVal(a)})"); return true; }
            case "Min" when ps.Length == 2:
            { var b = Pop(); var a = Pop(); Push(StackKind.Struct, "Dn2CppDecimal", $"(dn2cpp_decimal_cmp({DecVal(a)}, {DecVal(b)}) <= 0 ? {DecVal(a)} : {DecVal(b)})"); return true; }
            case "Max" when ps.Length == 2:
            { var b = Pop(); var a = Pop(); Push(StackKind.Struct, "Dn2CppDecimal", $"(dn2cpp_decimal_cmp({DecVal(a)}, {DecVal(b)}) >= 0 ? {DecVal(a)} : {DecVal(b)})"); return true; }
            // The INumberBase<decimal> magnitude selectors (the *Number twins are identical —
            // decimal has no NaN): |x| vs |y| with the BCL tie-break (Max prefers the
            // non-negative operand, Min the negative one; equal-sign ties keep x for Max,
            // y for Min).
            case "MaxMagnitude" or "MaxMagnitudeNumber" when ps.Length == 2:
            { var b = Pop(); var a = Pop(); Push(StackKind.Struct, "Dn2CppDecimal", $"dn2cpp_decimal_max_magnitude({DecVal(a)}, {DecVal(b)})"); return true; }
            case "MinMagnitude" or "MinMagnitudeNumber" when ps.Length == 2:
            { var b = Pop(); var a = Pop(); Push(StackKind.Struct, "Dn2CppDecimal", $"dn2cpp_decimal_min_magnitude({DecVal(a)}, {DecVal(b)})"); return true; }
            case "Round":
            {
                // Round(d) / Round(d, int digits) / Round(d, MidpointRounding) /
                // Round(d, int, MidpointRounding). The mode (enum -> int on the
                // stack) selects the rounding at runtime; an invalid mode raises
                // the BCL's catchable ArgumentException inside the helper.
                string mode = "0", digits = "0";
                if (ps.Length == 3) { var m = Pop(); mode = Cast(m, "int32_t"); var dg = Pop(); digits = $"(int32_t)({dg.Expr})"; }
                else if (ps.Length == 2)
                {
                    var p1 = Pop();
                    if (IsMidpointRounding(ps[1])) mode = Cast(p1, "int32_t");
                    else digits = $"(int32_t)({p1.Expr})";
                }
                var a = Pop();
                Push(StackKind.Struct, "Dn2CppDecimal", $"dn2cpp_decimal_round({DecVal(a)}, {digits}, {mode})");
                return true;
            }
            case "Clamp" when ps.Length == 3:
            {
                // Math.Clamp(decimal value, decimal min, decimal max): min > max
                // raises the BCL's catchable ArgumentException; otherwise the
                // comparison chain over dn2cpp_decimal_cmp. Every operand is
                // spilled once — each appears more than once across the checks.
                var hi = Pop(); var lo = Pop(); var v = Pop();
                string vt = NewTemp("Dn2CppDecimal"), lot = NewTemp("Dn2CppDecimal"), hit = NewTemp("Dn2CppDecimal");
                Emit($"{vt} = {DecVal(v)};");
                Emit($"{lot} = {DecVal(lo)};");
                Emit($"{hit} = {DecVal(hi)};");
                Emit($"if (dn2cpp_decimal_cmp({hit}, {lot}) < 0) dn2cpp_throw_argument();");
                Push(StackKind.Struct, "Dn2CppDecimal",
                    $"(dn2cpp_decimal_cmp({vt}, {lot}) < 0 ? {lot} : (dn2cpp_decimal_cmp({vt}, {hit}) > 0 ? {hit} : {vt}))");
                return true;
            }
        }

        // Comparison / equality helpers returning int / bool.
        switch (name)
        {
            case "Compare" when ps.Length == 2:
            { var b = Pop(); var a = Pop(); Push(StackKind.I4, "int32_t", $"dn2cpp_decimal_cmp({DecVal(a)}, {DecVal(b)})"); return true; }
            case "CompareTo" when ps.Length == 1 && IsDecimal(ps[0]):
            { var b = Pop(); var a = Pop(); Push(StackKind.I4, "int32_t", $"dn2cpp_decimal_cmp({DecVal(a)}, {DecVal(b)})"); return true; }
            case "Equals" when ps.Length == 1 && IsDecimal(ps[0]):
            { var b = Pop(); var a = Pop(); Push(StackKind.I4, "int32_t", $"(dn2cpp_decimal_cmp({DecVal(a)}, {DecVal(b)}) == 0 ? 1 : 0)"); return true; }
            case "GetHashCode" when ps.Length == 0:
            { var a = Pop(); Push(StackKind.I4, "int32_t", $"(int32_t)(dn2cpp_decimal_to_i8({DecVal(a)}) ^ {DecVal(a)}.scale)"); return true; }
        }

        // The INumberBase<decimal> category predicates (the plainly-named public statics the
        // constrained resolver finds, and the explicit impls the generic-math table delegates
        // here). A decimal is always finite and real, never NaN/infinite/complex/subnormal;
        // IsZero tests the mantissa (a negatively signed zero IS zero), the sign predicates
        // read the sign flag (that same -0m IS negative-signed, where a compare would say
        // otherwise), and IsInteger compares against the truncation like the BCL body.
        if (ps.Length == 1 && IsDecimal(ps[0]))
        {
            switch (name)
            {
                case "IsFinite" or "IsRealNumber":
                    Pop();
                    Push(StackKind.I4, "int32_t", "1");
                    return true;
                case "IsNaN" or "IsInfinity" or "IsPositiveInfinity" or "IsNegativeInfinity"
                    or "IsComplexNumber" or "IsImaginaryNumber" or "IsSubnormal":
                    Pop();
                    Push(StackKind.I4, "int32_t", "0");
                    return true;
                case "IsZero":
                {
                    var a = Pop();
                    Push(StackKind.I4, "int32_t",
                        $"(dn2cpp_decimal_cmp({DecVal(a)}, dn2cpp_decimal_from_i4(0)) == 0 ? 1 : 0)");
                    return true;
                }
                case "IsNegative":
                {
                    var a = Pop();
                    Push(StackKind.I4, "int32_t", $"({DecVal(a)}.sign != 0 ? 1 : 0)");
                    return true;
                }
                case "IsPositive":
                {
                    var a = Pop();
                    Push(StackKind.I4, "int32_t", $"({DecVal(a)}.sign == 0 ? 1 : 0)");
                    return true;
                }
                case "IsInteger":
                {
                    var a = Pop();
                    string t = NewTemp("Dn2CppDecimal");
                    Emit($"{t} = {DecVal(a)};");
                    Push(StackKind.I4, "int32_t",
                        $"(dn2cpp_decimal_cmp({t}, dn2cpp_decimal_truncate({t})) == 0 ? 1 : 0)");
                    return true;
                }
                // Representation-sensitive canonicality and the integer-parity pair, matching
                // the BCL bodies: 0.50m is not canonical, 2.00m is an even integer, 2.5m is
                // neither even nor odd.
                case "IsCanonical":
                {
                    var a = Pop();
                    Push(StackKind.I4, "int32_t", $"dn2cpp_decimal_is_canonical({DecVal(a)})");
                    return true;
                }
                case "IsEvenInteger" or "IsOddInteger":
                {
                    var a = Pop();
                    string fn = name == "IsEvenInteger"
                        ? "dn2cpp_decimal_is_even_integer"
                        : "dn2cpp_decimal_is_odd_integer";
                    Push(StackKind.I4, "int32_t", $"{fn}({DecVal(a)})");
                    return true;
                }
            }
        }

        // Parsing — string or ReadOnlySpan<char> input, in the plain /
        // (NumberStyles) / (IFormatProvider) / (NumberStyles, IFormatProvider)
        // shapes and their TryParse out-forms, routed to the runtime
        // NumberStyles engine (default style: NumberStyles.Number; a
        // null/absent provider parses invariant).
        if (name is "Parse" or "TryParse"
            && ps.Length >= 1 && (ps[0].IsString || IsReadOnlyCharSpan(ps[0])))
        {
            bool isTry = name == "TryParse";
            int valueArgs = isTry ? ps.Length - 1 : ps.Length;
            bool shapeOk = valueArgs is >= 1 and <= 3
                && (!isTry || (ps.Length >= 2 && ps[^1].Kind == TypeKind.ByRef));
            bool hasStyles = false, hasProvider = false;
            for (int i = 1; shapeOk && i < valueArgs; i++)
            {
                if (i == 1 && IsNumberStyles(ps[i]))
                    hasStyles = true;
                else if (i == valueArgs - 1 && IsFormatProvider(ps[i]))
                    hasProvider = true;
                else
                    shapeOk = false;
            }
            if (shapeOk)
            {
                string? outRef = isTry ? Pop().Expr : null;
                string nfiExpr = hasProvider ? Cast(Pop(), "const Dn2CppNumberFormatInfo*") : "nullptr";
                string stylesExpr = hasStyles ? $"(int32_t)({Pop().Expr})" : "111"; // NumberStyles.Number
                string args, suffix;
                if (ps[0].IsString)
                {
                    args = $"{Cast(Pop(), "Dn2CppString*")}, {stylesExpr}, {nfiExpr}";
                    suffix = "str";
                }
                else
                {
                    string sp = SpanValue(Pop(), CppTypes.Of(ps[0]));
                    args = $"(const char16_t*){sp}.f__reference, {sp}.f__length, {stylesExpr}, {nfiExpr}";
                    suffix = "chars";
                }
                if (isTry)
                    Push(StackKind.I4, "int32_t",
                        $"dn2cpp_decimal_tryparse_styles_{suffix}({args}, (Dn2CppDecimal*)({outRef}))");
                else
                    Push(StackKind.Struct, "Dn2CppDecimal", $"dn2cpp_decimal_parse_styles_{suffix}({args})");
                return true;
            }
        }

        // ToString — default / format / culture variants.
        if (name == "ToString")
        {
            string? fmt = null, prov = null;
            if (sig.ParameterTypes is [{ IsString: true }, _]) { var p = Pop(); prov = Cast(p, "const Dn2CppNumberFormatInfo*"); var f = Pop(); fmt = Cast(f, "Dn2CppString*"); }
            else if (sig.ParameterTypes is [{ IsString: true }]) { var f = Pop(); fmt = Cast(f, "Dn2CppString*"); }
            else if (sig.ParameterTypes is [_]) { var p = Pop(); prov = Cast(p, "const Dn2CppNumberFormatInfo*"); }
            var recv = Pop();
            string expr = (fmt, prov) switch
            {
                (null, null) => $"dn2cpp_decimal_to_string({DecVal(recv)})",
                (not null, null) => $"dn2cpp_decimal_format({DecVal(recv)}, {fmt})",
                (null, not null) => $"dn2cpp_decimal_format_c({DecVal(recv)}, nullptr, {prov})",
                _ => $"dn2cpp_decimal_format_c({DecVal(recv)}, {fmt}, {prov})",
            };
            Push(StackKind.Ref, "Dn2CppString*", expr);
            return true;
        }

        // Decimal.GetBits(decimal) -> int[4] and GetBits(decimal, Span<int>) -> int:
        // the .NET bit layout { lo32, mid32, hi32, flags }, where lo32|mid32<<32 is the
        // low 64 mantissa bits, hi32 the high 32, and flags packs scale (bits 16..23)
        // and sign (bit 31). Extracted in the runtime from the Dn2CppDecimal fields.
        if (name == "GetBits" && ps.Length == 1 && IsDecimal(ps[0]))
        {
            var a = Pop();
            // int[] lowers to Dn2CppArrayI4* — the exact type the runtime helper returns.
            Push(StackKind.Ref, CppTypes.Of(sig.ReturnType),
                $"dn2cpp_decimal_get_bits({DecVal(a)}, {PrimArrayTypeInfoExpr(PrimitiveTypeCode.Int32)})");
            return true;
        }
        if (name == "GetBits" && ps is [var gbD, var gbS] && IsDecimal(gbD) && IsSpanOfPrimitive(gbS, PrimitiveTypeCode.Int32))
        {
            string sp = SpanValue(Pop(), CppTypes.Of(ps[1])); // Span<int> destination
            var a = Pop();
            Push(StackKind.I4, "int32_t",
                $"dn2cpp_decimal_get_bits_span({DecVal(a)}, (int32_t*){sp}.f__reference, {sp}.f__length)");
            return true;
        }

        // Decimal.get_High — the internal `uint High` accessor: the high 32 mantissa
        // bits, straight off the runtime field.
        if (name == "get_High" && ps.Length == 0)
        {
            var a = Pop();
            Push(StackKind.I4, "int32_t", $"(int32_t)({DecVal(a)}.hi)");
            return true;
        }

        // Decimal.get_Low64 — the internal `ulong Low64` accessor: the low 64 mantissa bits.
        // Dn2CppDecimal stores exactly that as its 64-bit `lo` field (the .NET lo32|mid32<<32
        // pair already fused), so it is a straight read.
        if (name == "get_Low64" && ps.Length == 0)
        {
            var a = Pop();
            Push(StackKind.I8, "int64_t", $"(int64_t)({DecVal(a)}.lo)");
            return true;
        }

        // Decimal.get_Low / get_Mid — the internal `uint Low` / `uint Mid` accessors: the low
        // and high halves of the 64-bit `lo` mantissa field (the .NET lo32 and mid32 words).
        if (name == "get_Low" && ps.Length == 0)
        {
            var a = Pop();
            Push(StackKind.I4, "int32_t", $"(int32_t)(uint32_t)({DecVal(a)}.lo)");
            return true;
        }
        if (name == "get_Mid" && ps.Length == 0)
        {
            var a = Pop();
            Push(StackKind.I4, "int32_t", $"(int32_t)(uint32_t)(({DecVal(a)}.lo) >> 32)");
            return true;
        }

        // Decimal.ToDouble(decimal) static — the same conversion as op_Explicit(decimal)->double.
        if (name == "ToDouble" && ps.Length == 1 && IsDecimal(ps[0]))
        {
            var a = Pop();
            Push(StackKind.R8, "double", $"dn2cpp_decimal_to_double({DecVal(a)})");
            return true;
        }

        // Decimal.TryFormat(Span<char> dest, out int charsWritten, ReadOnlySpan<char>
        // format, IFormatProvider): render into the destination span (fits -> copy + count
        // + true; too short -> false). An empty/default format takes the "G" round-trip
        // (preserves scale); a standard specifier routes through dn2cpp_decimal_format,
        // which throws loudly on one it does not cover. The provider is invariant.
        if (name == "TryFormat" && ps is [var tfDest, { Kind: TypeKind.ByRef }, var tfFmt, _]
            && IsSpanOfPrimitive(tfDest, PrimitiveTypeCode.Char) && IsReadOnlySpanChar(tfFmt))
        {
            Pop();                                                  // IFormatProvider — invariant
            string fmtSpan = SpanValue(Pop(), CppTypes.Of(ps[2]));  // ReadOnlySpan<char> format
            string wrote = Cast(Pop(), "int32_t*");                 // out int charsWritten
            string dPtr = SpanPtr(Pop(), CppTypes.Of(ps[0]));       // Span<char> destination
            var recv = Pop();
            string sv = NewTemp("Dn2CppString*");
            Emit($"{sv} = {fmtSpan}.f__length == 0 ? dn2cpp_decimal_to_string({DecVal(recv)}) " +
                 $": dn2cpp_decimal_format({DecVal(recv)}, dn2cpp_string_from_chars((const char16_t*){fmtSpan}.f__reference, {fmtSpan}.f__length));");
            Push(StackKind.I4, "int32_t",
                $"dn2cpp_string_try_copy_to_span({sv}, (char16_t*){dPtr}->f__reference, {dPtr}->f__length, {wrote})");
            return true;
        }

        // Decimal's own generic-math TryConvertFrom/To* bodies (reached when the
        // constrained static-virtual dispatch resolves the INumberBase<decimal>
        // conversion to Decimal's implementing method).
        if (TryEmitDecimalTryConvert(name, sig))
            return true;

        return false;
    }

    // ===== System.TimeSpan / System.DateTime =====

    private static bool IsTimeSpan(TypeDesc t) =>
        t is { Kind: TypeKind.Class, Class.FullName: "System.TimeSpan" };

    // A constrained IComparable<TimeSpan>/IEquatable<TimeSpan> callvirt reaches
    // TryEmitTimeSpanIntrinsic with the interface's OWN generic parameter (!0) as the argument
    // type: the callee signature is decoded under the caller's GenericContext, which binds the
    // method's !!T but not IComparable<T>'s !0, so it stays a GenericVar. Dispatch is already
    // keyed to declType "System.TimeSpan", and TimeSpan's only single-generic-arg interface
    // methods are CompareTo/Equals over TimeSpan itself, so a GenericVar argument here IS
    // TimeSpan.
    private static bool IsTimeSpanOrConstrainedSelf(TypeDesc t) =>
        IsTimeSpan(t) || t.Kind == TypeKind.GenericVar;

    /// <summary>True for a blocking-call timeout parameter — an <c>int</c> milliseconds
    /// count or a <c>TimeSpan</c> (the two shapes accepted by Wait/WaitOne/TryEnter/Join
    /// timeout overloads).</summary>
    private static bool IsTimeoutParam(TypeDesc t) =>
        t is { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Int32 } || IsTimeSpan(t);

    /// <summary>The C++ int32 millisecond expression for a blocking-call timeout argument:
    /// an <c>int</c> passes through; a <c>TimeSpan</c> converts via its tick count
    /// (TicksPerMillisecond = 10000), matching <c>(long)TimeSpan.TotalMilliseconds</c> — so
    /// Timeout.InfiniteTimeSpan (-1 ms) maps to -1, which the runtime reads as an infinite
    /// wait. Throws for any other parameter shape.</summary>
    private string TimeoutMs(StackEntry arg, TypeDesc paramType) =>
        paramType is { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Int32 }
            ? arg.Expr
            : IsTimeSpan(paramType)
                ? $"(int32_t)(({TSVal(arg)}).ticks / 10000)"
                : throw new NotSupportedException(
                    $"{Method.DeclaringClass.FullName}.{Method.Name}: unsupported timeout " +
                    $"parameter type '{paramType}'");

    /// <summary>The C++ int64 millisecond expression for a Timer dueTime/period argument:
    /// an <c>int</c>/<c>long</c> passes through (widened); a <c>TimeSpan</c> converts via
    /// its tick count (TicksPerMillisecond = 10000), matching
    /// <c>(long)TimeSpan.TotalMilliseconds</c> — so Timeout.InfiniteTimeSpan (-1 ms) maps
    /// to -1 (idle). A <c>uint</c> widens unsigned, mapping the unsigned-infinity sentinel
    /// 0xFFFFFFFF to -1 (idle), matching the real uint Timer overloads. Throws for any
    /// other parameter shape.</summary>
    private string TimerMs(StackEntry arg, TypeDesc paramType) => paramType switch
    {
        { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Int32 } => $"(int64_t)({arg.Expr})",
        { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Int64 } => arg.Expr,
        { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.UInt32 } =>
            $"((uint32_t)({arg.Expr}) == 0xFFFFFFFFu ? (int64_t)-1 : (int64_t)(uint32_t)({arg.Expr}))",
        _ when IsTimeSpan(paramType) => $"(int64_t)(({TSVal(arg)}).ticks / 10000)",
        _ => throw new NotSupportedException(
            $"{Method.DeclaringClass.FullName}.{Method.Name}: unsupported Timer " +
            $"dueTime/period parameter type '{paramType}'"),
    };
    private static bool IsDateTime(TypeDesc t) =>
        t is { Kind: TypeKind.Class, Class.FullName: "System.DateTime" };
    private static bool IsDateTimeOffset(TypeDesc t) =>
        t is { Kind: TypeKind.Class, Class.FullName: "System.DateTimeOffset" };
    private bool IsReadOnlySpanChar(TypeDesc t) =>
        t is { Kind: TypeKind.Class, Class: { } cls }
        && Comp.GenericDefFullName(cls) == "System.ReadOnlySpan"
        && cls.Context.TypeArgs is [{ Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Char }];
    private static bool IsDateOnly(TypeDesc t) =>
        t is { Kind: TypeKind.Class, Class.FullName: "System.DateOnly" };
    private static bool IsTimeOnly(TypeDesc t) =>
        t is { Kind: TypeKind.Class, Class.FullName: "System.TimeOnly" };
    private static bool IsDateTimeKind(TypeDesc t) =>
        (t.Kind == TypeKind.Class && t.Class?.FullName == "System.DateTimeKind")
        || (t.Kind == TypeKind.External && t.ExternalName == "System.DateTimeKind");

    /// <summary>A TimeSpan/DateTime value/pointer operand as a struct rvalue (deref a
    /// managed pointer from <c>ldloca</c>; pass a struct value through).</summary>
    private string TSVal(StackEntry e) =>
        e.Kind == StackKind.Ptr ? $"(*(Dn2CppTimeSpan*)({e.Expr}))" : e.Expr;
    private string DTVal(StackEntry e) =>
        e.Kind == StackKind.Ptr ? $"(*(Dn2CppDateTime*)({e.Expr}))" : e.Expr;
    private string DTOVal(StackEntry e) =>
        e.Kind == StackKind.Ptr ? $"(*(Dn2CppDateTimeOffset*)({e.Expr}))" : e.Expr;
    private string DOnlyVal(StackEntry e) =>
        e.Kind == StackKind.Ptr ? $"(*(Dn2CppDateOnly*)({e.Expr}))" : e.Expr;
    private string TOnlyVal(StackEntry e) =>
        e.Kind == StackKind.Ptr ? $"(*(Dn2CppTimeOnly*)({e.Expr}))" : e.Expr;

    /// <summary>Pushes an intrinsic value-type static-field constant (Decimal/TimeSpan/
    /// DateTime — e.g. TimeSpan.Zero, DateTime.MaxValue), which have no emitted storage,
    /// either as a value (<c>ldsfld</c>) or, with <paramref name="wantAddress"/>,
    /// materialized into a temp whose address is pushed (<c>ldsflda</c>, for an instance
    /// call on the constant). Returns false if the declaring type isn't one.</summary>
    private bool TryIntrinsicStaticField(FieldInfo fld, bool wantAddress)
    {
        string cppType;
        string expr;
        // A custom async task type adopted into the intrinsic Task family has no emitted
        // static storage either. Its one universal static constant is the already-completed
        // task, spelled CompletedTask — a FIELD on some such types where the BCL makes it a
        // property, which is why it lands here rather than in the intrinsic call tables. Any
        // other static is named and refused, never folded to a constant that might be wrong.
        if (Comp.AdoptedAsyncKey(fld.DeclaringClass) is not null)
        {
            if (fld.Name != "CompletedTask")
                throw new NotSupportedException(
                    $"{Method.DeclaringClass.FullName}.{Method.Name}: "
                    + $"{Comp.GenericDefFullName(fld.DeclaringClass)}.{fld.Name} — a custom async task "
                    + "type is adopted into dn2cpp's intrinsic Task family and so has no static "
                    + "storage; only its CompletedTask constant is modeled");
            bool byRef = fld.DeclaringClass.IntrinsicCppName == "Dn2CppTask*";
            cppType = byRef ? "Dn2CppTask*" : "Dn2CppTaskAwaiter";
            expr = byRef ? "dn2cpp_task_completed()" : "Dn2CppTaskAwaiter{ dn2cpp_task_completed() }";
            if (wantAddress)
            {
                string t = NewTemp(cppType);
                Emit($"{t} = {expr};");
                Push(StackKind.Ptr, cppType + "*", $"&{t}");
            }
            else
            {
                Push(byRef ? StackKind.Ref : StackKind.Struct, cppType, expr);
            }
            return true;
        }
        switch (fld.DeclaringClass.FullName)
        {
            case "System.Decimal":
                cppType = "Dn2CppDecimal";
                expr = fld.Name switch
                {
                    "Zero" => "dn2cpp_decimal_from_i4(0)",
                    "One" => "dn2cpp_decimal_from_i4(1)",
                    "MinusOne" => "dn2cpp_decimal_from_i4(-1)",
                    "MaxValue" => "dn2cpp_decimal_from_parts(-1, -1, -1, 0, 0)",
                    "MinValue" => "dn2cpp_decimal_from_parts(-1, -1, -1, 1, 0)",
                    _ => throw new NotSupportedException(
                        $"{Method.DeclaringClass.FullName}.{Method.Name}: decimal static field {fld.Name} is not supported"),
                };
                break;
            case "System.TimeSpan":
                cppType = "Dn2CppTimeSpan";
                expr = fld.Name switch
                {
                    "Zero" => "dn2cpp_timespan_from_ticks(0)",
                    "MinValue" => "dn2cpp_timespan_from_ticks(-9223372036854775807LL - 1)",
                    "MaxValue" => "dn2cpp_timespan_from_ticks(9223372036854775807LL)",
                    _ => throw new NotSupportedException(
                        $"{Method.DeclaringClass.FullName}.{Method.Name}: TimeSpan static field {fld.Name} is not supported"),
                };
                break;
            case "System.DateTime":
                cppType = "Dn2CppDateTime";
                // Now/UtcNow/Today are static *properties* (get_*), lowered in
                // TryEmitDateTimeIntrinsic, not static fields.
                expr = fld.Name switch
                {
                    "MinValue" => "dn2cpp_datetime_from_ticks(0, 0)",
                    "MaxValue" => "dn2cpp_datetime_from_ticks(3155378975999999999LL, 0)",
                    // 1970-01-01T00:00:00Z (621355968000000000 ticks, Kind=Utc).
                    "UnixEpoch" => "dn2cpp_datetime_from_ticks(621355968000000000LL, 1)",
                    _ => throw new NotSupportedException(
                        $"{Method.DeclaringClass.FullName}.{Method.Name}: DateTime static field {fld.Name} is not supported"),
                };
                break;
            case "System.DateTimeOffset":
                cppType = "Dn2CppDateTimeOffset";
                // MinValue/MaxValue are at offset 0 (UTC); Now/UtcNow are get_* properties.
                expr = fld.Name switch
                {
                    "MinValue" => "dn2cpp_datetimeoffset_make(0, 0)",
                    "MaxValue" => "dn2cpp_datetimeoffset_make(3155378975999999999LL, 0)",
                    // 1970-01-01T00:00:00+00:00.
                    "UnixEpoch" => "dn2cpp_datetimeoffset_make(621355968000000000LL, 0)",
                    _ => throw new NotSupportedException(
                        $"{Method.DeclaringClass.FullName}.{Method.Name}: DateTimeOffset static field {fld.Name} is not supported"),
                };
                break;
            default:
                return false;
        }
        if (wantAddress)
        {
            string tmp = NewTemp(cppType);
            Emit($"{tmp} = {expr};");
            Push(StackKind.Ptr, cppType + "*", $"&{tmp}");
        }
        else
        {
            Push(StackKind.Struct, cppType, expr);
        }
        return true;
    }

    /// <summary>Pops a TimeSpan ctor's args (pushed left-to-right) and returns the
    /// C++ expression constructing the Dn2CppTimeSpan.</summary>
    private string EmitTimeSpanCtorExpr(MethodSignature<TypeDesc> sig)
    {
        var ps = sig.ParameterTypes;
        var args = new StackEntry[ps.Length];
        for (int i = ps.Length - 1; i >= 0; i--) args[i] = Pop();
        string I(int i) => $"(int32_t)({args[i].Expr})";
        return ps.Length switch
        {
            1 => $"dn2cpp_timespan_from_ticks((int64_t)({args[0].Expr}))",
            3 => $"dn2cpp_timespan_from_hms({I(0)}, {I(1)}, {I(2)})",
            4 => $"dn2cpp_timespan_from_dhms({I(0)}, {I(1)}, {I(2)}, {I(3)})",
            5 => $"dn2cpp_timespan_from_dhmsms({I(0)}, {I(1)}, {I(2)}, {I(3)}, {I(4)})",
            _ => throw new NotSupportedException(
                $"{Method.DeclaringClass.FullName}.{Method.Name}: TimeSpan ctor with {ps.Length} args is not supported"),
        };
    }

    /// <summary>Pops a DateTime ctor's args (pushed left-to-right) and returns the
    /// C++ expression constructing the Dn2CppDateTime. A trailing DateTimeKind arg
    /// disambiguates the 7-arg (…,Kind) form from the 7-int (…,millisecond) form;
    /// Calendar/microsecond overloads are carve-outs (NotSupported).</summary>
    private string EmitDateTimeCtorExpr(MethodSignature<TypeDesc> sig)
    {
        var ps = sig.ParameterTypes;
        var args = new StackEntry[ps.Length];
        for (int i = ps.Length - 1; i >= 0; i--) args[i] = Pop();
        string I(int i) => $"(int32_t)({args[i].Expr})";
        bool lastKind = ps.Length > 0 && IsDateTimeKind(ps[^1]);
        string kind = lastKind ? $"(int32_t)({args[^1].Expr})" : "0";
        return (ps.Length, lastKind) switch
        {
            (1, _) => $"dn2cpp_datetime_from_ticks((int64_t)({args[0].Expr}), 0)",
            (2, true) => $"dn2cpp_datetime_from_ticks((int64_t)({args[0].Expr}), {kind})",
            (3, _) => $"dn2cpp_datetime_ymd({I(0)}, {I(1)}, {I(2)}, 0)",
            (6, _) => $"dn2cpp_datetime_ymdhms({I(0)}, {I(1)}, {I(2)}, {I(3)}, {I(4)}, {I(5)}, 0)",
            (7, true) => $"dn2cpp_datetime_ymdhms({I(0)}, {I(1)}, {I(2)}, {I(3)}, {I(4)}, {I(5)}, {kind})",
            (7, false) => $"dn2cpp_datetime_ymdhmsms({I(0)}, {I(1)}, {I(2)}, {I(3)}, {I(4)}, {I(5)}, {I(6)}, 0)",
            (8, true) => $"dn2cpp_datetime_ymdhmsms({I(0)}, {I(1)}, {I(2)}, {I(3)}, {I(4)}, {I(5)}, {I(6)}, {kind})",
            _ => throw new NotSupportedException(
                $"{Method.DeclaringClass.FullName}.{Method.Name}: DateTime ctor with {ps.Length} args is not supported"),
        };
    }

    /// <summary>Lowers a System.TimeSpan member to its Dn2CppTimeSpan runtime helper.
    /// Returns false if unmapped so the caller reports a clear error.</summary>
    private bool TryEmitTimeSpanIntrinsic(string name, MethodSignature<TypeDesc> sig)
    {
        var ps = sig.ParameterTypes;

        //.ctor — the ldloca+call (address-init) form (newobj form is in TranslateNewobj).
        if (name == ".ctor")
        {
            string expr = EmitTimeSpanCtorExpr(sig);
            var addr = Pop();
            Emit($"*({Cast(addr, "Dn2CppTimeSpan*")}) = {expr};");
            return true;
        }

        // Static factories: From<Unit>(value). A double arg truncates value*ticks toward
        // zero; an integer arg is the exact.NET 8+ integer overload (value*ticks). Both
        // shapes produce identical ticks for whole values, so one path serves either.
        switch (name)
        {
            case "FromDays" or "FromHours" or "FromMinutes" or "FromSeconds"
                or "FromMilliseconds" or "FromTicks" when ps.Length == 1 && ps[0].Kind == TypeKind.Primitive:
            {
                string tpu = name switch
                {
                    "FromDays" => "864000000000LL",
                    "FromHours" => "36000000000LL",
                    "FromMinutes" => "600000000LL",
                    "FromSeconds" => "10000000LL",
                    "FromMilliseconds" => "10000LL",
                    _ => "1LL", // FromTicks
                };
                var a = Pop();
                string expr = ps[0].Primitive is PrimitiveTypeCode.Double or PrimitiveTypeCode.Single
                    ? $"dn2cpp_timespan_from_unit((double)({a.Expr}), {tpu})"
                    : $"dn2cpp_timespan_from_ticks((int64_t)({a.Expr}) * {tpu})";
                Push(StackKind.Struct, "Dn2CppTimeSpan", expr);
                return true;
            }
            case "Compare" when ps.Length == 2:
            {
                var b = Pop(); var a = Pop();
                Push(StackKind.I4, "int32_t", $"dn2cpp_timespan_cmp({TSVal(a)}, {TSVal(b)})");
                return true;
            }
        }

        // Parse / TryParse / ParseExact — static, string overloads only (the
        // ReadOnlySpan<char> and format-array overloads stay carve-outs). The trailing
        // IFormatProvider / TimeSpanStyles args are dropped (invariant); the out result
        // is the last param (a ByRef).
        if (name == "Parse" && ps.Length >= 1 && ps[0].IsString)
        {
            for (int x = ps.Length - 1; x >= 1; x--) Pop();
            var s = Pop();
            Push(StackKind.Struct, "Dn2CppTimeSpan", $"dn2cpp_timespan_parse({Cast(s, "Dn2CppString*")})");
            return true;
        }
        if (name == "ParseExact" && ps.Length >= 2 && ps[0].IsString && ps[1].IsString)
        {
            for (int x = ps.Length - 1; x >= 2; x--) Pop(); // provider [, styles]
            var f = Pop(); var s = Pop();
            Push(StackKind.Struct, "Dn2CppTimeSpan", $"dn2cpp_timespan_parse_exact({Cast(s, "Dn2CppString*")}, {Cast(f, "Dn2CppString*")})");
            return true;
        }
        if (name == "TryParse" && ps.Length >= 2 && ps[0].IsString)
        {
            var outAddr = Pop();
            for (int x = ps.Length - 2; x >= 1; x--) Pop(); // provider, if present
            var s = Pop();
            Push(StackKind.I4, "int32_t", $"dn2cpp_timespan_try_parse({Cast(s, "Dn2CppString*")}, {Cast(outAddr, "Dn2CppTimeSpan*")})");
            return true;
        }
        if (name == "TryParseExact" && ps.Length >= 3 && ps[0].IsString && ps[1].IsString)
        {
            var outAddr = Pop();
            for (int x = ps.Length - 2; x >= 2; x--) Pop(); // provider [, styles]
            var f = Pop(); var s = Pop();
            Push(StackKind.I4, "int32_t", $"dn2cpp_timespan_try_parse_exact({Cast(s, "Dn2CppString*")}, {Cast(f, "Dn2CppString*")}, {Cast(outAddr, "Dn2CppTimeSpan*")})");
            return true;
        }
        // Span overloads (ReadOnlySpan<char> input + single format span): materialize both
        // spans to strings and reuse the string parser. TimeZoneInfo.TZif_ParseOffsetString
        // (POSIX TZ offset parsing) is the reachable caller; provider/styles dropped (invariant).
        // Mirrors the DateTimeOffset span TryParseExact arm below.
        if (name == "TryParseExact" && ps.Length >= 3 && IsReadOnlySpanChar(ps[0]) && IsReadOnlySpanChar(ps[1]))
        {
            var outAddr = Pop();
            for (int x = ps.Length - 2; x >= 2; x--) Pop(); // provider [, styles]
            string fPtr = SpanPtr(Pop(), CppTypes.Of(ps[1]));
            string f = NewTemp("Dn2CppString*");
            Emit($"{f} = dn2cpp_string_from_chars((const char16_t*){fPtr}->f__reference, {fPtr}->f__length);");
            string sPtr = SpanPtr(Pop(), CppTypes.Of(ps[0]));
            string sv = NewTemp("Dn2CppString*");
            Emit($"{sv} = dn2cpp_string_from_chars((const char16_t*){sPtr}->f__reference, {sPtr}->f__length);");
            Push(StackKind.I4, "int32_t", $"dn2cpp_timespan_try_parse_exact({sv}, {f}, {Cast(outAddr, "Dn2CppTimeSpan*")})");
            return true;
        }

        // Property getters.
        switch (name)
        {
            case "get_Ticks": { var r = Pop(); Push(StackKind.I8, "int64_t", $"({TSVal(r)}).ticks"); return true; }
            case "get_Days": { var r = Pop(); Push(StackKind.I4, "int32_t", $"dn2cpp_timespan_days({TSVal(r)})"); return true; }
            case "get_Hours": { var r = Pop(); Push(StackKind.I4, "int32_t", $"dn2cpp_timespan_hours({TSVal(r)})"); return true; }
            case "get_Minutes": { var r = Pop(); Push(StackKind.I4, "int32_t", $"dn2cpp_timespan_minutes({TSVal(r)})"); return true; }
            case "get_Seconds": { var r = Pop(); Push(StackKind.I4, "int32_t", $"dn2cpp_timespan_seconds({TSVal(r)})"); return true; }
            case "get_Milliseconds": { var r = Pop(); Push(StackKind.I4, "int32_t", $"dn2cpp_timespan_milliseconds({TSVal(r)})"); return true; }
            case "get_TotalDays": { var r = Pop(); Push(StackKind.R8, "double", $"dn2cpp_timespan_total({TSVal(r)}, 864000000000LL)"); return true; }
            case "get_TotalHours": { var r = Pop(); Push(StackKind.R8, "double", $"dn2cpp_timespan_total({TSVal(r)}, 36000000000LL)"); return true; }
            case "get_TotalMinutes": { var r = Pop(); Push(StackKind.R8, "double", $"dn2cpp_timespan_total({TSVal(r)}, 600000000LL)"); return true; }
            case "get_TotalSeconds": { var r = Pop(); Push(StackKind.R8, "double", $"dn2cpp_timespan_total({TSVal(r)}, 10000000LL)"); return true; }
            case "get_TotalMilliseconds": { var r = Pop(); Push(StackKind.R8, "double", $"dn2cpp_timespan_total({TSVal(r)}, 10000LL)"); return true; }
        }

        // Instance methods.
        switch (name)
        {
            case "Add" when ps.Length == 1:
            { var b = Pop(); var a = Pop(); Push(StackKind.Struct, "Dn2CppTimeSpan", $"dn2cpp_timespan_add({TSVal(a)}, {TSVal(b)})"); return true; }
            case "Subtract" when ps.Length == 1:
            { var b = Pop(); var a = Pop(); Push(StackKind.Struct, "Dn2CppTimeSpan", $"dn2cpp_timespan_sub({TSVal(a)}, {TSVal(b)})"); return true; }
            case "Negate" when ps.Length == 0:
            { var a = Pop(); Push(StackKind.Struct, "Dn2CppTimeSpan", $"dn2cpp_timespan_neg({TSVal(a)})"); return true; }
            case "Duration" when ps.Length == 0:
            { var a = Pop(); Push(StackKind.Struct, "Dn2CppTimeSpan", $"dn2cpp_timespan_duration({TSVal(a)})"); return true; }
            case "CompareTo" when ps.Length == 1 && IsTimeSpanOrConstrainedSelf(ps[0]):
            { var b = Pop(); var a = Pop(); Push(StackKind.I4, "int32_t", $"dn2cpp_timespan_cmp({TSVal(a)}, {TSVal(b)})"); return true; }
            case "Equals" when ps.Length == 1 && IsTimeSpanOrConstrainedSelf(ps[0]):
            { var b = Pop(); var a = Pop(); Push(StackKind.I4, "int32_t", $"(dn2cpp_timespan_cmp({TSVal(a)}, {TSVal(b)}) == 0 ? 1 : 0)"); return true; }
            case "GetHashCode" when ps.Length == 0:
            { var a = Pop(); Push(StackKind.I4, "int32_t", $"dn2cpp_timespan_hash({TSVal(a)})"); return true; }
            case "ToString" when ps.Length == 0:
            { var a = Pop(); Push(StackKind.Ref, "Dn2CppString*", $"dn2cpp_timespan_to_string({TSVal(a)})"); return true; }
            // ToString(format) / ToString(format, IFormatProvider) — invariant.
            case "ToString" when ps is [{ IsString: true }]:
            { var f = Pop(); var a = Pop(); Push(StackKind.Ref, "Dn2CppString*", $"dn2cpp_timespan_format({TSVal(a)}, {Cast(f, "Dn2CppString*")})"); return true; }
            case "ToString" when ps is [{ IsString: true }, _]:
            { Pop(); var f = Pop(); var a = Pop(); Push(StackKind.Ref, "Dn2CppString*", $"dn2cpp_timespan_format({TSVal(a)}, {Cast(f, "Dn2CppString*")})"); return true; }
        }

        // Operators.
        string? bin = name switch
        {
            "op_Addition" => "dn2cpp_timespan_add",
            "op_Subtraction" => "dn2cpp_timespan_sub",
            _ => null,
        };
        if (bin is not null && ps.Length == 2 && IsTimeSpan(ps[0]) && IsTimeSpan(ps[1]))
        {
            var b = Pop(); var a = Pop();
            Push(StackKind.Struct, "Dn2CppTimeSpan", $"{bin}({TSVal(a)}, {TSVal(b)})");
            return true;
        }
        switch (name)
        {
            case "op_UnaryNegation" when ps.Length == 1:
            { var a = Pop(); Push(StackKind.Struct, "Dn2CppTimeSpan", $"dn2cpp_timespan_neg({TSVal(a)})"); return true; }
            case "op_UnaryPlus" when ps.Length == 1:
            { var a = Pop(); Push(StackKind.Struct, "Dn2CppTimeSpan", TSVal(a)); return true; }
        }
        string? cmp = name switch
        {
            "op_Equality" => "== 0", "op_Inequality" => "!= 0",
            "op_LessThan" => "< 0", "op_GreaterThan" => "> 0",
            "op_LessThanOrEqual" => "<= 0", "op_GreaterThanOrEqual" => ">= 0",
            _ => null,
        };
        if (cmp is not null && ps.Length == 2 && IsTimeSpan(ps[0]) && IsTimeSpan(ps[1]))
        {
            var b = Pop(); var a = Pop();
            Push(StackKind.I4, "int32_t", $"(dn2cpp_timespan_cmp({TSVal(a)}, {TSVal(b)}) {cmp} ? 1 : 0)");
            return true;
        }

        return false;
    }

    /// <summary>Lowers a System.DateTime member to its Dn2CppDateTime runtime helper.
    /// Returns false if unmapped so the caller reports a clear error.</summary>
    private bool TryEmitDateTimeIntrinsic(string name, MethodSignature<TypeDesc> sig)
    {
        var ps = sig.ParameterTypes;
        const string TPD = "864000000000LL";

        if (name == ".ctor")
        {
            string expr = EmitDateTimeCtorExpr(sig);
            var addr = Pop();
            Emit($"*({Cast(addr, "Dn2CppDateTime*")}) = {expr};");
            return true;
        }

        // Static wall-clock properties. These are get_* statics — no receiver to
        // pop — so they must precede the instance property getters below. Now/Today read
        // local time (Kind=Local), UtcNow reads UTC (Kind=Utc).
        switch (name)
        {
            case "get_Now" when ps.Length == 0:
                Push(StackKind.Struct, "Dn2CppDateTime", "dn2cpp_datetime_now()"); return true;
            case "get_UtcNow" when ps.Length == 0:
                Push(StackKind.Struct, "Dn2CppDateTime", "dn2cpp_datetime_utc_now()"); return true;
            case "get_Today" when ps.Length == 0:
                Push(StackKind.Struct, "Dn2CppDateTime", "dn2cpp_datetime_today()"); return true;
        }

        // Static helpers.
        switch (name)
        {
            case "IsLeapYear" when ps.Length == 1:
            { var a = Pop(); Push(StackKind.I4, "int32_t", $"dn2cpp_datetime_is_leap_year((int32_t)({a.Expr}))"); return true; }
            case "DaysInMonth" when ps.Length == 2:
            { var m = Pop(); var y = Pop(); Push(StackKind.I4, "int32_t", $"dn2cpp_datetime_days_in_month((int32_t)({y.Expr}), (int32_t)({m.Expr}))"); return true; }
            case "Compare" when ps.Length == 2:
            { var b = Pop(); var a = Pop(); Push(StackKind.I4, "int32_t", $"dn2cpp_datetime_cmp({DTVal(a)}, {DTVal(b)})"); return true; }
            case "SpecifyKind" when ps.Length == 2:
            { var k = Pop(); var a = Pop(); Push(StackKind.Struct, "Dn2CppDateTime", $"dn2cpp_datetime_from_ticks(({DTVal(a)}).ticks(), (int32_t)({k.Expr}))"); return true; }
            // Windows FILETIME (100ns intervals since 1601-01-01 UTC) -> Utc DateTime.
            // dn2cpp_datetime_from_file_time_utc validates fileTime's range
            // (ArgumentOutOfRangeException), matching the real BCL.
            case "FromFileTimeUtc" when ps.Length == 1:
            { var a = Pop(); Push(StackKind.Struct, "Dn2CppDateTime", $"dn2cpp_datetime_from_file_time_utc((int64_t)({a.Expr}))"); return true; }
            case "FromFileTime" when ps.Length == 1:
            { var a = Pop(); Push(StackKind.Struct, "Dn2CppDateTime", $"dn2cpp_datetime_to_local(dn2cpp_datetime_from_file_time_utc((int64_t)({a.Expr})))"); return true; }
        }

        // Parse / TryParse / ParseExact — static, string overloads only (the
        // ReadOnlySpan<char> and format-array overloads stay carve-outs). The trailing
        // IFormatProvider / DateTimeStyles args are dropped (invariant); the out result
        // is the last param (a ByRef).
        if (name == "Parse" && ps.Length >= 1 && ps[0].IsString)
        {
            for (int x = ps.Length - 1; x >= 1; x--) Pop();
            var s = Pop();
            Push(StackKind.Struct, "Dn2CppDateTime", $"dn2cpp_datetime_parse({Cast(s, "Dn2CppString*")})");
            return true;
        }
        if (name == "ParseExact" && ps.Length >= 2 && ps[0].IsString && ps[1].IsString)
        {
            for (int x = ps.Length - 1; x >= 2; x--) Pop(); // provider [, styles]
            var f = Pop(); var s = Pop();
            Push(StackKind.Struct, "Dn2CppDateTime", $"dn2cpp_datetime_parse_exact({Cast(s, "Dn2CppString*")}, {Cast(f, "Dn2CppString*")})");
            return true;
        }
        if (name == "TryParse" && ps.Length >= 2 && ps[0].IsString)
        {
            var outAddr = Pop();
            for (int x = ps.Length - 2; x >= 1; x--) Pop(); // provider, if present
            var s = Pop();
            Push(StackKind.I4, "int32_t", $"dn2cpp_datetime_try_parse({Cast(s, "Dn2CppString*")}, {Cast(outAddr, "Dn2CppDateTime*")})");
            return true;
        }
        if (name == "TryParseExact" && ps.Length >= 3 && ps[0].IsString && ps[1].IsString)
        {
            var outAddr = Pop();
            for (int x = ps.Length - 2; x >= 2; x--) Pop(); // provider [, styles]
            var f = Pop(); var s = Pop();
            Push(StackKind.I4, "int32_t", $"dn2cpp_datetime_try_parse_exact({Cast(s, "Dn2CppString*")}, {Cast(f, "Dn2CppString*")}, {Cast(outAddr, "Dn2CppDateTime*")})");
            return true;
        }

        // Property getters.
        switch (name)
        {
            case "get_Ticks": { var r = Pop(); Push(StackKind.I8, "int64_t", $"({DTVal(r)}).ticks()"); return true; }
            case "get_Year": { var r = Pop(); Push(StackKind.I4, "int32_t", $"dn2cpp_datetime_year({DTVal(r)})"); return true; }
            case "get_Month": { var r = Pop(); Push(StackKind.I4, "int32_t", $"dn2cpp_datetime_month({DTVal(r)})"); return true; }
            case "get_Day": { var r = Pop(); Push(StackKind.I4, "int32_t", $"dn2cpp_datetime_day({DTVal(r)})"); return true; }
            case "get_Hour": { var r = Pop(); Push(StackKind.I4, "int32_t", $"dn2cpp_datetime_hour({DTVal(r)})"); return true; }
            case "get_Minute": { var r = Pop(); Push(StackKind.I4, "int32_t", $"dn2cpp_datetime_minute({DTVal(r)})"); return true; }
            case "get_Second": { var r = Pop(); Push(StackKind.I4, "int32_t", $"dn2cpp_datetime_second({DTVal(r)})"); return true; }
            case "get_Millisecond": { var r = Pop(); Push(StackKind.I4, "int32_t", $"dn2cpp_datetime_millisecond({DTVal(r)})"); return true; }
            case "get_DayOfWeek": { var r = Pop(); Push(StackKind.I4, "int32_t", $"dn2cpp_datetime_dayofweek({DTVal(r)})"); return true; }
            case "get_DayOfYear": { var r = Pop(); Push(StackKind.I4, "int32_t", $"dn2cpp_datetime_dayofyear({DTVal(r)})"); return true; }
            case "get_Kind": { var r = Pop(); Push(StackKind.I4, "int32_t", $"({DTVal(r)}).kind()"); return true; }
            case "get_Date":
            {
                var r = Pop(); string t = NewTemp("Dn2CppDateTime"); Emit($"{t} = {DTVal(r)};");
                Push(StackKind.Struct, "Dn2CppDateTime", $"dn2cpp_datetime_from_ticks({t}.ticks() - ({t}.ticks() % {TPD}), {t}.kind())");
                return true;
            }
            case "get_TimeOfDay":
            { var r = Pop(); Push(StackKind.Struct, "Dn2CppTimeSpan", $"dn2cpp_timespan_from_ticks(({DTVal(r)}).ticks() % {TPD})"); return true; }
        }

        // Add<Unit>(double) — truncates value*ticks toward zero (no millisecond rounding).
        string? addUnit = name switch
        {
            "AddDays" => "864000000000LL", "AddHours" => "36000000000LL",
            "AddMinutes" => "600000000LL", "AddSeconds" => "10000000LL",
            "AddMilliseconds" => "10000LL", _ => null,
        };
        if (addUnit is not null && ps.Length == 1)
        {
            var b = Pop(); var a = Pop();
            Push(StackKind.Struct, "Dn2CppDateTime", $"dn2cpp_datetime_add_unit({DTVal(a)}, (double)({b.Expr}), {addUnit})");
            return true;
        }

        // Other instance methods.
        switch (name)
        {
            case "AddTicks" when ps.Length == 1:
            { var b = Pop(); var a = Pop(); Push(StackKind.Struct, "Dn2CppDateTime", $"dn2cpp_datetime_add_ticks({DTVal(a)}, (int64_t)({b.Expr}))"); return true; }
            case "AddMonths" when ps.Length == 1:
            { var b = Pop(); var a = Pop(); Push(StackKind.Struct, "Dn2CppDateTime", $"dn2cpp_datetime_add_months({DTVal(a)}, (int32_t)({b.Expr}))"); return true; }
            case "AddYears" when ps.Length == 1:
            { var b = Pop(); var a = Pop(); Push(StackKind.Struct, "Dn2CppDateTime", $"dn2cpp_datetime_add_years({DTVal(a)}, (int32_t)({b.Expr}))"); return true; }
            case "Add" when ps.Length == 1 && IsTimeSpan(ps[0]):
            { var b = Pop(); var a = Pop(); Push(StackKind.Struct, "Dn2CppDateTime", $"dn2cpp_datetime_add_ticks({DTVal(a)}, ({TSVal(b)}).ticks)"); return true; }
            case "Subtract" when ps.Length == 1 && IsTimeSpan(ps[0]):
            { var b = Pop(); var a = Pop(); Push(StackKind.Struct, "Dn2CppDateTime", $"dn2cpp_datetime_add_ticks({DTVal(a)}, -({TSVal(b)}).ticks)"); return true; }
            case "Subtract" when ps.Length == 1 && IsDateTime(ps[0]):
            { var b = Pop(); var a = Pop(); Push(StackKind.Struct, "Dn2CppTimeSpan", $"dn2cpp_timespan_from_ticks(({DTVal(a)}).ticks() - ({DTVal(b)}).ticks())"); return true; }
            case "CompareTo" when ps.Length == 1 && IsDateTime(ps[0]):
            { var b = Pop(); var a = Pop(); Push(StackKind.I4, "int32_t", $"dn2cpp_datetime_cmp({DTVal(a)}, {DTVal(b)})"); return true; }
            case "Equals" when ps.Length == 1 && IsDateTime(ps[0]):
            { var b = Pop(); var a = Pop(); Push(StackKind.I4, "int32_t", $"(dn2cpp_datetime_cmp({DTVal(a)}, {DTVal(b)}) == 0 ? 1 : 0)"); return true; }
            case "GetHashCode" when ps.Length == 0:
            { var a = Pop(); Push(StackKind.I4, "int32_t", $"dn2cpp_datetime_hash({DTVal(a)})"); return true; }
            // IsAmbiguousDaylightSavingTime() — an internal predicate TimeZoneInfo's real BCL
            // IL reads to disambiguate a wall-clock instant landing in the fall-back DST
            // overlap. Real .NET reads DateTime's KindLocalAmbiguousDst flag (both high
            // _dateData bits set), set ONLY inside TimeZoneInfo.ConvertTime's ambiguous-time
            // path; Dn2CppDateTime.kind is one of {Unspecified=0, Utc=1, Local=2} and never
            // carries that flag, so the predicate is always false.
            case "IsAmbiguousDaylightSavingTime" when ps.Length == 0:
            { Pop(); Push(StackKind.I4, "int32_t", "0"); return true; }
            // Time-zone conversion against the host's local zone. Utc/Unspecified
            // -> Local and Local/Unspecified -> Utc; an already-matching Kind is a no-op.
            case "ToLocalTime" when ps.Length == 0:
            { var a = Pop(); Push(StackKind.Struct, "Dn2CppDateTime", $"dn2cpp_datetime_to_local({DTVal(a)})"); return true; }
            case "ToUniversalTime" when ps.Length == 0:
            { var a = Pop(); Push(StackKind.Struct, "Dn2CppDateTime", $"dn2cpp_datetime_to_universal({DTVal(a)})"); return true; }
            // Windows FILETIME round trip (inverse of FromFileTime[Utc] above).
            // ToFileTimeUtc converts a Local value to universal first (Unspecified passes
            // through as-is — see dn2cpp_datetime_to_file_time_utc's comment); ToFileTime
            // always goes through ToUniversalTime first (Unspecified treated as Local there),
            // matching the real BCL's own composition.
            case "ToFileTimeUtc" when ps.Length == 0:
            { var a = Pop(); Push(StackKind.I8, "int64_t", $"dn2cpp_datetime_to_file_time_utc({DTVal(a)})"); return true; }
            case "ToFileTime" when ps.Length == 0:
            { var a = Pop(); Push(StackKind.I8, "int64_t", $"dn2cpp_datetime_to_file_time_utc(dn2cpp_datetime_to_universal({DTVal(a)}))"); return true; }
            // ISpanFormattable.TryFormat(Span<char>, out charsWritten, ROS<char> format,
            // IFormatProvider): render into the destination span (fits -> copy + count +
            // true; too short -> false). An empty/default format takes the invariant
            // default representation; a standard format string routes through
            // dn2cpp_datetime_format (which throws loudly on one it does not cover).
            case "TryFormat" when ps is [var d0, { Kind: TypeKind.ByRef }, var d2, _]
                && IsSpanOfPrimitive(d0, PrimitiveTypeCode.Char) && IsReadOnlySpanChar(d2):
            {
                Pop();                                            // IFormatProvider — invariant
                string fmtSpan = SpanValue(Pop(), CppTypes.Of(ps[2]));
                string wrote = Cast(Pop(), "int32_t*");
                string dPtr = SpanPtr(Pop(), CppTypes.Of(ps[0]));
                var recv = Pop();
                string sv = NewTemp("Dn2CppString*");
                Emit($"{sv} = {fmtSpan}.f__length == 0 ? dn2cpp_datetime_to_string({DTVal(recv)}) " +
                     $": dn2cpp_datetime_format({DTVal(recv)}, dn2cpp_string_from_chars((const char16_t*){fmtSpan}.f__reference, {fmtSpan}.f__length));");
                Push(StackKind.I4, "int32_t",
                    $"dn2cpp_string_try_copy_to_span({sv}, (char16_t*){dPtr}->f__reference, {dPtr}->f__length, {wrote})");
                return true;
            }
            case "ToString":
            {
                // default + provider (invariant) adds the format string.
                if (ps.Length == 0)
                { var a = Pop(); Push(StackKind.Ref, "Dn2CppString*", $"dn2cpp_datetime_to_string({DTVal(a)})"); return true; }
                if (ps is [{ IsString: true }]) // ToString(string format) -> invariant
                { var f = Pop(); var a = Pop(); Push(StackKind.Ref, "Dn2CppString*", $"dn2cpp_datetime_format({DTVal(a)}, {Cast(f, "Dn2CppString*")})"); return true; }
                if (ps is [{ IsString: true }, _]) // ToString(string format, IFormatProvider)
                { Pop(); var f = Pop(); var a = Pop(); Push(StackKind.Ref, "Dn2CppString*", $"dn2cpp_datetime_format({DTVal(a)}, {Cast(f, "Dn2CppString*")})"); return true; }
                if (ps is [{ IsString: false }]) // ToString(IFormatProvider) -> invariant default
                { Pop(); var a = Pop(); Push(StackKind.Ref, "Dn2CppString*", $"dn2cpp_datetime_to_string({DTVal(a)})"); return true; }
                return false;
            }
        }

        // Operators.
        if (name == "op_Addition" && ps.Length == 2 && IsDateTime(ps[0]) && IsTimeSpan(ps[1]))
        { var b = Pop(); var a = Pop(); Push(StackKind.Struct, "Dn2CppDateTime", $"dn2cpp_datetime_add_ticks({DTVal(a)}, ({TSVal(b)}).ticks)"); return true; }
        if (name == "op_Subtraction" && ps.Length == 2 && IsDateTime(ps[0]) && IsTimeSpan(ps[1]))
        { var b = Pop(); var a = Pop(); Push(StackKind.Struct, "Dn2CppDateTime", $"dn2cpp_datetime_add_ticks({DTVal(a)}, -({TSVal(b)}).ticks)"); return true; }
        if (name == "op_Subtraction" && ps.Length == 2 && IsDateTime(ps[0]) && IsDateTime(ps[1]))
        { var b = Pop(); var a = Pop(); Push(StackKind.Struct, "Dn2CppTimeSpan", $"dn2cpp_timespan_from_ticks(({DTVal(a)}).ticks() - ({DTVal(b)}).ticks())"); return true; }
        string? dtCmp = name switch
        {
            "op_Equality" => "== 0", "op_Inequality" => "!= 0",
            "op_LessThan" => "< 0", "op_GreaterThan" => "> 0",
            "op_LessThanOrEqual" => "<= 0", "op_GreaterThanOrEqual" => ">= 0",
            _ => null,
        };
        if (dtCmp is not null && ps.Length == 2 && IsDateTime(ps[0]) && IsDateTime(ps[1]))
        {
            var b = Pop(); var a = Pop();
            Push(StackKind.I4, "int32_t", $"(dn2cpp_datetime_cmp({DTVal(a)}, {DTVal(b)}) {dtCmp} ? 1 : 0)");
            return true;
        }

        return false;
    }

    /// <summary>Pops a DateOnly ctor's args (pushed left-to-right) and returns the C++
    /// expression constructing the Dn2CppDateOnly. Only (year, month, day) is supported;
    /// the Calendar overload is a carve-out (NotSupported).</summary>
    private string EmitDateOnlyCtorExpr(MethodSignature<TypeDesc> sig)
    {
        var ps = sig.ParameterTypes;
        var args = new StackEntry[ps.Length];
        for (int i = ps.Length - 1; i >= 0; i--) args[i] = Pop();
        string I(int i) => $"(int32_t)({args[i].Expr})";
        return ps.Length switch
        {
            3 => $"dn2cpp_dateonly_ymd({I(0)}, {I(1)}, {I(2)})",
            _ => throw new NotSupportedException(
                $"{Method.DeclaringClass.FullName}.{Method.Name}: DateOnly ctor with {ps.Length} args is not supported"),
        };
    }

    /// <summary>Lowers a System.DateOnly member to its Dn2CppDateOnly runtime helper.
    /// Returns false if unmapped so the caller reports a clear error.</summary>
    private bool TryEmitDateOnlyIntrinsic(string name, MethodSignature<TypeDesc> sig)
    {
        var ps = sig.ParameterTypes;

        //.ctor — the ldloca+call (address-init) form (the newobj form is in TranslateNewobj).
        if (name == ".ctor")
        {
            string expr = EmitDateOnlyCtorExpr(sig);
            var addr = Pop();
            Emit($"*({Cast(addr, "Dn2CppDateOnly*")}) = {expr};");
            return true;
        }

        // Statics (no receiver to pop) — must precede the instance getters below. MinValue /
        // MaxValue are static *properties* on DateOnly (unlike DateTime's static fields):
        // MinValue = 0001-01-01 (dayNumber 0), MaxValue = 9999-12-31 (dayNumber 3652058).
        switch (name)
        {
            case "get_MinValue" when ps.Length == 0:
                Push(StackKind.Struct, "Dn2CppDateOnly", "dn2cpp_dateonly_from_daynum(0)"); return true;
            case "get_MaxValue" when ps.Length == 0:
                Push(StackKind.Struct, "Dn2CppDateOnly", "dn2cpp_dateonly_from_daynum(3652058)"); return true;
            case "FromDayNumber" when ps.Length == 1:
            { var a = Pop(); Push(StackKind.Struct, "Dn2CppDateOnly", $"dn2cpp_dateonly_from_daynum((int32_t)({a.Expr}))"); return true; }
            case "FromDateTime" when ps.Length == 1 && IsDateTime(ps[0]):
            { var a = Pop(); Push(StackKind.Struct, "Dn2CppDateOnly", $"dn2cpp_dateonly_from_datetime({DTVal(a)})"); return true; }
        }

        // Parse / TryParse / ParseExact / TryParseExact — static, string overloads only (the
        // ReadOnlySpan<char> / format-array overloads stay carve-outs). The trailing
        // IFormatProvider / DateTimeStyles args are dropped (invariant); the out result of
        // the Try* forms is the last param (a ByRef).
        if (name == "Parse" && ps.Length >= 1 && ps[0].IsString)
        {
            for (int x = ps.Length - 1; x >= 1; x--) Pop();
            var s = Pop();
            Push(StackKind.Struct, "Dn2CppDateOnly", $"dn2cpp_dateonly_parse({Cast(s, "Dn2CppString*")})");
            return true;
        }
        if (name == "ParseExact" && ps.Length >= 2 && ps[0].IsString && ps[1].IsString)
        {
            for (int x = ps.Length - 1; x >= 2; x--) Pop(); // provider [, styles]
            var f = Pop(); var s = Pop();
            Push(StackKind.Struct, "Dn2CppDateOnly", $"dn2cpp_dateonly_parse_exact({Cast(s, "Dn2CppString*")}, {Cast(f, "Dn2CppString*")})");
            return true;
        }
        if (name == "TryParse" && ps.Length >= 2 && ps[0].IsString)
        {
            var outAddr = Pop();
            for (int x = ps.Length - 2; x >= 1; x--) Pop(); // provider [, styles]
            var s = Pop();
            Push(StackKind.I4, "int32_t", $"dn2cpp_dateonly_try_parse({Cast(s, "Dn2CppString*")}, {Cast(outAddr, "Dn2CppDateOnly*")})");
            return true;
        }
        if (name == "TryParseExact" && ps.Length >= 3 && ps[0].IsString && ps[1].IsString)
        {
            var outAddr = Pop();
            for (int x = ps.Length - 2; x >= 2; x--) Pop(); // provider [, styles]
            var f = Pop(); var s = Pop();
            Push(StackKind.I4, "int32_t", $"dn2cpp_dateonly_try_parse_exact({Cast(s, "Dn2CppString*")}, {Cast(f, "Dn2CppString*")}, {Cast(outAddr, "Dn2CppDateOnly*")})");
            return true;
        }

        // Property getters (pop the receiver).
        switch (name)
        {
            case "get_Year": { var r = Pop(); Push(StackKind.I4, "int32_t", $"dn2cpp_dateonly_year({DOnlyVal(r)})"); return true; }
            case "get_Month": { var r = Pop(); Push(StackKind.I4, "int32_t", $"dn2cpp_dateonly_month({DOnlyVal(r)})"); return true; }
            case "get_Day": { var r = Pop(); Push(StackKind.I4, "int32_t", $"dn2cpp_dateonly_day({DOnlyVal(r)})"); return true; }
            case "get_DayOfWeek": { var r = Pop(); Push(StackKind.I4, "int32_t", $"dn2cpp_dateonly_dayofweek({DOnlyVal(r)})"); return true; }
            case "get_DayOfYear": { var r = Pop(); Push(StackKind.I4, "int32_t", $"dn2cpp_dateonly_dayofyear({DOnlyVal(r)})"); return true; }
            case "get_DayNumber": { var r = Pop(); Push(StackKind.I4, "int32_t", $"({DOnlyVal(r)}).dayNumber"); return true; }
        }

        // Instance methods.
        switch (name)
        {
            case "AddDays" when ps.Length == 1:
            { var b = Pop(); var a = Pop(); Push(StackKind.Struct, "Dn2CppDateOnly", $"dn2cpp_dateonly_add_days({DOnlyVal(a)}, (int32_t)({b.Expr}))"); return true; }
            case "AddMonths" when ps.Length == 1:
            { var b = Pop(); var a = Pop(); Push(StackKind.Struct, "Dn2CppDateOnly", $"dn2cpp_dateonly_add_months({DOnlyVal(a)}, (int32_t)({b.Expr}))"); return true; }
            case "AddYears" when ps.Length == 1:
            { var b = Pop(); var a = Pop(); Push(StackKind.Struct, "Dn2CppDateOnly", $"dn2cpp_dateonly_add_years({DOnlyVal(a)}, (int32_t)({b.Expr}))"); return true; }
            // ToDateTime(TimeOnly[, DateTimeKind]) — combine the date and time at dayNumber*TPD
            // + time ticks. The kind defaults to Unspecified (0).
            case "ToDateTime" when ps.Length == 1 && IsTimeOnly(ps[0]):
            { var b = Pop(); var a = Pop(); Push(StackKind.Struct, "Dn2CppDateTime", $"dn2cpp_datetime_from_ticks((int64_t)({DOnlyVal(a)}).dayNumber * 864000000000LL + ({TOnlyVal(b)}).ticks, 0)"); return true; }
            case "ToDateTime" when ps.Length == 2 && IsTimeOnly(ps[0]) && IsDateTimeKind(ps[1]):
            { var k = Pop(); var b = Pop(); var a = Pop(); Push(StackKind.Struct, "Dn2CppDateTime", $"dn2cpp_datetime_from_ticks((int64_t)({DOnlyVal(a)}).dayNumber * 864000000000LL + ({TOnlyVal(b)}).ticks, (int32_t)({k.Expr}))"); return true; }
            case "CompareTo" when ps.Length == 1 && IsDateOnly(ps[0]):
            { var b = Pop(); var a = Pop(); Push(StackKind.I4, "int32_t", $"dn2cpp_dateonly_cmp({DOnlyVal(a)}, {DOnlyVal(b)})"); return true; }
            case "Equals" when ps.Length == 1 && IsDateOnly(ps[0]):
            { var b = Pop(); var a = Pop(); Push(StackKind.I4, "int32_t", $"(dn2cpp_dateonly_cmp({DOnlyVal(a)}, {DOnlyVal(b)}) == 0 ? 1 : 0)"); return true; }
            case "GetHashCode" when ps.Length == 0:
            { var a = Pop(); Push(StackKind.I4, "int32_t", $"dn2cpp_dateonly_hash({DOnlyVal(a)})"); return true; }
            // TryFormat(Span<char>, out charsWritten, ROS<char> format, IFormatProvider) —
            // render into the span (too short -> false); empty format is the invariant
            // default, a standard format routes through dn2cpp_dateonly_format.
            case "TryFormat" when ps is [var d0, { Kind: TypeKind.ByRef }, var d2, _]
                && IsSpanOfPrimitive(d0, PrimitiveTypeCode.Char) && IsReadOnlySpanChar(d2):
            {
                Pop();                                            // IFormatProvider — invariant
                string fmtSpan = SpanValue(Pop(), CppTypes.Of(ps[2]));
                string wrote = Cast(Pop(), "int32_t*");
                string dPtr = SpanPtr(Pop(), CppTypes.Of(ps[0]));
                var recv = Pop();
                string sv = NewTemp("Dn2CppString*");
                Emit($"{sv} = {fmtSpan}.f__length == 0 ? dn2cpp_dateonly_to_string({DOnlyVal(recv)}) " +
                     $": dn2cpp_dateonly_format({DOnlyVal(recv)}, dn2cpp_string_from_chars((const char16_t*){fmtSpan}.f__reference, {fmtSpan}.f__length));");
                Push(StackKind.I4, "int32_t",
                    $"dn2cpp_string_try_copy_to_span({sv}, (char16_t*){dPtr}->f__reference, {dPtr}->f__length, {wrote})");
                return true;
            }
            case "ToString":
            {
                if (ps.Length == 0)
                { var a = Pop(); Push(StackKind.Ref, "Dn2CppString*", $"dn2cpp_dateonly_to_string({DOnlyVal(a)})"); return true; }
                if (ps is [{ IsString: true }]) // ToString(string format) -> invariant
                { var f = Pop(); var a = Pop(); Push(StackKind.Ref, "Dn2CppString*", $"dn2cpp_dateonly_format({DOnlyVal(a)}, {Cast(f, "Dn2CppString*")})"); return true; }
                if (ps is [{ IsString: true }, _]) // ToString(string format, IFormatProvider)
                { Pop(); var f = Pop(); var a = Pop(); Push(StackKind.Ref, "Dn2CppString*", $"dn2cpp_dateonly_format({DOnlyVal(a)}, {Cast(f, "Dn2CppString*")})"); return true; }
                if (ps is [{ IsString: false }]) // ToString(IFormatProvider) -> invariant default
                { Pop(); var a = Pop(); Push(StackKind.Ref, "Dn2CppString*", $"dn2cpp_dateonly_to_string({DOnlyVal(a)})"); return true; }
                return false;
            }
        }

        // Operators (DateOnly defines comparison operators only — no arithmetic operators).
        string? doCmp = name switch
        {
            "op_Equality" => "== 0", "op_Inequality" => "!= 0",
            "op_LessThan" => "< 0", "op_GreaterThan" => "> 0",
            "op_LessThanOrEqual" => "<= 0", "op_GreaterThanOrEqual" => ">= 0",
            _ => null,
        };
        if (doCmp is not null && ps.Length == 2 && IsDateOnly(ps[0]) && IsDateOnly(ps[1]))
        {
            var b = Pop(); var a = Pop();
            Push(StackKind.I4, "int32_t", $"(dn2cpp_dateonly_cmp({DOnlyVal(a)}, {DOnlyVal(b)}) {doCmp} ? 1 : 0)");
            return true;
        }

        return false;
    }

    /// <summary>Pops a TimeOnly ctor's args and returns the C++ expression constructing the
    /// Dn2CppTimeOnly. (long ticks) / (hour, minute) / (hour, minute, second) / (…, millisecond);
    /// the (…, microsecond).NET 7+ overload is a carve-out (NotSupported).</summary>
    private string EmitTimeOnlyCtorExpr(MethodSignature<TypeDesc> sig)
    {
        var ps = sig.ParameterTypes;
        var args = new StackEntry[ps.Length];
        for (int i = ps.Length - 1; i >= 0; i--) args[i] = Pop();
        string I(int i) => $"(int32_t)({args[i].Expr})";
        return ps.Length switch
        {
            1 => $"dn2cpp_timeonly_from_ticks((int64_t)({args[0].Expr}))",
            2 => $"dn2cpp_timeonly_hms({I(0)}, {I(1)}, 0)",
            3 => $"dn2cpp_timeonly_hms({I(0)}, {I(1)}, {I(2)})",
            4 => $"dn2cpp_timeonly_hmsms({I(0)}, {I(1)}, {I(2)}, {I(3)})",
            _ => throw new NotSupportedException(
                $"{Method.DeclaringClass.FullName}.{Method.Name}: TimeOnly ctor with {ps.Length} args is not supported"),
        };
    }

    /// <summary>Lowers a System.TimeOnly member to its Dn2CppTimeOnly runtime helper.
    /// Returns false if unmapped so the caller reports a clear error.</summary>
    private bool TryEmitTimeOnlyIntrinsic(string name, MethodSignature<TypeDesc> sig)
    {
        var ps = sig.ParameterTypes;

        //.ctor — the ldloca+call (address-init) form (the newobj form is in TranslateNewobj).
        if (name == ".ctor")
        {
            string expr = EmitTimeOnlyCtorExpr(sig);
            var addr = Pop();
            Emit($"*({Cast(addr, "Dn2CppTimeOnly*")}) = {expr};");
            return true;
        }

        // Statics (no receiver to pop) — must precede the instance getters below. MinValue /
        // MaxValue are static properties: MinValue = 00:00 (ticks 0), MaxValue = 23:59:59.9999999
        // (ticks 863999999999 = ticks-per-day - 1).
        switch (name)
        {
            case "get_MinValue" when ps.Length == 0:
                Push(StackKind.Struct, "Dn2CppTimeOnly", "dn2cpp_timeonly_from_ticks(0)"); return true;
            case "get_MaxValue" when ps.Length == 0:
                Push(StackKind.Struct, "Dn2CppTimeOnly", "dn2cpp_timeonly_from_ticks(863999999999LL)"); return true;
            case "FromTimeSpan" when ps.Length == 1 && IsTimeSpan(ps[0]):
            { var a = Pop(); Push(StackKind.Struct, "Dn2CppTimeOnly", $"dn2cpp_timeonly_from_timespan({TSVal(a)})"); return true; }
            case "FromDateTime" when ps.Length == 1 && IsDateTime(ps[0]):
            { var a = Pop(); Push(StackKind.Struct, "Dn2CppTimeOnly", $"dn2cpp_timeonly_from_datetime({DTVal(a)})"); return true; }
        }

        // Parse / TryParse / ParseExact / TryParseExact — static, string overloads only. Trailing
        // IFormatProvider / DateTimeStyles args are dropped (invariant); the Try* out is last.
        if (name == "Parse" && ps.Length >= 1 && ps[0].IsString)
        {
            for (int x = ps.Length - 1; x >= 1; x--) Pop();
            var s = Pop();
            Push(StackKind.Struct, "Dn2CppTimeOnly", $"dn2cpp_timeonly_parse({Cast(s, "Dn2CppString*")})");
            return true;
        }
        if (name == "ParseExact" && ps.Length >= 2 && ps[0].IsString && ps[1].IsString)
        {
            for (int x = ps.Length - 1; x >= 2; x--) Pop();
            var f = Pop(); var s = Pop();
            Push(StackKind.Struct, "Dn2CppTimeOnly", $"dn2cpp_timeonly_parse_exact({Cast(s, "Dn2CppString*")}, {Cast(f, "Dn2CppString*")})");
            return true;
        }
        if (name == "TryParse" && ps.Length >= 2 && ps[0].IsString)
        {
            var outAddr = Pop();
            for (int x = ps.Length - 2; x >= 1; x--) Pop();
            var s = Pop();
            Push(StackKind.I4, "int32_t", $"dn2cpp_timeonly_try_parse({Cast(s, "Dn2CppString*")}, {Cast(outAddr, "Dn2CppTimeOnly*")})");
            return true;
        }
        if (name == "TryParseExact" && ps.Length >= 3 && ps[0].IsString && ps[1].IsString)
        {
            var outAddr = Pop();
            for (int x = ps.Length - 2; x >= 2; x--) Pop();
            var f = Pop(); var s = Pop();
            Push(StackKind.I4, "int32_t", $"dn2cpp_timeonly_try_parse_exact({Cast(s, "Dn2CppString*")}, {Cast(f, "Dn2CppString*")}, {Cast(outAddr, "Dn2CppTimeOnly*")})");
            return true;
        }

        // Property getters (pop the receiver).
        switch (name)
        {
            case "get_Ticks": { var r = Pop(); Push(StackKind.I8, "int64_t", $"({TOnlyVal(r)}).ticks"); return true; }
            case "get_Hour": { var r = Pop(); Push(StackKind.I4, "int32_t", $"dn2cpp_timeonly_hour({TOnlyVal(r)})"); return true; }
            case "get_Minute": { var r = Pop(); Push(StackKind.I4, "int32_t", $"dn2cpp_timeonly_minute({TOnlyVal(r)})"); return true; }
            case "get_Second": { var r = Pop(); Push(StackKind.I4, "int32_t", $"dn2cpp_timeonly_second({TOnlyVal(r)})"); return true; }
            case "get_Millisecond": { var r = Pop(); Push(StackKind.I4, "int32_t", $"dn2cpp_timeonly_millisecond({TOnlyVal(r)})"); return true; }
            // Microsecond = (int)((Ticks / TicksPerMicrosecond) % 1000), TicksPerMicrosecond = 10.
            case "get_Microsecond": { var r = Pop(); Push(StackKind.I4, "int32_t", $"(int32_t)((({TOnlyVal(r)}).ticks / 10) % 1000)"); return true; }
        }

        // Add<Unit>(double) — wraps within the day (the `out int wrappedDays` overload is a carve-out).
        string? addUnit = name switch { "AddHours" => "36000000000LL", "AddMinutes" => "600000000LL", _ => null };
        if (addUnit is not null && ps.Length == 1)
        {
            var b = Pop(); var a = Pop();
            Push(StackKind.Struct, "Dn2CppTimeOnly", $"dn2cpp_timeonly_add_unit({TOnlyVal(a)}, (double)({b.Expr}), {addUnit})");
            return true;
        }

        // Instance methods.
        switch (name)
        {
            case "Add" when ps.Length == 1 && IsTimeSpan(ps[0]):
            { var b = Pop(); var a = Pop(); Push(StackKind.Struct, "Dn2CppTimeOnly", $"dn2cpp_timeonly_add({TOnlyVal(a)}, ({TSVal(b)}).ticks)"); return true; }
            case "ToTimeSpan" when ps.Length == 0:
            { var a = Pop(); Push(StackKind.Struct, "Dn2CppTimeSpan", $"dn2cpp_timespan_from_ticks(({TOnlyVal(a)}).ticks)"); return true; }
            case "CompareTo" when ps.Length == 1 && IsTimeOnly(ps[0]):
            { var b = Pop(); var a = Pop(); Push(StackKind.I4, "int32_t", $"dn2cpp_timeonly_cmp({TOnlyVal(a)}, {TOnlyVal(b)})"); return true; }
            case "Equals" when ps.Length == 1 && IsTimeOnly(ps[0]):
            { var b = Pop(); var a = Pop(); Push(StackKind.I4, "int32_t", $"(dn2cpp_timeonly_cmp({TOnlyVal(a)}, {TOnlyVal(b)}) == 0 ? 1 : 0)"); return true; }
            case "GetHashCode" when ps.Length == 0:
            { var a = Pop(); Push(StackKind.I4, "int32_t", $"dn2cpp_timeonly_hash({TOnlyVal(a)})"); return true; }
            // TryFormat(Span<char>, out charsWritten, ROS<char> format, IFormatProvider) —
            // render into the span (too short -> false); empty format is the invariant
            // default, a standard format routes through dn2cpp_timeonly_format.
            case "TryFormat" when ps is [var d0, { Kind: TypeKind.ByRef }, var d2, _]
                && IsSpanOfPrimitive(d0, PrimitiveTypeCode.Char) && IsReadOnlySpanChar(d2):
            {
                Pop();                                            // IFormatProvider — invariant
                string fmtSpan = SpanValue(Pop(), CppTypes.Of(ps[2]));
                string wrote = Cast(Pop(), "int32_t*");
                string dPtr = SpanPtr(Pop(), CppTypes.Of(ps[0]));
                var recv = Pop();
                string sv = NewTemp("Dn2CppString*");
                Emit($"{sv} = {fmtSpan}.f__length == 0 ? dn2cpp_timeonly_to_string({TOnlyVal(recv)}) " +
                     $": dn2cpp_timeonly_format({TOnlyVal(recv)}, dn2cpp_string_from_chars((const char16_t*){fmtSpan}.f__reference, {fmtSpan}.f__length));");
                Push(StackKind.I4, "int32_t",
                    $"dn2cpp_string_try_copy_to_span({sv}, (char16_t*){dPtr}->f__reference, {dPtr}->f__length, {wrote})");
                return true;
            }
            case "ToString":
            {
                if (ps.Length == 0)
                { var a = Pop(); Push(StackKind.Ref, "Dn2CppString*", $"dn2cpp_timeonly_to_string({TOnlyVal(a)})"); return true; }
                if (ps is [{ IsString: true }]) // ToString(string format) -> invariant
                { var f = Pop(); var a = Pop(); Push(StackKind.Ref, "Dn2CppString*", $"dn2cpp_timeonly_format({TOnlyVal(a)}, {Cast(f, "Dn2CppString*")})"); return true; }
                if (ps is [{ IsString: true }, _]) // ToString(string format, IFormatProvider)
                { Pop(); var f = Pop(); var a = Pop(); Push(StackKind.Ref, "Dn2CppString*", $"dn2cpp_timeonly_format({TOnlyVal(a)}, {Cast(f, "Dn2CppString*")})"); return true; }
                if (ps is [{ IsString: false }]) // ToString(IFormatProvider) -> invariant default
                { Pop(); var a = Pop(); Push(StackKind.Ref, "Dn2CppString*", $"dn2cpp_timeonly_to_string({TOnlyVal(a)})"); return true; }
                return false;
            }
        }

        // TimeOnly - TimeOnly = the wrapped elapsed TimeSpan ((a-b+day)%day).
        if (name == "op_Subtraction" && ps.Length == 2 && IsTimeOnly(ps[0]) && IsTimeOnly(ps[1]))
        { var b = Pop(); var a = Pop(); Push(StackKind.Struct, "Dn2CppTimeSpan", $"dn2cpp_timeonly_diff({TOnlyVal(a)}, {TOnlyVal(b)})"); return true; }

        // Comparison operators.
        string? toCmp = name switch
        {
            "op_Equality" => "== 0", "op_Inequality" => "!= 0",
            "op_LessThan" => "< 0", "op_GreaterThan" => "> 0",
            "op_LessThanOrEqual" => "<= 0", "op_GreaterThanOrEqual" => ">= 0",
            _ => null,
        };
        if (toCmp is not null && ps.Length == 2 && IsTimeOnly(ps[0]) && IsTimeOnly(ps[1]))
        {
            var b = Pop(); var a = Pop();
            Push(StackKind.I4, "int32_t", $"(dn2cpp_timeonly_cmp({TOnlyVal(a)}, {TOnlyVal(b)}) {toCmp} ? 1 : 0)");
            return true;
        }

        return false;
    }

    /// <summary>Builds the C++ expression for a System.DateTimeOffset constructor,
    /// popping the args. The clock ticks come from the date/time
    /// components (or a DateTime/ticks operand) and the offset from the trailing
    /// TimeSpan in whole minutes. The.NET Kind/offset consistency validation is a
    /// carve-out (relaxed).</summary>
    private string EmitDateTimeOffsetCtorExpr(MethodSignature<TypeDesc> sig)
    {
        var ps = sig.ParameterTypes;
        var args = new StackEntry[ps.Length];
        for (int i = ps.Length - 1; i >= 0; i--) args[i] = Pop();
        string I(int i) => $"(int32_t)({args[i].Expr})";
        // The trailing TimeSpan offset as whole minutes (600000000 ticks = 1 minute).
        string OffMin(int i) => $"(int32_t)(({TSVal(args[i])}).ticks / 600000000LL)";

        // new DateTimeOffset(DateTime) — the offset is derived from the DateTime's Kind.
        if (ps.Length == 1 && IsDateTime(ps[0]))
            return $"dn2cpp_datetimeoffset_from_datetime({DTVal(args[0])})";
        // new DateTimeOffset(DateTime, TimeSpan)
        if (ps.Length == 2 && IsDateTime(ps[0]) && IsTimeSpan(ps[1]))
            return $"dn2cpp_datetimeoffset_from_dt_offset({DTVal(args[0])}, {TSVal(args[1])})";
        // new DateTimeOffset(long ticks, TimeSpan)
        if (ps.Length == 2 && ps[0] is { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Int64 } && IsTimeSpan(ps[1]))
            return $"dn2cpp_datetimeoffset_make((int64_t)({args[0].Expr}), {OffMin(1)})";
        // new DateTimeOffset(year, month, day, hour, minute, second, TimeSpan)
        if (ps.Length == 7 && IsTimeSpan(ps[6]))
            return $"dn2cpp_datetimeoffset_make(dn2cpp_datetime_ymdhms({I(0)}, {I(1)}, {I(2)}, {I(3)}, {I(4)}, {I(5)}, 0).ticks(), {OffMin(6)})";
        // new DateTimeOffset(year, month, day, hour, minute, second, millisecond, TimeSpan)
        if (ps.Length == 8 && IsTimeSpan(ps[7]))
            return $"dn2cpp_datetimeoffset_make(dn2cpp_datetime_ymdhmsms({I(0)}, {I(1)}, {I(2)}, {I(3)}, {I(4)}, {I(5)}, {I(6)}, 0).ticks(), {OffMin(7)})";
        throw new NotSupportedException(
            $"{Method.DeclaringClass.FullName}.{Method.Name}: DateTimeOffset ctor with {ps.Length} args is not supported");
    }

    /// <summary>Lowers a System.DateTimeOffset member to its Dn2CppDateTimeOffset runtime
    /// helper. Returns false if unmapped so the caller reports a clear error.</summary>
    private bool TryEmitDateTimeOffsetIntrinsic(string name, MethodSignature<TypeDesc> sig)
    {
        var ps = sig.ParameterTypes;
        const string TPD = "864000000000LL";

        //.ctor — the ldloca+call (address-init) form (the newobj form is in TranslateNewobj).
        if (name == ".ctor")
        {
            string expr = EmitDateTimeOffsetCtorExpr(sig);
            var addr = Pop();
            Emit($"*({Cast(addr, "Dn2CppDateTimeOffset*")}) = {expr};");
            return true;
        }

        // Statics (no receiver to pop) — must precede the instance getters below.
        switch (name)
        {
            // Wall clock. Now = local clock + host offset; UtcNow = UTC instant at offset 0.
            case "get_Now" when ps.Length == 0:
                Push(StackKind.Struct, "Dn2CppDateTimeOffset", "dn2cpp_datetimeoffset_now()"); return true;
            case "get_UtcNow" when ps.Length == 0:
                Push(StackKind.Struct, "Dn2CppDateTimeOffset", "dn2cpp_datetimeoffset_utc_now()"); return true;
            case "FromUnixTimeSeconds" when ps.Length == 1:
            { var a = Pop(); Push(StackKind.Struct, "Dn2CppDateTimeOffset", $"dn2cpp_datetimeoffset_from_unix_seconds((int64_t)({a.Expr}))"); return true; }
            case "FromUnixTimeMilliseconds" when ps.Length == 1:
            { var a = Pop(); Push(StackKind.Struct, "Dn2CppDateTimeOffset", $"dn2cpp_datetimeoffset_from_unix_millis((int64_t)({a.Expr}))"); return true; }
            // DateTimeOffset.FromFileTime = ToLocalTime(DateTime.FromFileTimeUtc(fileTime), true)
            // in the real BCL: the FILETIME instant projected onto the host's local wall clock,
            // carrying that clock's offset (dn2cpp_datetimeoffset_from_datetime computes the
            // offset for a Local-kind DateTime the same way `new DateTimeOffset(DateTime)` does).
            case "FromFileTime" when ps.Length == 1:
            {
                var a = Pop();
                Push(StackKind.Struct, "Dn2CppDateTimeOffset",
                    $"dn2cpp_datetimeoffset_from_datetime(dn2cpp_datetime_to_local("
                    + $"dn2cpp_datetime_from_file_time_utc((int64_t)({a.Expr}))))");
                return true;
            }
            case "Compare" when ps.Length == 2:
            { var b = Pop(); var a = Pop(); Push(StackKind.I4, "int32_t", $"dn2cpp_datetimeoffset_cmp({DTOVal(a)}, {DTOVal(b)})"); return true; }
            case "op_Implicit" when ps.Length == 1 && IsDateTime(ps[0]):
            { var a = Pop(); Push(StackKind.Struct, "Dn2CppDateTimeOffset", $"dn2cpp_datetimeoffset_from_datetime({DTVal(a)})"); return true; }
        }

        // Parse / TryParse / ParseExact / TryParseExact — static, string
        // overloads only (the ReadOnlySpan<char> and format-array overloads stay carve-outs,
        // guarded by IsString). The trailing IFormatProvider / DateTimeStyles args are dropped
        // (invariant); the out result of the Try* forms is the last param (a ByRef).
        if (name == "Parse" && ps.Length >= 1 && ps[0].IsString)
        {
            for (int x = ps.Length - 1; x >= 1; x--) Pop();
            var s = Pop();
            Push(StackKind.Struct, "Dn2CppDateTimeOffset", $"dn2cpp_datetimeoffset_parse({Cast(s, "Dn2CppString*")})");
            return true;
        }
        if (name == "ParseExact" && ps.Length >= 2 && ps[0].IsString && ps[1].IsString)
        {
            for (int x = ps.Length - 1; x >= 2; x--) Pop(); // provider [, styles]
            var f = Pop(); var s = Pop();
            Push(StackKind.Struct, "Dn2CppDateTimeOffset", $"dn2cpp_datetimeoffset_parse_exact({Cast(s, "Dn2CppString*")}, {Cast(f, "Dn2CppString*")})");
            return true;
        }
        if (name == "TryParse" && ps.Length >= 2 && ps[0].IsString)
        {
            var outAddr = Pop();
            for (int x = ps.Length - 2; x >= 1; x--) Pop(); // provider [, styles]
            var s = Pop();
            Push(StackKind.I4, "int32_t", $"dn2cpp_datetimeoffset_try_parse({Cast(s, "Dn2CppString*")}, {Cast(outAddr, "Dn2CppDateTimeOffset*")})");
            return true;
        }
        if (name == "TryParseExact" && ps.Length >= 3 && ps[0].IsString && ps[1].IsString)
        {
            var outAddr = Pop();
            for (int x = ps.Length - 2; x >= 2; x--) Pop(); // provider [, styles]
            var f = Pop(); var s = Pop();
            Push(StackKind.I4, "int32_t", $"dn2cpp_datetimeoffset_try_parse_exact({Cast(s, "Dn2CppString*")}, {Cast(f, "Dn2CppString*")}, {Cast(outAddr, "Dn2CppDateTimeOffset*")})");
            return true;
        }
        // Span overloads (ReadOnlySpan<char> input + single format span): materialize both
        // spans to strings and reuse the string parser; provider/styles dropped (invariant).
        if (name == "TryParseExact" && ps.Length >= 3 && IsReadOnlySpanChar(ps[0]) && IsReadOnlySpanChar(ps[1]))
        {
            var outAddr = Pop();
            for (int x = ps.Length - 2; x >= 2; x--) Pop(); // provider [, styles]
            string fPtr = SpanPtr(Pop(), CppTypes.Of(ps[1]));
            string f = NewTemp("Dn2CppString*");
            Emit($"{f} = dn2cpp_string_from_chars((const char16_t*){fPtr}->f__reference, {fPtr}->f__length);");
            string sPtr = SpanPtr(Pop(), CppTypes.Of(ps[0]));
            string sv = NewTemp("Dn2CppString*");
            Emit($"{sv} = dn2cpp_string_from_chars((const char16_t*){sPtr}->f__reference, {sPtr}->f__length);");
            Push(StackKind.I4, "int32_t", $"dn2cpp_datetimeoffset_try_parse_exact({sv}, {f}, {Cast(outAddr, "Dn2CppDateTimeOffset*")})");
            return true;
        }
        // Span input + string[] formats: try each in turn (a format set — RFC1123/RFC850/
        // asctime — against one value).
        if (name == "TryParseExact" && ps.Length >= 3 && IsReadOnlySpanChar(ps[0])
            && ps[1] is { Kind: TypeKind.SZArray, Element.IsString: true })
        {
            var outAddr = Pop();
            for (int x = ps.Length - 2; x >= 2; x--) Pop(); // provider [, styles]
            var formats = Pop();                            // string[] formats
            string sPtr = SpanPtr(Pop(), CppTypes.Of(ps[0]));
            string sv = NewTemp("Dn2CppString*");
            Emit($"{sv} = dn2cpp_string_from_chars((const char16_t*){sPtr}->f__reference, {sPtr}->f__length);");
            Push(StackKind.I4, "int32_t", $"dn2cpp_datetimeoffset_try_parse_exact_multi({sv}, {Cast(formats, "Dn2CppArrayRef*")}, {Cast(outAddr, "Dn2CppDateTimeOffset*")})");
            return true;
        }

        // Property getters (pop the receiver). Component properties read the clock; ordering
        // properties (UtcTicks) read the UTC instant.
        switch (name)
        {
            case "get_Ticks": { var r = Pop(); Push(StackKind.I8, "int64_t", $"({DTOVal(r)}).ticks"); return true; }
            case "get_UtcTicks": { var r = Pop(); Push(StackKind.I8, "int64_t", $"dn2cpp_datetimeoffset_utc_ticks({DTOVal(r)})"); return true; }
            case "get_Offset": { var r = Pop(); Push(StackKind.Struct, "Dn2CppTimeSpan", $"dn2cpp_datetimeoffset_offset({DTOVal(r)})"); return true; }
            case "get_DateTime": { var r = Pop(); Push(StackKind.Struct, "Dn2CppDateTime", $"dn2cpp_datetimeoffset_clock({DTOVal(r)})"); return true; }
            case "get_UtcDateTime": { var r = Pop(); Push(StackKind.Struct, "Dn2CppDateTime", $"dn2cpp_datetimeoffset_utc({DTOVal(r)})"); return true; }
            case "get_LocalDateTime": { var r = Pop(); Push(StackKind.Struct, "Dn2CppDateTime", $"dn2cpp_datetime_to_local(dn2cpp_datetimeoffset_utc({DTOVal(r)}))"); return true; }
            case "get_Year": { var r = Pop(); Push(StackKind.I4, "int32_t", $"dn2cpp_datetime_year(dn2cpp_datetimeoffset_clock({DTOVal(r)}))"); return true; }
            case "get_Month": { var r = Pop(); Push(StackKind.I4, "int32_t", $"dn2cpp_datetime_month(dn2cpp_datetimeoffset_clock({DTOVal(r)}))"); return true; }
            case "get_Day": { var r = Pop(); Push(StackKind.I4, "int32_t", $"dn2cpp_datetime_day(dn2cpp_datetimeoffset_clock({DTOVal(r)}))"); return true; }
            case "get_Hour": { var r = Pop(); Push(StackKind.I4, "int32_t", $"dn2cpp_datetime_hour(dn2cpp_datetimeoffset_clock({DTOVal(r)}))"); return true; }
            case "get_Minute": { var r = Pop(); Push(StackKind.I4, "int32_t", $"dn2cpp_datetime_minute(dn2cpp_datetimeoffset_clock({DTOVal(r)}))"); return true; }
            case "get_Second": { var r = Pop(); Push(StackKind.I4, "int32_t", $"dn2cpp_datetime_second(dn2cpp_datetimeoffset_clock({DTOVal(r)}))"); return true; }
            case "get_Millisecond": { var r = Pop(); Push(StackKind.I4, "int32_t", $"dn2cpp_datetime_millisecond(dn2cpp_datetimeoffset_clock({DTOVal(r)}))"); return true; }
            case "get_DayOfWeek": { var r = Pop(); Push(StackKind.I4, "int32_t", $"dn2cpp_datetime_dayofweek(dn2cpp_datetimeoffset_clock({DTOVal(r)}))"); return true; }
            case "get_DayOfYear": { var r = Pop(); Push(StackKind.I4, "int32_t", $"dn2cpp_datetime_dayofyear(dn2cpp_datetimeoffset_clock({DTOVal(r)}))"); return true; }
            case "get_Date":
            {
                var r = Pop(); string t = NewTemp("Dn2CppDateTime"); Emit($"{t} = dn2cpp_datetimeoffset_clock({DTOVal(r)});");
                Push(StackKind.Struct, "Dn2CppDateTime", $"dn2cpp_datetime_from_ticks({t}.ticks() - ({t}.ticks() % {TPD}), 0)");
                return true;
            }
            case "get_TimeOfDay":
            { var r = Pop(); Push(StackKind.Struct, "Dn2CppTimeSpan", $"dn2cpp_timespan_from_ticks(({DTOVal(r)}).ticks % {TPD})"); return true; }
        }

        // Instance methods.
        switch (name)
        {
            // ToFileTime = UtcDateTime.ToFileTimeUtc() in the real BCL: dn2cpp_datetimeoffset_utc
            // already yields a Kind=Utc DateTime, so no further Local/Unspecified handling is
            // needed here (unlike DateTime.ToFileTime, which must go through ToUniversalTime).
            case "ToFileTime" when ps.Length == 0:
            { var a = Pop(); Push(StackKind.I8, "int64_t", $"dn2cpp_datetime_to_file_time_utc(dn2cpp_datetimeoffset_utc({DTOVal(a)}))"); return true; }
            case "ToUnixTimeSeconds" when ps.Length == 0:
            { var a = Pop(); Push(StackKind.I8, "int64_t", $"dn2cpp_datetimeoffset_to_unix_seconds({DTOVal(a)})"); return true; }
            case "ToUnixTimeMilliseconds" when ps.Length == 0:
            { var a = Pop(); Push(StackKind.I8, "int64_t", $"dn2cpp_datetimeoffset_to_unix_millis({DTOVal(a)})"); return true; }
            case "ToOffset" when ps.Length == 1 && IsTimeSpan(ps[0]):
            { var b = Pop(); var a = Pop(); Push(StackKind.Struct, "Dn2CppDateTimeOffset", $"dn2cpp_datetimeoffset_to_offset({DTOVal(a)}, {TSVal(b)})"); return true; }
            case "CompareTo" when ps.Length == 1 && IsDateTimeOffset(ps[0]):
            { var b = Pop(); var a = Pop(); Push(StackKind.I4, "int32_t", $"dn2cpp_datetimeoffset_cmp({DTOVal(a)}, {DTOVal(b)})"); return true; }
            case "Equals" when ps.Length == 1 && IsDateTimeOffset(ps[0]):
            { var b = Pop(); var a = Pop(); Push(StackKind.I4, "int32_t", $"dn2cpp_datetimeoffset_equals({DTOVal(a)}, {DTOVal(b)})"); return true; }
            case "GetHashCode" when ps.Length == 0:
            { var a = Pop(); Push(StackKind.I4, "int32_t", $"dn2cpp_datetimeoffset_hash({DTOVal(a)})"); return true; }
            // ISpanFormattable.TryFormat(Span<char> dest, out charsWritten, ROS<char> format,
            // IFormatProvider): write the round-trip representation into the destination span
            // (fits -> copy + count + true; too short -> false). The format/provider are
            // dropped (invariant) — only the default form is produced.
            case "TryFormat" when ps.Length == 4 && IsSpanOfPrimitive(ps[0], PrimitiveTypeCode.Char):
            {
                Pop();                                  // IFormatProvider — invariant
                Pop();                                  // ReadOnlySpan<char> format — default form
                string wrote = Cast(Pop(), "int32_t*"); // out int charsWritten
                string dPtr = SpanPtr(Pop(), CppTypes.Of(ps[0])); // Span<char> destination
                var recv = Pop();
                string sv = NewTemp("Dn2CppString*");
                Emit($"{sv} = dn2cpp_datetimeoffset_to_string({DTOVal(recv)});");
                Push(StackKind.I4, "int32_t",
                    $"dn2cpp_string_try_copy_to_span({sv}, (char16_t*){dPtr}->f__reference, {dPtr}->f__length, {wrote})");
                return true;
            }
            // Arithmetic — keep the offset, act on the clock value.
            case "AddTicks" when ps.Length == 1:
            { var b = Pop(); var a = Pop(); Push(StackKind.Struct, "Dn2CppDateTimeOffset", $"dn2cpp_datetimeoffset_add_ticks({DTOVal(a)}, (int64_t)({b.Expr}))"); return true; }
            case "AddMonths" when ps.Length == 1:
            { var b = Pop(); var a = Pop(); Push(StackKind.Struct, "Dn2CppDateTimeOffset", $"dn2cpp_datetimeoffset_add_months({DTOVal(a)}, (int32_t)({b.Expr}))"); return true; }
            case "AddYears" when ps.Length == 1:
            { var b = Pop(); var a = Pop(); Push(StackKind.Struct, "Dn2CppDateTimeOffset", $"dn2cpp_datetimeoffset_add_years({DTOVal(a)}, (int32_t)({b.Expr}))"); return true; }
            case "Add" when ps.Length == 1 && IsTimeSpan(ps[0]):
            { var b = Pop(); var a = Pop(); Push(StackKind.Struct, "Dn2CppDateTimeOffset", $"dn2cpp_datetimeoffset_add_ticks({DTOVal(a)}, ({TSVal(b)}).ticks)"); return true; }
            case "Subtract" when ps.Length == 1 && IsTimeSpan(ps[0]):
            { var b = Pop(); var a = Pop(); Push(StackKind.Struct, "Dn2CppDateTimeOffset", $"dn2cpp_datetimeoffset_add_ticks({DTOVal(a)}, -({TSVal(b)}).ticks)"); return true; }
            case "Subtract" when ps.Length == 1 && IsDateTimeOffset(ps[0]):
            { var b = Pop(); var a = Pop(); Push(StackKind.Struct, "Dn2CppTimeSpan", $"dn2cpp_timespan_from_ticks(dn2cpp_datetimeoffset_utc_ticks({DTOVal(a)}) - dn2cpp_datetimeoffset_utc_ticks({DTOVal(b)}))"); return true; }
            case "ToString":
            {
                if (ps.Length == 0) // default invariant "MM/dd/yyyy HH:mm:ss zzz"
                { var a = Pop(); Push(StackKind.Ref, "Dn2CppString*", $"dn2cpp_datetimeoffset_to_string({DTOVal(a)})"); return true; }
                if (ps is [{ IsString: true }]) // ToString(string format) -> invariant
                { var f = Pop(); var a = Pop(); Push(StackKind.Ref, "Dn2CppString*", $"dn2cpp_datetimeoffset_format({DTOVal(a)}, {Cast(f, "Dn2CppString*")})"); return true; }
                if (ps is [{ IsString: true }, _]) // ToString(string format, IFormatProvider)
                { Pop(); var f = Pop(); var a = Pop(); Push(StackKind.Ref, "Dn2CppString*", $"dn2cpp_datetimeoffset_format({DTOVal(a)}, {Cast(f, "Dn2CppString*")})"); return true; }
                if (ps is [{ IsString: false }]) // ToString(IFormatProvider) -> invariant default
                { Pop(); var a = Pop(); Push(StackKind.Ref, "Dn2CppString*", $"dn2cpp_datetimeoffset_to_string({DTOVal(a)})"); return true; }
                return false;
            }
        }

        // Add<Unit>(double) — truncates value*ticks toward zero, like DateTime.
        string? dtoAddUnit = name switch
        {
            "AddDays" => "864000000000LL", "AddHours" => "36000000000LL",
            "AddMinutes" => "600000000LL", "AddSeconds" => "10000000LL",
            "AddMilliseconds" => "10000LL", _ => null,
        };
        if (dtoAddUnit is not null && ps.Length == 1)
        {
            var b = Pop(); var a = Pop();
            Push(StackKind.Struct, "Dn2CppDateTimeOffset", $"dn2cpp_datetimeoffset_add_unit({DTOVal(a)}, (double)({b.Expr}), {dtoAddUnit})");
            return true;
        }

        // Operators. DateTimeOffset - DateTimeOffset -> TimeSpan (UTC-instant difference);
        // DTO +/- TimeSpan keeps the offset; comparison is by the UTC instant.
        if (name == "op_Addition" && ps.Length == 2 && IsDateTimeOffset(ps[0]) && IsTimeSpan(ps[1]))
        { var b = Pop(); var a = Pop(); Push(StackKind.Struct, "Dn2CppDateTimeOffset", $"dn2cpp_datetimeoffset_add_ticks({DTOVal(a)}, ({TSVal(b)}).ticks)"); return true; }
        if (name == "op_Subtraction" && ps.Length == 2 && IsDateTimeOffset(ps[0]) && IsTimeSpan(ps[1]))
        { var b = Pop(); var a = Pop(); Push(StackKind.Struct, "Dn2CppDateTimeOffset", $"dn2cpp_datetimeoffset_add_ticks({DTOVal(a)}, -({TSVal(b)}).ticks)"); return true; }
        if (name == "op_Subtraction" && ps.Length == 2 && IsDateTimeOffset(ps[0]) && IsDateTimeOffset(ps[1]))
        {
            var b = Pop(); var a = Pop();
            Push(StackKind.Struct, "Dn2CppTimeSpan",
                $"dn2cpp_timespan_from_ticks(dn2cpp_datetimeoffset_utc_ticks({DTOVal(a)}) - dn2cpp_datetimeoffset_utc_ticks({DTOVal(b)}))");
            return true;
        }
        string? dtoCmp = name switch
        {
            "op_Equality" => "== 0", "op_Inequality" => "!= 0",
            "op_LessThan" => "< 0", "op_GreaterThan" => "> 0",
            "op_LessThanOrEqual" => "<= 0", "op_GreaterThanOrEqual" => ">= 0",
            _ => null,
        };
        if (dtoCmp is not null && ps.Length == 2 && IsDateTimeOffset(ps[0]) && IsDateTimeOffset(ps[1]))
        {
            var b = Pop(); var a = Pop();
            Push(StackKind.I4, "int32_t", $"(dn2cpp_datetimeoffset_cmp({DTOVal(a)}, {DTOVal(b)}) {dtoCmp} ? 1 : 0)");
            return true;
        }

        return false;
    }

    /// <summary>A GCHandle value/pointer operand as a struct rvalue (deref a managed
    /// pointer from <c>ldloca</c>/<c>ldflda</c> — a handle held in a field reaches
    /// Free/AddrOfPinnedObject as Ptr; pass a struct value through). Internal so a backend's
    /// call intrinsics can reuse it — a target whose interop layer wraps GCHandle must
    /// forward to the same lowering rather than restate it.</summary>
    internal string GCHVal(StackEntry e) =>
        e.Kind == StackKind.Ptr ? $"(*(Dn2CppGCHandle*)({e.Expr}))" : e.Expr;

    /// <summary>The data address a pinned <paramref name="value"/> exposes via
    /// AddrOfPinnedObject. When the static C++ rep is known at the Alloc site it is
    /// captured directly: an array yields &amp;element[0]
    /// (= MemoryMarshal.GetArrayDataReference), a string the first char
    /// (= String.GetRawStringData). When the value is statically typed `object` / an
    /// interface / a base class the rep is NOT known, so the address is discovered from the
    /// RUNTIME type via dn2cpp_pinned_data_addr — a fixed `header + 1` would read an array's
    /// length field as data. The heap is non-moving, so the address is stable for the
    /// handle's lifetime.</summary>
    private static string GCHandleDataAddr(StackEntry value) => value.CppType switch
    {
        "Dn2CppArrayI4*" => $"(void*)(&((Dn2CppArrayI4*)({value.Expr}))->data[0])",
        "Dn2CppArrayRef*" => $"(void*)(&((Dn2CppArrayRef*)({value.Expr}))->data[0])",
        "Dn2CppArrayN*" => $"(void*)(&((Dn2CppArrayN*)({value.Expr}))->data[0])",
        "Dn2CppString*" => $"(void*)(((Dn2CppString*)({value.Expr}))->chars)",
        _ => $"dn2cpp_pinned_data_addr((Dn2CppObject*)({value.Expr}))",
    };

    /// <summary>The Dn2CppGCHandle a GCHandle.Alloc / new GCHandle(...) lowers to. Shared
    /// by TryEmitGCHandleIntrinsic, TranslateNewobj, and any backend call intrinsic that
    /// forwards a target's own GCHandle wrapper here, so the Alloc entry points cannot
    /// drift. <paramref name="handleTypeExpr"/> is the GCHandleType as an int32 C++
    /// expression (Normal = "2" for the type-less overload); the runtime uses it to pick
    /// strong vs weak-table storage.</summary>
    internal static string EmitGCHandleAlloc(StackEntry value, string handleTypeExpr) =>
        $"dn2cpp_gchandle_alloc((Dn2CppObject*)({value.Expr}), {GCHandleDataAddr(value)}, {handleTypeExpr})";

    /// <summary>The runtime trap call a <c>System.ThrowHelper.Throw*</c> closure lowers to.
    /// The name selects the matching catchable managed exception so a typed <c>catch</c>
    /// resolves correctly; both the generic and non-generic ThrowHelper paths share this.
    /// More specific names must precede less specific ones (ArgumentNull /
    /// ArgumentOutOfRange before Argument).
    ///
    /// <para><b>The default is where a wrong type hides.</b> A fall-through is invisible
    /// exactly when the real exception type DERIVES from InvalidOperationException (as
    /// ObjectDisposedException does): everything catching the base keeps working, and only
    /// the precise <c>catch</c> — the one case that matters — silently fails to match. A
    /// helper whose exception type is not a subtype of InvalidOperationException belongs in
    /// an arm of its own, not in the default.</para></summary>
    private static string ThrowHelperTrap(string name) =>
        name.Contains("IndexOutOfRange") ? "dn2cpp_throw_index_out_of_range();"
        : name.Contains("ArgumentOutOfRange") ? "dn2cpp_throw_argument_out_of_range();"
        : name.Contains("ArgumentNull") ? "dn2cpp_throw_argument_null();"
        : name.Contains("Argument") ? "dn2cpp_throw_argument();"
        : name.Contains("KeyNotFound") ? "dn2cpp_throw_key_not_found();"
        : name.Contains("ObjectDisposed") ? "dn2cpp_throw_object_disposed();"
        : name.Contains("PlatformNotSupported") ? $"dn2cpp_throw_platform_not_supported(\"{name}\");"
        : name.Contains("NotSupported") ? "dn2cpp_throw_not_supported();"
        : "dn2cpp_throw_invalid_operation();";

    /// <summary>The runtime type-info handle the same name selects, for the message-carrying
    /// trap. Kept beside <see cref="ThrowHelperTrap"/> and in the same order for the reason
    /// stated there — a name that lands on the wrong arm here lands on the wrong catch.</summary>
    private static string ThrowHelperTypeInfo(string name) =>
        name.Contains("IndexOutOfRange") ? "dn2cpp_index_out_of_range_exception_type"
        : name.Contains("ArgumentOutOfRange") ? "dn2cpp_argument_out_of_range_exception_type"
        : name.Contains("ArgumentNull") ? "dn2cpp_argument_null_exception_type"
        : name.Contains("Argument") ? "dn2cpp_argument_exception_type"
        : name.Contains("KeyNotFound") ? "dn2cpp_key_not_found_exception_type"
        : name.Contains("ObjectDisposed") ? "dn2cpp_object_disposed_exception_type"
        : name.Contains("PlatformNotSupported") ? "dn2cpp_platform_not_supported_exception_type"
        : name.Contains("NotSupported") ? "dn2cpp_not_supported_exception_type"
        : "dn2cpp_invalid_operation_exception_type";

    /// <summary>Lowers a <c>System.ThrowHelper.Throw*</c> SINK, recovering the message real
    /// .NET raises when the call site states it. Most of the family names its resource in
    /// the ARGUMENTS — <c>ThrowInvalidOperationException(ExceptionResource
    /// .InvalidOperation_EmptyStack)</c> — so the constant is on our own evaluation stack
    /// here, and the enum member's NAME is the SR key, resolved against the ThrowHelper
    /// copy's own module (the type is a per-assembly polyfill, and so is its SR). A sibling
    /// <c>ExceptionArgument</c> operand is the paramName, which ArgumentException.Message
    /// appends — baked in, since a runtime-raised exception has no managed _paramName field
    /// for the override to read. The other half bakes its resource into its own body, which
    /// <see cref="ThrowHelperResources"/> reads off the metadata. Either way an unrecovered
    /// resource keeps the bare trap, whose message is the exception type's real .NET
    /// default.</summary>
    private void EmitThrowHelperSink(string name, MethodSignature<TypeDesc> sig)
    {
        int n = sig.ParameterTypes.Length;
        var popped = new StackEntry[n];
        for (int i = n - 1; i >= 0; i--)
            popped[i] = Pop();
        var (resSrc, argSrc) = ThrowHelperResources.Sources(Module, name, n);
        string? key = ValueOf(resSrc, popped) is { } rv
            ? ThrowHelperResources.ResourceKey(Module, rv) : null;
        string? text = key is null ? null : Comp.SrResourceText(Module, key);
        if (text is null)
        {
            Emit(ThrowHelperTrap(name));
            return;
        }
        if (ValueOf(argSrc, popped) is { } av
            && ThrowHelperResources.ArgumentName(Module, av) is { } paramName
            && Comp.SrResourceText(Module, "Arg_ParamName_Name") is { } tail)
            text += " " + tail.Replace("{0}", paramName);
        Emit($"dn2cpp_throw_of_msg(&{ThrowHelperTypeInfo(name)}, \"{CppLiteralBody(text)}\");");
    }

    /// <summary>The int a source names: its own constant, or the constant the call site
    /// pushed for the sink's argument it points at (null when that operand was not one).</summary>
    private int? ValueOf(ThrowHelperResources.Source? s, StackEntry[] popped) => s switch
    {
        { IsConst: true } c => c.Value,
        { ArgIndex: var i } when i >= 0 && i < popped.Length => ConstIntOf(popped[i]),
        _ => null,
    };

    /// <summary>Body of a C++ narrow string literal (resource text carries quotes and,
    /// rarely, newlines).</summary>
    private static string CppLiteralBody(string s) => s
        .Replace("\\", "\\\\").Replace("\"", "\\\"")
        .Replace("\r", "\\r").Replace("\n", "\\n").Replace("\t", "\\t");

    /// <summary>System.Runtime.InteropServices.GCHandle. Non-moving heap => Normal/Pinned
    /// pinning is identity. The Dn2CppGCHandle struct is one pointer to a shared,
    /// uncollectable-but-scanned cell (the equivalent of real .NET's handle-table slot):
    /// the cell roots a Normal/Pinned target even when only a native-side ToIntPtr value
    /// survives, Free clears the cell so it propagates to every copy, and ToIntPtr returns
    /// the stable cell address. Weak/WeakTrackResurrection delegate to the low-level weak
    /// table (dn2cpp_gchandle_internal_*) so a weak handle is really weak. Like real .NET,
    /// AddrOfPinnedObject throws on a non-Pinned handle and FromIntPtr(0) throws; the .ctor
    /// newobj form is handled in TranslateNewobj. Unmodeled members raise
    /// NotSupportedException (no silent carve-out).</summary>
    private bool TryEmitGCHandleIntrinsic(string name, MethodSignature<TypeDesc> sig)
    {
        var ps = sig.ParameterTypes;
        switch (name)
        {
            // Alloc(object value [, GCHandleType type]) — static factory returning a
            // GCHandle by value. Forward the GCHandleType so the runtime picks strong
            // (Normal/Pinned) vs weak-table (Weak/WeakTrackResurrection) storage; the
            // 1-arg overload is Alloc(value, Normal) = 2.
            case "Alloc" when ps.Length is 1 or 2:
            {
                var handleType = ps.Length == 2 ? $"(int32_t)({Pop().Expr})" : "2";
                var value = Pop();
                Push(StackKind.Struct, "Dn2CppGCHandle", EmitGCHandleAlloc(value, handleType));
                return true;
            }
            // get_Target / Free / AddrOfPinnedObject / get_IsAllocated — instance members.
            case "get_Target":
            {
                var r = Pop();
                Push(StackKind.Ref, "Dn2CppObject*", $"dn2cpp_gchandle_target({GCHVal(r)})");
                return true;
            }
            case "set_Target":
            {
                // Writes the shared cell, so the new target is visible through every
                // copy of the handle; on a Pinned handle the runtime re-pins (the data
                // address is rediscovered from the new referent's runtime type).
                var value = Pop();
                var r = Pop();
                Emit($"dn2cpp_gchandle_set_target({GCHVal(r)}, (Dn2CppObject*)({value.Expr}));");
                return true;
            }
            case "AddrOfPinnedObject":
            {
                var r = Pop();
                Push(StackKind.I8, "intptr_t", $"(intptr_t)dn2cpp_gchandle_addr({GCHVal(r)})");
                return true;
            }
            case "Free":
            {
                var r = Pop();
                // A local/field receiver arrives by address (ldloca/ldflda) — free
                // through the pointer so the write-back (cell -> null) sticks and a
                // second Free through THIS copy throws; an rvalue receiver has no
                // storage to zero, but the shared cell is still cleared either way,
                // which is what invalidates every other copy.
                Emit(r.Kind == StackKind.Ptr
                    ? $"dn2cpp_gchandle_free((Dn2CppGCHandle*)({r.Expr}));"
                    : $"dn2cpp_gchandle_free_value({r.Expr});");
                return true;
            }
            case "get_IsAllocated":
            {
                var r = Pop();
                Push(StackKind.I4, "int32_t", $"dn2cpp_gchandle_is_allocated({GCHVal(r)})");
                return true;
            }
            // GetHashCode: hashes the handle's IntPtr — here the shared cell's address
            // (dn2cpp_gchandle_to_intptr; 0 for a zeroed handle) — folded 64->32 as
            // low ^ high like IntPtr.GetHashCode, so two copies of one handle hash equally
            // and default(GCHandle) hashes to 0.
            case "GetHashCode" when ps.Length == 0:
            {
                var r = Pop();
                string h = NewTemp("uint64_t");
                Emit($"{h} = (uint64_t)dn2cpp_gchandle_to_intptr({GCHVal(r)});");
                Push(StackKind.I4, "int32_t", $"(int32_t)(({h}) ^ (({h}) >> 32))");
                return true;
            }
            // ToIntPtr/FromIntPtr (and the equivalent explicit cast operators) — an
            // opaque IntPtr the handle can round-trip through. to_intptr returns the
            // shared cell's address (stable across calls and collections, and the
            // uncollectable cell roots the target even when only this integer
            // survives); from_intptr wraps it back (0 throws, like real .NET).
            case "ToIntPtr" when ps.Length == 1:
            {
                var r = Pop();
                Push(StackKind.I8, "intptr_t", $"dn2cpp_gchandle_to_intptr({GCHVal(r)})");
                return true;
            }
            case "FromIntPtr" when ps.Length == 1:
            {
                var v = Pop();
                Push(StackKind.Struct, "Dn2CppGCHandle",
                    $"dn2cpp_gchandle_from_intptr((intptr_t)({v.Expr}))");
                return true;
            }
            // (IntPtr)handle / (GCHandle)ptr — disambiguated by direction: a GCHandle
            // return type is the IntPtr->GCHandle operator (FromIntPtr), else it is the
            // GCHandle->IntPtr operator (ToIntPtr).
            case "op_Explicit" when ps.Length == 1 && IsGCHandle(sig.ReturnType):
            {
                var v = Pop();
                Push(StackKind.Struct, "Dn2CppGCHandle",
                    $"dn2cpp_gchandle_from_intptr((intptr_t)({v.Expr}))");
                return true;
            }
            case "op_Explicit" when ps.Length == 1 && IsGCHandle(ps[0]):
            {
                var r = Pop();
                Push(StackKind.I8, "intptr_t", $"dn2cpp_gchandle_to_intptr({GCHVal(r)})");
                return true;
            }
            // The low-level IntPtr-handle table (InternalAlloc/Get/Set/Free/
            // CompareExchange) WeakReference / WeakReference<T> drive directly — distinct
            // from the struct pinning model above. Each handle is a real Boehm weak
            // link (dn2cpp_gchandle_internal_*).
            case "InternalAlloc" when ps.Length == 2:
            {
                var handleType = Pop(); // GCHandleType: 0=Weak, 1=WeakTrackResurrection
                var value = Pop();
                Push(StackKind.I8, "intptr_t",
                    $"dn2cpp_gchandle_internal_alloc((Dn2CppObject*)({value.Expr}), (int32_t)({handleType.Expr}))");
                return true;
            }
            case "InternalGet":
            {
                var h = Pop();
                Push(StackKind.Ref, "Dn2CppObject*",
                    $"dn2cpp_gchandle_internal_get((intptr_t)({h.Expr}))");
                return true;
            }
            case "InternalSet":
            {
                var value = Pop();
                var h = Pop();
                Emit($"dn2cpp_gchandle_internal_set((intptr_t)({h.Expr}), (Dn2CppObject*)({value.Expr}));");
                return true;
            }
            case "InternalFree":
            {
                var h = Pop();
                Emit($"dn2cpp_gchandle_internal_free((intptr_t)({h.Expr}));");
                return true;
            }
            case "InternalCompareExchange":
            {
                var oldValue = Pop();
                var value = Pop();
                var h = Pop();
                Push(StackKind.Ref, "Dn2CppObject*",
                    $"dn2cpp_gchandle_internal_compare_exchange((intptr_t)({h.Expr}), "
                    + $"(Dn2CppObject*)({value.Expr}), (Dn2CppObject*)({oldValue.Expr}))");
                return true;
            }
        }
        throw new NotSupportedException(
            $"{Method.DeclaringClass.FullName}.{Method.Name}: GCHandle::{name} "
            + "is not modeled (supported: Alloc/Free/AddrOfPinnedObject/Target get+set/"
            + "IsAllocated/GetHashCode/ToIntPtr/FromIntPtr/op_Explicit)");
    }

    /// <summary>A DependentHandle value/pointer operand as a struct rvalue (deref a
    /// managed pointer from <c>ldloca</c>/<c>ldflda</c> — ConditionalWeakTable calls
    /// every member on the <c>depHnd</c> field's address; pass a struct value
    /// through).</summary>
    private string DHVal(StackEntry e) =>
        e.Kind == StackKind.Ptr ? $"(*(Dn2CppDependentHandle*)({e.Expr}))" : e.Expr;

    /// <summary>System.Runtime.DependentHandle — the ephemeron primitive behind
    /// ConditionalWeakTable, whose real members are InternalCalls over the CLR handle table.
    /// Lowered to the Dn2CppDependentHandle cell model: the target is a real Boehm weak link
    /// (dn2cpp_gchandle_internal_*), the dependent a strong reference conditioned on the
    /// target still being alive at read time (CoreIntrinsics states the
    /// ephemeron-approximation bound). Only the surface ConditionalWeakTable drives is
    /// modeled; the .ctor newobj form is in TranslateNewobj, and an unmodeled member throws
    /// (no silent carve-out).</summary>
    private bool TryEmitDependentHandleIntrinsic(string name, MethodSignature<TypeDesc> sig)
    {
        var ps = sig.ParameterTypes;
        switch (name)
        {
            case "get_IsAllocated" when ps.Length == 0:
            {
                var r = Pop();
                Push(StackKind.I4, "int32_t", $"dn2cpp_dependenthandle_is_allocated({DHVal(r)})");
                return true;
            }
            // object? UnsafeGetTarget() — null once the target was collected or
            // UnsafeSetTargetToNull ran (the free-entry probe in CWT's resize scan).
            case "UnsafeGetTarget" when ps.Length == 0:
            {
                var r = Pop();
                Push(StackKind.Ref, "Dn2CppObject*", $"dn2cpp_dependenthandle_target({DHVal(r)})");
                return true;
            }
            // object? UnsafeGetTargetAndDependent(out object? dependent) — the
            // TryGetValue read: both halves in one shot, the dependent handed out
            // only while the target is still alive.
            case "UnsafeGetTargetAndDependent" when ps is [{ Kind: TypeKind.ByRef }]:
            {
                var dep = Pop(); // out object? dependent
                var r = Pop();
                string t = NewTemp("Dn2CppObject*");
                Emit($"{t} = dn2cpp_dependenthandle_target_and_dependent({DHVal(r)}, (Dn2CppObject**)({dep.Expr}));");
                Push(StackKind.Ref, "Dn2CppObject*", t);
                return true;
            }
            // void UnsafeSetTargetToNull() — CWT's Remove: retire the entry without
            // freeing the cell (the container's resize/finalizer disposes it later).
            case "UnsafeSetTargetToNull" when ps.Length == 0:
            {
                var r = Pop();
                Emit($"dn2cpp_dependenthandle_set_target_null({DHVal(r)});");
                return true;
            }
            // void UnsafeSetDependent(object? dependent) — CWT's AddOrUpdate on an
            // existing key.
            case "UnsafeSetDependent" when ps.Length == 1:
            {
                var value = Pop();
                var r = Pop();
                Emit($"dn2cpp_dependenthandle_set_dependent({DHVal(r)}, (Dn2CppObject*)({value.Expr}));");
                return true;
            }
            case "Dispose" when ps.Length == 0:
            {
                var r = Pop();
                // A field/local receiver arrives by address (ldflda/ldloca) — free
                // through the pointer so the write-back (_handle -> 0) sticks; an
                // rvalue receiver has no storage to zero, but the cell is still
                // retired either way.
                Emit(r.Kind == StackKind.Ptr
                    ? $"dn2cpp_dependenthandle_free((Dn2CppDependentHandle*)({r.Expr}));"
                    : $"dn2cpp_dependenthandle_free_value({r.Expr});");
                return true;
            }
        }
        throw new NotSupportedException(
            $"{Method.DeclaringClass.FullName}.{Method.Name}: DependentHandle::{name} "
            + "is not modeled (supported: IsAllocated/UnsafeGetTarget/"
            + "UnsafeGetTargetAndDependent/UnsafeSetTargetToNull/UnsafeSetDependent/Dispose)");
    }

    // The file-backed MemoryMappedFile handles as struct rvalues (deref a managed
    // pointer from ldloca/ldflda; pass a struct value through). Instance members
    // load the receiver by value when called on a local (ldloc) and by address when
    // its address is taken first, so both shapes are accepted.
    private string MmfVal(StackEntry e) => e.Kind == StackKind.Ptr ? $"(*(Dn2CppMappedFile*)({e.Expr}))" : e.Expr;
    private string MmvVal(StackEntry e) => e.Kind == StackKind.Ptr ? $"(*(Dn2CppMappedView*)({e.Expr}))" : e.Expr;
    private string MmhVal(StackEntry e) => e.Kind == StackKind.Ptr ? $"(*(Dn2CppMappedSafeHandle*)({e.Expr}))" : e.Expr;

    /// <summary>The packed C++ storage type a MemoryMappedViewAccessor named primitive
    /// reader (ReadInt32/ReadByte/…) loads from, keyed on the type suffix, or null when the
    /// suffix is not a modeled primitive (ReadDecimal and the generic Read/ReadArray fall
    /// through — Decimal is a carve-out; the generics are MethodSpecs handled elsewhere). The
    /// non-generic writes are one overloaded Write distinguished by the value param's type.</summary>
    private static string? MmapPrimStorage(string suffix) => suffix switch
    {
        "Byte" => "uint8_t",
        "SByte" => "int8_t",
        "Boolean" => "uint8_t",
        "Int16" => "int16_t",
        "UInt16" => "uint16_t",
        "Char" => "char16_t",
        "Int32" => "int32_t",
        "UInt32" => "uint32_t",
        "Int64" => "int64_t",
        "UInt64" => "uint64_t",
        "Single" => "float",
        "Double" => "double",
        _ => null,
    };

    /// <summary>System.IO.MemoryMappedFiles file-backed map subset (POSIX mmap). The three
    /// BCL reference types are modeled as small by-value intrinsic structs (a non-moving
    /// handle, like GCHandle); the view's Read*/Write* primitive accessors are inline typed
    /// loads/stores over the mapped bytes. The generic Read/Write/ReadArray/WriteArray&lt;T&gt;
    /// forms are handled in TranslateGenericIntrinsic. Unmodeled members (named maps,
    /// CreateViewStream, ReadDecimal, the CreateNew/Truncate file modes, …) raise
    /// NotSupportedException.</summary>
    private bool TryEmitMemoryMappedFileIntrinsic(string declType, string name, MethodSignature<TypeDesc> sig)
    {
        var ps = sig.ParameterTypes;
        switch (declType)
        {
            case "System.IO.MemoryMappedFiles.MemoryMappedFile":
                switch (name)
                {
                    // CreateFromFile(path, mode): the default access is ReadWrite (0),
                    // capacity 0 (= file size), no named map.
                    case "CreateFromFile" when ps.Length == 2:
                    {
                        var mode = Pop();
                        var path = Pop();
                        Push(StackKind.Struct, "Dn2CppMappedFile",
                            $"dn2cpp_mmap_create_from_file({Cast(path, "Dn2CppString*")}, nullptr, {mode.Expr}, 0, 0)");
                        return true;
                    }
                    // CreateFromFile(path, mode, mapName, capacity, access): a non-null
                    // mapName throws NotSupportedException in the runtime (named maps carve-out).
                    case "CreateFromFile" when ps.Length == 5:
                    {
                        var access = Pop();
                        var capacity = Pop();
                        var mapName = Pop();
                        var mode = Pop();
                        var path = Pop();
                        Push(StackKind.Struct, "Dn2CppMappedFile",
                            $"dn2cpp_mmap_create_from_file({Cast(path, "Dn2CppString*")}, "
                            + $"{Cast(mapName, "Dn2CppString*")}, {mode.Expr}, {access.Expr}, {Cast(capacity, "int64_t")})");
                        return true;
                    }
                    // CreateViewAccessor(): whole file, the file's own access mode.
                    case "CreateViewAccessor" when ps.Length == 0:
                    {
                        var file = Pop();
                        string ft = NewTemp("Dn2CppMappedFile");
                        Emit($"{ft} = {MmfVal(file)};");
                        Push(StackKind.Struct, "Dn2CppMappedView",
                            $"dn2cpp_mmap_create_view({ft}, 0, 0, {ft}.access)");
                        return true;
                    }
                    case "CreateViewAccessor" when ps.Length == 2: // (offset, size)
                    {
                        var size = Pop();
                        var offset = Pop();
                        var file = Pop();
                        string ft = NewTemp("Dn2CppMappedFile");
                        Emit($"{ft} = {MmfVal(file)};");
                        Push(StackKind.Struct, "Dn2CppMappedView",
                            $"dn2cpp_mmap_create_view({ft}, {Cast(offset, "int64_t")}, {Cast(size, "int64_t")}, {ft}.access)");
                        return true;
                    }
                    case "CreateViewAccessor" when ps.Length == 3: // (offset, size, access)
                    {
                        var access = Pop();
                        var size = Pop();
                        var offset = Pop();
                        var file = Pop();
                        Push(StackKind.Struct, "Dn2CppMappedView",
                            $"dn2cpp_mmap_create_view({MmfVal(file)}, {Cast(offset, "int64_t")}, {Cast(size, "int64_t")}, {access.Expr})");
                        return true;
                    }
                    case "Dispose" when ps.Length == 0:
                    {
                        var file = Pop();
                        Emit($"dn2cpp_mmap_file_dispose({MmfVal(file)});");
                        return true;
                    }
                }
                break;

            // The view's typed accessors (Read*/Write*/Capacity/Dispose) are declared on
            // the UnmanagedMemoryAccessor base; Flush/SafeMemoryMappedViewHandle on the
            // MemoryMappedViewAccessor itself. Both carry a Dn2CppMappedView receiver.
            case "System.IO.MemoryMappedFiles.MemoryMappedViewAccessor":
            case "System.IO.UnmanagedMemoryAccessor":
                switch (name)
                {
                    case "get_Capacity" when ps.Length == 0:
                    {
                        var view = Pop();
                        Push(StackKind.I8, "int64_t", $"({MmvVal(view)}).capacity");
                        return true;
                    }
                    case "Flush" when ps.Length == 0:
                    {
                        var view = Pop();
                        Emit($"dn2cpp_mmap_view_flush({MmvVal(view)});");
                        return true;
                    }
                    case "Dispose" when ps.Length == 0:
                    {
                        var view = Pop();
                        Emit($"dn2cpp_mmap_view_dispose({MmvVal(view)});");
                        return true;
                    }
                    case "get_SafeMemoryMappedViewHandle" when ps.Length == 0:
                    {
                        var view = Pop();
                        string vt = NewTemp("Dn2CppMappedView");
                        Emit($"{vt} = {MmvVal(view)};");
                        Push(StackKind.Struct, "Dn2CppMappedSafeHandle",
                            $"Dn2CppMappedSafeHandle{{ {vt}.addr, {vt}.capacity }}");
                        return true;
                    }
                }
                // Read<suffix>(long position) — an inline typed load over the mapped bytes.
                if (name.StartsWith("Read", StringComparison.Ordinal)
                    && MmapPrimStorage(name.Substring(4)) is { } rst && ps.Length == 1)
                {
                    var pos = Pop();
                    var view = Pop();
                    string vbase = $"(({MmvVal(view)}).addr + ({pos.Expr}))";
                    if (name.Substring(4) == "Boolean")
                        Push(StackKind.I4, "int32_t", $"((*(uint8_t*){vbase}) != 0 ? 1 : 0)");
                    else if (rst is "int64_t" or "uint64_t")
                        Push(StackKind.I8, "int64_t", $"(int64_t)(*({rst}*){vbase})");
                    else if (rst is "float" or "double")
                        Push(StackKind.R8, "double", $"(double)(*({rst}*){vbase})");
                    else
                        Push(StackKind.I4, "int32_t", $"(int32_t)(*({rst}*){vbase})");
                    return true;
                }
                // Write(long position, <prim> value) — an inline typed store. The non-generic
                // writes are a single overloaded `Write` distinguished by the value param's
                // primitive type (the generic Write<T>(long, ref T) form is a MethodSpec
                // handled in TranslateGenericIntrinsic).
                if (name == "Write" && ps is [{ Kind: TypeKind.Primitive }, { Kind: TypeKind.Primitive } vp])
                {
                    string wst = CppTypes.StorageOf(vp);
                    var value = Pop();
                    var pos = Pop();
                    var view = Pop();
                    string vbase = $"(({MmvVal(view)}).addr + ({pos.Expr}))";
                    string rhs = vp.Primitive == PrimitiveTypeCode.Boolean
                        ? $"(uint8_t)(({value.Expr}) != 0 ? 1 : 0)"
                        : $"({wst})({value.Expr})";
                    Emit($"*({wst}*){vbase} = {rhs};");
                    return true;
                }
                break;

            case "Microsoft.Win32.SafeHandles.SafeMemoryMappedViewHandle":
                switch (name)
                {
                    case "AcquirePointer" when ps.Length == 1: // AcquirePointer(ref byte* pointer)
                    {
                        var ptrArg = Pop();
                        var handle = Pop();
                        Emit($"*(uint8_t**)({ptrArg.Expr}) = ({MmhVal(handle)}).addr;");
                        return true;
                    }
                    case "ReleasePointer" when ps.Length == 0:
                    {
                        Pop(); // receiver — the mapping stays valid until the view is disposed
                        return true;
                    }
                    case "get_ByteLength" when ps.Length == 0:
                    {
                        var handle = Pop();
                        Push(StackKind.I8, "uint64_t", $"(uint64_t)({MmhVal(handle)}).byteLength");
                        return true;
                    }
                    case "DangerousGetHandle" when ps.Length == 0:
                    {
                        var handle = Pop();
                        Push(StackKind.I8, "intptr_t", $"(intptr_t)({MmhVal(handle)}).addr");
                        return true;
                    }
                }
                break;
        }
        throw new NotSupportedException(
            $"{Method.DeclaringClass.FullName}.{Method.Name}: {declType}::{name} "
            + "is not modeled (file-backed MemoryMappedFile subset: CreateFromFile/CreateViewAccessor/"
            + "Read*/Write*/Capacity/Flush/Dispose + AcquirePointer/ReleasePointer/ByteLength/DangerousGetHandle; "
            + "named maps / CreateViewStream / CreateNew / Windows are carve-outs)");
    }
}
