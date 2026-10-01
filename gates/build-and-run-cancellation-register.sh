#!/usr/bin/env bash
# Consolidated CancellationToken.Register gate. StateTokenCallback.cs covers the
# state-AND-token shape Register/UnsafeRegister(Action<object,CancellationToken>,
# object) and UnsafeRegister(Action<object>, object) — that the callback receives the
# CANCELLING token, and that the two spellings are indistinguishable. The rest: a cross-thread Cancel()
# runs registered callbacks, LIFO ordering, immediate run when already canceled,
# Dispose() detaches a registration, copied registration handles preserve equality
# and hash identity, distinct handles remain unequal, and concurrent register/cancel stays
# data-race-free. Every cross-thread Cancel() is joined before printing, so the
# program is deterministic and diffed exact vs real .NET.
# CancellationTokenHash.cs pins the token's source identity through its direct,
# EqualityComparer, Dictionary, record, boxed, and constrained equality/hash paths.
#
# Plus the CancelAfter timer, which is the one part of this surface that runs on a
# real OS clock rather than the scheduler's virtual one: CancelAfter reschedules a
# single timer (both directions), Cancel() ahead of it fires the callbacks once,
# Dispose() stops it (through both the direct call and `using`), the timed ctors arm
# the same timer, Timeout.Infinite disarms and any other negative delay throws. The
# last section is a liveness test — a source whose every managed reference is
# dropped before its timer is due must survive a collection storm and still fire.
# Nothing asserts a duration: a cancel that must happen is waited for with a budget
# 200x its delay, one that must not is armed a minute out.
#
# TernaryDefaultMerge is NOT about cancellation and must not be pruned with it: it is
# the eval-stack join of a struct-returning call with `default`, which lands
# here only because CancellationTokenRegistration is the intrinsic pointer value type
# that shows the bug. It carries a RuntimeTypeHandle arm for the same reason.
# Named argument faults, boxed bounds, validation order and GC-retained fields.
source "$(dirname "$0")/_common.sh"

gate_extra_asserts() {
    local out="$1" native before prefix line
    native=$(run_bounded "./$out/CancellationRegister")
    native=$(strip_cr_win "$native")
    before=$(dotnet "$_CG_APP" before-argument-fields)
    before=$(strip_cr_win "$before")
    prefix=$(awk '/^== Cancellation delay fields ==$/ { exit } { print }' <<< "$native")
    assert_output "$prefix" "$before"
    for line in '== Cancellation delay fields ==' \
        'cts int:-2 param=millisecondsDelay' \
        'cts span:9223372036854775807 param=delay' \
        'cts null int:-2 type=NullReferenceException' \
        'cts state int:0:-2 param=millisecondsDelay' \
        'cts state int:0:-1 type=ObjectDisposedException' \
        'cts state int:1:-1 success=armed' \
        'cancellation delay fields GC actual=null' \
        'Cancellation delay fields end'; do
        grep -Fxq -- "$line" <<< "$native" \
            || { echo "FAIL: CancellationRegister validation witness missing: $line" >&2; exit 1; }
    done
}

corelib_diff_gate CancellationRegister
