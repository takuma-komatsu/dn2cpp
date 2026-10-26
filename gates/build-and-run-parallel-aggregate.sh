#!/usr/bin/env bash
# Parallel exception aggregation. Parallel.For / ForEach / Invoke now collect every
# body exception thrown across the OS-thread fan-out and rethrow them on the calling thread
# as a single System.AggregateException (matching real .NET, which always wraps Parallel.*
# body exceptions — even a single one). Sections assert ae.InnerExceptions (read through
# IReadOnlyList<Exception>) — its Count and the SORTED inner messages — so the output is
# independent of the non-deterministic parallel order; catch (Exception) over the same
# aggregate; and ae.InnerException. Parallel.Invoke runs every action, so its inner count is
# deterministic; the For/ForEach cases throw from exactly one iteration (count 1) to stay
# deterministic vs real .NET, whose post-throw scheduling leaves a multi-throw count
# unspecified. Diffed exact vs real .NET.
# AggregateException.Flatten preserves inner order and identity in a fresh aggregate.
source "$(dirname "$0")/_common.sh"
DN2CPP_GATE_EXTRA_CONTEXT="${DN2CPP_GATE_EXTRA_CONTEXT:-}|before-flatten"
gate_extra_asserts() {
    local out="$1" native before prefix line
    gate_capture_ok "$out/assert-native-1" run_bounded "./$out/ParallelAggregate"
    native="$out/assert-native-1.stdout"

    gate_capture_ok "$out/assert-before-2" run_bounded dotnet "$_CG_APP" before-flatten
    before="$out/assert-before-2.stdout"
    prefix="$out/assert-prefix-4.stdout"
    gate_output select "$native" "$prefix" --before '== Aggregate flatten ==' --policy crlf
    gate_output compare "$prefix" "$before"
    gate_capture_ok "$out/assert-before-3" run_bounded "./$out/ParallelAggregate" before-flatten
    before="$out/assert-before-3.stdout"
    gate_output compare "$prefix" "$before"
    for line in '== Aggregate flatten ==' 'order=a,d,a,b,c' 'identity=True:True' \
        'message=root (a) (d) (a) (b) (c)' 'fresh=True:True' 'empty=empty' \
        'derived=override (b)' 'deep=1' 'Aggregate flatten end'; do
        gate_output witness "$native" "$line" --count 1 --policy crlf || { echo "FAIL: missing flatten witness: $line" >&2; exit 1; }
    done
}
corelib_diff_gate ParallelAggregate
