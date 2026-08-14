#!/usr/bin/env bash
# [HotPath] perf A/B — wall-clock of the SAME kernels built with and without the
# attribute's knob set (SkipBoundsChecks + NoAlias + FastMath).
#
# This is a measurement aid, NOT a regression gate: timings are non-deterministic, so it
# never asserts on them (the `measure-*` name keeps it out of run-all-gates.sh, which
# discovers gates by the `build-and-run-*` glob). It does verify the two builds agree on
# the exact integer checksum — a correctness signal, not a timing — and a divergence
# exits nonzero, after the full timing report has printed so the investigation has the
# numbers in hand.
#
# Unlike measure-vectorperf.sh, the axis here cannot be a native build option: the knobs
# live in the IL, so the C# is compiled TWICE — once with DN2CPP_HOTPATH_OFF defined,
# which strips the attribute off every kernel — and each assembly is transpiled and built
# separately. Everything else is held fixed: same source, same runtime, same CMake
# configuration. HotPathBench's kernels are an array axpy, a span dot product and a
# float reduction, sized so the loops dominate process startup.
#
# The float reduction's own value is printed but NOT cross-checked: FastMath is allowed
# to reassociate it, which over the bench's repetition count moves far more than the last
# bits. The integer checksum is exact under every knob combination, and that is the one
# the cross-check uses.
source "$(dirname "$0")/_common.sh"

PROJ=samples/dotnet/HotPathBench/HotPathBench.csproj
OUT_ON=artifacts/hotpathbench-on
OUT_OFF=artifacts/hotpathbench-off
BIN_ON=samples/dotnet/HotPathBench/bin/hp-on
BIN_OFF=samples/dotnet/HotPathBench/bin/hp-off
REPS="${REPS:-5}"

echo "== 1/4 Building HotPathBench twice (knobs on / knobs off) =="
# Not build_proj: that helper takes no extra MSBuild arguments and no-ops under
# DN2CPP_SKIP_BUILD, and this script needs two differently-defined builds of one
# project regardless of what a runner pre-built.
dotnet build "$PROJ" -c "$CONFIG" --nologo -v q -o "$BIN_ON"
dotnet build "$PROJ" -c "$CONFIG" --nologo -v q -o "$BIN_OFF" -p:DefineConstants=DN2CPP_HOTPATH_OFF

echo "== 2/4 Transpiling IL -> C++ (once per assembly) =="
CORELIB="$(locate_corelib)"
invoke_cli "$BIN_ON/HotPathBench.dll" -r "$CORELIB" -o "$OUT_ON"
invoke_cli "$BIN_OFF/HotPathBench.dll" -r "$CORELIB" -o "$OUT_OFF"

# Placement, printed rather than asserted: which TUs the knobs actually produced is
# the first thing to check when a ratio looks wrong.
hot_tus() {
    local files
    files="$(cd "$1" && ls generated_hot*.cpp 2>/dev/null | tr '\n' ' ')"
    printf '%s' "${files:-none}"
}
echo
echo "translation units:"
echo "  knobs on  : $(hot_tus "$OUT_ON")"
echo "  knobs off : $(hot_tus "$OUT_OFF")"

echo "== 3/4 Building both native binaries =="
compile_console "$OUT_ON" HotPathBench
compile_console "$OUT_OFF" HotPathBench

echo "== 4/4 Measuring wall-clock ($REPS reps each; lower is better) =="

# best_of BIN — run BIN REPS times and echo the minimum wall-clock seconds (min is the
# most stable estimator for perf).
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

ON_OUT="$("$OUT_ON/HotPathBench")"
OFF_OUT="$("$OUT_OFF/HotPathBench")"
ON_SUM="$(printf '%s\n' "$ON_OUT" | awk 'NR == 1')"
OFF_SUM="$(printf '%s\n' "$OFF_OUT" | awk 'NR == 1')"

echo
echo "exact integer checksum (must match):"
echo "  .NET      : $(dotnet "$BIN_OFF/HotPathBench.dll" | awk 'NR == 1')"
echo "  knobs on  : $ON_SUM"
echo "  knobs off : $OFF_SUM"
CHECKSUM_MISMATCH=0
if [ "$ON_SUM" != "$OFF_SUM" ]; then
    CHECKSUM_MISMATCH=1
    echo
    echo "WARNING: the two builds disagree on the exact checksum — the knobs changed a"
    echo "         result they must not change. Every timing below is meaningless until"
    echo "         that is understood; start with the emitted bodies in $OUT_ON."
fi
echo
echo "float reduction (may legitimately differ — FastMath reassociates it):"
echo "  knobs on  : $(printf '%s\n' "$ON_OUT" | awk 'NR == 2')"
echo "  knobs off : $(printf '%s\n' "$OFF_OUT" | awk 'NR == 2')"

ON_T="$(best_of "$OUT_ON/HotPathBench")"
OFF_T="$(best_of "$OUT_OFF/HotPathBench")"

echo
echo "──────── wall-clock (best of $REPS) ────────"
echo "  [HotPath] knobs off : ${OFF_T} s"
echo "  [HotPath] knobs on  : ${ON_T} s"
awk "BEGIN{ if ($ON_T>0) printf \"  speedup             : %.2fx (off/on)\n\", $OFF_T/$ON_T }"
echo
echo "Reading: speedup > 1 means the knob set ran the same kernels faster. What it is"
echo "measuring is the knobs' semantics — elided bounds checks, the aliasing claim, and"
echo "relaxed floating point — plus the hot TUs' own flags, which on clang/gcc add -O3"
echo "over the flat -O2 but on MSVC add nothing beyond DN2CPP_HOT_ARCH (unset by"
echo "default; try -DDN2CPP_HOT_ARCH=/arch:AVX2 or -march=native to widen the ISA)."
echo "A ratio near 1 on a memory-bound kernel is itself a finding: the checks and the"
echo "aliasing conservatism were not what the loop was waiting on (timings vary by run)."

# A checksum divergence is a correctness signal, not a timing, so it fails the script —
# but only here, after the full timing report, which the investigation will want.
if [ "$CHECKSUM_MISMATCH" -ne 0 ]; then
    echo
    echo "FAIL: exact integer checksum diverged between the two builds (see WARNING above)."
    exit 1
fi
