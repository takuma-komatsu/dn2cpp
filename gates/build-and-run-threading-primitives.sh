#!/usr/bin/env bash
# Real synchronization primitives — Interlocked (i4/i8/ref atomics + return-value
# contract), Volatile.Read/Write (int/long/double/bool), Interlocked.MemoryBarrier, and
# [ThreadStatic] (main-thread behavior). All single-threaded, so the output is identical
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
source "$(dirname "$0")/_common.sh"
corelib_diff_gate ThreadingPrimitives
