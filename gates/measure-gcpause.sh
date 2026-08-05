#!/usr/bin/env bash
# GC real-time spike — A/B stop-the-world pause times for the SAME native binary
# under the current stop-the-world default vs Boehm's incremental mode.
#
# This is a measurement aid, NOT a regression gate: timings are non-deterministic,
# so it prints numbers only and never asserts pass/fail. The `measure-*` name keeps
# it out of run-all-gates.sh, which discovers gates by the `build-and-run-*` glob.
#
# The pause numbers come from the runtime's DN2CPP_GC_STATS instrumentation
# (GC_set_on_collection_event in runtime/core/dn2cpp_gc.cpp); the same binary
# switches modes via DN2CPP_GC_INCREMENTAL at startup, so no rebuild between runs.
source "$(dirname "$0")/_common.sh"

OUT=artifacts/gcpausebench

echo "== 1/3 Building GcPauseBench C# assembly =="
build_proj samples/dotnet/GcPauseBench/GcPauseBench.csproj

echo "== 2/3 Transpiling IL -> C++ and compiling native binary =="
invoke_cli "samples/dotnet/GcPauseBench/bin/$CONFIG/$TFM/GcPauseBench.dll" -o "$OUT"
compile_console "$OUT" GcPauseBench

echo "== 3/3 Measuring stop-the-world pauses (baseline vs incremental) =="

# run_one LABEL VAR=VAL... — run the bench with the given env, separating the
# program's stderr (the GC pause summary) from `time`'s wall-clock report.
run_one() {
    local label="$1"; shift
    local errfile timefile
    errfile="$(mktemp)"; timefile="$(mktemp)"
    local TIMEFORMAT='%R'
    { time env "$@" "./$OUT/GcPauseBench" >/dev/null 2>"$errfile"; } 2>"$timefile"
    echo
    echo "──────── $label ────────"
    echo "wall-clock:          $(cat "$timefile") s"
    cat "$errfile"
    rm -f "$errfile" "$timefile"
}

run_one "baseline (stop-the-world)"     DN2CPP_GC_STATS=1
run_one "incremental (time-limit 5ms)"  DN2CPP_GC_INCREMENTAL=1 DN2CPP_GC_TIME_LIMIT_MS=5 DN2CPP_GC_STATS=1

echo
echo "Reading: lower 'max pause' = better real-time behaviour (incremental caps it"
echo "at the time limit). 'total pause' / wall-clock show the throughput trade-off:"
echo "VDB page-protection adds mutator cost, but generational young-gen collection"
echo "can offset it — judge from all three numbers, not max pause alone."
