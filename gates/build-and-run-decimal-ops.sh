#!/usr/bin/env bash
# System.Decimal modeled as an intrinsic value type backed by the runtime's 96-bit
# Dn2CppDecimal. Consolidated bucket — one sample project, one .cs per section,
# driven in order by samples/dotnet/DecimalOps/Program.cs.
# OLE currency conversion preserves ToEven rounding, signed bounds and decimal scale.
# Scale reads the stored exponent, including signed zero and equal-value representations.
#
#   * DecimalSubset — the STATICALLY TYPED surface. The real corelib
#     Decimal.ToString reaches Number.FormatDecimal -> ArrayPool -> EventSource ->
#     calli (untranspilable), so the ctors / operators / conversions / ToString /
#     rounding statics / Parse are lowered at the CALL SITE to dn2cpp_decimal_*
#     helpers. Also the overflow faults (typed catch, finally on the unwind path,
#     a running total that survives a bad row) and the LINQ decimal Sum/Average
#     overloads bound to the *real* System.Linq.dll via -r, whose generic-math
#     path lowers onto the intrinsic Decimal.
#   * BoxedDecimalSubset — the same value once BOXED, where there is no call site
#     to lower: `object o = someDecimal;` names the shared runtime handle
#     dn2cpp_decimal_type (Decimal has no emitted ti_*) and
#     dn2cpp_object_tostring/_equals/_gethashcode recover the payload. ToString
#     preserves scale, Equals is scale-insensitive value equality, GetHashCode
#     honours equal-value->equal-hash (a boxed-decimal Dictionary key), and the
#     string.Format holes (no spec, :F2, :N2) work. Its tail also asserts the
#     STATICALLY TYPED GetHashCode — a different lowering, the same contract, and it
#     must agree with the boxed one value for value; it sits there rather than in
#     DecimalSubset only because appending is what keeps the earlier output a prefix.
#   * NegativeZeroSubset — constructor, parse, Negate, mul/div/rem and add/sub
#     cancellation all preserve zero's sign flag; the arithmetic runs on PARSED
#     operands because Roslyn folds constant decimal expressions (to +0 for
#     -1m + 1m, unlike the runtime's DecCalc), which would bypass the operators.
#
# Diffed exact vs real .NET. Both sections used to assert a hard-coded string
# instead, because both print grouped/percent-formatted decimals; the project's
# InvariantGlobalization pins the oracle's culture, which is what makes the diff
# host-independent. See the csproj.
source "$(dirname "$0")/_common.sh"
source "$(dirname "$0")/decimal-divmod-probe.sh"
DN2CPP_GATE_EXTRA_CONTEXT="${DN2CPP_GATE_EXTRA_CONTEXT:-}|before-oa-currency|oa-currency-direct-and-delegate|decimal-scale-direct-and-delegate|decimal-divmod-exact-ref-raw-diet|cli:$(_gate_cli_hash)"
DN2CPP_GATE_EXTRA_INPUTS="${DN2CPP_GATE_EXTRA_INPUTS:-} gates/decimal-divmod-probe.sh gates/fixtures/decimal-divmod gates/fixtures/ldftn-local/DecimalDivModFixture.cs gates/fixtures/ldftn-local/DecimalDivModGuards.cs"

gate_extra_asserts() {
    local out="$1" native before prefix line
    gate_capture_ok "$out/assert-native-1" run_bounded "./$out/DecimalOps$EXE_EXT"
    native="$out/assert-native-1.stdout"

    gate_capture_ok "$out/assert-before-2" run_bounded dotnet "$_CG_APP" before-decimal-bits
    before="$out/assert-before-2.stdout"
    prefix="$out/assert-prefix-3.stdout"
    gate_output select "$native" "$prefix" --before '== decimal bit conversions ==' --policy crlf
    gate_output compare "$prefix" "$before"
    for line in '== decimal bit conversions ==' \
        'try bits:0:3=False:0:77,77,77' 'try bits:0:4=True:4:0,0,0,0' \
        'parts:28=-1,-1,-1,-2145648640' 'decimal bit conversions end'; do
        gate_output witness "$native" "$line" --count any --policy crlf \
            || { echo "FAIL: decimal bits witness missing: $line" >&2; exit 1; }
    done
    gate_capture_ok "$out/assert-before-oa-currency" run_bounded dotnet "$_CG_APP" before-oa-currency || return 1
    before="$out/assert-before-oa-currency.stdout"
    prefix="$out/assert-prefix-oa-currency.stdout"
    gate_output select "$native" "$prefix" --before '== decimal OLE currency ==' --policy crlf || return 1
    gate_output compare "$prefix" "$before" || return 1
    for line in '== decimal OLE currency ==' \
        'to direct:0.00005=0' 'to delegate:0.00015=2' \
        'to direct:-922337203685477.58085=-9223372036854775808' \
        'from direct:0=0,0,0,262144:0' 'from delegate:10000=1,0,0,0:10000' \
        'decimal OLE currency end'; do
        gate_output byte-witness "$native" "$line" --count any --policy crlf \
            || { echo "FAIL: OLE currency witness missing: $line" >&2; exit 1; }
    done
    gate_capture_ok "$out/assert-before-scale" run_bounded dotnet "$_CG_APP" before-decimal-scale || return 1
    before="$out/assert-before-scale.stdout"
    prefix="$out/assert-prefix-scale.stdout"
    gate_output select "$native" "$prefix" --before '== decimal scale ==' --policy crlf || return 1
    gate_output compare "$prefix" "$before" || return 1
    for line in '== decimal scale ==' \
        'scale:zero:False:0=0:0' 'scale:zero:True:28=28:28' \
        'scale:96:False:28=28:28' 'scale:96:True:0=0:0' \
        'scale:equal:1.00=2:2' 'decimal scale end'; do
        gate_output byte-witness "$native" "$line" --count 1 --policy crlf \
            || { echo "FAIL: decimal scale witness missing: $line" >&2; return 1; }
    done
    gate_capture_ok "$out/assert-before-divmod" run_bounded dotnet "$_CG_APP" before-decimal-divmod || return 1
    gate_output select "$native" "$out/assert-prefix-divmod.stdout" --before '== decimal coefficient divmod ==' --policy crlf || return 1
    gate_output compare "$out/assert-prefix-divmod.stdout" "$out/assert-before-divmod.stdout" || return 1
    for line in '== decimal coefficient divmod ==' 'divmod magnitude/sign/scale cases:1044' \
        'divmod repeated:543950335' 'divmod repeated:0' 'decimal coefficient divmod end'; do
        gate_output byte-witness "$native" "$line" --count 1 --policy crlf || return 1
    done
    decimal_divmod_probe "$out" "$native" || return 1
}

corelib_diff_gate DecimalOps System.Linq System.Collections System.Runtime
