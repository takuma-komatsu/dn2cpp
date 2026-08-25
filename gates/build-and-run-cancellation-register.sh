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
source "$(dirname "$0")/_common.sh"

corelib_diff_gate CancellationRegister
