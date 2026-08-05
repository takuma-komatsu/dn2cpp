#!/usr/bin/env bash
# The whole LinqCore program (~149 LINQ ops across 13 subsets) transpiled against
# the real System.Linq.dll (passed with -r) — the only LINQ the transpiler
# consumes. The deferred operators were already gap-free; generic-math lowering,
# ldftn/ldvirtftn of external methods, the StringComparer.CurrentCulture ->
# ordinal intercept, and the residual SequenceEqual/Unsafe/GC/AppContext/OOM
# intrinsics brought the real-System.Linq gap from 132 to 0. Output diffs exact
# vs real .NET.
source "$(dirname "$0")/_common.sh"

corelib_diff_gate LinqCore \
    System.Linq System.Collections.Immutable System.Collections System.Runtime
