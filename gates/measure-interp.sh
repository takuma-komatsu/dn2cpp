#!/usr/bin/env bash
# Interpreter throughput baseline — wall-clock of the SAME kernel workload run
# as AOT-transpiled native code vs the BPI stack interpreter vs the BPI
# register interpreter (a hot-update patch overriding the kernel virtuals with
# textually identical bodies, so all configs execute the same logic and must
# print identical checksums).
#
# This is a measurement aid, NOT a regression gate: timings are
# non-deterministic, so it prints numbers only and never asserts pass/fail
# (the `measure-*` name keeps it out of run-all-gates.sh, which discovers
# gates by the `build-and-run-*` glob). Checksum/impl-marker mismatches print
# a loud WARNING but never exit nonzero.
#
# Sections:
#   noop   — runs no kernel: process startup + BPI load cost.
#   scalar — mixed int/long/double arithmetic loop INSIDE one virtual call
#   calls  — per-iteration kernel-internal method call      (one dispatch
#   array  — int[256] wrap-indexed read-modify-write         amortizes over
#                                                            the whole loop).
#   virt   — tiny virtual body called per iteration from the AOT side: the
#            per-call AOT->interpreter dispatch (N2M transition) cost.
source "$(dirname "$0")/_common.sh"

OUT=artifacts/interpbench
REPS="${REPS:-5}"

# Iteration counts, sized so each stack-interpreter run lands around 0.5-1.5s
# wall on an Apple-Silicon dev machine; override via env for other hardware.
NOOP_ITERS=1
SCALAR_ITERS="${SCALAR_ITERS:-8000000}"
CALLS_ITERS="${CALLS_ITERS:-16000000}"
ARRAY_ITERS="${ARRAY_ITERS:-12000000}"
VIRT_ITERS="${VIRT_ITERS:-50000000}"

# name:bpiPath pairs; an empty path runs pure AOT.
CONFIGS=("aot:" "stack:$OUT/InterpBenchPatch.bpi" "reg:$OUT/reg/InterpBenchPatch.bpi")
SECTIONS=(noop scalar calls array virt)

iters_for() {
    case "$1" in
        noop)   echo "$NOOP_ITERS" ;;
        scalar) echo "$SCALAR_ITERS" ;;
        calls)  echo "$CALLS_ITERS" ;;
        array)  echo "$ARRAY_ITERS" ;;
        virt)   echo "$VIRT_ITERS" ;;
    esac
}

echo "== 1/4 Building base + patch C# assemblies =="
build_proj samples/dotnet/InterpBench/InterpBench.csproj
build_proj samples/dotnet/InterpBenchPatch/InterpBenchPatch.csproj
base_app="samples/dotnet/InterpBench/bin/$CONFIG/$TFM/InterpBench.dll"
patch_app="samples/dotnet/InterpBenchPatch/bin/$CONFIG/$TFM/InterpBenchPatch.dll"

echo "== 2/4 Transpiling base (--hotupdate-base) + baking the patch BPIs (stack + reg) =="
invoke_cli "$base_app" --hotupdate-base -o "$OUT"
invoke_cli --emit-patch "$patch_app" --base-abi "$OUT/base-abi.json" --patch-stackcode -o "$OUT"
invoke_cli --emit-patch "$patch_app" --base-abi "$OUT/base-abi.json" --patch-regcode -o "$OUT/reg"

echo "== 3/4 Compiling native binary =="
compile_console "$OUT" InterpBench

echo "== 4/4 Measuring wall-clock (best of $REPS reps; lower is better) =="

# best_of SECTION ITERS BPI OUTFILE ERRFILE — run the bench REPS times, leave
# the last run's stdout/stderr in OUTFILE/ERRFILE, and print the minimum
# wall-clock seconds across reps (min is the most stable estimator for perf).
best_of() {
    local section="$1" iters="$2" bpi="$3" outfile="$4" errfile="$5"
    local best="" t i tf
    local TIMEFORMAT='%R'
    for i in $(seq 1 "$REPS"); do
        tf="$(mktemp)"
        if [ -n "$bpi" ]; then
            { time "./$OUT/InterpBench" "$section" "$iters" "$bpi" >"$outfile" 2>"$errfile"; } 2>"$tf"
        else
            { time "./$OUT/InterpBench" "$section" "$iters" >"$outfile" 2>"$errfile"; } 2>"$tf"
        fi
        t="$(cat "$tf")"; rm -f "$tf"
        if [ -z "$best" ] || awk "BEGIN{exit !($t < $best)}"; then best="$t"; fi
    done
    printf '%s' "$best"
}

outfile="$(mktemp)"; errfile="$(mktemp)"

echo
printf '%-8s %-8s' "section" "iters"
for cfg in "${CONFIGS[@]}"; do
    name="${cfg%%:*}"
    printf ' %10s' "$name (s)"
    [ "$name" != "aot" ] && printf ' %11s' "$name/aot"
done
printf ' %11s' "stack/reg"
printf '\n'

for section in "${SECTIONS[@]}"; do
    iters="$(iters_for "$section")"
    aot_t=""
    stack_t=""
    reg_t=""
    printf '%-8s %-8s' "$section" "$iters"
    ref_checksum=""
    for cfg in "${CONFIGS[@]}"; do
        name="${cfg%%:*}"
        bpi="${cfg#*:}"
        t="$(best_of "$section" "$iters" "$bpi" "$outfile" "$errfile")"
        checksum="$(cat "$outfile")"
        impl="$(cat "$errfile")"
        # The impl marker guards against a silently failed patch load reading
        # as a flattering 1.0x ratio.
        if [ "$name" = "aot" ]; then
            expected_impl="impl:Kernel"
        else
            expected_impl="impl:PatchedKernel"
        fi
        if [ "$impl" != "$expected_impl" ]; then
            echo
            echo "WARNING: $name/$section ran '$impl' (expected '$expected_impl') — ratios below are not meaningful" >&2
        fi
        if [ -z "$ref_checksum" ]; then
            ref_checksum="$checksum"
        elif [ "$checksum" != "$ref_checksum" ]; then
            echo
            echo "WARNING: $name/$section checksum '$checksum' differs from aot '$ref_checksum'" >&2
        fi
        printf ' %10s' "$t"
        if [ "$name" = "aot" ]; then
            aot_t="$t"
        else
            ratio="$(awk -v t="$t" -v a="$aot_t" 'BEGIN{ if (a > 0) printf "%.1fx", t / a; else printf "n/a" }')"
            printf ' %11s' "$ratio"
            [ "$name" = "stack" ] && stack_t="$t"
            [ "$name" = "reg" ] && reg_t="$t"
        fi
    done
    # The headline number: how much faster the register bytecode runs the same
    # section than the stack bytecode.
    speedup="$(awk -v s="$stack_t" -v r="$reg_t" 'BEGIN{ if (r > 0) printf "%.2fx", s / r; else printf "n/a" }')"
    printf ' %11s' "$speedup"
    printf '\n'
done

rm -f "$outfile" "$errfile"

echo
echo "Reading: noop is pure startup + BPI load (iters=1), so its stack/reg"
echo "columns are the fixed per-process patching overhead, not interpreter"
echo "speed. scalar/calls/array run the whole loop inside one virtual call —"
echo "their ratio is raw interpreter loop throughput vs AOT. virt loops on the"
echo "AOT side and dispatches into a tiny virtual per iteration — its ratio is"
echo "the per-call AOT->interpreter (N2M) dispatch cost. The reg column is the"
echo "register bytecode (the --emit-patch default): it removes the stack push/"
echo "pop shuffling, so stack/reg is the speedup the register format buys on"
echo "that section. Timings vary by run; checksums must match across configs"
echo "or a WARNING is printed above."
