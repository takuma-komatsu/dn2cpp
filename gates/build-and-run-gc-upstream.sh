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
