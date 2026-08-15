#!/usr/bin/env bash
# The upstream bdwgc 8.2.8 backend (DN2CPP_GC_BACKEND=upstream,
# third_party/bdwgc-upstream/) must run the WeakReferences corpus identically
# to real .NET in both stop-the-world and incremental modes, with dn2cpp's own
# write barriers as the dirty-bit source either way — collection itself is
# manual-VDB on both backends, this gate only swaps which GC source tree is
# linked. The printed GC version line is the witness that the upstream tree
# was actually linked and not the default Unity fork; the emitted barrier
# calls are backend-independent and already asserted by
# gates/build-and-run-weak-references.sh, so this gate does not repeat them.
source "$(dirname "$0")/_common.sh"

export DN2CPP_GC_BACKEND=upstream

project=WeakReferences
out="$(_corelib_gate_out "$project")-gcupstream"

_corelib_gate_core "$project" "$out"

ctx="gc_upstream|$_CG_CORELIB|backend:$DN2CPP_GC_BACKEND"
ctx="$ctx|runs:DN2CPP_GC_INCREMENTAL=0+1|DN2CPP_GC_STATS=1"
ctx="$ctx|assert:gc-version-8.2.8+mode+diff+exit+bounce-off-stw+bounce-on-incremental"
ctx="$ctx$(_gate_ctx_extras)"
if _corelib_gate_check "$out" "$ctx"; then
    gate_cache_hit_msg
    exit 0
fi

echo "== 4/4 Compiling C++ (upstream GC backend) and running (STW + incremental; exact diff vs real .NET) =="
compile_console "$out" "$project"

log_dir=$(mktemp -d "${TMPDIR:-/tmp}/dn2cpp_gcupstream.XXXXXX")
stw=
stw_code=
incremental=
incremental_code=
diagnostics_printed=0

print_arm_diagnostics() {
    local label="$1" code="$2" stdout="$3" stderr_file="$4"
    local signal_no signal_name

    echo "---- GC-upstream $label diagnostics ----" >&2
    echo "exit code: ${code:-<not run>}" >&2
    if [ -z "$code" ]; then
        echo "termination: not run" >&2
    elif [ -f "$stderr_file" ] && \
            grep -qF 'WATCHDOG: no exit after ' "$stderr_file"; then
        echo "termination: watchdog" >&2
    elif [ "$code" -ge 128 ] && [ "$code" -le 255 ]; then
        signal_no=$((code - 128))
        signal_name=$(kill -l "$signal_no" 2>/dev/null || true)
        echo "termination: signal $signal_no${signal_name:+ ($signal_name)}" >&2
    elif [ "$code" -eq 0 ]; then
        echo "termination: normal exit" >&2
    else
        echo "termination: nonzero exit" >&2
    fi
    if [ -n "$stdout" ]; then
        echo "stdout:" >&2
        printf '%s\n' "$stdout" >&2
    else
        echo "stdout: <empty>" >&2
    fi
    if [ -s "$stderr_file" ]; then
        echo "stderr:" >&2
        cat "$stderr_file" >&2
    else
        echo "stderr: <empty>" >&2
    fi
}

print_run_diagnostics() {
    print_arm_diagnostics "STW" "$stw_code" "$stw" "$log_dir/stw.log"
    print_arm_diagnostics "incremental" "$incremental_code" "$incremental" \
        "$log_dir/incremental.log"
    diagnostics_printed=1
}

cleanup_run_logs() {
    local code=$?
    if [ "$code" -eq 0 ]; then
        rm -rf "$log_dir"
        return
    fi
    if [ "$diagnostics_printed" -eq 0 ]; then
        print_run_diagnostics
    fi
    echo "GC-upstream stderr logs preserved in $log_dir" >&2
}
trap cleanup_run_logs EXIT

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
    print_run_diagnostics
    exit 1
fi

# The version line is the ONLY direct evidence the upstream tree, not the
# default Unity fork (which prints 7.7.0), was actually linked into this
# binary — everything above would pass identically either way.
if [ "$(grep -cF '[dn2cpp] GC version: 8.2.8' "$log_dir/stw.log")" -ne 1 ]; then
    echo "FAIL: GC-upstream STW arm did not report GC version 8.2.8 (upstream tree not linked?)" >&2
    sed -n '1,40p' "$log_dir/stw.log" >&2
    exit 1
fi
if [ "$(grep -cF '[dn2cpp] GC version: 8.2.8' "$log_dir/incremental.log")" -ne 1 ]; then
    echo "FAIL: GC-upstream incremental arm did not report GC version 8.2.8 (upstream tree not linked?)" >&2
    sed -n '1,40p' "$log_dir/incremental.log" >&2
    exit 1
fi
if ! grep -qF '[dn2cpp] GC mode: stop-the-world' "$log_dir/stw.log"; then
    echo "FAIL: GC-upstream STW arm did not report stop-the-world mode" >&2
    sed -n '1,40p' "$log_dir/stw.log" >&2
    exit 1
fi
if ! grep -qF '[dn2cpp] GC mode: incremental' "$log_dir/incremental.log"; then
    echo "FAIL: GC-upstream incremental arm did not report incremental mode" >&2
    sed -n '1,40p' "$log_dir/incremental.log" >&2
    exit 1
fi

# Upstream is built with -DMANUAL_VDB but keeps MPROTECT_VDB/GWW_VDB compiled
# in (only the fork's gcconfig.h strips them unconditionally), so
# GC_incremental_protection_needs() answers the compiled-in CAPABILITY, not
# the manual VDB actually selected at run time — nonzero even though nothing
# is ever mprotect'd. Collection is manual-VDB on both backends; only this
# stats line differs, and only because upstream's query is conservative
# rather than wrong. STW: bounce is unconditionally "off" regardless of that
# capability, since the conjunction with incremental mode is false.
if ! grep -qF 'kernel-write bounce: off' "$log_dir/stw.log"; then
    echo "FAIL: GC-upstream STW arm did not report kernel-write bounce: off" >&2
    sed -n '1,40p' "$log_dir/stw.log" >&2
    exit 1
fi
# Incremental: upstream's capability answer is nonzero, so the reported
# bounce is "on" here — the fork answers 0 (GC_PROTECTS_NONE) and would print
# "off" in this same arm; this is a per-backend fact, not a bug in either.
if ! grep -qF 'kernel-write bounce: on' "$log_dir/incremental.log"; then
    echo "FAIL: GC-upstream incremental arm did not report kernel-write bounce: on" >&2
    sed -n '1,40p' "$log_dir/incremental.log" >&2
    exit 1
fi

gate_cache_commit
