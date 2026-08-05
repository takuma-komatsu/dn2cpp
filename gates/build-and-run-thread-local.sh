#!/usr/bin/env bash
# ThreadLocal<T>: per-instance, per-thread storage with an optional lazy valueFactory.
# N worker threads each set/read their own value (value-type + reference-type) behind a
# spin barrier proving no cross-thread bleed; a lazy factory runs exactly once per thread
# (counter == N, fixed value sum); plus default(T)/IsValueCreated and the long/double/
# float factory kinds on the main thread. Every cross-thread result is accumulated with
# Interlocked and read only after Join, so the output is fully deterministic and diffed
# exact vs real .NET (a racy/leaky implementation would not reproduce the totals).
source "$(dirname "$0")/_common.sh"
corelib_diff_gate ThreadLocalState
