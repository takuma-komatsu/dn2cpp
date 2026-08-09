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
# IncrementalWriteBarrier mutates old holders through field, array, byref,
# reference-containing struct, bulk-copy, Interlocked and Volatile stores while
# allocation advances collection cycles; both GC modes run from one native image.
# It also drives Array.Fill of a reference element and the only proven-same-element
# Array.Copy of a reference-bearing struct array in the corpus — the two inline
# fast paths that bypass the barriered runtime helpers. InternBarrier covers
# string.Intern / IsInterned over a run-time-built string, whose only root is the
# intern cell. Former gates: none (new area).
source "$(dirname "$0")/_common.sh"

project=WeakReferences
out="$(_corelib_gate_out "$project")"
[ -n "${HIGHWAY:-}" ] && out="$out-hwy"
[ -n "${SCALAR:-}" ] && out="$out-scalar"
[ -n "${DN2CPP_OUT_SUFFIX:-}" ] && out="$out$DN2CPP_OUT_SUFFIX"

_corelib_gate_core "$project" "$out"
for barrier in \
    'dn2cpp_gc_write_barrier((void*' \
    'dn2cpp_gc_write_barrier_if_heap((void*' \
    'dn2cpp_gc_memmove_refs('
do
    if ! grep -qF "$barrier" "$out"/generated*.cpp; then
        echo "FAIL: generated WeakReferences output is missing barrier call: $barrier" >&2
        exit 1
    fi
done

ctx="weak_references|$_CG_CORELIB|runs:DN2CPP_GC_INCREMENTAL=0+1|DN2CPP_GC_STATS=1|assert:mode+diff+exit+generated-barriers+memmove-refs"
ctx="$ctx$(_gate_ctx_extras)"
if _corelib_gate_check "$out" "$ctx"; then
    gate_cache_hit_msg
    exit 0
fi

echo "== 4/4 Compiling C++ and running (STW + incremental; exact diff vs real .NET) =="
compile_console "$out" "$project"

log_dir=$(mktemp -d "${TMPDIR:-/tmp}/dn2cpp_weakrefs.XXXXXX")
trap 'rm -rf "$log_dir"' EXIT

set +e
_gate_run_argv
expected=$(run_bounded dotnet "$_CG_APP" \
    ${_GATE_RUN_ARGV[@]+"${_GATE_RUN_ARGV[@]}"}); expected_code=$?
_gate_run_argv
stw=$(DN2CPP_GC_INCREMENTAL=0 DN2CPP_GC_STATS=1 \
    run_bounded "./$out/$project" \
    ${_GATE_RUN_ARGV[@]+"${_GATE_RUN_ARGV[@]}"} 2>"$log_dir/stw.log"); stw_code=$?
_gate_run_argv
incremental=$(DN2CPP_GC_INCREMENTAL=1 DN2CPP_GC_STATS=1 \
    run_bounded "./$out/$project" \
    ${_GATE_RUN_ARGV[@]+"${_GATE_RUN_ARGV[@]}"} 2>"$log_dir/incremental.log"); incremental_code=$?
set -e
_gate_scratch_cleanup

assert_output "$stw" "$expected"
assert_exit_code "$stw_code" "$expected_code"
assert_output "$incremental" "$expected"
assert_exit_code "$incremental_code" "$expected_code"

if ! grep -qF '[dn2cpp] GC mode: stop-the-world' "$log_dir/stw.log"; then
    echo "FAIL: WeakReferences STW arm did not report stop-the-world mode" >&2
    sed -n '1,40p' "$log_dir/stw.log" >&2
    exit 1
fi
if ! grep -qF '[dn2cpp] GC mode: incremental' "$log_dir/incremental.log"; then
    echo "FAIL: WeakReferences incremental arm did not report incremental mode" >&2
    sed -n '1,40p' "$log_dir/incremental.log" >&2
    exit 1
fi
if [ "$(grep -cF 'incremental write barriers:' <<<"$incremental")" -ne 1 ]; then
    echo "FAIL: incremental write-barrier section did not run exactly once" >&2
    exit 1
fi

gate_cache_commit
