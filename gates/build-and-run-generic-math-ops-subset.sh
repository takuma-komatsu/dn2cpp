#!/usr/bin/env bash
# Generic-math static-abstract interface members lowered to scalar primitive ops:
# the constants (INumberBase<T>.Zero/One), arithmetic operators (op_Addition/
# op_Subtraction/op_Multiply/op_Division/op_UnaryNegation), comparison operators
# (op_LessThan/op_GreaterThan) and the interface-routed conversion
# (T.CreateChecked -> INumberBase<T>.TryConvertFromChecked). These are exactly the
# shapes the real System.Linq Sum/Average/Min/Max/Range reach once T is closed to a
# primitive; MethodCompiler.TryEmitGenericMathIntrinsic emits the primitive op and
# the constrained `call` to a static-abstract member devirtualizes to the concrete
# impl. The app drives the patterns through generic methods; output diffs exact vs
# real .NET using genuine BCL assemblies (no Linq shim). FloatingPointMath covers
# the floating-point interfaces (IRootFunctions/ITrigonometricFunctions/IExponentialFunctions/
# ILogarithmicFunctions/IPowerFunctions/IHyperbolicFunctions/IFloatingPointIeee754/
# IFloatingPoint) with TSelf closed to double/float/Half. ShiftModulusOps covers
# op_Modulus (IModulusOperators; integers/float/double/decimal), the IShiftOperators
# trio (<< / >> / >>>, storage-width amount masking) and op_CheckedDivision.
# ClassOperators covers reference-type TSelf: class operator implementors (base-
# class-provided impls included) plus the canonical-shared fallback to
# per-instantiation expansion. ConformanceSweep covers the remaining
# INumber<T>-family surface (Clamp/CopySign/Max/Min/Sign, Abs + the magnitude
# selectors, Radix + the category predicates, storage-width Min/MaxValue,
# IsPow2/Log2/AllBitsSet, the bit counts/rotates/DivRem, ~) and the
# IParsable/ISpanParsable constrained parse route, with decimal/Half spot-checks
# and the IEEE NaN / negative-zero edges pinned. OutSlotWidth is not about generic
# math at all: it pins the STORE WIDTH of the `out` those conversions write through —
# a packed array element and a struct field must get a storage-width store, and a
# promoted local slot must additionally be re-normalized — for the all-primitive arm
# and for the intrinsic-decimal one. It is also the corpus's only direct caller of the
# `protected` static-abstract TryConvert* members, reached through a helper interface
# deriving from INumberBase<byte>, which is the whole reason that interface exists.
# StaticAbstractGenericMethod covers a static abstract GENERIC method on a generic
# interface (IPackable<TSelf>.Pack<TSink>) dispatched through a constrained call:
# the explicit implementation's .override row names an open generic MemberRef, and
# the impl is template-matched and instantiated at the caller's method args. Its
# static implementation selection section pins that a static explicit body beats
# a plain one, that overloads closing to one signature keep their own bodies, and
# that a class's own static body beats a derived-interface static default, and
# that without a class body the most specific derived interface's explicit body
# binds a generic or plain member, abstract or carrying a default.
# InterfaceStaticImpl covers an INTERFACE as the constrained type argument: the
# interface's own explicit static impls resolve the static abstract members, and
# an unimplemented static-virtual default still binds the default body.
# Scalar leaves retain exponent/significand writers and signaling-NaN payloads.
# Endian reads and primitive span parsing cover scalar storage, bounds and faults.
# Finite-source wide conversions retain managed IL.
source "$(dirname "$0")/_common.sh"
DN2CPP_GATE_EXTRA_CONTEXT="${DN2CPP_GATE_EXTRA_CONTEXT:-}|static-impl-selection-prefix:${DN2CPP_BEFORE_STATIC_IMPL_SELECTION:-}|scalar-leaf-prefix:${DN2CPP_BEFORE_SCALAR_NUMERIC_LEAVES:-}|numeric-corelib-prefix:${DN2CPP_BEFORE_NUMERIC_CORELIB:-}"

gate_extra_asserts() {
    local out="$1"
    gate_capture_ok "$out/static-selection" run_bounded "$out/GenericMathOpsSubset$EXE_EXT"
    gate_output witness "$out/static-selection.stdout" --count any --policy crlf \
        '== Static interface implementation selection ==' \
        'static explicit explicit' \
        'static class class' \
        'static overload plain/explicit-int' \
        'static inherited class derived:Int32/derived:String/derived/derived/derived' \
        'static inherited struct derived:Int32/derived:String/derived/derived/derived' \
        'static inherited generic derived:Int32/derived:String/derived/derived/derived' \
        'static inherited most most:Int32/most:String/derived/derived/most'
    DN2CPP_BEFORE_STATIC_IMPL_SELECTION=1 gate_capture_ok "$out/static-selection-before" \
        run_bounded "$out/GenericMathOpsSubset$EXE_EXT"
    gate_output select "$out/static-selection.stdout" "$out/static-selection-prefix.stdout" \
        --before '== Static interface implementation selection ==' --policy crlf
    gate_output compare "$out/static-selection-prefix.stdout" "$out/static-selection-before.stdout"

    gate_output witness "$out/native.stdout" '== Primitive span parsing done ==' --count 1 --policy crlf
    DN2CPP_BEFORE_SCALAR_NUMERIC_LEAVES=1 gate_capture_ok "$out/scalar-leaves-before" \
        run_bounded "$out/GenericMathOpsSubset$EXE_EXT"
    gate_output select "$out/native.stdout" "$out/scalar-leaves-prefix.stdout" \
        --before '== Primitive span parsing managed bodies ==' --policy crlf
    gate_output compare "$out/scalar-leaves-prefix.stdout" "$out/scalar-leaves-before.stdout"
    DN2CPP_BEFORE_SCALAR_NUMERIC_LEAVES=1 gate_capture_ok "$out/scalar-leaves-oracle-before" \
        run_bounded dotnet "$_CG_APP"
    gate_output compare "$out/scalar-leaves-prefix.stdout" "$out/scalar-leaves-oracle-before.stdout"
    DN2CPP_BEFORE_NUMERIC_CORELIB=1 gate_capture_ok "$out/numeric-corelib-before" \
        run_bounded "$out/GenericMathOpsSubset$EXE_EXT"
    DN2CPP_BEFORE_NUMERIC_CORELIB=1 gate_capture_ok "$out/numeric-corelib-oracle-before" \
        run_bounded dotnet "$_CG_APP"
    gate_output select "$out/native.stdout" "$out/numeric-corelib-prefix.stdout" \
        --before '== scalar numeric managed leaves ==' --policy crlf
    gate_output compare "$out/numeric-corelib-prefix.stdout" "$out/numeric-corelib-before.stdout"
    gate_output compare "$out/numeric-corelib-prefix.stdout" "$out/numeric-corelib-oracle-before.stdout"
    gate_output byte-witness "$out/native.stdout" --count 1 --policy crlf \
        'scalar numeric managed leaves end' '== endian complete ==' \
        '== Native and char span parsing done ==' 'wide.done' \
        'leaf-write:f32-7F800001:2:4=True:4:A500800001A5' \
        'leaf-write:f32-FF812345:2:4=True:4:A500812345A5' \
        'leaf-sign:-2147483648=-1:-1:2'
}

corelib_diff_gate GenericMathOpsSubset System.Runtime System.Runtime.Numerics
