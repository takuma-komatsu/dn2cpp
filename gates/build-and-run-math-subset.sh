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
# and Math.BigMul overloads plus primitive BigMul facades (wide results via halves),
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
DN2CPP_GATE_EXTRA_INPUTS="${DN2CPP_GATE_EXTRA_INPUTS:-} gates/fixtures/math-bigmul/MathBigMulFixture.csproj gates/fixtures/math-bigmul/Program.cs gates/fixtures/primitive-bigmul/PrimitiveBigMul.csproj gates/fixtures/primitive-bigmul/Entry.cs samples/dotnet/MathSubset/PrimitiveBigMulSubset.cs gates/fixtures/primitive-bigmul/check-generated.py gates/fixtures/ldftn-local/PrimitiveBigMulFixture.cs gates/fixtures/ldftn-local/PrimitiveBigMulGuards.cs"
if [ "${WASM:-}" = 1 ]; then
    dn2cpp_emsdk_resolve --no-bundled
    for tool in em++ emcmake node; do
        command -v "$tool" >/dev/null 2>&1 || gate_skip "$tool not found — no Emscripten toolchain to build wasm with"
    done
fi

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

    gate_capture_ok "$out/assert-before-mixed-bigmul" run_bounded dotnet "$_CG_APP" before-mixed-bigmul
    before="$out/assert-before-mixed-bigmul.stdout"
    prefix="$out/assert-mixed-bigmul-prefix.stdout"
    gate_output select "$native" "$prefix" --before '== mixed-width BigMul ==' --policy crlf
    gate_output compare "$prefix" "$before"
    for line in '== mixed-width BigMul ==' 'direct32-max=4294967294:18446744069414584321' \
        'direct64-carry=1:0' 'mixed-width BigMul end'; do
        gate_output byte-witness "$native" "$line" --count any --policy crlf \
            || { echo "FAIL: mixed-width BigMul coverage missing: $line" >&2; exit 1; }
    done
    gate_capture_ok "$out/assert-before-primitive-bigmul" run_bounded dotnet "$_CG_APP" before-primitive-bigmul
    gate_output select "$native" "$out/assert-primitive-bigmul-prefix.stdout" --before '== primitive BigMul ==' --policy crlf
    gate_output compare "$out/assert-primitive-bigmul-prefix.stdout" "$out/assert-before-primitive-bigmul.stdout"
    gate_output byte-witness "$native" 'primitive BigMul end' --count any --policy crlf
}

mixed_width_bigmul_gate() {
    build_proj samples/dotnet/MathSubset/MathSubset.csproj
    build_gate_proj gates/fixtures/math-bigmul/MathBigMulFixture.csproj
    local app="samples/dotnet/MathSubset/bin/$CONFIG/$TFM/MathSubset.dll"
    local fixture="gates/fixtures/math-bigmul/bin/$CONFIG/$TFM/MathBigMulFixture.dll"
    local root="artifacts/math-mixed-bigmul-$CONFIG" core expected mode out diagnostic
    if [ "${WASM:-}" = 1 ]; then
        root="$root-wasm"
        core=$(locate_corelib_cross_posix net10)
    else
        core=$(locate_corelib)
    fi
    mkdir -p "$root"
    gate_capture_ok "$root/expected" run_bounded dotnet "$app" mixed-bigmul-only
    expected="$root/expected.stdout"
    gate_capture_ok "$root/oracle" run_bounded dotnet "$fixture" --oracle
    gate_output compare "$root/oracle.stdout" "$expected"
    gate_capture_ok "$root/create-mixed" run_bounded dotnet "$fixture" "$app" "$core" "$root/mixed/MathSubset.dll" mixed
    for mode in raw full; do
        out="$root/$mode"
        local flags=()
        [ "$mode" != raw ] || flags+=(--no-ildiet)
        gate_capture_ok "$root/transpile-$mode" invoke_cli "$root/mixed/MathSubset.dll" -r "$core" --auto-ref --stripping-level high \
            --no-metadata-compression --no-shared-generics ${flags[@]+"${flags[@]}"} -o "$out"
        if [ "${WASM:-}" = 1 ]; then
            compile_console_wasm "$out" MixedBigMul
            gate_capture_ok "$out/native" run_bounded node "$out/MixedBigMul.js"
            gate_output compare "$out/native.stdout" "$expected" --policy crlf
        else
            compile_console "$out" MixedBigMul
            gate_capture_ok "$out/native" run_bounded "$out/MixedBigMul$EXE_EXT"
            gate_output compare "$out/native.stdout" "$expected"
        fi
        echo "mixed-width-bigmul=$mode:direct-and-address"
    done
    for mode in bad-signed bad-value; do
        gate_capture_ok "$root/create-$mode" run_bounded dotnet "$fixture" "$app" "$core" "$root/$mode/MathSubset.dll" "$mode"
        diagnostic="$root/rejected-$mode"
        gate_capture "$diagnostic" invoke_cli "$root/$mode/MathSubset.dll" -r "$core" --auto-ref --no-ildiet -o "$root/rejected-$mode-output" || return 1
        gate_output status "$(cat "$diagnostic.status")" 2
        gate_output match-record "$diagnostic.stderr" '.*BigMul.*' --count any --policy crlf
    done
}

# The primitive lane uses width-independent assertions derived from fixed-width
# products, not host64 native halves as a Wasm32 oracle.
primitive_bigmul_gate() {
    build_gate_proj gates/fixtures/primitive-bigmul/PrimitiveBigMul.csproj
    build_gate_proj gates/fixtures/ldftn-local/LdftnLocalFixture.csproj
    local app="gates/fixtures/primitive-bigmul/bin/$CONFIG/$TFM/PrimitiveBigMul.dll"
    local tool="gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll"
    local root="artifacts/math-primitive-bigmul-$CONFIG" core mode out
    if [ "${WASM:-}" = 1 ]; then
        root="$root-wasm"; core=$(locate_corelib_cross_posix net10)
    else
        core=$(locate_corelib)
    fi
    mkdir -p "$root"
    gate_capture_ok "$root/oracle" run_bounded dotnet "$app"
    gate_output byte-witness "$root/oracle.stdout" 'primitive BigMul end' --count any --policy crlf
    gate_capture_ok "$root/guards" run_bounded dotnet "$tool" --primitive-bigmul-guards \
        "src/Dn2Cpp.Cli/bin/$CONFIG/$TFM/Dn2Cpp.Transpiler.dll" "$core" "$app"
    gate_capture_ok "$root/address-fixture" run_bounded dotnet "$tool" --primitive-bigmul-fixture \
        "$app" "$core" "$root/address/PrimitiveBigMul.dll" address
    # Runtime-owned primitive TypeInfo does not expose a general method catalog;
    # callable replacement coverage is distinct from reflection lookup support.
    for mode in raw diet address; do
        out="$root/$mode-output"
        local flags=() input="$app" expected="$root/oracle.stdout"
        [ "$mode" != raw ] || flags+=(--no-ildiet)
        if [ "$mode" = address ]; then
            input="$root/address/PrimitiveBigMul.dll"
            printf '%s\n' 'primitive all-six address=True' 'primitive all-six delegate=True' > "$root/address.expected"
            expected="$root/address.expected"
        fi
        gate_capture_ok "$root/transpile-$mode" invoke_cli "$input" -r "$core" --auto-ref --stripping-level high \
            --no-metadata-compression ${flags[@]+"${flags[@]}"} -o "$out"
        gate_capture_ok "$root/generated-$mode" python3 gates/fixtures/primitive-bigmul/check-generated.py "$out"
        if [ "${WASM:-}" = 1 ]; then
            compile_console_wasm "$out" PrimitiveBigMul
            gate_capture_ok "$out/native" run_bounded node "$out/PrimitiveBigMul.js"
        else
            compile_console "$out" PrimitiveBigMul
            gate_capture_ok "$out/native" run_bounded "$out/PrimitiveBigMul$EXE_EXT"
        fi
        gate_output compare "$out/native.stdout" "$expected" --policy crlf
        echo "primitive-bigmul=$mode:executed"
    done
    for mode in call-signed call-return call-out call-value call-instance call-vararg call-modifier \
        address-signed address-return address-out address-value address-instance address-vararg address-modifier; do
        gate_capture_ok "$root/create-$mode" run_bounded dotnet "$tool" --primitive-bigmul-fixture \
            "$app" "$core" "$root/$mode/PrimitiveBigMul.dll" "$mode"
        gate_capture "$root/rejected-$mode" invoke_cli "$root/$mode/PrimitiveBigMul.dll" -r "$core" \
            --auto-ref --no-ildiet -o "$root/rejected-$mode-output" || return 1
        gate_output status "$(cat "$root/rejected-$mode.status")" 2
        gate_output match-record "$root/rejected-$mode.stderr" '.*BigMul.*' --count any --policy crlf
    done
}

# Focused lanes avoid rebuilding unrelated arithmetic during development.
if [ "${1:-}" = --math-bucket-only ]; then
    [ "${WASM:-}" != 1 ] || { echo "Math bucket is native-only; use the primitive lane on Wasm" >&2; exit 1; }
    corelib_diff_gate MathSubset
else
    if [ "${1:-}" != --primitive-bigmul-only ]; then
        if [ "${WASM:-}" != 1 ]; then corelib_diff_gate MathSubset; fi
        mixed_width_bigmul_gate
    fi
    primitive_bigmul_gate
fi
