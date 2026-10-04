#!/usr/bin/env bash
# System.Decimal modeled as an intrinsic value type backed by the runtime's 96-bit
# Dn2CppDecimal. Consolidated bucket — one sample project, one .cs per section,
# driven in order by samples/dotnet/DecimalOps/Program.cs.
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

gate_extra_asserts() {
    local out="$1" native before prefix line
    native=$(run_bounded "./$out/DecimalOps$EXE_EXT")
    native=$(strip_cr_win "$native")
    before=$(run_bounded dotnet "$_CG_APP" before-decimal-bits)
    prefix=$(awk '/^== decimal bit conversions ==$/ { exit } { print }' <<< "$native")
    assert_output "$prefix" "$(strip_cr_win "$before")"
    for line in '== decimal bit conversions ==' \
        'try bits:0:3=False:0:77,77,77' 'try bits:0:4=True:4:0,0,0,0' \
        'parts:28=-1,-1,-1,-2145648640' 'decimal bit conversions end'; do
        grep -Fxq -- "$line" <<< "$native" \
            || { echo "FAIL: decimal bits witness missing: $line" >&2; exit 1; }
    done
}

corelib_diff_gate DecimalOps System.Linq System.Collections System.Runtime
