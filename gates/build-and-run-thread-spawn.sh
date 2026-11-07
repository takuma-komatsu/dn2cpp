#!/usr/bin/env bash
# Real OS threads (System.Threading.Thread). Spawns N worker threads that race a
# shared counter (Interlocked) and a locked accumulator, joins them, and reads results
# only after Join — so the output is fully deterministic (N*K) and diffed exact vs real
# .NET. Also covers ParameterizedThreadStart (boxed arg), a Volatile producer->consumer
# handoff, Thread.Sleep, and IsAlive. A racy atomics/lock implementation would not
# reproduce the exact totals, so this is the genuine cross-thread correctness gate.
# A cross-thread static-constructor cycle completes: the thread whose wait would close
# the cycle proceeds on the partial state, as .NET does, instead of deadlocking.
source "$(dirname "$0")/_common.sh"
DN2CPP_GATE_EXTRA_CONTEXT="${DN2CPP_GATE_EXTRA_CONTEXT:-}|prefix:before-init-cycle"
gate_extra_asserts() {
    local out="$1" native before prefix line
    native="$out/native.stdout"
    gate_output status "$(cat "$out/native.status")" 0 || return 1

    gate_capture_ok "$out/assert-before-1" run_bounded dotnet "$_CG_APP" before-init-cycle
    before="$out/assert-before-1.stdout"

    prefix="$out/assert-prefix-2.stdout"

    gate_output select "$native" "$prefix" --before '== init cycle ==' --policy crlf
    gate_output compare "$prefix" "$before"
    for line in '== init cycle ==' \
        'init cycle joined=True' \
        'init cycle worker reads=1/2' \
        'init cycle values=1/2' \
        'init cycle end'; do
        gate_output witness "$native" "$line" --count any --policy crlf \
            || { echo "FAIL: init cycle witness missing: $line" >&2; exit 1; }
    done
}
corelib_diff_gate ThreadSpawn
