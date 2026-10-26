#!/usr/bin/env bash
# The suppressed-finalizer set is searched through an index whose hit paths
# read one slot, so a program that suppresses a batch post-enqueue and then
# drains it must NOT pay the set's size once per dequeue. DN2CPP_GC_SUPPRESS_STATS
# counts the slot reads directly — a wall clock cannot separate them from
# collection time.
#
# This is a measurement aid, NOT a regression gate: the walk cost is not
# asserted against a bound, only printed alongside the linear-scan target for
# comparison. The `measure-*` name keeps it out of run-all-gates.sh, which
# discovers gates by the `build-and-run-*` glob.
source "$(dirname "$0")/_common.sh"

project=FinalizerSuppressCost
out="$(_corelib_gate_out "$project")"

_corelib_gate_core "$project" "$out"

echo "== 2/2 Compiling C++ and running (exact diff vs real .NET + suppress-set walk cost) =="
compile_console "$out" "$project"

gate_run_logs_init suppresscost suppress-scan
log_dir="$_GATE_RUN_LOG_DIR"
native="$log_dir/native.stdout"
native_code=
expected="$log_dir/oracle.stdout"
expected_code=

gate_run_diagnostics() {
    gate_run_diag_files "suppress-scan native" "$native_code" "$native" \
        "$log_dir/native.stderr"
    gate_run_diag_files "suppress-scan oracle" "$expected_code" "$expected" \
        "$log_dir/oracle.stderr"
}

_gate_run_argv
DN2CPP_GC_SUPPRESS_STATS=1 gate_capture "$log_dir/native" run_bounded "./$out/$project$EXE_EXT" \
    ${_GATE_RUN_ARGV[@]+"${_GATE_RUN_ARGV[@]}"}
native_code=$(cat "$log_dir/native.status")
_gate_run_argv
gate_capture "$log_dir/oracle" run_bounded dotnet "$_CG_APP" \
    ${_GATE_RUN_ARGV[@]+"${_GATE_RUN_ARGV[@]}"}
expected_code=$(cat "$log_dir/oracle.status")
_gate_scratch_cleanup

assertions_failed=0
gate_output compare "$native" "$expected" || assertions_failed=1
gate_output status "$native_code" 0 || assertions_failed=1
gate_output status "$expected_code" 0 || assertions_failed=1
if [ "$assertions_failed" -ne 0 ]; then
    gate_run_diag_once
    exit 1
fi

# Numeric diagnostics use a separate pair-only CRLF view, never raw parity.
stats="$log_dir/native.records.stderr"
gate_output view "$log_dir/native.stderr" "$stats" --policy crlf
gate_output view "$log_dir/oracle.stderr" "$log_dir/oracle.records.stderr" --policy crlf
cat "$stats"

# The knob is opt-in and reported from the exit funnel, so its absence is
# indistinguishable from a cost of zero: pin the frame to exactly one summary.
if [ "$(grep -cF '=== dn2cpp GC suppress stats ===' "$stats")" -ne 1 ]; then
    echo "FAIL: expected exactly one suppress-stats summary on stderr (knob not honoured?)" >&2
    exit 1
fi

# Both operands are the constants that bound the walk, read where they are
# declared rather than restated here: the sample suppresses (and then dequeues)
# `Count` victims, and the set grows a chunk at a time. A chunk's worth of reads
# per dequeue is the widest a healthy search may be, so their product is the
# linear target; walking the whole set per dequeue costs the square of the set's
# size. Freezing the measured number instead would only track this machine's
# collector.
suppresses=$(sed -n 's/^ *private const int Count = \([0-9_]*\);.*/\1/p' \
    "samples/dotnet/$project/Program.cs" | LC_ALL=C tr -d '_')
chunk_entries=$(sed -n 's/^constexpr uint32_t kSuppressChunkEntries = \([0-9]*\);.*/\1/p' \
    runtime/core/dn2cpp_gc.cpp)
if [ -z "$suppresses" ] || [ -z "$chunk_entries" ]; then
    echo "FAIL: could not read the suppress count / chunk size the target is derived from" >&2
    exit 1
fi
limit=$((suppresses * chunk_entries))

suppress_stat_number() {
    local python
    python=$(resolve_python) || return 1
    # Input is already normalized once. Full byte matches must retain payload CR.
    $python - "$1" "$2" <<'PYTHON'
from pathlib import Path
import re
import sys
patterns = dict(
    walked=rb'slots walked: *([0-9]+) *',
    added=rb' *set entries added: *([0-9]+) *',
)
data = Path(sys.argv[1]).read_bytes()
if b'\0' in data:
    sys.exit('FAIL: NUL in suppress-stats records')
values = [match.group(1) for row in data.split(b'\n')
          if (match := re.fullmatch(patterns[sys.argv[2]], row))]
if len(values) != 1:
    sys.exit(f'FAIL: expected one {sys.argv[2]} numeric record, found {len(values)}')
sys.stdout.buffer.write(values[0] + b'\n')
PYTHON
}
walked=$(suppress_stat_number "$stats" walked) || exit 1
if [ -z "$walked" ]; then
    echo "FAIL: no 'slots walked' line in the suppress-stats summary" >&2
    exit 1
fi

ratio_x10=$(( (walked * 10) / limit ))
ratio="$((ratio_x10 / 10)).$((ratio_x10 % 10))"

echo "slots walked: $walked  (linear-scan target: $suppresses x $chunk_entries = $limit, ratio: ${ratio}x)"

# The contended arm's timing is only about the suppress serialization if its
# lanes stayed on the common path: a leak into the set would add entries far
# past the drain phase's own, so the recorded count is the guard.
added=$(suppress_stat_number "$stats" added) || exit 1
if [ -z "$added" ] || [ "$added" -gt "$suppresses" ]; then
    echo "FAIL: contended lanes reached the suppress set (entries added: ${added:-none}, drain phase adds at most $suppresses)" >&2
    exit 1
fi

echo
echo "== contended SuppressFinalize (Dispose shape, best-of-3 ns/op; suppress_marginal = suppress lane - alloc control) =="
grep '^contended-suppress ' "$stats" | sed 's/^/native /'
grep '^contended-suppress ' "$log_dir/oracle.records.stderr" | sed 's/^/dotnet /'

echo
echo "Reading: 'slots walked' near or below the linear-scan target means the scan"
echo "cost tracks the work actually done (one chunk per dequeue). A ratio many"
echo "multiples above 1x means the scan is paying closer to the full set's size"
echo "per dequeue — i.e. cost grows with the square of the suppress set, not"
echo "linearly with it. The contended table compares the native suppress (a mutex"
echo "plus two atomic RMWs, serialized across threads) against real .NET's"
echo "lock-free header-bit write. The serialization is read off the suppress"
echo "lane's alloc_suppress_ns_per_op as the thread count rises, against dotnet's"
echo "same column; the marginal column prices the suppress against the"
echo "enqueue-and-drain it cancels, so a negative there means the suppress is"
echo "the cheaper side of the Dispose shape."
