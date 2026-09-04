#!/usr/bin/env bash
# Real synchronization primitives — Interlocked (i4/i8/ref atomics + return-value
# contract), Volatile.Read/Write (int/long/double/bool), legacy
# Thread.VolatileRead/Write (integer/reference/float/double), Interlocked.MemoryBarrier, and
# [ThreadStatic] (main-thread behavior), and managed WaitHandle subclass key lifetime
# across collection and address reuse. All single-threaded, so the output is identical
# to real .NET and is diffed exact (corelib_diff_gate). The genuine cross-thread test
# (N threads racing a shared counter, Join, exact total) is the Thread gate.
# Also covers the two lowerings of the `lock` STATEMENT, single-threaded and so
# equally deterministic: LockSubset (`lock (object)` -> Monitor.Enter(ref taken) /
# finally Exit, plus the explicit Monitor.TryEnter forms) and LockTypeSubset (the
# .NET 9 `System.Threading.Lock` -> EnterScope() / finally Scope.Dispose(), plus
# the `using`-statement form and TryEnter/Enter/Exit). Both exercise a
# value-returning body, re-entrant nesting and a throwing body that must still
# release.
# Former gates: lock-subset, locktype-subset.
#
# MonotonicClock is here for its SURFACE, not for this gate's theme:
# Stopwatch.GetTimestamp and the user assembly's Environment.TickCount64
# MemberReference reach separate libSystem.Native clocks through their real BCL
# bodies. This is the native axis's only diff of that contract against real .NET;
# its wasm twin also proves both PAL symbols link. Do not prune it by reading the
# gate's name.
source "$(dirname "$0")/_common.sh"
corelib_diff_gate ThreadingPrimitives
corelib_diff_gate MonotonicClock
