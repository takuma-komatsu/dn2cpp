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
    gate_capture_ok "$out/assert-native-1" run_bounded "./$out/ParallelLoops"
    native="$out/assert-native-1.stdout"

    gate_capture_ok "$out/assert-before-2" run_bounded dotnet "$_CG_APP" before-argument-fields
    before="$out/assert-before-2.stdout"

    prefix="$out/assert-prefix-4.stdout"

    gate_output select "$native" "$prefix" --before '== ParallelOptions fields ==' --policy crlf
    gate_output compare "$prefix" "$before"
    for line in '== ParallelOptions fields ==' \
        'dop:0 actual=Int32:0' \
        'dop:-2 actual=Int32:-2' \
        'dop getter:0 type=NullReferenceException' \
        'dop receiver:0:0 type=NullReferenceException' \
        'parallel options fields GC param=MaxDegreeOfParallelism' \
        'ParallelOptions fields end'; do
        gate_output witness "$native" "$line" --count any --policy crlf \
            || { echo "FAIL: ParallelLoops validation witness missing: $line" >&2; exit 1; }
    done
    gate_capture_ok "$out/assert-before-3" run_bounded dotnet "$_CG_APP" before-callback-validation
    before="$out/assert-before-3.stdout"

    prefix="$out/assert-prefix-5.stdout"

    gate_output select "$native" "$prefix" --before '== argument checks ==' --policy crlf
    gate_output compare "$prefix" "$before"
    for line in '== argument checks ==' 'actions run: 0' 'argument checks end'; do
        gate_output witness "$native" "$line" --count any --policy crlf \
            || { echo "FAIL: Parallel callback validation witness missing: $line" >&2; exit 1; }
    done
}

corelib_diff_gate ParallelLoops
