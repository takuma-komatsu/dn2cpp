#!/usr/bin/env bash
# Highway perf A/B — wall-clock of the SAME transpiled vector workload built with the
# scalar emulation vs the Google Highway SIMD backend.
#
# This is a measurement aid, NOT a regression gate: timings are non-deterministic, so it
# prints numbers only and never asserts pass/fail (the `measure-*` name keeps it out of
# run-all-gates.sh, which discovers gates by the `build-and-run-*` glob). It does verify
# the two builds produce identical checksums — a correctness cross-check, not a timing one.
#
# The transpiler is backend-agnostic, so VectorBench is transpiled ONCE; the two native
# binaries differ only in the runtime they link (-DDN2CPP_USE_HIGHWAY OFF vs ON — the
# SCALAR and HIGHWAY axes in gates/_common.sh; the default build carries Highway). VectorBench runs a heavy memory-bound
# System.Numerics.Vector<T> loop (int + float) so the lane-loop emulation vs native SIMD
# gap shows above process-startup noise.
source "$(dirname "$0")/_common.sh"

OUT=artifacts/vectorbench
REPS="${REPS:-5}"

echo "== 1/4 Building VectorBench C# assembly =="
build_proj samples/dotnet/VectorBench/VectorBench.csproj
APP="samples/dotnet/VectorBench/bin/$CONFIG/$TFM/VectorBench.dll"

echo "== 2/4 Transpiling IL -> C++ (backend-agnostic, once) =="
CORELIB="$(locate_corelib)"
invoke_cli "$APP" -r "$CORELIB" -o "$OUT"

echo "== 3/4 Building both native binaries (scalar + Highway) from the same C++ =="
compile_console_dual_backend "$OUT" VectorBench

echo "== 4/4 Measuring wall-clock ($REPS reps each; lower is better) =="

# best_of BIN — run BIN REPS times, echo the program's first run output once, then the
# minimum wall-clock seconds across reps (min is the most stable estimator for perf).
best_of() {
    local bin="$1" best="" t i
    local TIMEFORMAT='%R'
    for i in $(seq 1 "$REPS"); do
        local tf; tf="$(mktemp)"
        { time "$bin" >/dev/null 2>&1; } 2>"$tf"
        t="$(cat "$tf")"; rm -f "$tf"
        if [ -z "$best" ] || awk "BEGIN{exit !($t < $best)}"; then best="$t"; fi
    done
    printf '%s' "$best"
}

echo
echo "checksums (must match):"
echo "  .NET    : $(dotnet "$APP" | tr '\n' ' ')"
echo "  scalar  : $("$OUT/VectorBench.scalar" | tr '\n' ' ')"
echo "  Highway : $("$OUT/VectorBench.hwy" | tr '\n' ' ')"

SCALAR_T="$(best_of "$OUT/VectorBench.scalar")"
HWY_T="$(best_of "$OUT/VectorBench.hwy")"

echo
echo "──────── wall-clock (best of $REPS) ────────"
echo "  scalar emulation : ${SCALAR_T} s"
echo "  Highway SIMD     : ${HWY_T} s"
awk "BEGIN{ if ($HWY_T>0) printf \"  speedup          : %.2fx (scalar/Highway)\n\", $SCALAR_T/$HWY_T }"
echo
echo "Reading: speedup > 1 means the Highway backend ran the vector workload faster."
echo "A ratio near 1 means clang already auto-vectorized the scalar lane loops, so the"
echo "thin-wrapper buys little on this ISA — itself a useful finding (timings vary by run)."
