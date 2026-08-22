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
#
# Diffed exact vs real .NET. Both sections used to assert a hard-coded string
# instead, because both print grouped/percent-formatted decimals; the project's
# InvariantGlobalization pins the oracle's culture, which is what makes the diff
# host-independent. See the csproj.
source "$(dirname "$0")/_common.sh"

corelib_diff_gate DecimalOps System.Linq System.Collections System.Runtime
