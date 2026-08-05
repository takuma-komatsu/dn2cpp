#!/usr/bin/env bash
# Data-parallel loops — Parallel.For (int + long), Parallel.ForEach (reference / int /
# double element arrays), and Parallel.Invoke — on a real OS-thread fan-out with a
# deterministic join barrier. Each loop uses disjoint per-element slot writes + a
# sequential reduction, or a commutative Interlocked accumulation, and reads results only
# AFTER the barriering call, so the output is deterministic and diffed exact vs real .NET.
# A non-barriering or racy implementation would not reproduce these totals.
source "$(dirname "$0")/_common.sh"
corelib_diff_gate ParallelLoops
