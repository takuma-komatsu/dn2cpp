using System.Reflection.Metadata;
using System.Text;
using SRME = System.Reflection.Metadata.Ecma335.MetadataTokens;

namespace Dn2Cpp;

internal sealed partial class MethodCompiler
{
    private void Binary(string op)
    {
        var b = Pop();
        var a = Pop();
        // add/sub with a pointer operand is true (byte-addressed) pointer
        // arithmetic; any other op treats the pointer as its uintptr_t address
        // value and falls through to the generic path.
        if ((a.Kind == StackKind.Ptr || b.Kind == StackKind.Ptr) && op is "+" or "-")
        {
            EmitPointerArithmetic(op, a, b);
            return;
        }
        var (kind, type) = Promote(a, b);
        Push(kind, type, $"{Cast(a, type)} {op} {Cast(b, type)}");
    }

    /// <summary>add/sub where a managed pointer or byref is involved. IL pointer
    /// arithmetic is byte-addressed (pointers are native int), so we offset
    /// through a byte pointer; the eventual ldind/stind reinterprets it.</summary>
    private void EmitPointerArithmetic(string op, StackEntry a, StackEntry b)
    {
        switch (op)
        {
            case "+":
            {
                var (p, n) = a.Kind == StackKind.Ptr ? (a, b) : (b, a);
                Push(StackKind.Ptr, "int8_t*", $"((int8_t*)({p.Expr}) + ({n.Expr}))");
                return;
            }
            case "-" when a.Kind == StackKind.Ptr && b.Kind == StackKind.Ptr:
                Push(StackKind.I8, "int64_t", $"((int64_t)((int8_t*)({a.Expr}) - (int8_t*)({b.Expr})))");
                return;
            case "-" when a.Kind == StackKind.Ptr:
                Push(StackKind.Ptr, "int8_t*", $"((int8_t*)({a.Expr}) - ({b.Expr}))");
                return;
            case "-" when b.Kind == StackKind.Ptr:
                // left operand is a native int that actually holds an address (e.g.
                // `ldind.i` of a pointer-typed slot, then `sub charStart` in
                // EncoderFallbackBuffer.InternalFallback): an address difference, same
                // byte-addressed model as the both-Ptr case.
                Push(StackKind.I8, "int64_t", $"((int64_t)((int8_t*)({a.Expr}) - (int8_t*)({b.Expr})))");
                return;
            default:
                throw new NotSupportedException($"pointer arithmetic '{op}' on this operand shape is not supported");
        }
    }

    // ECMA-335 III shl/shr: the result type is that of the value (left) operand;
    // the shift amount (right) is independent. We mask the amount to the operand
    // width — matching C# and the x86/Arm hardware.NET runs on — so an
    // out-of-range amount can never hit C++'s shift UB, and we perform left
    // shifts in unsigned arithmetic so a value with its high bit set can never
    // hit C++'s signed-left-shift-overflow UB (which -O2 clang miscompiles;
    // found via HexConverter.IsHexChar).
    private void Shift(bool right, bool arithmetic)
    {
        var amount = Pop();
        var value = Pop();
        bool native = value.Kind == StackKind.Ptr;
        bool wide = value.Kind == StackKind.I8;
        string ut = native ? "uintptr_t" : wide ? "uint64_t" : "uint32_t";
        string st = native ? "intptr_t" : wide ? "int64_t" : "int32_t";
        string mask = native ? "(int32_t)(sizeof(uintptr_t) * 8 - 1)" : wide ? "63" : "31";
        string amt = $"((int32_t){amount.Expr} & {mask})";
        string expr = !right
            ? $"({value.CppType})((({ut}){value.Expr}) << {amt})"
            : arithmetic
                ? $"({value.CppType})((({st}){value.Expr}) >> {amt})"
                : $"({value.CppType})((({ut}){value.Expr}) >> {amt})";
        Push(value.Kind, value.CppType, expr);
    }

    /// <summary>IL <c>div</c> / <c>rem</c> — the SIGNED integer forms, and the only
    /// two arithmetic opcodes with an operand value the hardware cannot answer for.
    /// Floating point is untouched (IEEE division by zero is ±Infinity, and .NET
    /// agrees — measured); the integer forms route through the guards in
    /// <c>dn2cpp_core.h</c>, which raise DivideByZeroException on a zero divisor and
    /// OverflowException on <c>MinValue / -1</c>, matching real .NET on both.
    ///
    /// The guard is not an optimization: a raw C++ <c>/</c> on a zero divisor is
    /// UNDEFINED, and undefined is platform-split — SIGFPE on x86-64, a SILENT 0 on
    /// arm64 (<c>sdiv</c> does not trap, i.e. every Apple and Android target), a
    /// deterministic trap on wasm. A wrong answer that continues is the worst of those
    /// and the one most of dn2cpp's targets gave. <c>MinValue / -1</c> is UB on all three.
    ///
    /// A pointer-tainted operand promotes to <c>uintptr_t</c> (an address value,
    /// see <see cref="Promote"/>) and takes the UNSIGNED guard: there is no signed
    /// minimum to overflow on, and the signed guards are written per width.</summary>
    private void BinaryDivRem(bool rem)
    {
        var b = Pop();
        var a = Pop();
        var (kind, type) = Promote(a, b);
        if (kind == StackKind.R8)
        {
            // The remainder of two doubles is fmod, not `%` (which C++ does not
            // define for floating point); division stays the plain operator.
            Push(kind, type, rem ? $"fmod({a.Expr}, {b.Expr})" : $"{Cast(a, type)} / {Cast(b, type)}");
            return;
        }
        Push(kind, type, $"{DivRemGuard(type, rem)}({Cast(a, type)}, {Cast(b, type)})");
    }

    /// <summary>IL <c>div.un</c> / <c>rem.un</c> — the operands are reinterpreted as
    /// unsigned at the promoted width. Only the zero divisor can fault here (no
    /// unsigned pair overflows), so both route to the one unsigned guard; see
    /// <see cref="BinaryDivRem"/> for why the check is unconditional.</summary>
    private void BinaryUnsignedDivRem(bool rem)
    {
        var b = Pop();
        var a = Pop();
        var (kind, type) = Promote(a, b);
        string ut = kind == StackKind.I8 ? "uint64_t" : "uint32_t";
        string helper = rem ? "dn2cpp_rem_unsigned" : "dn2cpp_div_unsigned";
        Push(kind, type, $"({type}){helper}(({ut}){a.Expr}, ({ut}){b.Expr})");
    }

    /// <summary>The <c>dn2cpp_core.h</c> div/rem guard for a C++ integer type — the
    /// signed pair when the type is signed, the unsigned pair otherwise (they differ:
    /// only the signed one can overflow, and applying its <c>b == -1</c> arm to an
    /// unsigned operand would raise OverflowException for <c>0u / UINT_MAX</c>).
    /// The signedness test is on the C++ type name because that is the only thing
    /// every caller has: <see cref="BinaryDivRem"/> works from <see cref="Promote"/>'s
    /// promoted type and the Math.DivRem arm from <c>CppTypes.Of</c>'s operand type,
    /// and the two do not share a stack kind (StackKind.I4 covers both int32_t and
    /// the sub-word overloads). <c>uintptr_t</c> lands on the unsigned arm, which is
    /// what an address value is.</summary>
    internal static string DivRemGuard(string cppType, bool rem)
    {
        // uint8_t/uint16_t/uint32_t/uint64_t/uintptr_t, plus the two unsigned
        // types that do not spell it: char16_t (System.Char) and bool. Neither
        // reaches a div today — Promote() never produces them and Math.DivRem
        // has no such overload — but naming them here is what keeps a future
        // one off the signed arm, where it would be silently wrong rather than
        // a compile error.
        bool unsigned_ = cppType.StartsWith('u') || cppType is "char16_t" or "bool";
        return unsigned_
            ? (rem ? "dn2cpp_rem_unsigned" : "dn2cpp_div_unsigned")
            : (rem ? "dn2cpp_rem_signed" : "dn2cpp_div_signed");
    }


    private void Compare(string op, bool unsigned)
    {
        var b = Pop();
        var a = Pop();
        NfiCompareFixup(ref a, ref b);
        Push(StackKind.I4, "int32_t", $"({CompareCond(a, b, op, unsigned)}) ? 1 : 0");
    }

    /// <summary>A reference equality compare mixing a headerless-typed operand
    /// (the NFI trio's `const Dn2CppNumberFormatInfo*`, Assembly/Module's
    /// `const char*`) with an object-typed one: the object side holds the
    /// interned wrapper the escape minted (<see cref="Cast"/>), so a raw pointer
    /// compare would answer false for the very same instance. Wrap the headerless
    /// side — interning makes the wrap stable, so identity is preserved. The
    /// known-null constant is excluded: `x != null` stays the plain pointer test
    /// (a wrap of a non-null value is non-null, and the wrap call would be a
    /// pointless allocation probe).</summary>
    private static void NfiCompareFixup(ref StackEntry a, ref StackEntry b)
    {
        if (IsHeaderlessWrapCpp(a.CppType)
            && b is { Kind: StackKind.Ref, CppType: "Dn2CppObject*", KnownNull: false })
            a = a with { Expr = Cast(a, "Dn2CppObject*"), CppType = "Dn2CppObject*" };
        else if (IsHeaderlessWrapCpp(b.CppType)
            && a is { Kind: StackKind.Ref, CppType: "Dn2CppObject*", KnownNull: false })
            b = b with { Expr = Cast(b, "Dn2CppObject*"), CppType = "Dn2CppObject*" };
    }

    /// <summary>The C++ condition for a binary comparison — shared by the
    /// value-producing ops (<c>ceq</c>/<c>cgt[.un]</c>/<c>clt[.un]</c>, via
    /// <see cref="Compare"/>) and the matching branch ops (<c>beq</c>/<c>bne.un</c>/
    /// <c>b{ge,gt,le,lt}[.un]</c>, via <see cref="CondBranch"/>). Operands fall into
    /// three classes:
    /// <list type="bullet">
    /// <item>a reference — compared as <c>void*</c> (with the <c>cgt.un</c>→<c>!=</c>
    /// "!= null" rewrite the C# compiler emits for reference inequality);</item>
    /// <item>a raw pointer (<c>T*</c>): equality (<c>==</c>/<c>!=</c>)
    /// compares as <c>void*</c> so <c>p == null</c> (the null arrives as
    /// <c>ldnull;conv.u</c>, i.e. an I8) is a clean pointer compare rather than
    /// <c>void* == int64_t</c>; ordering compares as <c>uintptr_t</c> so a 64-bit
    /// address is never truncated — the numeric unsigned path below would cast a
    /// pointer to <c>uint32_t</c> and lose the high half. The reference branch's
    /// <c>cgt.un</c>→<c>!=</c> rewrite is for reference "!= null" only and is NOT
    /// applied to pointers;</item>
    /// <item>numerics — unsigned-integer compare (<c>.un</c> on non-float),
    /// otherwise a plain ordered compare (for floats <c>.un</c> means "unordered",
    /// not unsigned, so casting to an unsigned int would be wrong).</item>
    /// </list></summary>
    private static string CompareCond(StackEntry a, StackEntry b, string op, bool unsigned)
    {
        if (a.Kind == StackKind.Ref || b.Kind == StackKind.Ref)
        {
            // cgt.un on refs is the canonical "!= null" pattern.
            string effOp = (op == ">" && unsigned) ? "!=" : op;
            return $"((void*){a.Expr}) {effOp} ((void*){b.Expr})";
        }
        if (a.Kind == StackKind.Ptr || b.Kind == StackKind.Ptr)
        {
            return op is "==" or "!="
                ? $"((void*){a.Expr}) {op} ((void*){b.Expr})"
                : $"((uintptr_t){a.Expr}) {op} ((uintptr_t){b.Expr})";
        }
        if (unsigned && a.Kind != StackKind.R8 && b.Kind != StackKind.R8)
        {
            string ut = (a.Kind == StackKind.I8 || b.Kind == StackKind.I8) ? "uint64_t" : "uint32_t";
            return $"(({ut}){a.Expr}) {op} (({ut}){b.Expr})";
        }
        // A SIGNED ordered IL compare (clt/cgt / blt/bgt/ble/bge, no `.un`) over an
        // operand whose C++ storage type is unsigned. Signedness lives only in the
        // value stack's C++ TYPE STRING, and the plain ordered path below ignores it,
        // so C++ would pick an UNSIGNED comparison on two `uint64_t`/`uint32_t`
        // operands and drop the sign — this is exactly Int128.op_LessThan's
        // `(long)left._upper < (long)right._upper`, a signed opcode over the `ulong`
        // _upper field, where a negative Int128 would compare as its unsigned
        // reading. Reaching this branch IS that reinterpret idiom: an unsigned-source
        // operand under a signed opcode, because two genuinely unsigned operands come
        // through `clt.un`/`cgt.un` (the `unsigned:true` path above). Restore the sign
        // by casting both operands to the signed integer of matching width. Equality
        // (==/!=) is sign-independent and stays on the plain path, so its output is
        // byte-identical.
        if (op is "<" or ">" or "<=" or ">="
            && a.Kind != StackKind.R8 && b.Kind != StackKind.R8
            && (IsUnsignedCppInt(a.CppType) || IsUnsignedCppInt(b.CppType)))
        {
            string st = (a.Kind == StackKind.I8 || b.Kind == StackKind.I8) ? "int64_t" : "int32_t";
            return $"(({st}){a.Expr}) {op} (({st}){b.Expr})";
        }
        // Ordered comparison. For floating-point operands the `.un` suffix means
        // "unordered" (NaN-aware), NOT unsigned integer — casting a float to
        // uint32_t would truncate it (every value in [0,1) -> 0). A plain C++
        // float comparison matches the IL for all non-NaN inputs.
        return $"{a.Expr} {op} {b.Expr}";
    }

    // The unsigned integer C++ storage types a value-stack entry can carry (uint /
    // ulong; the sub-word forms promote to int32_t but are matched too so any
    // unsigned-narrow entry a helper pushes is covered). Used by CompareCond to spot
    // a signed IL compare whose operand type would otherwise make C++ pick an
    // unsigned comparison. Native-width unsigned (nuint / UIntPtr, `uintptr_t`) is
    // deliberately not here: those go through the pointer branch above.
    private static bool IsUnsignedCppInt(string? t) =>
        t is "uint8_t" or "uint16_t" or "uint32_t" or "uint64_t";

    private void Convert(string cppType, StackKind kind, string castOp)
    {
        var a = Pop();
        // .NET defines the unchecked float->integer conversions as saturating:
        // truncate toward zero, out-of-range pins to the target's bounds, NaN
        // -> 0; the sub-word forms (conv.i1/u1/i2/u2) first saturate to int32
        // and then truncate to the small width ((short)70000f == 4464,
        // (int)float.PositiveInfinity == int.MaxValue — verified against real
        // .NET 10). A plain C++ cast is undefined behavior on out-of-range/NaN
        // input (clang really does fold it to garbage), so a float source
        // routes through the runtime's saturating helper — the same one the
        // ConvertToIntegerNative intrinsic uses. Integer sources and the
        // float-target forms (conv.r4/r8/r.un) keep the plain cast.
        if (a.Kind == StackKind.R8 && FloatConvIntegerTarget(castOp) is { } target)
        {
            string expr = target is "int8_t" or "uint8_t" or "int16_t" or "uint16_t"
                ? $"({cppType})({target})dn2cpp_convert_to_integer_native<int32_t>({a.Expr})"
                : $"({cppType})dn2cpp_convert_to_integer_native<{target}>({a.Expr})";
            Push(kind, cppType, expr);
            return;
        }
        // A pointer / managed-reference source narrowed to a sub-64-bit integer (conv.i4/u4/
        // i2/…): C++ rejects casting a pointer straight to a smaller integer type, so bounce
        // through intptr_t first. The truncation is exactly what the IL asks for — e.g. the
        // `(uint)ptr % vectorWidth` alignment probe in the vectorized ASCII case-changers.
        string src = a.Expr;
        if (a.CppType is { } ct && ct.EndsWith('*'))
            src = $"(intptr_t)({src})";
        Push(kind, cppType, $"({cppType})({castOp}({src}))");
    }

    /// <summary>The integer type a <c>conv.*</c> cast operator narrows to, when the
    /// saturating float->integer semantics apply — null for the float-target forms
    /// and for "(int64_t)", which only the checked conv.ovf.i8/conv.ovf.i lowering
    /// passes here (overflow behavior there is a separate pre-existing carve-out,
    /// not silently changed to saturation).</summary>
    private static string? FloatConvIntegerTarget(string castOp) => castOp switch
    {
        "(int8_t)" => "int8_t",
        "(uint8_t)" => "uint8_t",
        "(int16_t)" => "int16_t",
        "(uint16_t)" => "uint16_t",
        "(int32_t)" => "int32_t",
        "(uint32_t)" => "uint32_t",
        _ => null,
    };

    // conv.i8/conv.u8/conv.i/conv.u widen to 64 bits. ECMA-335 sign-extends the
    // signed forms and zero-extends the unsigned forms. A 32-bit source must be
    // narrowed to the matching signedness *first*: a bare (uint64_t)int32_t in
    // C++ sign-extends through int64_t, so e.g. conv.u8 of -15 would wrongly
    // become 0xFFFF_FFFF_FFFF_FFF1 instead of 0x0000_0000_FFFF_FFF1.
    private void ConvertWiden(bool unsigned)
    {
        var a = Pop();
        // Float source: the same defined saturating semantics as Convert above
        // ((long)1e19 == long.MaxValue, (ulong)-5.0 == 0, NaN -> 0), where the
        // plain C++ cast is UB out of range.
        if (a.Kind == StackKind.R8)
        {
            string target = unsigned ? "uint64_t" : "int64_t";
            Push(StackKind.I8, "int64_t",
                $"(int64_t)dn2cpp_convert_to_integer_native<{target}>({a.Expr})");
            return;
        }
        string narrow = a.Kind == StackKind.I4 ? (unsigned ? "(uint32_t)" : "(int32_t)") : "";
        string mid = unsigned ? "(uint64_t)" : "(int64_t)";
        Push(StackKind.I8, "int64_t", $"(int64_t)({mid}({narrow}({a.Expr})))");
    }

    // ---- arrays ----
    // Element representation: I4-kind -> Dn2CppArrayI4, Ref -> Dn2CppArrayRef,
    // everything else -> element-sized Dn2CppArrayN.

    private enum ArrRep { I4, Ref, N }

    // Representation must match the opcodes Roslyn emits per element type:
    // int/uint/enum use ldelem.i4 (Dn2CppArrayI4); object/string/class use
    // ldelem.ref (Dn2CppArrayRef); everything else (bool/char/byte/short/long/
    // float/double/struct) uses element-sized Dn2CppArrayN.
    private static ArrRep RepOf(TypeDesc element)
    {
        if (CppTypes.KindOf(element) == StackKind.Ref)
            return ArrRep.Ref;
        if (element.Kind == TypeKind.Primitive
            && element.Primitive is PrimitiveTypeCode.Int32 or PrimitiveTypeCode.UInt32)
            return ArrRep.I4;
        // An enum uses the i4 rep only for a 4-byte underlying (int/uint); a sub-word
        // or 64-bit underlying packs into Dn2CppArrayN at its real width, matching the
        // underlying-width ldelem/stelem opcodes Roslyn emits for it.
        if (element.Kind == TypeKind.Class && element.Class!.IsEnum)
            return CppTypes.EnumArrayIsI4(element.Class!.EnumUnderlying) ? ArrRep.I4 : ArrRep.N;
        return ArrRep.N;
    }

    private void EmitNewarr(TypeDesc element)
    {
        var len = Pop();
        EmitNewarr(element, len.Expr);
    }

    private void EmitNewarr(TypeDesc element, string lenExpr, int token = 0)
    {
        // A shared body allocating a placeholder-element array tags it with the
        // real instantiation's precise handle out of an rgctx slot keyed on the
        // newarr instruction's element token — observable through GetType/is,
        // so the group handle must never leak. Intrinsic materializations with
        // no site token (Enum.GetValues copies, …) still fall back per
        // instantiation.
        if (token == 0)
            TaintIfCanonical(element, "newarr");
        // Record element[] so a per-element array type-info is emitted for it and
        // give the allocation that precise handle so arr.GetType is exact.
        _c.NoteArrayElementType(element);
        string ti = token != 0 && SharedTrial && Compilation.ContainsCanonPlaceholder(element)
            ? "(const Dn2CppTypeInfo*)" + RgctxSlotAccess(RgctxSlotKind.ArrayTypeInfo, token, "newarr", element)
            : PreciseArrayTypeInfoExpr(element);
        switch (RepOf(element))
        {
            case ArrRep.I4:
                Push(StackKind.Ref, "Dn2CppArrayI4*", $"dn2cpp_newarr_i4_t({lenExpr}, {ti})");
                break;
            case ArrRep.Ref: Push(StackKind.Ref, "Dn2CppArrayRef*", $"dn2cpp_newarr_ref_t({lenExpr}, {ti})"); break;
            default:
            {
                // Element storage is the packed natural width (byte/sbyte -> 1), not
                // the int32 stack-promoted type, so byte[] is a packed buffer.
                string st = CppTypes.StorageOf(element);
                // The sizeof names the value element's t_ struct; force its full layout and
                // record the naming (a no-op when the element is otherwise reached, as it
                // almost always is once its elements are written — see NoteArraySizeofStruct).
                NoteArraySizeofStruct(element);
                // Reference-free element storage (primitives, enums, ref-free structs)
                // holds no managed pointers, so allocate it unscanned; a struct that
                // embeds a string/object still needs the scanned allocator.
                bool refFree = !element.ContainsGcReferences();
                string alloc = refFree ? "dn2cpp_newarr_n_atomic_t" : "dn2cpp_newarr_n_t";
                Push(StackKind.Ref, "Dn2CppArrayN*", $"{alloc}({lenExpr}, (int32_t)sizeof({st}), {ti})");
                break;
            }
        }
        // Thread the array's static type so a freshly-allocated array flowing straight
        // into an IEnumerable<T> position (a single-use array local Roslyn elides, so
        // there is no ldloc to set it) is still recognised as T[] and wrapped.
        _stack[^1] = _stack[^1] with { StaticType = TypeDesc.MakeSZArray(element) };
    }

    /// <summary><c>Array.Empty&lt;T&gt;</c> -&gt; the per-element-type cached length-0
    /// singleton (<c>dn2cpp_array_empty_*</c>): the same instance every call, no
    /// per-call allocation, matching .NET's <c>EmptyArray&lt;T&gt;.Value</c>. Mirrors
    /// <see cref="EmitNewarr(TypeDesc, string, int)"/>'s rep / precise-type-info
    /// selection; the ti_arr_&lt;T&gt; handle doubles as the cache key.</summary>
    private void EmitEmptyArray(TypeDesc element)
    {
        // Same canonical-placeholder rule as newarr: the singleton is tagged (and
        // keyed) by the precise handle, so a shared body must never bake the
        // group handle in.
        TaintIfCanonical(element, "newarr");
        _c.NoteArrayElementType(element);
        string ti = PreciseArrayTypeInfoExpr(element);
        switch (RepOf(element))
        {
            case ArrRep.I4:
                Push(StackKind.Ref, "Dn2CppArrayI4*", $"dn2cpp_array_empty_i4({ti})");
                break;
            case ArrRep.Ref:
                Push(StackKind.Ref, "Dn2CppArrayRef*", $"dn2cpp_array_empty_ref({ti})");
                break;
            default:
            {
                string st = CppTypes.StorageOf(element);
                // The sizeof names the value element's t_ struct, but Array.Empty<T> never
                // instantiates T, so a value struct reached through no other edge (a
                // parameter-list-only BCL type behind Array.Empty<ParameterModifier>()) would
                // be undeclared here. Force its full layout and record the naming.
                NoteArraySizeofStruct(element);
                bool refFree = !element.ContainsGcReferences();
                string helper = refFree ? "dn2cpp_array_empty_n_atomic" : "dn2cpp_array_empty_n";
                Push(StackKind.Ref, "Dn2CppArrayN*", $"{helper}({ti}, (int32_t)sizeof({st}))");
                break;
            }
        }
        _stack[^1] = _stack[^1] with { StaticType = TypeDesc.MakeSZArray(element) };
    }

    /// <summary><c>Encoding.GetString(byte[][, int index, int count])</c>
    /// lowered to a portable,.NET-exact decode helper. dn2cpp's PEReader reads PE
    /// section names via <c>Encoding.UTF8.GetString(byte[], int, int)</c>; the real
    /// GetString body reaches <c>String.CreateStringFromEncoding</c> -&gt; the SIMD
    /// UTF-8 transcode subtree (<c>Ascii.WidenAsciiToUtf16</c> / <c>Vector128&lt;byte&gt;</c>)
    /// we never transpile. The decode helpers (<c>dn2cpp_string_decode_ascii</c> /
    /// <c>_decode_utf8</c> / <c>_decode_utf16le</c>) reproduce Encoding.ASCII /
    /// Encoding.UTF8 / Encoding.Unicode byte-for-byte, including their replacement
    /// fallback. The matching ResolveCallTarget cut makes the real GetString bodies
    /// unreachable, collapsing the whole cascade.
    ///
    /// When the static receiver type is concretely ASCIIEncoding / UTF8Encoding /
    /// UnicodeEncoding the decoder is chosen at emit time; for the base
    /// System.Text.Encoding the receiver's runtime type-info picks it. The array,
    /// range, pointer and span overloads are intercepted; any other shape or encoding
    /// raises NotSupportedException.</summary>
    private bool TryEmitEncodingGetString(string parentType, MethodSignature<TypeDesc> sig)
    {
        // Match the reached/supported overloads: GetString(byte[]),
        // GetString(byte[], int, int), GetString(byte*, int) (the SRM metadata decode
        // path) and GetString(ReadOnlySpan<byte>) (STJ's JsonHelpers.Utf8GetString key
        // decode). Anything else falls through to a loud throw.
        bool oneArg = sig.ParameterTypes is [{ Kind: TypeKind.SZArray }];
        bool threeArg = sig.ParameterTypes is
            [{ Kind: TypeKind.SZArray },
             { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Int32 },
             { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Int32 }];
        bool ptrArg = sig.ParameterTypes is
            [{ Kind: TypeKind.Pointer },
             { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Int32 }];
        bool spanArg = sig.ParameterTypes is [{ Kind: TypeKind.Class, Class: { } spCls }]
            && _c.GenericDefFullName(spCls) is "System.ReadOnlySpan" or "System.Span";
        if (!oneArg && !threeArg && !ptrArg && !spanArg)
        {
            throw new NotSupportedException(
                $"{_method.DeclaringClass.FullName}.{_method.Name}: {parentType}.GetString "
                + $"({sig.ParameterTypes.Length} args) is not supported "
                + "(only GetString(byte[]), GetString(byte[], int, int), GetString(byte*, int) and GetString(ReadOnlySpan<byte>) are intercepted)");
        }
        // The span form GetString(ReadOnlySpan<byte>): decode the span's referenced
        // bytes, exactly like the byte* form but reading the span's reference + length.
        if (spanArg)
        {
            string spanCt = CppTypes.Of(sig.ParameterTypes[0]);
            var span = Pop();
            var sEnc = Pop();
            string sv = SpanValue(span, spanCt);
            string spanCall = parentType switch
            {
                "System.Text.ASCIIEncoding" =>
                    $"dn2cpp_string_decode_ascii((const char*){sv}.f__reference, {sv}.f__length)",
                "System.Text.UTF8Encoding" =>
                    $"dn2cpp_string_decode_utf8((const char*){sv}.f__reference, {sv}.f__length)",
                "System.Text.UnicodeEncoding" =>
                    $"dn2cpp_string_decode_utf16le((const char*){sv}.f__reference, {sv}.f__length)",
                _ =>
                    $"dn2cpp_encoding_get_string_ptr({Cast(sEnc, "Dn2CppObject*")}, (const char*){sv}.f__reference, {sv}.f__length)",
            };
            Push(StackKind.Ref, "Dn2CppString*", spanCall);
            return true;
        }
        // The pointer form GetString(byte*, int): pop count, then the byte pointer,
        // then the encoding receiver. Decode the raw buffer (no array header).
        if (ptrArg)
        {
            string ptrCount = Pop().Expr;
            var ptr = Pop();
            var pEnc = Pop();
            string ptrCall = parentType switch
            {
                "System.Text.UTF8Encoding" =>
                    $"dn2cpp_string_decode_utf8((const char*)({ptr.Expr}), {ptrCount})",
                "System.Text.UnicodeEncoding" =>
                    $"dn2cpp_string_decode_utf16le((const char*)({ptr.Expr}), {ptrCount})",
                _ =>
                    $"dn2cpp_encoding_get_string_ptr({Cast(pEnc, "Dn2CppObject*")}, (const char*)({ptr.Expr}), {ptrCount})",
            };
            Push(StackKind.Ref, "Dn2CppString*", ptrCall);
            return true;
        }
        // Pop the explicit args (top-down), then the encoding receiver beneath them.
        string index, count;
        StackEntry bytes;
        if (threeArg)
        {
            count = Pop().Expr;
            index = Pop().Expr;
            bytes = Pop();
        }
        else
        {
            bytes = Pop();
            index = "0";
            // count = the whole array length; the range helper handles null (which it
            // checks before reading ->length, matching.NET's ArgumentNullException).
            count = $"(({Cast(bytes, "Dn2CppArrayN*")}) != nullptr ? ({Cast(bytes, "Dn2CppArrayN*")})->length : 0)";
        }
        var enc = Pop(); // encoding receiver
        string byteArr = Cast(bytes, "Dn2CppArrayN*");
        string call = parentType switch
        {
            // Static receiver type is known: pick the decoder at emit time.
            "System.Text.ASCIIEncoding" =>
                $"dn2cpp_encoding_decode_range({byteArr}, {index}, {count}, dn2cpp_string_decode_ascii)",
            "System.Text.UTF8Encoding" =>
                $"dn2cpp_encoding_decode_range({byteArr}, {index}, {count}, dn2cpp_string_decode_utf8)",
            "System.Text.UnicodeEncoding" =>
                $"dn2cpp_encoding_decode_range({byteArr}, {index}, {count}, dn2cpp_string_decode_utf16le)",
            // Base System.Text.Encoding (the virtual call): dispatch on the receiver's
            // runtime type-info name.
            _ =>
                $"dn2cpp_encoding_get_string({Cast(enc, "Dn2CppObject*")}, {byteArr}, {index}, {count})",
        };
        Push(StackKind.Ref, "Dn2CppString*", call);
        return true;
    }

    /// <summary>How a bad array operand of the inline copy/clear lowerings must
    /// be reported. The inline arms dereference their operands directly, so the
    /// check has to be spliced in at the call site — and the exception
    /// type is the one the surface the operand came from would raise, exactly the
    /// line the _dyn fallbacks already draw. Array.Copy/Array.Clear are STATIC and
    /// take the array as an argument (.NET: ArgumentNullException);
    /// Array.CopyTo's source IS the instance (.NET: NullReferenceException).
    /// <c>CopyDest</c> adds the RANK refusal on top of the null one: an
    /// inline arm reached its rep off the SOURCE's static type, so a rank>=2
    /// destination would be moved through the wrong layout. It rides the operand
    /// guard rather than the call site precisely because the _dyn fallback makes
    /// the same test itself, symmetrically, over both operands.
    /// <c>Unchecked</c> is for an operand the caller has already proved non-null
    /// — Array.Resize's freshly allocated destination and its explicitly
    /// null-guarded source, and any operand a caller has itself just tested (a
    /// second check there would answer nothing and could disagree about the type
    /// with the first, since the two would be unsequenced arguments of one call).
    /// </summary>
    private enum ArrayOperandKind { Argument, Receiver, CopyDest, Unchecked }

    /// <summary>Wraps an array operand's cast in the guard <paramref name="kind"/>
    /// names, so the check is sequenced BEFORE the member address is formed —
    /// which splicing a test beside the dereference would not be.</summary>
    private static string GuardArray(string castExpr, ArrayOperandKind kind) => kind switch
    {
        ArrayOperandKind.Argument => $"dn2cpp_array_require_arg({castExpr})",
        ArrayOperandKind.Receiver => $"dn2cpp_array_require_receiver({castExpr})",
        ArrayOperandKind.CopyDest => $"dn2cpp_array_require_copy_dest({castExpr})",
        _ => castExpr,
    };

    /// <summary>Whether two Array.Copy operand element types are provably the
    /// SAME type at transpile time — the precondition for the inline
    /// memmove arms. Structural, not reference, equality: SZArray TypeDescs are
    /// not interned. Conservative by design — an unprovable pair funnels into
    /// dn2cpp_array_copy_dyn, whose runtime verdict answers every pair.</summary>
    private static bool SameCopyElement(TypeDesc? a, TypeDesc? b)
    {
        if (a is null || b is null)
            return false;
        if (ReferenceEquals(a, b))
            return true;
        if (a.Kind != b.Kind)
            return false;
        return a.Kind switch
        {
            TypeKind.Primitive => a.Primitive == b.Primitive && a.IsCanonPlaceholder == b.IsCanonPlaceholder,
            TypeKind.Class => a.Class == b.Class,
            TypeKind.External => a.ExternalName == b.ExternalName,
            TypeKind.SZArray => SameCopyElement(a.Element, b.Element),
            _ => false,
        };
    }

    /// <summary>Emits a typed element memmove for Array.Copy. The element
    /// representation (I4 / ref / element-sized) is read from the source array's
    /// static C++ type, which the transpiler tracks per IL value — but the inline
    /// arms are emitted only for a pair PROVEN same-element: the caller's
    /// by-construction claim, both operands' tracked static element types
    /// agreeing, or both reps being I4 (int32/uint32/int-width enums are one CLR
    /// normalization class, so any I4 pair moves raw whatever the elements).
    /// Everything else — a non-concrete rep (a shared-generic T[] body), a
    /// degraded destination, a mixed pair — funnels into dn2cpp_array_copy_dyn,
    /// which checks null, rank and range itself (why only the inline arms carry
    /// the operand guards below) and runs the CLR's type-compatibility verdict:
    /// widening/boxing/unboxing pairs convert per element, incompatible pairs
    /// refuse with ArrayTypeMismatchException. The inline ref-element arm keeps
    /// the raw memmove for statically-equal elements even though a COVARIANT
    /// receiver could demand per-element checks (Base[] holding a Der[]) — the
    /// same documented carve-out as the stelem helpers.</summary>
    private void EmitArrayCopy(StackEntry src, string srcIdx, StackEntry dst, string dstIdx, string len,
                               ArrayOperandKind srcKind = ArrayOperandKind.Argument,
                               ArrayOperandKind dstKind = ArrayOperandKind.CopyDest,
                               bool sameElementByConstruction = false,
                               TypeDesc? elementType = null)
    {
        ArrRep? rep = ArrayRepOfCppTypeOrNull(src.CppType);
        ArrRep? dstRep = ArrayRepOfCppTypeOrNull(dst.CppType);
        bool proven = sameElementByConstruction
            || (src.StaticType is { Kind: TypeKind.SZArray } sa
                && dst.StaticType is { Kind: TypeKind.SZArray } sb
                && SameCopyElement(sa.Element, sb.Element))
            || (rep == ArrRep.I4 && dstRep == ArrRep.I4);
        if (rep is null || !proven)
        {
            Emit($"dn2cpp_array_copy_dyn({Cast(src, "Dn2CppObject*")}, (int32_t)({srcIdx}), " +
                 $"{Cast(dst, "Dn2CppObject*")}, (int32_t)({dstIdx}), (int32_t)({len}));");
            return;
        }
        // Operands land in temps in .NET's own check order — the two nulls, then
        // the range — so a bad call meets the fault .NET raises for it.
        // The index/length expressions are spilled because each is now read
        // twice (check, then move) and one of them may carry a side effect.
        string cpp = ArrayCppPtrOf(rep.Value);
        string si = NewTemp("int32_t"), di = NewTemp("int32_t"), n = NewTemp("int32_t");
        Emit($"{si} = (int32_t)({srcIdx});");
        Emit($"{di} = (int32_t)({dstIdx});");
        Emit($"{n} = (int32_t)({len});");
        string cs = NewTemp(cpp), cd = NewTemp(cpp);
        Emit($"{cs} = {GuardArray(Cast(src, cpp), srcKind)};");
        Emit($"{cd} = {GuardArray(Cast(dst, cpp), dstKind)};");
        Emit($"dn2cpp_array_copy_range({cs}->length, {si}, {cd}->length, {di}, {n});");
        string move = CopyMovesRefs(rep.Value, src, elementType) ? "dn2cpp_gc_memmove_refs" : "std::memmove";
        Emit(rep == ArrRep.N
            ? $"{move}({cd}->data + (size_t){di} * {cd}->elemSize, {cs}->data + (size_t){si} * {cs}->elemSize, (size_t){n} * {cs}->elemSize);"
            : $"{move}(&{cd}->data[{di}], &{cs}->data[{si}], (size_t){n} * sizeof({(rep == ArrRep.I4 ? "int32_t" : "Dn2CppObject*")}));");
    }

    /// <summary>Whether an inline Array.Copy arm moves GC references, so the move must
    /// dirty its destination. An element-sized rep with no element type in hand answers
    /// yes — the verdict dn2cpp_array_copy_dyn already makes for the same move.</summary>
    private static bool CopyMovesRefs(ArrRep rep, StackEntry src, TypeDesc? element)
    {
        if (rep == ArrRep.Ref)
            return true;
        if (rep == ArrRep.I4)
            return false;
        TypeDesc? e = element
            ?? (src.StaticType is { Kind: TypeKind.SZArray } s ? s.Element : null);
        return e is null || e.ContainsGcReferences();
    }

    private void EmitArrayClear(StackEntry arr, string idx, string len,
                                ArrayOperandKind kind = ArrayOperandKind.Argument)
    {
        ArrRep? rep = ArrayRepOfCppTypeOrNull(arr.CppType);
        if (rep is null)
        {
            Emit($"dn2cpp_array_clear_dyn({Cast(arr, "Dn2CppObject*")}, (int32_t)({idx}), (int32_t)({len}));");
            return;
        }
        string cpp = ArrayCppPtrOf(rep.Value);
        string i = NewTemp("int32_t"), n = NewTemp("int32_t");
        Emit($"{i} = (int32_t)({idx});");
        Emit($"{n} = (int32_t)({len});");
        string ca = NewTemp(cpp);
        Emit($"{ca} = {GuardArray(Cast(arr, cpp), kind)};");
        Emit($"dn2cpp_array_clear_range({ca}->length, {i}, {n});");
        Emit(rep == ArrRep.N
            ? $"std::memset({ca}->data + (size_t){i} * {ca}->elemSize, 0, (size_t){n} * {ca}->elemSize);"
            : $"std::memset(&{ca}->data[{i}], 0, (size_t){n} * sizeof({(rep == ArrRep.I4 ? "int32_t" : "Dn2CppObject*")}));");
    }

    /// <summary>The C++ pointer type an already-classified rep is addressed
    /// through — <see cref="ArrayCppPtr"/> keyed on the rep rather than on an
    /// element TypeDesc, which the copy/clear lowerings do not have.</summary>
    private static string ArrayCppPtrOf(ArrRep rep) => rep switch
    {
        ArrRep.I4 => "Dn2CppArrayI4*",
        ArrRep.Ref => "Dn2CppArrayRef*",
        _ => "Dn2CppArrayN*",
    };

    /// <summary>The C++ pointer type an SZArray of <paramref name="elem"/> is
    /// addressed through — the header the element rep lives behind.</summary>
    private static string ArrayCppPtr(TypeDesc elem) => RepOf(elem) switch
    {
        ArrRep.I4 => "Dn2CppArrayI4*",
        ArrRep.Ref => "Dn2CppArrayRef*",
        _ => "Dn2CppArrayN*",
    };

    /// <summary>Loads element <paramref name="idx"/> of the SZArray in the temp
    /// <paramref name="arr"/> (typed <see cref="ArrayCppPtr"/>) as a value of
    /// <c>CppTypes.Of(elem)</c> — the inverse of the store in EmitArrayFill. The
    /// element-sized rep is read through its storage width (a char[] is a packed
    /// char16_t buffer, a byte-backed enum[] a packed uint8_t one) and widened to
    /// the stack type; a struct's storage IS its stack type, so it copies.</summary>
    private static string ArrayElemLoad(TypeDesc elem, string arr, string idx)
    {
        string ct = CppTypes.Of(elem), st = CppTypes.StorageOf(elem);
        return RepOf(elem) switch
        {
            ArrRep.I4 => $"({ct}){arr}->data[{idx}]",
            ArrRep.Ref => $"({ct}){arr}->data[{idx}]",
            _ when st == ct => $"*({st}*)dn2cpp_elem_addr({arr}, {idx})",
            _ => $"({ct})(*({st}*)dn2cpp_elem_addr({arr}, {idx}))",
        };
    }

    /// <summary>Inline linear scan for Array.IndexOf&lt;T&gt; / Array.LastIndexOf&lt;T&gt;
    /// (array, value [, startIndex [, count]]) over any element type: the element
    /// equality is the devirtualized EqualityComparer&lt;T&gt;.Default.Equals every
    /// other key site uses (see TryEqualityEqualsLValue), so a struct element compares
    /// by its IEquatable&lt;T&gt;.Equals and a reference element by its Equals(object)
    /// override. Pushes the matching index, or -1. Avoids the real body's
    /// EqualityComparer&lt;T&gt;.Default / vectorized SpanHelpers.
    ///
    /// Every operand is spilled first, in IL push order: they are arbitrary
    /// expressions (an <c>xs.ToArray()</c> receiver, a <c>Next()</c> search value),
    /// and the loop reads each once per iteration.</summary>
    private void EmitArrayIndexOf(MethodSpecificationHandle msh, string name, TypeDesc elem)
    {
        var ms = _reader.GetMethodSpecification(msh);
        var ctx = new GenericContext(System.Array.Empty<TypeDesc>(), new[] { elem });
        int argc = ms.Method.Kind switch
        {
            HandleKind.MemberReference =>
                _reader.GetMemberReference((MemberReferenceHandle)ms.Method).DecodeMethodSignature(_c.SigProvider, ctx).ParameterTypes.Length,
            HandleKind.MethodDefinition =>
                _reader.GetMethodDefinition((MethodDefinitionHandle)ms.Method).DecodeSignature(_c.SigProvider, ctx).ParameterTypes.Length,
            _ => throw new NotSupportedException($"{_method.DeclaringClass.FullName}.{_method.Name}: Array.{name} spec has no method handle"),
        };
        if (!CanEqualityEquals(elem))
            throw new NotSupportedException(
                $"{_method.DeclaringClass.FullName}.{_method.Name}: Array.{name}<{elem}> element equality is not supported yet "
                + "(the element type has neither a typed Equals(T) nor an Equals(object) override)");

        bool last = name == "LastIndexOf";
        var count = argc >= 4 ? Pop() : null;
        var start = argc >= 3 ? Pop() : null;
        var value = Pop();
        var array = Pop();

        string arrCt = ArrayCppPtr(elem), elemCt = CppTypes.Of(elem);
        string arrT = NewTemp(arrCt);
        Emit($"{arrT} = {Cast(array, arrCt)};");
        var valT = new StackEntry(NewTemp(elemCt), CppTypes.KindOf(elem), elemCt);
        Emit($"{valT.Expr} = {Cast(value, elemCt)};");
        string len = $"((Dn2CppArray*){arrT})->length";
        string startT = NewTemp("int32_t");
        // Defaults, straight from the real overload chain: a forward scan starts at 0
        // and runs to the end; a backward one starts at the LAST element and runs to
        // the start. An empty array makes that startIndex -1 — which is why the
        // 1-argument LastIndexOf must not derive its count as `startIndex + 1`
        // (it would be 0 there, but the 2-argument form's caller-supplied 0 start
        // would give 1 and read data[0] of an empty buffer).
        Emit($"{startT} = {(start is null ? (last ? $"{len} - 1" : "0") : Cast(start, "int32_t"))};");
        string countT = NewTemp("int32_t");
        Emit($"{countT} = {(count is not null ? Cast(count, "int32_t")
            : last ? (start is null ? len : $"({len} == 0 ? 0 : {startT} + 1)")
            : $"{len} - {startT}")};");

        string r = NewTemp("int32_t");
        string i = NewTemp("int32_t");
        string stop = NewTemp("int32_t");
        var e = new StackEntry(NewTemp(elemCt), CppTypes.KindOf(elem), elemCt);
        string cmp = TryEqualityEqualsLValue(elem, e, valT)!;
        Emit($"{r} = -1;");
        Emit($"{stop} = {(last ? $"{startT} - {countT}" : $"{startT} + {countT}")};");
        Emit(last
            ? $"for ({i} = {startT}; {i} > {stop}; {i}--) {{"
            : $"for ({i} = {startT}; {i} < {stop}; {i}++) {{");
        Emit($"    {e.Expr} = {ArrayElemLoad(elem, arrT, i)};");
        Emit($"    if ({cmp}) {{ {r} = {i}; break; }}");
        Emit("}");
        _stack.Add(new StackEntry(r, StackKind.I4, "int32_t"));
    }

    /// <summary>Array.Fill&lt;T&gt;(array, value [, startIndex, count]) as a scalar
    /// element-store loop (the BCL vectorizes via SpanHelpers/Unsafe.BitCast). Whole-
    /// array (2 args) or the index/count range (4 args); all element reps.</summary>
    private void EmitArrayFill(MethodSpecificationHandle msh, TypeDesc elem)
    {
        var ms = _reader.GetMethodSpecification(msh);
        var ctx = new GenericContext(System.Array.Empty<TypeDesc>(), new[] { elem });
        int argc = ms.Method.Kind == HandleKind.MemberReference
            ? _reader.GetMemberReference((MemberReferenceHandle)ms.Method).DecodeMethodSignature(_c.SigProvider, ctx).ParameterTypes.Length
            : _reader.GetMethodDefinition((MethodDefinitionHandle)ms.Method).DecodeSignature(_c.SigProvider, ctx).ParameterTypes.Length;

        string? countExpr = argc >= 4 ? Pop().Expr : null;
        string startExpr = argc >= 4 ? Pop().Expr : "0";
        var value = Pop();
        var array = Pop();
        string len = $"((Dn2CppArray*)({array.Expr}))->length";
        countExpr ??= len;
        string elemCt = CppTypes.Of(elem);
        string vt = NewTemp(elemCt);
        Emit($"{vt} = {Cast(value, elemCt)};");
        string store = RepOf(elem) switch
        {
            ArrRep.I4 => $"((Dn2CppArrayI4*)({array.Expr}))->data[__fi] = (int32_t)({vt})",
            ArrRep.Ref => $"((Dn2CppArrayRef*)({array.Expr}))->data[__fi] = (Dn2CppObject*)({vt})",
            _ => $"*({CppTypes.StorageOf(elem)}*)dn2cpp_elem_addr(({Cast(array, "Dn2CppArrayN*")}), __fi) = ({CppTypes.StorageOf(elem)})({vt})",
        };
        Emit($"for (int32_t __fi = ({startExpr}); __fi < ({startExpr}) + ({countExpr}); __fi++) {{ {store}; }}");
        // Dirtying the array dirties every slot the loop wrote; an empty range
        // costs one no-op call.
        if (elem.ContainsGcReferences())
            Emit($"dn2cpp_gc_write_barrier((void*)({array.Expr}));");
    }

    // ---- element ordering: Array.Sort / Array.BinarySearch / MemoryExtensions.Sort ----

    /// <summary>The C++ type a sort callback receives an element by. The i4 and
    /// reference reps pass the array slot's own type by value; the element-sized rep
    /// passes an ADDRESS — a struct of arbitrary width has no uniform by-value C
    /// signature. <paramref name="byAddr"/> forces the address form for a callback whose
    /// helper addresses elements generically whatever the rep (the key+value sort).
    /// </summary>
    private static string SortCbParam(TypeDesc elem, bool byAddr = false) =>
        byAddr ? "const void*" : RepOf(elem) switch
        {
            ArrRep.I4 => "int32_t",
            ArrRep.Ref => "Dn2CppObject*",
            _ => "const void*",
        };

    /// <summary>The statements that give a thunk its two element lvalues (<c>_a</c> and
    /// <c>_b</c>, of the element's real C++ type) from the raw callback parameters
    /// <c>_x</c>/<c>_y</c>. Every comparison is built over these, never over the raw
    /// parameters: a compare expression reads each operand several times, and the struct
    /// arm needs an address.
    ///
    /// An address-passed element is read at its STORAGE width and widened to the stack
    /// type, exactly as ArrayElemLoad does: a short[] is a packed int16_t buffer, a
    /// byte-backed enum[] a packed uint8_t one, and dereferencing either at the int32
    /// stack type Of() reports splices the neighbouring element into the comparison.
    /// Declared one per statement — <c>T* a = p, b = q;</c> would make the second a
    /// <c>T</c>.</summary>
    private static string SortCbLoad(TypeDesc elem, bool byAddr = false)
    {
        string ct = CppTypes.Of(elem), st = CppTypes.StorageOf(elem);
        if (!byAddr && RepOf(elem) != ArrRep.N)
            return $"{ct} _a = ({ct})_x; {ct} _b = ({ct})_y;";
        return st == ct
            ? $"{ct} _a = *({ct}*)_x; {ct} _b = *({ct}*)_y;"
            : $"{ct} _a = ({ct})(*({st}*)_x); {ct} _b = ({ct})(*({st}*)_y);";
    }

    /// <summary>The width one element of <paramref name="elem"/> occupies in an array or
    /// span buffer — its STORAGE type, not the int32-promoted stack type: the byte-generic
    /// sort helpers stride by this.</summary>
    private static string SortElemSize(TypeDesc elem) =>
        $"(int32_t)sizeof({CppTypes.StorageOf(elem)})";

    /// <summary>The captureless thunk a comparer-driven sort calls back through — ABI
    /// <c>int32_t(void* ctx, E, E)</c>, see <see cref="SortCbParam"/> — for a comparer
    /// OBJECT: an <c>IComparer&lt;T&gt;</c> (resolve the interface on the ctx object's
    /// runtime type, call its Compare slot) or a <c>Comparison&lt;T&gt;</c> delegate
    /// (invoke it). It closes over nothing; the comparer IS the ctx. A user comparer
    /// arrives as the interface itself (Array.Sort) or as a concrete comparer class
    /// (MemoryExtensions.Sort&lt;T,TComparer&gt;) — both dispatch through the
    /// IComparer&lt;T&gt; the object implements.</summary>
    private string ComparerThunk(TypeDesc elem, ClassInfo ccls, bool byAddr = false)
    {
        _c.EnsureCompleted(ccls);
        string p = SortCbParam(elem, byAddr), ct = CppTypes.Of(elem), load = SortCbLoad(elem, byAddr);
        if (ccls.IsDelegate)
        {
            // Names dginvoke_<CppName> — record it like the Invoke call site does
            // (Compilation.DelegateInvokerUses).
            _c.DelegateInvokerUses.Add(ccls);
            return $"[](void* _ctx, {p} _x, {p} _y) -> int32_t {{ {load} "
                 + $"return dginvoke_{ccls.CppName}(({ccls.CppStructName}*)_ctx, _a, _b); }}";
        }
        var itf = ccls.IsInterface && _c.GenericDefFullName(ccls) == "System.Collections.Generic.IComparer"
            ? ccls
            : FindComparerInterface(ccls)
              ?? throw new NotSupportedException(
                  $"{_method.DeclaringClass.FullName}.{_method.Name}: sort comparer {ccls.FullName} is neither "
                  + "IComparer<T> nor Comparison<T>");
        _c.EnsureCompleted(itf);
        var compare = itf.Methods.FirstOrDefault(m => m.Name == "Compare")
            ?? throw new NotSupportedException(
                $"{_method.DeclaringClass.FullName}.{_method.Name}: {itf.FullName} has no Compare method");
        return $"[](void* _ctx, {p} _x, {p} _y) -> int32_t {{ {load} Dn2CppObject* _o = (Dn2CppObject*)_ctx; "
             + $"return (({FnPtrType(compare)})dn2cpp_resolve_interface(_o->type, &{itf.CppTypeInfoName})"
             + $"[{compare.VtableSlot}])(({itf.CppStructName}*)_o, _a, _b); }}";
    }

    /// <summary>A freshly allocated <c>Comparer&lt;T&gt;.Default</c> — the concrete
    /// <c>GenericComparer&lt;T&gt;</c> object the real (reflection-based) getter would
    /// hand back. Same shape as the <c>Comparer&lt;T&gt;.get_Default</c> intrinsic, minus
    /// its rgctx slot: there is no get_Default call token at a sort site to key one on,
    /// so a placeholder-bearing element drops the body to per-instantiation instead —
    /// where T is real and its type-info can be named.</summary>
    private string DefaultComparerObject(TypeDesc elem, ClassInfo gc)
    {
        TaintIfCanonical(elem, "sort-default-comparer");
        var gctor = Compilation.ParameterlessCtor(gc)
            ?? throw new NotSupportedException(
                $"{_method.DeclaringClass.FullName}.{_method.Name}: {gc.FullName} has no parameterless ctor");
        string o = NewTemp(gc.CppStructName + "*");
        Emit($"{o} = ({gc.CppStructName}*)dn2cpp_alloc(sizeof({gc.CppStructName}));");
        Emit($"((Dn2CppObject*){o})->type = &{gc.CppTypeInfoName};");
        Emit($"{DirectCallSym(gctor)}({ArgsWithRgctx(o, gctor)});");
        return o;
    }

    /// <summary>The DEFAULT (<c>Comparer&lt;T&gt;.Default</c>) order of an element type as
    /// a sort callback: the thunk plus the ctx it dispatches on. Two forms —
    ///
    /// <list type="bullet">
    /// <item>a devirtualized compare for a type that orders inline (primitive, string,
    /// enum, intrinsic value type): the expression the JIT would inline, ctx unused, no
    /// allocation;</item>
    /// <item>for every other T, the <c>GenericComparer&lt;T&gt;</c> object dispatched
    /// exactly like a user comparer. The struct's own <c>CompareTo</c> is deliberately
    /// NOT inlined into the thunk: a captureless lambda cannot read <c>__rgctx</c>, so a
    /// direct call to a shared callee from inside one would lose its hidden argument. The
    /// comparer object carries the dispatch instead, and its Compare — a real transpiled
    /// body — devirtualizes <c>x.CompareTo(y)</c> there, with the rgctx it is entitled to.
    /// </item>
    /// </list>
    ///
    /// Null when T has no order at all; the caller emits what real .NET does then (the
    /// comparer throws). Emits (the allocation), so call it at the emit position.</summary>
    private (string Thunk, string Ctx)? DefaultOrderCallback(TypeDesc elem, bool byAddr = false)
    {
        RequireRealElement(elem);
        string p = SortCbParam(elem, byAddr), load = SortCbLoad(elem, byAddr), ct = CppTypes.Of(elem);
        var a = new StackEntry("_a", CppTypes.KindOf(elem), ct);
        var b = new StackEntry("_b", CppTypes.KindOf(elem), ct);
        if (TryCompareLValue(elem, a, b) is { } inline)
            return ($"[](void* _ctx, {p} _x, {p} _y) -> int32_t {{ (void)_ctx; {load} return {inline}; }}",
                    "nullptr");
        if (_c.DefaultComparerFor(elem) is not { } gc)
            return null;
        string o = DefaultComparerObject(elem, gc);
        return (ComparerThunk(elem, _c.ComparerInterfaceFor(elem)!, byAddr), $"(void*){o}");
    }

    /// <summary>A canonical body cannot answer WHAT the default order of its element is —
    /// the answer (an inline compare, a comparer object, or no order at all, and if an
    /// object then whose type-info) is a different one for every T the body would cover, and
    /// unlike <c>Comparer&lt;T&gt;::get_Default</c> a sort site has no call token to key an
    /// rgctx slot on. So a placeholder-bearing element drops the body to per-instantiation,
    /// where T is real. Called BEFORE the decision, not inside it: deciding against the
    /// placeholder answers "no order" for every reference T there is, and compiles the
    /// throw.</summary>
    private void RequireRealElement(TypeDesc elem) => TaintIfCanonical(elem, "sort-default-comparer");

    /// <summary>The base pointer and element stride an SZArray's elements are addressed
    /// through by the byte-generic (key+value) sort helper — the rep's own storage, read
    /// off the array header.</summary>
    private static (string Data, string Stride) ArrayDataStride(TypeDesc elem, string arrT) => RepOf(elem) switch
    {
        ArrRep.I4 => ($"(void*){arrT}->data", "(int32_t)sizeof(int32_t)"),
        ArrRep.Ref => ($"(void*){arrT}->data", "(int32_t)sizeof(Dn2CppObject*)"),
        // The element-sized rep carries its own width; it is StorageOf's, and the array
        // header is the authority on it.
        _ => ($"(void*){arrT}->data", $"{arrT}->elemSize"),
    };

    /// <summary>Whether <paramref name="p"/> is the trailing comparer parameter of a
    /// Sort/BinarySearch overload: a closed <c>IComparer&lt;T&gt;</c>, a
    /// <c>Comparison&lt;T&gt;</c>, or — <c>MemoryExtensions.Sort&lt;T,TComparer&gt;</c>, whose
    /// comparer arrives by its CONCRETE type — any class that implements IComparer&lt;T&gt;.
    ///
    /// NOT "is a class": <c>BinarySearch&lt;T&gt;(T[], T)</c> ends in a class whenever T is
    /// one, and <c>Sort&lt;TKey,TValue&gt;(TKey[], TValue[])</c> ends in an array. Reading the
    /// overload set off the parameter COUNT instead is what made the pair sort pop its items
    /// array as a comparer.</summary>
    private bool IsComparerParam(TypeDesc p)
    {
        if (p is not { Kind: TypeKind.Class, Class: { } c })
            return false;
        _c.EnsureCompleted(c);
        return _c.GenericDefFullName(c) is "System.Collections.Generic.IComparer" or "System.Comparison"
            || FindComparerInterface(c) is not null;
    }

    /// <summary>Array.Sort&lt;T&gt; / Array.Sort&lt;TKey,TValue&gt; / Array.Reverse&lt;T&gt;,
    /// in place. The real bodies route through <c>ArraySortHelper&lt;T&gt;.Default</c> —
    /// <c>CreateInstanceForAnotherGenericParameter</c>, a runtime type-loader operation
    /// with no static lowering — and EventSource/calli, so every shape is emitted here.
    ///
    /// The overload is classified by its PARAMETER TYPES, never by their count: Sort's
    /// two-parameter forms are both <c>(T[], comparer)</c> and <c>(TKey[], TValue[])</c>,
    /// and counting popped the items array as a comparer.
    /// <code>
    ///   Sort&lt;T&gt;         (T[]) (T[],cmp) (T[],int,int) (T[],int,int,cmp)
    ///   Sort&lt;TKey,TValue&gt; (K[],V[]) (K[],V[],cmp) (K[],V[],int,int) (K[],V[],int,int,cmp)
    ///   Reverse&lt;T&gt;      (T[]) (T[],int,int)
    /// </code>
    /// A comparer argument may be null at run time (<c>List&lt;T&gt;.Sort()</c> reaches
    /// <c>Array.Sort(…, (IComparer&lt;T&gt;)null)</c> — that IS the default-order path), so
    /// every comparer arm carries the default order in its else branch.</summary>
    private void EmitArraySort(MethodSpecificationHandle msh, string name, TypeDesc[] methodArgs)
    {
        var ms = _reader.GetMethodSpecification(msh);
        // Decode with the spec's own type arguments as the method context so the comparer
        // parameter resolves to the *closed* IComparer<T> (e.g. <int>), not the open
        // IComparer<!!0> the caller context would leave it.
        var ctx = new GenericContext(System.Array.Empty<TypeDesc>(), methodArgs);
        var sig = ms.Method.Kind == HandleKind.MethodDefinition
            ? _reader.GetMethodDefinition((MethodDefinitionHandle)ms.Method).DecodeSignature(_c.SigProvider, ctx)
            : _reader.GetMemberReference((MemberReferenceHandle)ms.Method).DecodeMethodSignature(_c.SigProvider, ctx);
        var ps = sig.ParameterTypes;
        var t = methodArgs[0];

        bool pair = name == "Sort" && methodArgs.Length == 2;
        bool hasComparer = name == "Sort" && ps.Length > 0 && IsComparerParam(ps[^1]);
        int lead = pair ? 2 : 1;                                  // the array parameter(s)
        int rangeParams = ps.Length - lead - (hasComparer ? 1 : 0);
        if (rangeParams is not (0 or 2))
            throw new NotSupportedException(
                $"{_method.DeclaringClass.FullName}.{_method.Name}: Array.{name}<{t}> "
                + $"overload (/{ps.Length}) is not supported yet");

        // IL push order is left to right, so pop right to left.
        var comparer = hasComparer ? Pop() : null;
        var countE = rangeParams == 2 ? Pop() : null;
        var startE = rangeParams == 2 ? Pop() : null;
        var itemsE = pair ? Pop() : null;
        var arrE = Pop();

        // Every operand is spilled: a stack entry is an arbitrary expression (an
        // xs.ToArray() receiver), and the range is read more than once below.
        string arrCt = ArrayCppPtr(t);
        string arrT = NewTemp(arrCt);
        Emit($"{arrT} = {Cast(arrE, arrCt)};");
        string startT = NewTemp("int32_t"), countT = NewTemp("int32_t");
        Emit($"{startT} = {(startE is null ? "0" : Cast(startE, "int32_t"))};");
        Emit($"{countT} = {(countE is null ? $"((Dn2CppArray*){arrT})->length" : Cast(countE, "int32_t"))};");

        if (name == "Reverse")
        {
            string rev = RepOf(t) switch
            {
                ArrRep.I4 => "dn2cpp_array_reverse_i4",
                ArrRep.Ref => "dn2cpp_array_reverse_ref",
                _ => "dn2cpp_array_reverse_n",
            };
            Emit($"{rev}({arrT}, {startT}, {countT});");
            return;
        }

        // Array.Sort<TKey,TValue>(keys, items, …): sort the keys and permute the items in
        // lockstep. A null items array is legal (it degrades to a plain key sort), so the
        // helper takes the buffer and checks it. Both keys reach the comparison BY ADDRESS
        // whatever their rep, so the one helper serves every element type.
        if (pair)
        {
            var tv = methodArgs[1];
            string itemsCt = ArrayCppPtr(tv);
            string itemsT = NewTemp(itemsCt);
            Emit($"{itemsT} = {Cast(itemsE!, itemsCt)};");
            var (kd, kst) = ArrayDataStride(t, arrT);
            var (vd, vst) = ArrayDataStride(tv, itemsT);
            string call = $"dn2cpp_sort_pair({kd}, {kst}, "
                        + $"({itemsT} != nullptr ? {vd} : nullptr), ({itemsT} != nullptr ? {vst} : 0), "
                        + $"{startT}, {countT}";
            if (comparer is { } pcmp)
            {
                string pcmpT = NewTemp("Dn2CppObject*");
                Emit($"{pcmpT} = {Cast(pcmp, "Dn2CppObject*")};");
                var pccls = ps[^1].Class
                    ?? throw new NotSupportedException(
                        $"{_method.DeclaringClass.FullName}.{_method.Name}: Array.Sort comparer parameter is not a class type");
                Emit($"if ({pcmpT} != nullptr) {{");
                Emit($"    {call}, (void*){pcmpT}, {ComparerThunk(t, pccls, byAddr: true)});");
                Emit("} else {");
                EmitDefaultSortPair(t, call);
                Emit("}");
                return;
            }
            EmitDefaultSortPair(t, call);
            return;
        }

        // Single-array sort. The comparer-driven helpers are keyed on the element's rep;
        // the comparerless int/long/double/string forms keep their dedicated natural-order
        // helpers (no callback, no allocation).
        string cmpFn = RepOf(t) switch
        {
            ArrRep.I4 => "dn2cpp_array_sort_cmp_i4",
            ArrRep.Ref => "dn2cpp_array_sort_cmp_ref",
            _ => "dn2cpp_array_sort_cmp_n",
        };
        if (comparer is not { } cmp)
        {
            EmitDefaultSort(t, arrT, startT, countT, cmpFn);
            return;
        }
        string cmpT = NewTemp("Dn2CppObject*");
        Emit($"{cmpT} = {Cast(cmp, "Dn2CppObject*")};");
        var ccls = ps[^1].Class
            ?? throw new NotSupportedException(
                $"{_method.DeclaringClass.FullName}.{_method.Name}: Array.Sort comparer parameter is not a class type");
        Emit($"if ({cmpT} != nullptr) {{");
        Emit($"    {cmpFn}({arrT}, {startT}, {countT}, (void*){cmpT}, {ComparerThunk(t, ccls)});");
        Emit("} else {");
        // A null comparer means Comparer<T>.Default — the arm List<T>.Sort() lands on.
        EmitDefaultSort(t, arrT, startT, countT, cmpFn);
        Emit("}");
    }

    /// <summary>The natural-order sort of a whole range: the dedicated helper for an
    /// element whose order is a machine compare (int/long/double/string — no callback, no
    /// allocation), else the rep's callback helper driven by
    /// <see cref="DefaultOrderCallback"/>. An element with no order at all faults the way
    /// real .NET's comparer does — <c>Array.Sort</c> on a T that implements no
    /// IComparable throws, it does not sort by something else (the old code fell back to
    /// the ORDINAL STRING compare for every reference element, which read a non-string
    /// object's bytes as a string).</summary>
    private void EmitDefaultSort(TypeDesc t, string arrT, string startT, string countT, string cmpFn)
    {
        string? natural = t switch
        {
            { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Int32 } => "dn2cpp_array_sort_i4",
            { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Int64 } => "dn2cpp_array_sort_i8",
            { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Double } => "dn2cpp_array_sort_r8",
            { IsString: true } => "dn2cpp_array_sort_str",
            _ => null,
        };
        if (natural is not null)
        {
            Emit($"    {natural}({arrT}, {startT}, {countT});");
            return;
        }
        if (DefaultOrderCallback(t) is not { } cb)
        {
            Emit("    dn2cpp_throw_invalid_operation();");
            return;
        }
        Emit($"    {cmpFn}({arrT}, {startT}, {countT}, {cb.Ctx}, {cb.Thunk});");
    }

    /// <summary>The natural-order sort of a whole span — the span twin of
    /// <see cref="EmitDefaultSort"/>, over a raw element pointer + length.</summary>
    private void EmitDefaultSpanSort(TypeDesc t, string pT, string nT, string cmpFn, string cmpArgs)
    {
        string? natural = t switch
        {
            { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Int32 } => "dn2cpp_span_sort_i4",
            { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Int64 } => "dn2cpp_span_sort_i8",
            { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Double } => "dn2cpp_span_sort_r8",
            { IsString: true } => "dn2cpp_span_sort_str",
            _ => null,
        };
        if (natural is not null)
        {
            // The natural helpers are typed; a span pointer arrives at the rep's width.
            string ptr = t switch
            {
                { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Int32 } => "int32_t*",
                { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Int64 } => "int64_t*",
                { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Double } => "double*",
                _ => "Dn2CppObject**",
            };
            Emit($"    {natural}(({ptr}){pT}, {nT});");
            return;
        }
        if (DefaultOrderCallback(t) is not { } cb)
        {
            Emit("    dn2cpp_throw_invalid_operation();");
            return;
        }
        Emit($"    {cmpFn}({cmpArgs}, {cb.Ctx}, {cb.Thunk});");
    }

    /// <summary>The key+value sort under the key's DEFAULT order — the comparerless
    /// overload, and the arm a null comparer falls into.</summary>
    private void EmitDefaultSortPair(TypeDesc key, string call)
    {
        if (DefaultOrderCallback(key, byAddr: true) is not { } cb)
        {
            Emit("    dn2cpp_throw_invalid_operation();");
            return;
        }
        Emit($"    {call}, {cb.Ctx}, {cb.Thunk});");
    }

    /// <summary>Array.BinarySearch&lt;T&gt;(array, [index, length,] value [, comparer]) as
    /// an inline binary search — the real body reaches ArraySortHelper the same way Sort
    /// does. The element compare is the devirtualized Comparer&lt;T&gt;.Default every other
    /// key site uses, so a struct element searches by its own IComparable&lt;T&gt;.CompareTo.
    /// Unlike the sort thunk, this compare sits in normal emitted code, so a shared body's
    /// <c>__rgctx</c> is in scope and a struct's CompareTo is called directly.
    /// Pushes the found index, or the bitwise complement of the insertion point (~lo) —
    /// .NET's contract.
    ///
    /// The overload set is unambiguous by count, and each shape is validated against the
    /// parameter types before it is popped:
    /// <code>
    ///   (T[],T) (T[],T,cmp) (T[],int,int,T) (T[],int,int,T,cmp)
    /// </code></summary>
    private void EmitArrayBinarySearch(MethodSpecificationHandle msh, TypeDesc[] methodArgs)
    {
        var ms = _reader.GetMethodSpecification(msh);
        var ctx = new GenericContext(System.Array.Empty<TypeDesc>(), methodArgs);
        var sig = ms.Method.Kind == HandleKind.MethodDefinition
            ? _reader.GetMethodDefinition((MethodDefinitionHandle)ms.Method).DecodeSignature(_c.SigProvider, ctx)
            : _reader.GetMemberReference((MemberReferenceHandle)ms.Method).DecodeMethodSignature(_c.SigProvider, ctx);
        var ps = sig.ParameterTypes;
        var t = methodArgs[0];

        bool hasComparer = ps.Length is 3 or 5;
        bool hasRange = ps.Length is 4 or 5;
        if (ps.Length is < 2 or > 5
            || (hasComparer && !IsComparerParam(ps[^1]))
            || (hasRange && ps[1] is not { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Int32 }))
            throw new NotSupportedException(
                $"{_method.DeclaringClass.FullName}.{_method.Name}: Array.BinarySearch<{t}> "
                + $"overload (/{ps.Length}) is not supported yet");
        // Even the comparer overload falls back to Comparer<T>.Default on a null comparer
        // (List<T>.BinarySearch(value) reaches it that way), so the element must be real.
        RequireRealElement(t);

        var comparer = hasComparer ? Pop() : null;
        var valueE = Pop();
        var countE = hasRange ? Pop() : null;
        var startE = hasRange ? Pop() : null;
        var arrE = Pop();

        string arrCt = ArrayCppPtr(t), elemCt = CppTypes.Of(t);
        string arrT = NewTemp(arrCt);
        Emit($"{arrT} = {Cast(arrE, arrCt)};");
        var valT = new StackEntry(NewTemp(elemCt), CppTypes.KindOf(t), elemCt);
        Emit($"{valT.Expr} = {Cast(valueE, elemCt)};");
        string lo = NewTemp("int32_t"), hi = NewTemp("int32_t");
        Emit($"{lo} = {(startE is null ? "0" : Cast(startE, "int32_t"))};");
        Emit($"{hi} = {lo} + {(countE is null ? $"((Dn2CppArray*){arrT})->length" : Cast(countE, "int32_t"))} - 1;");

        // The compare of one element against the search value, as an expression over two
        // lvalues. Three sources, in the order Comparer<T>.Default resolves them.
        var e = new StackEntry(NewTemp(elemCt), CppTypes.KindOf(t), elemCt);
        string? inlineCmp = TryDefaultCompareLValue(t, e, valT);
        string? cmpExpr = inlineCmp;
        if (comparer is { } cmp)
        {
            // A supplied IComparer<T> — dispatched through the interface. It may still be
            // null at run time (that is Comparer<T>.Default), so the else arm stands: the
            // branch is loop-invariant and the C++ compiler hoists it.
            var ccls = ps[^1].Class
                ?? throw new NotSupportedException(
                    $"{_method.DeclaringClass.FullName}.{_method.Name}: "
                    + "Array.BinarySearch comparer parameter is not a class type");
            string cmpT = NewTemp("Dn2CppObject*");
            Emit($"{cmpT} = {Cast(cmp, "Dn2CppObject*")};");
            if (inlineCmp is null && _c.DefaultComparerFor(t) is { } dgc)
            {
                // No inline order for T, but it has a real comparer: materialize
                // Comparer<T>.Default once, up front, and dispatch everything alike.
                Emit($"if ({cmpT} == nullptr) {{");
                Emit($"    {cmpT} = (Dn2CppObject*){DefaultComparerObject(t, dgc)};");
                Emit("}");
                cmpExpr = ComparerDispatchExpr(t, ccls, cmpT, e, valT);
            }
            else
            {
                string dispatch = ComparerDispatchExpr(t, ccls, cmpT, e, valT);
                cmpExpr = inlineCmp is null
                    ? $"({cmpT} != nullptr ? {dispatch} : (dn2cpp_throw_invalid_operation(), 0))"
                    : $"({cmpT} != nullptr ? {dispatch} : {inlineCmp})";
            }
        }
        else if (cmpExpr is null)
        {
            if (_c.DefaultComparerFor(t) is not { } dgc)
            {
                // No order at all — the real Comparer<T>.Default throws when it is asked to
                // compare, and so does this.
                Emit("dn2cpp_throw_invalid_operation();");
                Push(StackKind.I4, "int32_t", "-1");
                return;
            }
            string dcmpT = NewTemp("Dn2CppObject*");
            Emit($"{dcmpT} = (Dn2CppObject*){DefaultComparerObject(t, dgc)};");
            cmpExpr = ComparerDispatchExpr(t, _c.ComparerInterfaceFor(t)!, dcmpT, e, valT);
        }

        string mid = NewTemp("int32_t"), ord = NewTemp("int32_t"), found = NewTemp("int32_t");
        Emit($"{found} = -1;");
        Emit($"while ({lo} <= {hi}) {{");
        Emit($"    {mid} = {lo} + (({hi} - {lo}) >> 1);");
        Emit($"    {e.Expr} = {ArrayElemLoad(t, arrT, mid)};");
        Emit($"    {ord} = {cmpExpr};");
        Emit($"    if ({ord} == 0) {{ {found} = {mid}; break; }}");
        Emit($"    if ({ord} < 0) {lo} = {mid} + 1; else {hi} = {mid} - 1;");
        Emit("}");
        // Not found: the complement of the insertion point, the way .NET reports it.
        Push(StackKind.I4, "int32_t", $"({found} >= 0 ? {found} : ~{lo})");
    }

    /// <summary>An <c>IComparer&lt;T&gt;.Compare(x, y)</c> on a comparer object held in a
    /// temp, resolved through the interface table at each call. The comparer arrives as
    /// the interface itself or as a concrete comparer class; both dispatch through the
    /// IComparer&lt;T&gt; the object implements.</summary>
    private string ComparerDispatchExpr(TypeDesc elem, ClassInfo ccls, string cmpT, StackEntry a, StackEntry b)
    {
        _c.EnsureCompleted(ccls);
        var itf = ccls.IsInterface && _c.GenericDefFullName(ccls) == "System.Collections.Generic.IComparer"
            ? ccls
            : FindComparerInterface(ccls)
              ?? throw new NotSupportedException(
                  $"{_method.DeclaringClass.FullName}.{_method.Name}: comparer {ccls.FullName} is not an IComparer<T>");
        _c.EnsureCompleted(itf);
        var compare = itf.Methods.FirstOrDefault(m => m.Name == "Compare")
            ?? throw new NotSupportedException(
                $"{_method.DeclaringClass.FullName}.{_method.Name}: {itf.FullName} has no Compare method");
        string ct = CppTypes.Of(elem);
        return $"(({FnPtrType(compare)})dn2cpp_resolve_interface({cmpT}->type, &{itf.CppTypeInfoName})"
             + $"[{compare.VtableSlot}])(({itf.CppStructName}*){cmpT}, {Cast(a, ct)}, {Cast(b, ct)})";
    }

    /// <summary>The DEFAULT (<c>Comparer&lt;T&gt;.Default</c>) three-way compare over two
    /// element LVALUES, as a pure expression — the ordering twin of
    /// <see cref="TryEqualityEqualsLValue"/>. Unlike the sort THUNK (a captureless lambda
    /// that cannot read <c>__rgctx</c>), this sits in normal emitted code, so a struct's
    /// typed <c>CompareTo(T)</c> is called directly, with the hidden argument a shared
    /// callee is entitled to. Null when the element neither orders inline nor has a typed
    /// CompareTo — then the comparer OBJECT is the only route.</summary>
    private string? TryDefaultCompareLValue(TypeDesc t, StackEntry a, StackEntry b)
    {
        if (TryCompareLValue(t, a, b) is { } inline)
            return inline;
        if (t is { Kind: TypeKind.Class, Class: { IsValueType: true, IsEnum: false } sc }
            && Compilation.TranspiledTypedCompareTo(sc) is { } tct)
            return $"{DirectCallSym(tct)}({ArgsWithRgctx($"&{a.Expr}, {Cast(b, CppTypes.Of(t))}", tct)})";
        return null;
    }

    /// <summary>A pointer to the span struct from a popped receiver: an address
    /// (ldloca, the usual `this`-by-ref for a struct method) is used directly; a
    /// by-value span is spilled and its address taken.</summary>
    private string SpanPtr(StackEntry e, string spanCt)
    {
        if (e.Kind == StackKind.Ptr)
            return $"(({spanCt}*)({e.Expr}))";
        string t = NewTemp(spanCt);
        Emit($"{t} = {Cast(e, spanCt)};");
        return $"(&{t})";
    }

    /// <summary>Span&lt;T&gt;/ReadOnlySpan&lt;T&gt; instance bulk methods
    /// (Clear/Fill/CopyTo/ToArray) as element loops — the BCL bodies use SpanHelpers /
    /// Buffer.Memmove (Unsafe.WriteUnaligned / InternalCall), untranspilable. The
    /// element pointer is read at StorageOf width so sub-word element spans work.
    /// Returns false (fall through to a normal call) for any other member.</summary>
    private bool TryEmitSpanInstanceBulk(MemberReference mr, TypeDesc parent)
    {
        if (parent.Class is not { } sc
            || _c.GenericDefFullName(sc) is not ("System.Span" or "System.ReadOnlySpan"))
            return false;
        string name = _reader.GetString(mr.Name);
        if (name is not ("Clear" or "Fill" or "CopyTo" or "TryCopyTo" or "ToArray"))
            return false;

        var elem = sc.Context.TypeArgs[0];
        string elemCt = CppTypes.Of(elem);
        string elemSt = CppTypes.StorageOf(elem);
        string spanCt = CppTypes.Of(parent);
        var sig = mr.DecodeMethodSignature(_c.SigProvider, sc.Context);
        string i = NewTemp("int32_t");

        switch (name)
        {
            case "Clear":
            {
                string sp = SpanPtr(Pop(), spanCt);
                Emit($"for ({i} = 0; {i} < {sp}->f__length; {i}++) (({elemSt}*){sp}->f__reference)[{i}] = {CppTypes.ZeroInitExpr(elemSt)};");
                return true;
            }
            case "Fill":
            {
                var value = Pop();
                string sp = SpanPtr(Pop(), spanCt);
                string vt = NewTemp(elemCt);
                Emit($"{vt} = {Cast(value, elemCt)};");
                Emit($"for ({i} = 0; {i} < {sp}->f__length; {i}++) (({elemSt}*){sp}->f__reference)[{i}] = ({elemSt})({vt});");
                return true;
            }
            case "CopyTo":
            {
                string destCt = CppTypes.Of(sig.ParameterTypes[0]);
                var dest = Pop();
                string sp = SpanPtr(Pop(), spanCt);
                string dt = NewTemp(destCt);
                Emit($"{dt} = {(dest.Kind == StackKind.Ptr ? $"*({destCt}*)({dest.Expr})" : Cast(dest, destCt))};");
                Emit($"if ({sp}->f__length > {dt}.f__length) dn2cpp_fail(\"Destination too short (Span.CopyTo)\");");
                Emit($"for ({i} = 0; {i} < {sp}->f__length; {i}++) (({elemSt}*){dt}.f__reference)[{i}] = (({elemSt}*){sp}->f__reference)[{i}];");
                return true;
            }
            // TryCopyTo(destination) — like CopyTo but returns false instead of throwing
            // when the source doesn't fit. The real body routes through Buffer.Memmove ->
            // BulkMoveWithWriteBarrier (an InternalCall); reached from JsonReaderHelper's
            // string unescape. Emit the same element copy guarded by a fit check.
            case "TryCopyTo":
            {
                string destCt = CppTypes.Of(sig.ParameterTypes[0]);
                var dest = Pop();
                string sp = SpanPtr(Pop(), spanCt);
                string dt = NewTemp(destCt);
                Emit($"{dt} = {(dest.Kind == StackKind.Ptr ? $"*({destCt}*)({dest.Expr})" : Cast(dest, destCt))};");
                string okT = NewTemp("int32_t");
                Emit($"{okT} = ({sp}->f__length <= {dt}.f__length) ? 1 : 0;");
                Emit($"if ({okT}) for ({i} = 0; {i} < {sp}->f__length; {i}++) "
                    + $"(({elemSt}*){dt}.f__reference)[{i}] = (({elemSt}*){sp}->f__reference)[{i}];");
                Push(StackKind.I4, "int32_t", okT);
                return true;
            }
            case "ToArray":
            {
                string sp = SpanPtr(Pop(), spanCt);
                string lenT = NewTemp("int32_t");
                Emit($"{lenT} = {sp}->f__length;");
                EmitNewarr(elem, lenT);
                var arr = Pop();
                string baseExpr = RepOf(elem) switch
                {
                    ArrRep.I4 => $"({elemSt}*)((Dn2CppArrayI4*)({arr.Expr}))->data",
                    ArrRep.Ref => $"({elemSt}*)((Dn2CppArrayRef*)({arr.Expr}))->data",
                    _ => $"({elemSt}*)dn2cpp_elem_addr(({Cast(arr, "Dn2CppArrayN*")}), 0)",
                };
                string bt = NewTemp(elemSt + "*");
                // Compute the base (the N path bounds-checks index 0) only when non-empty.
                Emit($"if ({lenT} > 0) {{");
                Emit($"    {bt} = {baseExpr};");
                Emit($"    for ({i} = 0; {i} < {lenT}; {i}++) {bt}[{i}] = (({elemSt}*){sp}->f__reference)[{i}];");
                Emit("}");
                _stack.Add(arr);
                return true;
            }
        }
        return false;
    }

    /// <summary>Span&lt;T&gt;/ReadOnlySpan&lt;T&gt;.get_Item at a call site inside a
    /// <c>[HotPath(SkipBoundsChecks = true)]</c> body: the byref is raw
    /// <c>f__reference + index</c> pointer arithmetic instead of a call to the
    /// checked BCL body (the caller's in-range contract; out-of-range is the
    /// knob's documented UB). Route-without-cut — the real get_Item stays
    /// resolvable for every cold caller, <c>ResolveCallTarget</c> untouched —
    /// which is the safe direction of the intercept invariant (at worst bloat,
    /// never a dangling symbol). The pointer is pushed at StorageOf width so
    /// sub-word element spans address the packed slot, matching the bulk loops
    /// above. Declines (falls through to the normal call) for any other member
    /// or shape — a name-gated route may decline a shape, never pop one.</summary>
    private bool TryEmitHotSpanItem(MemberReference mr, TypeDesc parent)
    {
        if (parent.Class is not { } sc
            || _c.GenericDefFullName(sc) is not ("System.Span" or "System.ReadOnlySpan")
            || _reader.GetString(mr.Name) != "get_Item")
            return false;
        var sig = mr.DecodeMethodSignature(_c.SigProvider, sc.Context);
        if (!sig.Header.IsInstance || sig.ParameterTypes.Length != 1)
            return false;
        string elemSt = CppTypes.StorageOf(sc.Context.TypeArgs[0]);
        var idx = Pop();
        var recv = Pop();
        // [HotPath(NoAlias)]: address the hoisted __restrict base instead of
        // re-reading f__reference off the span struct, which is where the
        // aliasing claim lives (see SetupNoAliasSpanLocals). Declines to the
        // ordinary read for every receiver that is not a hoisted parameter.
        if (NoAliasSpanBase(recv) is { } naBase)
        {
            Push(StackKind.Ptr, elemSt + "*", $"{naBase} + ({idx.Expr})");
            return true;
        }
        string sp = SpanPtr(recv, CppTypes.Of(parent));
        Push(StackKind.Ptr, elemSt + "*", $"(({elemSt}*){sp}->f__reference) + ({idx.Expr})");
        return true;
    }

    /// <summary>The storage width in bytes of an integral/floating primitive, or
    /// null for non-fixed-width primitives (string/object/void/native int).</summary>
    private static int? PrimitiveByteWidth(PrimitiveTypeCode p) => p switch
    {
        PrimitiveTypeCode.Boolean or PrimitiveTypeCode.Byte or PrimitiveTypeCode.SByte => 1,
        PrimitiveTypeCode.Char or PrimitiveTypeCode.Int16 or PrimitiveTypeCode.UInt16 => 2,
        PrimitiveTypeCode.Int32 or PrimitiveTypeCode.UInt32 or PrimitiveTypeCode.Single => 4,
        PrimitiveTypeCode.Int64 or PrimitiveTypeCode.UInt64 or PrimitiveTypeCode.Double => 8,
        _ => null,
    };

    /// <summary>Reads the raw RVA bytes backing a static-array-initializer field
    /// (the <c>&lt;PrivateImplementationDetails&gt;</c> blob behind
    /// <c>RuntimeHelpers.InitializeArray</c>). The byte count is the field type's
    /// explicit layout size, or — when the blob is exactly a primitive's width and
    /// Roslyn types the field with that primitive (e.g. a 4-byte <c>byte[]{…}</c>
    /// initializer typed <c>int32</c>) — that primitive's width.</summary>
    private byte[] FieldRvaBlob(FieldDefinitionHandle fdh)
    {
        var fd = _reader.GetFieldDefinition(fdh);
        int rva = fd.GetRelativeVirtualAddress();
        var ftype = fd.DecodeSignature(_c.SigProvider, _method.Context);
        int size = ftype switch
        {
            { Kind: TypeKind.Class, Class: { } c } => _module.Reader.GetTypeDefinition(c.Handle).GetLayout().Size,
            { Kind: TypeKind.Primitive, Primitive: var p } when PrimitiveByteWidth(p) is { } w => w,
            _ => throw new NotSupportedException(
                $"{_method.DeclaringClass.FullName}.{_method.Name}: ldtoken field of non-struct type {ftype} is not supported"),
        };
        var reader = _module.PE.GetSectionData(rva).GetReader();
        return reader.ReadBytes(size);
    }

    /// <summary>RuntimeHelpers.InitializeArray(array, fieldHandle): copy the RVA
    /// blob (already on the stack as a byte pointer) into the array's element
    /// storage. Byte count is the array's element payload.</summary>
    private void EmitInitializeArray(StackEntry arr, string src)
    {
        // A multi-dim array (Dn2CppMDArray) stores its elements in a separately
        // allocated `data` buffer (not inline), so it can't be cast to the ArrayN
        // shape. Copy the packed RVA blob over the whole element payload
        // (total length × elemSize). With the elemSize now StorageOf-sized,
        // a sub-word T[,] literal's slots match the blob's packed layout.
        if (arr.CppType == "Dn2CppMDArray*")
        {
            Emit($"{{ Dn2CppMDArray* __md = ({Cast(arr, "Dn2CppMDArray*")}); " +
                 $"std::memcpy(__md->data, {src}, (size_t)dn2cpp_md_total_length(__md) * __md->elemSize); }}");
            return;
        }
        switch (ArrRepOfCppType(arr.CppType))
        {
            case ArrRep.I4:
                Emit($"std::memcpy(({Cast(arr, "Dn2CppArrayI4*")})->data, {src}, " +
                     $"(size_t)((Dn2CppArray*)({arr.Expr}))->length * sizeof(int32_t));");
                return;
            default:
                Emit($"{{ Dn2CppArrayN* __ia = ({Cast(arr, "Dn2CppArrayN*")}); " +
                     $"std::memcpy(__ia->data, {src}, (size_t)__ia->length * __ia->elemSize); }}");
                return;
        }
    }

    /// <summary>The array representation implied by a stack value's static C++
    /// type (e.g. <c>Dn2CppArrayI4*</c>).</summary>
    private ArrRep ArrRepOfCppType(string cppType)
    {
        return ArrayRepOfCppTypeOrNull(cppType) ?? throw new NotSupportedException(
            $"{_method.DeclaringClass.FullName}.{_method.Name}: array operation on a non-concrete array type {cppType}");
    }

    /// <summary>Like <see cref="ArrRepOfCppType"/>, but null when the static type
    /// carries no rep (a receiver degraded to <c>Dn2CppObject*</c>, e.g. by a
    /// <c>?.</c> merge) — the caller falls back to runtime type-info dispatch.</summary>
    private static ArrRep? ArrayRepOfCppTypeOrNull(string cppType)
    {
        if (cppType.Contains("ArrayI4"))
            return ArrRep.I4;
        if (cppType.Contains("ArrayRef"))
            return ArrRep.Ref;
        if (cppType.Contains("ArrayN"))
            return ArrRep.N;
        return null;
    }

    /// <summary>A <c>System.Buffer</c> operand as the <c>(object, rep)</c> pair
    /// <c>dn2cpp_buffer_blockcopy</c> / <c>dn2cpp_buffer_bytelength</c> take. Everything
    /// the guard needs past the pointer — the data offset, the element width, whether the
    /// elements can be blitted at all — differs per rep and none of it is stated by the
    /// <c>System.Array</c> the signature declares, so the verdict travels as a constant the
    /// switches there fold away. It is taken from the operand's STATIC type rather than
    /// left to the header, which cannot answer it: an array the C++ runtime allocated
    /// carries an imprecise handle with no elementType, and a run-time element test over
    /// that is fail-open. An operand whose static type names no element — an
    /// <c>Array</c>-typed one, a shared body's <c>T[]</c> — passes DYN and is classified at
    /// run time, where an imprecise handle is refused rather than guessed at.</summary>
    private string BufferOperand(StackEntry arr) =>
        $"{Cast(arr, "Dn2CppObject*")}, {BufferOperandRep(arr)}";

    private static string BufferOperandRep(StackEntry arr)
    {
        if (arr.StaticType is not { Kind: TypeKind.SZArray or TypeKind.MDArray, Element: { } el }
            || BufferBlittableElement(el) is not bool blittable)
        {
            return "DN2CPP_BCREP_DYN";
        }
        if (!blittable)
            return "DN2CPP_BCREP_NONPRIM";
        if (arr.StaticType.Kind == TypeKind.MDArray)
            return "DN2CPP_BCREP_MD";
        return RepOf(el) == ArrRep.I4 ? "DN2CPP_BCREP_I4" : "DN2CPP_BCREP_N";
    }

    /// <summary>Whether <c>System.Buffer</c> will blit an element of this type — .NET asks
    /// the element's CorElementType, so an enum counts and <c>decimal</c>, <c>DateTime</c>,
    /// <c>Nullable&lt;T&gt;</c> and every other struct do not (all measured). Null means
    /// "cannot tell from here" — an unloaded TypeRef, a generic placeholder — and defers to
    /// the runtime type-info; answering false there would refuse a blit .NET performs.</summary>
    private static bool? BufferBlittableElement(TypeDesc el) => el.Kind switch
    {
        TypeKind.Primitive => el.Primitive is not (PrimitiveTypeCode.Object or PrimitiveTypeCode.String
            or PrimitiveTypeCode.TypedReference or PrimitiveTypeCode.Void),
        TypeKind.Class => el.Class!.IsEnum,
        TypeKind.SZArray or TypeKind.MDArray or TypeKind.Pointer or TypeKind.ByRef => false,
        _ => null,
    };

    /// <summary>The checked element-access expression family — and, under
    /// <c>[HotPath(SkipBoundsChecks = true)]</c>, its raw twin: plain indexing with
    /// no bounds check and no null check either. Both are the knob's documented UB —
    /// a field access otherwise carries a catchable NullReferenceException.
    /// The knob opts out of BOTH checks rather than only the range one:
    /// its contract is that the caller has proved the access, and a guard on the
    /// pointer that the proof already covers is the cost [HotPath] exists to shed.
    /// Every ldelem/stelem/ldelema emission site routes through these five so the
    /// knob cannot cover one rep and miss another; with the knob off each returns
    /// byte-identically the string its call site used to inline. Duplicating
    /// <c>arr.Expr</c>/<c>idx.Expr</c> is sound by the standing invariant these
    /// sites already rely on (bounds check + use): a popped entry's expression is
    /// stable and side-effect-free.</summary>
    private string LdelemI4(StackEntry arr, StackEntry idx) => _method.SkipBoundsChecks
        ? $"(({Cast(arr, "Dn2CppArrayI4*")})->data[{idx.Expr}])"
        : $"dn2cpp_ldelem_i4(({Cast(arr, "Dn2CppArrayI4*")}), {idx.Expr})";

    private string LdelemRef(StackEntry arr, StackEntry idx) => _method.SkipBoundsChecks
        ? $"(({Cast(arr, "Dn2CppArrayRef*")})->data[{idx.Expr}])"
        : $"dn2cpp_ldelem_ref(({Cast(arr, "Dn2CppArrayRef*")}), {idx.Expr})";

    private string StelemI4(StackEntry arr, StackEntry idx, string valExpr) => _method.SkipBoundsChecks
        ? $"({Cast(arr, "Dn2CppArrayI4*")})->data[{idx.Expr}] = {valExpr};"
        : $"dn2cpp_stelem_i4(({Cast(arr, "Dn2CppArrayI4*")}), {idx.Expr}, {valExpr});";

    private string StelemRef(StackEntry arr, StackEntry idx, string valExpr) => _method.SkipBoundsChecks
        ? $"dn2cpp_gc_store_ref(&({Cast(arr, "Dn2CppArrayRef*")})->data[{idx.Expr}], {valExpr});"
        : $"dn2cpp_stelem_ref(({Cast(arr, "Dn2CppArrayRef*")}), {idx.Expr}, {valExpr});";

    private string ElemAddr(StackEntry arr, StackEntry idx) => _method.SkipBoundsChecks
        ? $"(({Cast(arr, "Dn2CppArrayN*")})->data + (size_t)({idx.Expr}) * ({Cast(arr, "Dn2CppArrayN*")})->elemSize)"
        : $"dn2cpp_elem_addr(({Cast(arr, "Dn2CppArrayN*")}), {idx.Expr})";

    private void EmitLdelem(TypeDesc element)
    {
        var idx = Pop();
        var arr = Pop();
        switch (RepOf(element))
        {
            case ArrRep.I4:
                Push(StackKind.I4, "int32_t", LdelemI4(arr, idx));
                break;
            case ArrRep.Ref:
            {
                string ct = CppTypes.Of(element);
                // A headerless element (CultureInfo[], Assembly[], …): the ref-array
                // slot holds the interned wrapper the stelem's object conversion
                // minted, so the typed read unwraps (tolerantly — a raw pointer
                // stored through an erased context passes through unchanged).
                Push(StackKind.Ref, ct, IsHeaderlessWrapCpp(ct)
                    ? HeaderlessUnwrapExpr(LdelemRef(arr, idx), ct)
                    : $"({ct}){LdelemRef(arr, idx)}");
                break;
            }
            default:
            {
                string ct = CppTypes.Of(element);
                string st = CppTypes.StorageOf(element);
                // Packed sub-word storage: read the narrow slot then widen (sign/zero
                // per the signedness of `st`) to the int32 stack type. When st == ct
                // (every non-sub-word type) this is the original expression.
                string addr = ElemAddr(arr, idx);
                Push(CppTypes.KindOf(element), ct,
                    st == ct ? $"*({ct}*){addr}" : $"({ct})(*({st}*){addr})");
                break;
            }
        }
    }

    private void EmitStelem(TypeDesc element)
    {
        var val = Pop();
        var idx = Pop();
        var arr = Pop();
        switch (RepOf(element))
        {
            case ArrRep.I4:
                Emit(StelemI4(arr, idx, Cast(val, "int32_t")));
                break;
            case ArrRep.Ref:
                Emit(StelemRef(arr, idx, Cast(val, "Dn2CppObject*")));
                break;
            default:
            {
                string ct = CppTypes.Of(element);
                string st = CppTypes.StorageOf(element);
                string addr = ElemAddr(arr, idx);
                // Narrow the int32 stack value into the packed slot (st == ct for
                // every non-sub-word type, so the cast is the original one).
                Emit(st == ct
                    ? $"*({ct}*){addr} = {Cast(val, ct)};"
                    : $"*({st}*){addr} = ({st})({Cast(val, ct)});");
                if (element.ContainsGcReferences())
                    Emit($"dn2cpp_gc_write_barrier((void*)({addr}));");
                break;
            }
        }
    }

    private void EmitLdelema(TypeDesc element)
    {
        var idx = Pop();
        var arr = Pop();
        string ct = CppTypes.Of(element);
        // The N-path address points at the packed slot, so its type is the storage
        // type (uint8_t* for byte[]); st == ct for every non-sub-word type.
        string st = CppTypes.StorageOf(element);
        string addr = RepOf(element) switch
        {
            // The I4 representation backs both int[] and uint[] with an int32_t[]
            // payload, so cast the element address to the element's own C++ type —
            // for uint[] that is uint32_t*, which differs in sign from &data[i]
            // (int32_t*); for int[] it is a no-op. enum[] (ct=int32_t) too.
            ArrRep.I4 => $"({ct}*)&(({Cast(arr, "Dn2CppArrayI4*")})->data[{idx.Expr}])",
            ArrRep.Ref => $"({ct}*)&(({Cast(arr, "Dn2CppArrayRef*")})->data[{idx.Expr}])",
            _ => $"({st}*){ElemAddr(arr, idx)}",
        };
        string ptrType = RepOf(element) == ArrRep.N ? st + "*" : ct + "*";
        // Bounds-check the I4/Ref paths (the N path checks inside dn2cpp_elem_addr).
        if (RepOf(element) != ArrRep.N && !_method.SkipBoundsChecks)
            Emit($"dn2cpp_bounds_check((Dn2CppArray*)({arr.Expr}), {idx.Expr});");
        Push(StackKind.Ptr, ptrType, addr);
    }

    private void EmitLdelemPrim(string memType, StackKind kind, string stackType)
    {
        var idx = Pop();
        var arr = Pop();
        if (!_method.SkipBoundsChecks)
            Emit($"dn2cpp_bounds_check((Dn2CppArray*)({arr.Expr}), {idx.Expr});");
        Push(kind, stackType, $"({stackType})(*({memType}*){ElemAddr(arr, idx)})");
    }

    private void EmitStelemPrim(string memType)
    {
        var val = Pop();
        var idx = Pop();
        var arr = Pop();
        Emit($"*({memType}*){ElemAddr(arr, idx)} = ({memType})({val.Expr});");
    }

    /// <summary>Overflow-checked add/sub/mul (the *.ovf opcodes).</summary>
    private void CheckedBinary(string op, bool signed)
    {
        var b = Pop();
        var a = Pop();
        var (kind, type) = Promote(a, b);
        string ut = kind == StackKind.I8 ? "uint64_t" : "uint32_t";
        string opType = signed ? type : ut;
        string r = NewTemp(type);
        string ax = signed ? Cast(a, type) : $"({ut}){a.Expr}";
        string bx = signed ? Cast(b, type) : $"({ut}){b.Expr}";
        Emit($"if (__builtin_{op}_overflow(({opType}){ax}, ({opType}){bx}, ({opType}*)&{r})) dn2cpp_overflow();");
        _stack.Add(new StackEntry(r, kind, type));
    }

    /// <summary>Overflow-checked narrowing conversion (the conv.ovf.* opcodes).
    /// Bounds are compared in 64-bit to represent every target range exactly.</summary>
    private void CheckedConv(string stackType, StackKind kind, string targetType, string lo, string hi)
    {
        var a = Pop();
        Push(kind, stackType,
            $"({stackType})dn2cpp_conv_ovf<{targetType}>((int64_t)({a.Expr}), (int64_t)({lo}), (int64_t)({hi}))");
    }

    /// <summary>ldind.*: dereference a managed pointer, widening to the stack type.</summary>
    private void LoadIndirect(string memType, StackKind kind, string stackType)
    {
        var p = Pop();
        Push(kind, stackType, $"({stackType})(*(({memType}*)({p.Expr})))");
    }

    /// <summary>stind.*: store through a managed pointer.</summary>
    private void StoreIndirect(string memType)
    {
        var val = Pop();
        var p = Pop();
        Emit($"*(({memType}*)({p.Expr})) = ({memType})({val.Expr});");
    }

    /// <summary>The C++ type a <c>ldind.ref</c>/<c>stind.ref</c> slot really holds.
    /// The opcodes are type-ERASED — ECMA-335 spells one "object reference" load and
    /// one store whatever the byref points at — but a headerless intrinsic
    /// representation (CultureInfo/NumberFormatInfo/TextInfo: the
    /// <c>const Dn2CppNumberFormatInfo*</c> lowering) is not an object, so punning the
    /// slot through <c>Dn2CppObject*</c> is exactly the misread the wrap boundary
    /// exists to prevent — <c>o.GetType()</c> on the loaded value reads the struct's
    /// first field as a type header. The pointer operand's own C++ spelling is the
    /// discriminator and is always present: a byref to a T-typed slot is emitted as
    /// <c>T*</c>, so stripping one <c>*</c> recovers the slot type. Anything that is
    /// not a headerless representation keeps the erased <c>Dn2CppObject*</c> exactly as
    /// before, so the emitted text is unchanged everywhere else.</summary>
    private static string RefSlotCppType(StackEntry p)
    {
        if (p.CppType.EndsWith("**", StringComparison.Ordinal))
        {
            string slot = p.CppType[..^1];
            if (IsHeaderlessWrapCpp(slot))
                return slot;
        }
        return "Dn2CppObject*";
    }

    /// <summary>ldind.ref over a byref whose slot holds a headerless intrinsic
    /// representation (see <see cref="RefSlotCppType"/>): load it in its own
    /// spelling and carry the pointee's static type, so the ordinary
    /// <see cref="Cast"/> boundary wraps it when — and only when — it escapes
    /// into an <c>object</c> context.</summary>
    private void LoadIndirectRef()
    {
        var p = Pop();
        string slot = RefSlotCppType(p);
        Push(StackKind.Ref, slot, $"({slot})(*(({slot}*)({p.Expr})))");
        if (p.StaticType is { Kind: TypeKind.ByRef, Element: { } el })
            _stack[^1] = _stack[^1] with { StaticType = el };
    }

    /// <summary>stind.ref, the store half of <see cref="LoadIndirectRef"/>: route the
    /// value through <see cref="Cast"/> so an NFI value entering an <c>object</c> slot
    /// is wrapped and a wrapper entering an NFI slot is unwrapped. For every other
    /// slot/value pair Cast is the same C cast the old code emitted.</summary>
    private void StoreIndirectRef()
    {
        var val = Pop();
        var p = Pop();
        string slot = RefSlotCppType(p);
        Emit($"*(({slot}*)({p.Expr})) = {Cast(val, slot)};");
        Emit($"dn2cpp_gc_write_barrier_if_heap((void*)({p.Expr}));");
    }

    /// <summary>The canonical <c>f_method</c> spelling of one delegate position: a
    /// wrappable HEADERLESS intrinsic (the NFI trio, Assembly/Module's
    /// <c>const char*</c>) erases to <c>Dn2CppObject*</c>, everything else keeps its
    /// own type. Both ends of the function pointer apply this to their OWN spelling of
    /// the position — the adapter to the target method's, the invoker to the delegate
    /// type's — and they cannot disagree, because the only way the two spellings
    /// differ at all is a headerless spelling against <c>Dn2CppObject*</c> and both
    /// erase to <c>Dn2CppObject*</c>. That is what makes the ABI survive delegate
    /// variance, where one instance is invoked through two delegate types (see
    /// <see cref="DelegateAdapter"/>). The reflection-bind trampolines
    /// (<c>dgrefl_*</c>) spell their signatures through this same function, which is
    /// what puts them under the unified ABI.</summary>
    internal static string NfiErasedAbi(string cpp) => IsHeaderlessWrapCpp(cpp) ? "Dn2CppObject*" : cpp;

    /// <summary>Whether a method group bound to a delegate needs the erasing
    /// adapter: its own signature spells a wrappable headerless intrinsic somewhere the
    /// delegate can see it — a parameter (the bound first argument of a closed static
    /// included: it rides the <c>Dn2CppObject*</c> target slot, so it erases like the
    /// rest) or the return. Asked of the TARGET alone, deliberately: the delegate type
    /// is not part of the answer, so one target reached through two delegate types gets
    /// one adapter and one ABI.</summary>
    private static bool NeedsNfiErasedAdapter(MethodInfo target)
    {
        if (!target.Signature.ReturnType.IsVoid
            && IsHeaderlessWrapCpp(CppTypes.Of(target.Signature.ReturnType)))
            return true;
        foreach (var p in target.Signature.ParameterTypes)
            if (IsHeaderlessWrapCpp(CppTypes.Of(p)))
                return true;
        return false;
    }

    /// <summary>Every ldftn / ldvirtftn arm that names a target's own symbol — the
    /// delegate adapters and the two raw-address arms — asks this, because a symbol
    /// named is a body required and reachability transpiles no body for a member whose
    /// calls are intercepted. Two disjoint reasons, one question: an intrinsic-mapped
    /// TYPE, whose whole surface is lowered inline, and a single member an intercept row
    /// CUT. Silent when neither answers: the real IL is transpiled as usual.</summary>
    private void NoteFtnTargetBody(MethodInfo target)
    {
        if (CoreIntrinsics.IsIntrinsicType(target.DeclaringClass.FullName))
            _c.NoteIntrinsicFtnTarget(target);
        else if (CoreIntrinsics.TryFindCutRow(target, out _))
            _c.NoteInterceptFtnTarget(target);
    }

    /// <summary>The namespace-qualified name of a delegate parameter type (External
    /// TypeRef or a transpiled Class), for distinguishing Task.Run(Action) from
    /// Task.Run(Func&lt;Task&gt;). Empty for shapes we don't name.</summary>
    private static string DelegateParamName(TypeDesc t) => t.Kind switch
    {
        TypeKind.External => t.ExternalName ?? "",
        TypeKind.Class => t.Class?.FullName ?? "",
        _ => "",
    };

    /// <summary>Whether <paramref name="t"/> names System.Threading.Tasks.Task or
    /// Task&lt;T&gt; (the generic definition, ignoring the type argument). Used to tell a
    /// Task.Run(Func&lt;Task&gt;/Func&lt;Task&lt;T&gt;&gt;) async-lambda overload — whose inner Task
    /// is unwrapped — from Task.Run(Action)/Task.Run(Func&lt;T&gt;).</summary>
    private bool NamesTask(TypeDesc t) => t.Kind switch
    {
        TypeKind.External => t.ExternalName == "System.Threading.Tasks.Task",
        TypeKind.Class => t.Class is { } c
            && (c.FullName == "System.Threading.Tasks.Task"
                || _c.GenericDefFullName(c) == "System.Threading.Tasks.Task"),
        _ => false,
    };

    /// <summary>Whether a Func&lt;...&gt; delegate parameter returns a Task — its last
    /// closed type argument names Task / Task&lt;T&gt;. (Action and Func&lt;T:non-Task&gt; are
    /// false.) Selects the Task.Run unwrap path.</summary>
    private bool DelegateReturnsTask(TypeDesc del) =>
        del is { Kind: TypeKind.Class, Class: { } d }
        && d.Context.TypeArgs.Length > 0
        && NamesTask(d.Context.TypeArgs[^1]);

    /// <summary>Volatile.Read(ref T): seq_cst atomic load, pushed at the IL stack
    /// width (small ints widen to int32, like ldind). float/double bit-cast via
    /// runtime helpers (not valid operands for the generic-N atomic builtins).</summary>
    private void EmitVolatileRead(TypeDesc elem, StackEntry loc)
    {
        string p = loc.Expr;
        if (elem.Kind == TypeKind.Primitive)
        {
            switch (elem.Primitive)
            {
                case PrimitiveTypeCode.Boolean:
                case PrimitiveTypeCode.Byte:
                    Push(StackKind.I4, "int32_t", $"(int32_t)__atomic_load_n((uint8_t*)({p}), __ATOMIC_SEQ_CST)"); return;
                case PrimitiveTypeCode.SByte:
                    Push(StackKind.I4, "int32_t", $"(int32_t)__atomic_load_n((int8_t*)({p}), __ATOMIC_SEQ_CST)"); return;
                case PrimitiveTypeCode.Int16:
                    Push(StackKind.I4, "int32_t", $"(int32_t)__atomic_load_n((int16_t*)({p}), __ATOMIC_SEQ_CST)"); return;
                case PrimitiveTypeCode.UInt16:
                case PrimitiveTypeCode.Char:
                    Push(StackKind.I4, "int32_t", $"(int32_t)__atomic_load_n((uint16_t*)({p}), __ATOMIC_SEQ_CST)"); return;
                case PrimitiveTypeCode.Int32:
                    Push(StackKind.I4, "int32_t", $"__atomic_load_n((int32_t*)({p}), __ATOMIC_SEQ_CST)"); return;
                case PrimitiveTypeCode.UInt32:
                    Push(StackKind.I4, "int32_t", $"(int32_t)__atomic_load_n((uint32_t*)({p}), __ATOMIC_SEQ_CST)"); return;
                case PrimitiveTypeCode.Int64:
                case PrimitiveTypeCode.UInt64:
                    Push(StackKind.I8, "int64_t", $"(int64_t)__atomic_load_n((int64_t*)({p}), __ATOMIC_SEQ_CST)"); return;
                case PrimitiveTypeCode.IntPtr:
                case PrimitiveTypeCode.UIntPtr:
                    Push(StackKind.I8, "int64_t", $"(int64_t)__atomic_load_n((intptr_t*)({p}), __ATOMIC_SEQ_CST)"); return;
                case PrimitiveTypeCode.Single:
                    Push(StackKind.R8, "double", $"(double)dn2cpp_volatile_read_r4((const float*)({p}))"); return;
                case PrimitiveTypeCode.Double:
                    Push(StackKind.R8, "double", $"dn2cpp_volatile_read_r8((const double*)({p}))"); return;
            }
        }
        // Reference type (object / class / interface / array / T : class).
        string ct = CppTypes.Of(elem);
        Push(StackKind.Ref, ct, $"({ct})__atomic_load_n((Dn2CppObject**)({p}), __ATOMIC_SEQ_CST)");
    }

    /// <summary>Volatile.Write(ref T, T): seq_cst atomic store at the element's
    /// storage width; float/double bit-cast via runtime helpers.</summary>
    private void EmitVolatileWrite(TypeDesc elem, StackEntry loc, StackEntry value)
    {
        string p = loc.Expr, v = value.Expr;
        if (elem.Kind == TypeKind.Primitive)
        {
            switch (elem.Primitive)
            {
                case PrimitiveTypeCode.Boolean:
                case PrimitiveTypeCode.Byte:
                    Emit($"__atomic_store_n((uint8_t*)({p}), (uint8_t)({v}), __ATOMIC_SEQ_CST);"); return;
                case PrimitiveTypeCode.SByte:
                    Emit($"__atomic_store_n((int8_t*)({p}), (int8_t)({v}), __ATOMIC_SEQ_CST);"); return;
                case PrimitiveTypeCode.Int16:
                    Emit($"__atomic_store_n((int16_t*)({p}), (int16_t)({v}), __ATOMIC_SEQ_CST);"); return;
                case PrimitiveTypeCode.UInt16:
                case PrimitiveTypeCode.Char:
                    Emit($"__atomic_store_n((uint16_t*)({p}), (uint16_t)({v}), __ATOMIC_SEQ_CST);"); return;
                case PrimitiveTypeCode.Int32:
                    Emit($"__atomic_store_n((int32_t*)({p}), (int32_t)({v}), __ATOMIC_SEQ_CST);"); return;
                case PrimitiveTypeCode.UInt32:
                    Emit($"__atomic_store_n((uint32_t*)({p}), (uint32_t)({v}), __ATOMIC_SEQ_CST);"); return;
                case PrimitiveTypeCode.Int64:
                case PrimitiveTypeCode.UInt64:
                    Emit($"__atomic_store_n((int64_t*)({p}), (int64_t)({v}), __ATOMIC_SEQ_CST);"); return;
                case PrimitiveTypeCode.IntPtr:
                case PrimitiveTypeCode.UIntPtr:
                    Emit($"__atomic_store_n((intptr_t*)({p}), (intptr_t)({v}), __ATOMIC_SEQ_CST);"); return;
                case PrimitiveTypeCode.Single:
                    Emit($"dn2cpp_volatile_write_r4((float*)({p}), (float)({v}));"); return;
                case PrimitiveTypeCode.Double:
                    Emit($"dn2cpp_volatile_write_r8((double*)({p}), (double)({v}));"); return;
            }
        }
        Emit($"__atomic_store_n((Dn2CppObject**)({p}), (Dn2CppObject*)({v}), __ATOMIC_SEQ_CST);");
        Emit($"dn2cpp_gc_write_barrier_if_heap((void*)({p}));");
    }

    private void CondBranch(Instruction insn, string op, bool unsigned)
    {
        var b = Pop();
        var a = Pop();
        NfiCompareFixup(ref a, ref b);
        BranchTo((int)insn.Operand, emitGoto: true, CompareCond(a, b, op, unsigned));
    }

    private static (StackKind, string) Promote(StackEntry a, StackEntry b)
    {
        if (a.Kind == StackKind.Struct || b.Kind == StackKind.Struct)
            throw new NotSupportedException("Arithmetic on struct stack entries is not supported");
        // A raw pointer used in non-add/sub integer arithmetic — `(nuint)p % align`,
        // `(nuint)p & (align-1)`, etc. — is its uintptr_t address value. The C#
        // void*->nuint/ulong cast emits no IL conv (pointer and native int share a
        // stack representation), so the Ptr arrives here directly; treat it as a
        // native-width unsigned integer. add/sub never reach this (Binary routes
        // them to byte-addressed pointer arithmetic first).
        if (a.Kind == StackKind.Ptr || b.Kind == StackKind.Ptr)
            return (StackKind.I8, "uintptr_t");
        if (a.Kind == StackKind.R8 || b.Kind == StackKind.R8)
            return (StackKind.R8, "double");
        if (a.Kind == StackKind.I8 || b.Kind == StackKind.I8)
            return (StackKind.I8, "int64_t");
        return (StackKind.I4, "int32_t");
    }
}
