#!/usr/bin/env bash
# The managed-fault guards' cost — code size and per-kernel wall clock
# of the SAME generated C++ built with the guards on (the shipped default) and off.
#
# This is a measurement aid, NOT a regression gate: byte counts and timings drift with
# the toolchain, so it prints numbers and never asserts pass/fail on them (the
# `measure-*` name keeps it out of run-all-gates.sh, which discovers gates by the
# `build-and-run-*` glob). It DOES cross-check that every kernel's checksum is
# identical across the arms — a guard that changed an answer would be a bug, not a
# cost — and a divergence exits nonzero after the report has printed.
#
# Why the axis is a native build option and not a transpiler flag: the guards are
# inline functions (dn2cpp_null_check, dn2cpp_div_signed and friends in
# dn2cpp_core.h) that the emitted code names unconditionally, so the generated*.cpp
# is BYTE-IDENTICAL across the arms and the transpile runs once. Every difference
# below is the check itself and nothing else — no re-emission, no different inlining
# of a differently-shaped call site, and nothing that could perturb the self-host
# fixpoint. The option reaches the app target directly (runtime/CMakeLists.txt's app
# arm), so no arm reconfigures the shared runtime build dir and this is safe to run
# beside a suite.
#
# Two things about reading the output, both learned the hard way here:
#
#  - Size is reported as the __TEXT/.text SECTION, never as the file length. A
#    Mach-O's length is page-padded, and the first run of this script reported all
#    four arms at exactly 340800 bytes while their md5s differed — a real delta
#    rounded to zero.
#  - Timing is PER KERNEL and internal to the benchmark (Stopwatch), never process
#    wall clock. These binaries run in a few hundred ms of which most is startup, so
#    a whole-process A/B compares two numbers that are mostly dynamic linker; the
#    first run of this script duly reported removing a check as a 22% SLOWDOWN.
#
# FaultCheckBench is the worst case on purpose — one guarded shape per loop, with
# receivers crossing NoInlining boundaries so nothing can be proved non-null. Real
# code dilutes the guard among the work; this does not. Its `divconst`/`remconst`
# kernels are the control: their divisors are compile-time constants, so if those
# rows move between arms then the "let clang be the constant-divisor analysis"
# premise the guards rest on is false.
#
#   ./gates/measure-faultchecks.sh          # bench kernels + size on two real corpora
#   REPS=9 ./gates/measure-faultchecks.sh   # more timing repetitions (default 3)
source "$(dirname "$0")/_common.sh"

REPS="${REPS:-3}"
DIVERGED=

# The arms. "on" is the shipped default and passes nothing, so its build is byte
# for byte the one every other gate produces. "on2" is a REPEAT of it under a
# different name: it measures nothing but the noise floor, which is the only
# honest way to read the small deltas below.
ARMS=(on on2 null-off arith-off both-off)
arm_args() {
    case "$1" in
        on|on2)    printf '%s' "" ;;
        null-off)  printf '%s' "-DDN2CPP_NULL_CHECKS=OFF" ;;
        arith-off) printf '%s' "-DDN2CPP_ARITH_CHECKS=OFF" ;;
        both-off)  printf '%s' "-DDN2CPP_NULL_CHECKS=OFF -DDN2CPP_ARITH_CHECKS=OFF" ;;
    esac
}

# text_bytes BIN — the executable code section's size, not the file's. See the
# header: the file length is page-padded and hides the delta entirely.
text_bytes() {
    # No `awk … exit` at the end of a pipe: awk quits, `size` behind it dies of
    # SIGPIPE, and `pipefail` (gates/_common.sh, where the whole hazard is
    # written out) reports the function as failed on every binary whose __text
    # section is not the last thing `size` prints. Drain, then take line 1.
    local all
    case "$DN2CPP_OS" in
        macos) all=$(size -m "$1" 2>/dev/null | awk '/Section __text/ { print $3 }' || true) ;;
        *)     all=$(size -A "$1" 2>/dev/null | awk '$1 == ".text" { print $2 }' || true) ;;
    esac
    printf '%s' "${all%%$'\n'*}"
}

pct() { python3 -c "import sys; b=float(sys.argv[1]); v=float(sys.argv[2]); print('n/a' if b==0 else f'{(v-b)*100/b:+.2f}')" "$1" "$2"; }

# build_arm OUT BIN ARM — build one arm into OUT/BIN.ARM.
build_arm() {
    local out="$1" bin="$2" arm="$3"
    ( export DN2CPP_EXTRA_CMAKE_ARGS="${DN2CPP_EXTRA_CMAKE_ARGS:-} $(arm_args "$arm")"
      compile_console "$out" "$bin" ) || return 1
    cp -f "$out/$bin" "$out/$bin.$arm"
}

echo "== 1/4 Building the bench assemblies =="
build_proj samples/dotnet/FaultCheckBench/FaultCheckBench.csproj
build_proj samples/dotnet/LinqSumBench/LinqSumBench.csproj
build_proj samples/dotnet/HotPathBench/HotPathBench.csproj

echo "== 2/4 Transpiling IL -> C++ (once per corpus; every arm shares the output) =="
CORELIB="$(locate_corelib)"
BCL="$(dirname "$CORELIB")"
invoke_cli "samples/dotnet/FaultCheckBench/bin/$CONFIG/$TFM/FaultCheckBench.dll" \
    -r "$CORELIB" -o artifacts/faultchecks-bench
LINQ_REFS=(-r "$CORELIB")
for name in System.Linq System.Collections System.Runtime; do
    [ -f "$BCL/$name.dll" ] && LINQ_REFS+=(-r "$BCL/$name.dll")
done
invoke_cli "samples/dotnet/LinqSumBench/bin/$CONFIG/$TFM/LinqSumBench.dll" \
    "${LINQ_REFS[@]}" -o artifacts/faultchecks-linq
invoke_cli "samples/dotnet/HotPathBench/bin/$CONFIG/$TFM/HotPathBench.dll" \
    -r "$CORELIB" -o artifacts/faultchecks-hot

echo "== 3/4 Per-kernel cost (FaultCheckBench — the worst case, best of $REPS) =="
# Accumulated into a flat TSV and reduced with awk rather than into an
# associative array: the macOS system bash is 3.2 and `declare -A` is a syntax
# error there, which is the same reason _pidlock_release cannot use BASHPID.
SAMPLES="artifacts/faultchecks-bench/_samples.tsv"
: > "$SAMPLES"
for arm in "${ARMS[@]}"; do
    build_arm artifacts/faultchecks-bench faultcheckbench "$arm" || exit 1
    for ((i = 0; i < REPS; i++)); do
        "artifacts/faultchecks-bench/faultcheckbench.$arm" \
            | awk -v a="$arm" '{ sub(/^checksum=/, "", $2); sub(/^ms=/, "", $3);
                                 print a "\t" $1 "\t" $2 "\t" $3 }' >> "$SAMPLES"
    done
done

awk -F'\t' -v arms="${ARMS[*]}" '
{
    key = $1 SUBSEP $2
    if (!((key) in best) || $4 + 0 < best[key]) best[key] = $4 + 0
    if (!($2 in kseen)) { kseen[$2] = 1; korder[++nk] = $2 }
    if (!($2 in csum)) csum[$2] = $3
    else if (csum[$2] != $3) { printf "  !! kernel %s answered %s under %s, %s elsewhere\n", $2, $3, $1, csum[$2] > "/dev/stderr"; bad = 1 }
}
END {
    na = split(arms, A, " ")
    printf "\n%-12s", "kernel"
    for (i = 1; i <= na; i++) printf "%12s", A[i]
    printf "   %s\n", "(ms, lower is better)"
    for (k = 1; k <= nk; k++) {
        name = korder[k]
        printf "%-12s", name
        for (i = 1; i <= na; i++) printf "%12s", best[A[i] SUBSEP name]
        off = best["both-off" SUBSEP name]
        on  = best["on" SUBSEP name]
        on2 = best["on2" SUBSEP name]
        # Parenthesised into locals first: the macOS system awk (BWK) rejects a
        # bare ternary in a printf argument list.
        cost = 0; noise = 0
        if (off > 0) cost = (on - off) * 100 / off
        if (on > 0) noise = (on2 - on) * 100 / on
        printf "   guard cost %+.1f%%  (noise floor %+.1f%%)\n", cost, noise
    }
    exit bad ? 3 : 0
}' "$SAMPLES" || DIVERGED=1

echo
echo "== 4/4 Code size on two REAL corpora (__text section bytes) =="
printf '%-14s %-10s %12s %10s\n' corpus arm bytes 'd%'
for spec in "faultchecks-bench:faultcheckbench" "faultchecks-linq:linqsum" "faultchecks-hot:hotpathbench"; do
    out="artifacts/${spec%%:*}"; bin="${spec##*:}"
    base=
    for arm in "${ARMS[@]}"; do
        [ "$arm" = on2 ] && continue
        [ -f "$out/$bin.$arm" ] || build_arm "$out" "$bin" "$arm" || exit 1
        b=$(text_bytes "$out/$bin.$arm")
        [ -z "$base" ] && base="$b"
        printf '%-14s %-10s %12s %9s%%\n' "$bin" "$arm" "$b" "$(pct "$base" "$b")"
    done
done

echo
echo "Read the on/on2 pair first: it is the same build twice, so any kernel whose"
echo "on-vs-on2 gap is as large as its on-vs-off gap has told you nothing. The"
echo "divconst/remconst rows are the constant-divisor control — they must not move."

[ -n "$DIVERGED" ] && { echo "FAIL: an arm changed an answer (see above)" >&2; exit 1; }
exit 0
