#!/usr/bin/env bash
# Math bucket: one themed program consolidating the System.Math coverage —
# Clamp/Sign plus the integral Min/Max/Clamp/Abs overloads across every width
# and signedness (catches the unsigned precision loss above 2^53 that a
# double-routed lowering would miss), the trig/log completion (Cbrt,
# Asin/Acos/Atan, Sinh/Cosh/Tanh, Log2, Log(x, base)) and the rounding
# completion (Truncate, Log10, Round(value, MidpointRounding) and the
# digits overloads Round(value, digits[, mode]) with their exception paths),
# the former two asserted by equality so the output is formatting-independent,
# the inverse
# hyperbolics (Acosh/Asinh/Atanh, boolean anchors — transcendental values are
# never printed raw), the IEEE-exact members (CopySign, IEEERemainder,
# FusedMultiplyAdd, ScaleB, BitIncrement/BitDecrement, ILogB, Max/MinMagnitude)
# and BigMul across all six overloads (the 128-bit results via their halves),
# plus the Reciprocal(Sqrt)Estimate pair (tolerance relations + the exact IEEE
# special cases only — the finite results are platform-dependent estimates)
# and the decimal members (Round across all five MidpointRounding modes with
# their scale behavior, Clamp with its min > max throw), plus the systematic
# MathF sweep (the whole single-precision surface: exact prints for the
# IEEE-exact members, float-vs-double divergence traps like Round(2.675f, 2)
# and the FusedMultiplyAdd cancellations, boolean/tolerance anchors for the
# transcendentals, and the float Clamp/Round exception paths), plus the
# System.Double/System.Single static generic-math surface (the shared-map
# pass-throughs, the exp/log compositions and the *Pi family — exact prints,
# the compositions bit-match real .NET on this host — Lerp and
# MultiplyAddEstimate, Ieee754Remainder, the NaN-dropping *Number and
# platform-defined *Native min-max variants, Pi/E/Tau, and the full IsXxx
# predicate truth table over the special values), plus the *Pi trig family
# proper (SinPi/CosPi/TanPi/SinCosPi), RootN and Hypot — verbatim
# contraction-free runtime ports of the BCL's managed AOCL algorithms,
# asserted with exact prints across the reduction intervals, small-argument
# tiers, integral-boundary parities and the Hypot rescale branches, plus the
# System.Half basic surface (creation/conversion/arithmetic/comparison/
# predicates through the transpiled BCL software bit algorithms, ToString and
# Parse/TryParse through the transpiled Number.FormatFloat/TryParseFloat
# subtree, CompareTo/Equals/GetHashCode ordering, boxing, struct fields, and
# the binary16 edges: subnormals, overflow-to-infinity, round-to-nearest-even
# — bit patterns asserted exactly via BitConverter.HalfToUInt16Bits), plus the
# System.Half static generic-math surface (the whole INumber /
# IFloatingPointIeee754 / ITrigonometricFunctions / ... static member set:
# min-max/clamp families with their NaN and ±0 semantics, roots/powers,
# trig + the *Pi family, hyperbolics, exp/log compositions, rounding, the
# IEEE-exact members, Lerp/degrees, the Create*/checked-narrowing conversions
# and the IsXxx predicates — almost all (Half)MathF.Xxx((float)x) compositions
# in the BCL, asserted bit-exactly; the platform-defined estimates and *Native
# variants keep tolerance/special-case anchors — and constrained-generic
# T.Abs/T.Sin/... dispatch with T = Half through the static-abstract
# interfaces), plus unchecked integer/FNV wrapping through narrowed char and byte
# inputs, native-width multiplication, checked overflow and floating-point isolation.
# One section file per area in samples/dotnet/MathSubset, driven in order by
# Program.cs.
# Driven by the real System.Private.CoreLib (passed with -r) -> cross-assembly
# resolve + tree-shake -> native binary -> run, diffed exactly against real
# .NET (the program is deterministic, so `dotnet $app` is its own oracle).
source "$(dirname "$0")/_common.sh"

# Default order and equality preserve the earlier bucket and run the appended cases.
# Direct integer DivRem calls cover every scalar width, zero divisors and signed minima.
gate_extra_asserts() {
    local out="$1" native before prefix line
    gate_capture_ok "$out/assert-native-1" run_bounded "./$out/MathSubset"
    native="$out/assert-native-1.stdout"

    gate_capture_ok "$out/assert-before-2" run_bounded dotnet "$_CG_APP" before-default-comparison
    before="$out/assert-before-2.stdout"

    prefix="$out/assert-prefix-4.stdout"

    gate_output select "$native" "$prefix" --before '== default comparison validation ==' --policy crlf
    gate_output compare "$prefix" "$before"
    for line in '== default comparison validation ==' \
        '== generic clamp bounds ==' \
        'Int128='"'"'5'"'"' cannot be greater than 2.' \
        'default comparison validation end'; do
        gate_output witness "$native" "$line" --count any --policy crlf \
            || { echo "FAIL: MathSubset comparison coverage missing: $line" >&2; exit 1; }
    done
    gate_capture_ok "$out/assert-before-3" run_bounded dotnet "$_CG_APP" before-scalar-divrem
    before="$out/assert-before-3.stdout"
    prefix="$out/assert-prefix-5.stdout"
    gate_output select "$native" "$prefix" --before '== scalar DivRem ==' --policy crlf
    gate_output compare "$prefix" "$before"
    for line in '== scalar DivRem ==' 'int:17:5=3:2' \
        'long:-9223372036854775808:-1=OverflowException' \
        'nuint:17:0=DivideByZeroException' 'scalar DivRem end'; do
        gate_output witness "$native" "$line" --count any --policy crlf \
            || { echo "FAIL: scalar DivRem coverage missing: $line" >&2; exit 1; }
    done
    gate_capture_ok "$out/assert-before-unchecked" run_bounded dotnet "$_CG_APP" before-unchecked-arithmetic
    before="$out/assert-before-unchecked.stdout"
    prefix="$out/assert-unchecked-prefix.stdout"
    gate_output select "$native" "$prefix" --before '== unchecked integer arithmetic ==' --policy crlf
    gate_output compare "$prefix" "$before"
    for line in '== unchecked integer arithmetic ==' \
        'i4 add=-2147483648' 'i4 sub=2147483647' 'i4 mul=2147483645' \
        'u4 hash mul=84696351' 'i8 add=-9223372036854775808' \
        'i8 sub=9223372036854775807' 'i8 mul=9223372036854775805' \
        'u8 mul=18446744073709551613' 'unchecked integer arithmetic end'; do
        gate_output witness "$native" "$line" --count any --policy crlf \
            || { echo "FAIL: unchecked arithmetic coverage missing: $line" >&2; exit 1; }
    done
    gate_capture_ok "$out/assert-before-hash" run_bounded dotnet "$_CG_APP" before-integer-hash-wrapping
    before="$out/assert-before-hash.stdout"
    prefix="$out/assert-hash-prefix.stdout"
    gate_output select "$native" "$prefix" --before '== integer hash wrapping ==' --policy crlf
    gate_output compare "$prefix" "$before"
    for line in '== integer hash wrapping ==' \
        'char hash empty=811c9dc5' 'char hash shape=5174e87e' 'char hash nul=b909fe5a' \
        'char hash wide=f9f5964d' 'byte hash=84657fcd' \
        'i4 min neg=-2147483648' 'i8 min neg=-9223372036854775808' \
        'u4 max squared=1' 'u8 max squared=1' \
        'native min neg=True' 'native mixed wrap=True' 'unative wrap=True' \
        'r4 multiply=True' 'r8 multiply=True' 'r4 infinity=True' 'r8 nan=True' \
        'checked i4=OverflowException' 'checked u4=OverflowException' \
        'checked i8=OverflowException' 'checked u8=OverflowException' \
        'checked native=OverflowException' 'checked unative=OverflowException' \
        'checked nonoverflow=True' 'integer hash wrapping end'; do
        gate_output witness "$native" "$line" --count any --policy crlf \
            || { echo "FAIL: integer hash wrapping coverage missing: $line" >&2; exit 1; }
    done

}

corelib_diff_gate MathSubset
