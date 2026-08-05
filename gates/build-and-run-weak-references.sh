#!/usr/bin/env bash
# WeakReference/WeakReference<T> now hold a
# real Boehm weak link (GC_general_register_disappearing_link for the default
# short reference, GC_register_long_link for trackResurrection: true) instead
# of the earlier GC-scanned-cell approximation that never actually let the
# referent go. Basic/Long only check the deterministic pre-collection path
# (construct + TryGetTarget/Target while still reachable); Memory is the
# actual "does the collector reclaim weakly-referenced garbage" check, done
# in aggregate (many objects, bounded heap growth) rather than per-object —
# verified empirically that a single object's post-collection state is not
# reliably observable from a managed test program (the conservative
# collector, and real .NET's JIT-driven liveness, can both retain any one
# specific referent indefinitely via incidental register/stack copies).
# Former gates: none (new area).
source "$(dirname "$0")/_common.sh"

corelib_diff_gate WeakReferences
