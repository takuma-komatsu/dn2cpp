#!/usr/bin/env bash
# LINQ reduction perf A/B — wall-clock of the real-BCL Enumerable.Sum/Max workload
# (LinqSumBench) as real .NET runs it vs the transpiled binary, scalar and Highway.
#
# This is a measurement aid, NOT a regression gate: timings are non-deterministic, so
# it prints numbers only and never asserts pass/fail (the `measure-*` name keeps it out
# of run-all-gates.sh, which discovers gates by the `build-and-run-*` glob). It does
# verify all three builds print identical checksums — a correctness cross-check.
#
# Context: real .NET's Enumerable.Sum pulls a span out of the array (TryGetSpan) and
# reduces it with a Vector<T> SIMD core. In the transpiled builds the System.Linq
# HW-accel gate (DN2CPP_SIMD_HW_ACCEL_LINQ in runtime/core/dn2cpp_vectors.h) is
# backend-selected: the scalar row runs the BCL's per-element fallback — an
# overflow-trapping checked-add loop clang cannot auto-vectorize — while the
# Highway row runs the BCL's own Vector<T> reduction core against the SIMD-backed
# vec ops. This script quantifies both against `dotnet` on the same machine.
source "$(dirname "$0")/_common.sh"

OUT=artifacts/linqsumbench
REPS="${REPS:-5}"

echo "== 1/4 Building LinqSumBench C# assembly =="
build_proj samples/dotnet/LinqSumBench/LinqSumBench.csproj
APP="samples/dotnet/LinqSumBench/bin/$CONFIG/$TFM/LinqSumBench.dll"

echo "== 2/4 Transpiling IL -> C++ (real System.Linq, backend-agnostic, once) =="
CORELIB="$(locate_corelib)"
BCL="$(dirname "$CORELIB")"
REFS=(-r "$CORELIB")
for name in System.Linq System.Collections System.Runtime; do
    [ -f "$BCL/$name.dll" ] && REFS+=(-r "$BCL/$name.dll")
done
invoke_cli "$APP" "${REFS[@]}" -o "$OUT"

echo "== 3/4 Building both native binaries (scalar + Highway) from the same C++ =="
compile_console_dual_backend "$OUT" LinqSumBench

echo "== 4/4 Measuring wall-clock ($REPS reps each; lower is better) =="

# best_of CMD... — run CMD REPS times, echo the minimum wall-clock seconds across
# reps (min is the most stable estimator for perf).
best_of() {
    local best="" t i
    local TIMEFORMAT='%R'
    for i in $(seq 1 "$REPS"); do
        local tf; tf="$(mktemp)"
        { time "$@" >/dev/null 2>&1; } 2>"$tf"
        t="$(cat "$tf")"; rm -f "$tf"
        if [ -z "$best" ] || awk "BEGIN{exit !($t < $best)}"; then best="$t"; fi
    done
    printf '%s' "$best"
}

echo
echo "checksums (must match):"
echo "  .NET    : $(dotnet "$APP" | tr '\n' ' ')"
echo "  scalar  : $("$OUT/LinqSumBench.scalar" | tr '\n' ' ')"
echo "  Highway : $("$OUT/LinqSumBench.hwy" | tr '\n' ' ')"

DOTNET_T="$(best_of dotnet "$APP")"
SCALAR_T="$(best_of "$OUT/LinqSumBench.scalar")"
HWY_T="$(best_of "$OUT/LinqSumBench.hwy")"

echo
echo "──────── wall-clock (best of $REPS) ────────"
echo "  real .NET        : ${DOTNET_T} s"
echo "  scalar emulation : ${SCALAR_T} s"
echo "  Highway SIMD     : ${HWY_T} s"
awk "BEGIN{ if ($DOTNET_T>0) printf \"  scalar / .NET    : %.2fx\n\", $SCALAR_T/$DOTNET_T }"
awk "BEGIN{ if ($DOTNET_T>0) printf \"  Highway / .NET   : %.2fx\n\", $HWY_T/$DOTNET_T }"
echo
echo "Reading: ratios near 1 mean the transpiled binary matches real .NET's LINQ"
echo "reduction throughput; the scalar row carries the per-element accessor-call and"
echo "no-SIMD overhead, the Highway row shows what the SIMD backend recovers."
