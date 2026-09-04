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
# reference-containing struct, bulk-copy, Interlocked, Volatile and Array.Resize
# stores while allocation advances collection cycles; both GC modes run from one
# native image.
# It also drives Array.Fill of a reference element and the only proven-same-element
# Array.Copy of a reference-bearing struct array in the corpus — the two inline
# fast paths that bypass the barriered runtime helpers. InternBarrier covers
# string.Intern / IsInterned over a run-time-built string, whose only root is the
# intern cell. PendingCallArgument keeps a Dictionary/string graph on the IL
# evaluation stack while the next nested-call argument forces collection.
# ConditionalWeakTable holds each pair through a DependentHandle embedded in its
# Entry[]; the runtime's dependent cell is ordinary collectible memory whose only
# edge is that array, so the Entry[] must come from the scanned allocator even
# though the handle is one pointer-sized word in IL. The generated allocation is
# asserted directly, and the section looks its keys up again after a collection.
# The Array.Resize run is a stress signal, not a deterministic proof of the GC's
# held window. The exact generated store-before-barrier check below and the
# deterministic runtime helper probe in build-and-run-gc-write-barrier.sh provide
# the two deterministic layers.
# Former gates: none (new area).
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

resize_shape=$(awk '
    FNR == 1 { in_method = 0 }
    $0 == "// IncrementalWriteBarrierSubset.Program::Resize" {
        in_method = 1
        methods++
        saw_store = 0
        next
    }
    in_method && /^\/\/ / { in_method = 0 }
    in_method && /^[[:space:]]*\(\*\(Dn2CppArrayRef\*\*\)\([^)]*\)\) =/ {
        stores++
        saw_store = 1
        next
    }
    in_method && /dn2cpp_gc_write_barrier_if_heap/ {
        barriers++
        if (saw_store)
            ordered++
    }
    END { print methods + 0 ":" stores + 0 ":" barriers + 0 ":" ordered + 0 }
' "$out/generated.h" "$out"/generated*.cpp)
if [ "$resize_shape" != "1:1:1:1" ]; then
    echo "FAIL: Array.Resize method/store/barrier/ordered counts were $resize_shape, expected 1:1:1:1" >&2
    exit 1
fi

pending_barriers=$(awk '
    FNR == 1 { in_method = 0 }
    $0 == "// PendingCallArgumentSubset.Program::__GateEntry" { in_method = 1; next }
    in_method && /^\/\/ / { in_method = 0 }
    in_method && /DN2CPP_WEB_GC_LIVENESS\(/ { count++ }
    END { print count + 0 }
' "$out/generated.h" "$out"/generated*.cpp)
if [ "$pending_barriers" -ne 1 ]; then
    echo "FAIL: pending nested-call regression emitted $pending_barriers Web liveness barriers, expected 1" >&2
    exit 1
fi

# Both the initial Entry[] (the Container constructor) and the resized one.
cwt_scanned=$(grep -hF 'dn2cpp_newarr_n_t(' "$out"/generated*.cpp \
    | grep -cF 'sizeof(t_ConditionalWeakTable_Entry' || true)
cwt_atomic=$(grep -hF 'dn2cpp_newarr_n_atomic_t(' "$out"/generated*.cpp \
    | grep -cF 'sizeof(t_ConditionalWeakTable_Entry' || true)
if [ "$cwt_scanned" -lt 2 ] || [ "$cwt_atomic" -ne 0 ]; then
    echo "FAIL: ConditionalWeakTable Entry[] allocations were $cwt_scanned scanned / $cwt_atomic unscanned, expected >=2 / 0" >&2
    exit 1
fi

ctx="weak_references|$_CG_CORELIB|runs:DN2CPP_GC_INCREMENTAL=0+1|DN2CPP_GC_STATS=1|assert:mode+diff+exit+generated-barriers+resize-ref+memmove-refs+pending-web-liveness+cwt-entry-scanned+cwt-section"
ctx="$ctx$(_gate_ctx_extras)"
if _corelib_gate_check "$out" "$ctx"; then
    gate_cache_hit_msg
    exit 0
fi

echo "== 4/4 Compiling C++ and running (STW + incremental; exact diff vs real .NET) =="
compile_console "$out" "$project"

gate_run_logs_init weakrefs WeakReferences
log_dir="$_GATE_RUN_LOG_DIR"
stw=
stw_code=
incremental=
incremental_code=

gate_run_diagnostics() {
    gate_run_diag "WeakReferences STW" "$stw_code" "$stw" "$log_dir/stw.log"
    gate_run_diag "WeakReferences incremental" "$incremental_code" "$incremental" \
        "$log_dir/incremental.log"
}

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

assertions_failed=0
assert_output "$stw" "$expected" || assertions_failed=1
assert_exit_code "$stw_code" "$expected_code" || assertions_failed=1
assert_output "$incremental" "$expected" || assertions_failed=1
assert_exit_code "$incremental_code" "$expected_code" || assertions_failed=1
if [ "$assertions_failed" -ne 0 ]; then
    gate_run_diag_once
    exit 1
fi

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
if [ "$(grep -cF 'Array.Resize field stress:' <<<"$incremental")" -ne 1 ]; then
    echo "FAIL: Array.Resize field-stress section did not run exactly once" >&2
    exit 1
fi
for run in "$stw" "$incremental"; do
    if [ "$(grep -cF 'conditional weak table after collection:' <<<"$run")" -ne 1 ]; then
        echo "FAIL: ConditionalWeakTable section did not run exactly once" >&2
        exit 1
    fi
done

gate_cache_commit
