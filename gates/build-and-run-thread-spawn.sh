#!/usr/bin/env bash
# Real OS threads (System.Threading.Thread). Spawns N worker threads that race a
# shared counter (Interlocked) and a locked accumulator, joins them, and reads results
# only after Join — so the output is fully deterministic (N*K) and diffed exact vs real
# .NET. Also covers ParameterizedThreadStart (boxed arg), a Volatile producer->consumer
# handoff, Thread.Sleep, and IsAlive. A racy atomics/lock implementation would not
# reproduce the exact totals, so this is the genuine cross-thread correctness gate.
source "$(dirname "$0")/_common.sh"
corelib_diff_gate ThreadSpawn
