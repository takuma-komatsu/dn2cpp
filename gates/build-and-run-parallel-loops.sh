#!/usr/bin/env bash
# Data-parallel loops — Parallel.For (int + long), Parallel.ForEach (reference / int /
# double element arrays), and Parallel.Invoke — on a real OS-thread fan-out with a
# deterministic join barrier. Each loop uses disjoint per-element slot writes + a
# sequential reduction, or a commutative Interlocked accumulation, and reads results only
# AFTER the barriering call, so the output is deterministic and diffed exact vs real .NET.
# A non-barriering or racy implementation would not reproduce these totals.
# Named argument faults, boxed bounds, validation order and GC-retained fields.
source "$(dirname "$0")/_common.sh"
gate_extra_asserts() {
    local out="$1" native before prefix line
    native=$(run_bounded "./$out/ParallelLoops")
    native=$(strip_cr_win "$native")
    before=$(dotnet "$_CG_APP" before-argument-fields)
    before=$(strip_cr_win "$before")
    prefix=$(awk '/^== ParallelOptions fields ==$/ { exit } { print }' <<< "$native")
    assert_output "$prefix" "$before"
    for line in '== ParallelOptions fields ==' \
        'dop:0 actual=Int32:0' \
        'dop:-2 actual=Int32:-2' \
        'dop getter:0 type=NullReferenceException' \
        'dop receiver:0:0 type=NullReferenceException' \
        'parallel options fields GC param=MaxDegreeOfParallelism' \
        'ParallelOptions fields end'; do
        grep -Fxq -- "$line" <<< "$native" \
            || { echo "FAIL: ParallelLoops validation witness missing: $line" >&2; exit 1; }
    done
    before=$(dotnet "$_CG_APP" before-callback-validation)
    before=$(strip_cr_win "$before")
    prefix=$(awk '/^== argument checks ==$/ { exit } { print }' <<< "$native")
    assert_output "$prefix" "$before"
    for line in '== argument checks ==' 'actions run: 0' 'argument checks end'; do
        grep -Fxq -- "$line" <<< "$native" \
            || { echo "FAIL: Parallel callback validation witness missing: $line" >&2; exit 1; }
    done
}

corelib_diff_gate ParallelLoops
