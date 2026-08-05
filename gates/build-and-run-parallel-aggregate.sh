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
source "$(dirname "$0")/_common.sh"
corelib_diff_gate ParallelAggregate
