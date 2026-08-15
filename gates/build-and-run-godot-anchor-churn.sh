#!/usr/bin/env bash
# The RefCounted anchor table's compaction barrier, asserted by running the real
# sweep inside a paused mark cycle rather than by reading it:
# samples/native/godotanchorchurn links the runtime's real Godot-lane objects
# (engine surface stubbed — the sweep's one engine call is get_reference_count)
# and churns full
# anchor/keep/drop rounds through dn2cpp_godot_sweep_anchored_refcounted. Each
# trial steps an incremental cycle one mark increment at a time until it
# observes, via sentinel mark bits, the table's own scan sitting split — head
# scanned, kept entries not — and only then compacts, so the backwards move
# lands behind the marker by construction; a trial that cannot reach that state
# fails by name. The teardown of every trial re-drops the same shims through the
# same sweep and requires them reclaimed, separating "the barrier saved it" from
# "this host never reclaims it". Needs no engine and takes no machine lock.
# The barrier PRIMITIVE (clean blocks are never rescanned; only
# GC_end_stubborn_change says otherwise) is settled by
# build-and-run-gc-write-barrier.sh; this gate asserts the sweep site uses it.
# Former gates: none (new area).
source "$(dirname "$0")/_common.sh"

out=artifacts/godotanchorchurn
mkdir -p "$out"

ctx="godot_anchor_churn|runs:DN2CPP_GC_INCREMENTAL=0+1|DN2CPP_GC_STATS=1"
ctx="$ctx|assert:mode+stw-all-survive+incremental-none-lost+window-held+teardown-reclaims"
ctx="$ctx$(_gate_ctx_extras)"
if gate_cache_check "$out" "$ctx" \
    samples/native/godotanchorchurn/churn.cpp samples/native/godotanchorchurn/CMakeLists.txt; then
    gate_cache_hit_msg
    exit 0
fi

echo "== 1/3 Building the runtime =="
# `|| exit 1` explicitly: a bare `x="$(f)"` does not abort under `set -e`, so a
# failed runtime build would link the stale archive.
export_file="$(ensure_cmake_runtime)" || exit 1

echo "== 2/3 Building the anchor-churn probe =="
# The probe links the runtime's own objects, so it must be compiled by the same
# compiler; unpinned, cmake takes whatever heads PATH.
probe_args=()
[ -n "${CMAKE_CXX_COMPILER:-}" ] && probe_args+=(-DCMAKE_CXX_COMPILER="${CMAKE_CXX_COMPILER}")
_cmake_configure always "" "$out" "$out/dn2cpp-configure.log" \
    "configuring the anchor-churn probe" \
    "$CMAKE" -S samples/native/godotanchorchurn -B "$out" -G Ninja \
    -DDN2CPP_RUNTIME_EXPORT="$export_file" ${probe_args[@]+"${probe_args[@]}"}
_cmake_step "$out/dn2cpp-build.log" "building the anchor-churn probe" \
    "$CMAKE" --build "$out"

echo "== 3/3 Running the probe (STW + incremental) =="
gate_run_logs_init anchorchurn "anchor-churn probe"
log_dir="$_GATE_RUN_LOG_DIR"
stw=
stw_code=
incremental=
incremental_code=

gate_run_diagnostics() {
    gate_run_diag "anchor-churn probe STW" "$stw_code" "$stw" "$log_dir/stw.log"
    gate_run_diag "anchor-churn probe incremental" "$incremental_code" \
        "$incremental" "$log_dir/incremental.log"
}

set +e
stw=$(DN2CPP_GC_INCREMENTAL=0 DN2CPP_GC_STATS=1 \
    run_bounded "./$out/churn" 2>"$log_dir/stw.log"); stw_code=$?
incremental=$(DN2CPP_GC_INCREMENTAL=1 DN2CPP_GC_STATS=1 \
    run_bounded "./$out/churn" 2>"$log_dir/incremental.log"); incremental_code=$?
set -e

# Ahead of the diffs, not after them: an arm that came up in the wrong mode
# otherwise reports as wrong counts with its cause buried.
if ! grep -qF '[dn2cpp] GC mode: stop-the-world' "$log_dir/stw.log"; then
    echo "FAIL: anchor-churn probe STW arm did not report stop-the-world mode" >&2
    sed -n '1,40p' "$log_dir/stw.log" >&2
    exit 1
fi
if ! grep -qF '[dn2cpp] GC mode: incremental' "$log_dir/incremental.log"; then
    echo "FAIL: anchor-churn probe incremental arm did not report incremental mode" >&2
    sed -n '1,40p' "$log_dir/incremental.log" >&2
    exit 1
fi

# Stop-the-world has no window at all, so this arm is the control on the probe
# itself: full churn, every shim survives, every teardown reclaims.
assertions_failed=0
assert_output "$(strip_cr_win "$stw")" "incremental mode: off
kept shims lost: 0 of 6144
reclaimed at teardown: 6144 of 6144" || assertions_failed=1
assert_exit_code "$stw_code" 0 || assertions_failed=1

# Incremental: every trial's compaction ran inside an observed split of the
# table's scan (the probe exits non-zero naming the trial otherwise), so the
# `0 of 6144` is a claim about the barrier at the sweep site, not about luck.
assert_output "$(strip_cr_win "$incremental")" "incremental mode: on
kept shims lost: 0 of 6144
reclaimed at teardown: 6144 of 6144
window held: 16 of 16" || assertions_failed=1
assert_exit_code "$incremental_code" 0 || assertions_failed=1
if [ "$assertions_failed" -ne 0 ]; then
    gate_run_diag_once
    exit 1
fi

gate_cache_commit
