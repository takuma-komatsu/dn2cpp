#!/usr/bin/env bash
# The incremental collector's write barrier, asserted deterministically rather
# than by racing a managed program against a cycle: samples/native/gcbarrier
# links the real runtime and drives the cycle itself, so each store lands at a
# known point of a known cycle. Five subjects — plain store, barriered store,
# plain memmove, dn2cpp_gc_memmove_refs, and a payload published nowhere — of
# which the three unbarriered ones are negative controls: their loss under
# DN2CPP_GC_INCREMENTAL=1 is what proves the barriered ones assert anything, and
# their survival under =0 is what pins that loss on incrementality rather than on
# the probe. Each store runs on a thread joined before the cycle resumes, because
# a conservative scan of any live stack holding the target dirties it and saves an
# unbarriered store. A partial cycle is only as long as its dirty-block count, so
# the probe dirties a ballast heap to hold the window open independently of this
# host's root set, and proves per trial that it did (`window held`). Barrier calls
# in EMITTED code are a separate question, asserted by
# build-and-run-weak-references.sh and build-and-run-external-ref-barrier.sh.
# Former gates: none (new area).
source "$(dirname "$0")/_common.sh"

out=artifacts/gcwritebarrier
mkdir -p "$out"

ctx="gc_write_barrier|runs:DN2CPP_GC_INCREMENTAL=0+1|DN2CPP_GC_STATS=1"
ctx="$ctx|assert:mode+stw-all-survive+incremental-unbarriered-lost+window-held"
ctx="$ctx|assert:unpublished-control"
ctx="$ctx$(_gate_ctx_extras)"
if gate_cache_check "$out" "$ctx" \
    samples/native/gcbarrier/gcbarrier.cpp samples/native/gcbarrier/CMakeLists.txt; then
    gate_cache_hit_msg
    exit 0
fi

echo "== 1/3 Building the runtime =="
# `|| exit 1` explicitly: a bare `x="$(f)"` does not abort under `set -e`, so a
# failed runtime build would link the stale archive.
export_file="$(ensure_cmake_runtime)" || exit 1

echo "== 2/3 Building the write-barrier probe =="
# The probe links the runtime's own objects, so it must be compiled by the same
# compiler; unpinned, cmake takes whatever heads PATH.
probe_args=()
[ -n "${CMAKE_CXX_COMPILER:-}" ] && probe_args+=(-DCMAKE_CXX_COMPILER="${CMAKE_CXX_COMPILER}")
_cmake_configure always "" "$out" "$out/dn2cpp-configure.log" \
    "configuring the write-barrier probe" \
    "$CMAKE" -S samples/native/gcbarrier -B "$out" -G Ninja \
    -DDN2CPP_RUNTIME_EXPORT="$export_file" ${probe_args[@]+"${probe_args[@]}"}
_cmake_step "$out/dn2cpp-build.log" "building the write-barrier probe" \
    "$CMAKE" --build "$out"

echo "== 3/3 Running the probe (STW + incremental) =="
gate_run_logs_init gcbarrier "write-barrier probe"
log_dir="$_GATE_RUN_LOG_DIR"
stw=
stw_code=
incremental=
incremental_code=

gate_run_diagnostics() {
    gate_run_diag "write-barrier probe STW" "$stw_code" "$stw" "$log_dir/stw.log"
    gate_run_diag "write-barrier probe incremental" "$incremental_code" \
        "$incremental" "$log_dir/incremental.log"
}

set +e
stw=$(DN2CPP_GC_INCREMENTAL=0 DN2CPP_GC_STATS=1 \
    run_bounded "./$out/gcbarrier" 2>"$log_dir/stw.log"); stw_code=$?
incremental=$(DN2CPP_GC_INCREMENTAL=1 DN2CPP_GC_STATS=1 \
    run_bounded "./$out/gcbarrier" 2>"$log_dir/incremental.log"); incremental_code=$?
set -e

# Ahead of the diffs, not after them: an arm that came up in the wrong mode
# otherwise reports as four wrong counts with its cause buried.
if ! grep -qF '[dn2cpp] GC mode: stop-the-world' "$log_dir/stw.log"; then
    echo "FAIL: write-barrier probe STW arm did not report stop-the-world mode" >&2
    sed -n '1,40p' "$log_dir/stw.log" >&2
    exit 1
fi
if ! grep -qF '[dn2cpp] GC mode: incremental' "$log_dir/incremental.log"; then
    echo "FAIL: write-barrier probe incremental arm did not report incremental mode" >&2
    sed -n '1,40p' "$log_dir/incremental.log" >&2
    exit 1
fi

# Stop-the-world has no window at all, so every subject survives — including the
# unbarriered ones, which is the control on the incremental arm below. No window
# to report either, which is why the arms differ by that line.
assertions_failed=0
assert_output "$(strip_cr_win "$stw")" "incremental mode: off
plain store: lost 0 of 64
barriered store: lost 0 of 64
plain memmove: lost 0 of 64
dn2cpp_gc_memmove_refs: lost 0 of 64
unpublished control: lost 0 of 64" || assertions_failed=1
assert_exit_code "$stw_code" 0 || assertions_failed=1

# Incremental: an unbarriered store is lost at every increment count (the payload
# is allocated white inside the cycle and the block stored into is clean-marked,
# so the partial cycle never looks at it again), a barriered one at none. The
# unpublished control is the same trial with no store at all: it is what separates
# "the barrier saved it" from "this host was never going to reclaim it". The probe
# exits non-zero naming the cause if the window ever shut early, so the `64 of 64`
# below is a claim about the barrier and not about luck.
assert_output "$(strip_cr_win "$incremental")" "incremental mode: on
plain store: lost 64 of 64
barriered store: lost 0 of 64
plain memmove: lost 64 of 64
dn2cpp_gc_memmove_refs: lost 0 of 64
unpublished control: lost 64 of 64
window held: 64 of 64" || assertions_failed=1
assert_exit_code "$incremental_code" 0 || assertions_failed=1
if [ "$assertions_failed" -ne 0 ]; then
    gate_run_diag_once
    exit 1
fi

gate_cache_commit
