#!/usr/bin/env bash
# The suppressed-finalizer set is searched by walking its slots, so a program
# that suppresses a batch post-enqueue and then drains it may or may not pay
# the set's size once per dequeue. DN2CPP_GC_SUPPRESS_STATS counts that walk
# directly — a wall clock cannot separate it from collection time.
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

log_dir=$(mktemp -d "${TMPDIR:-/tmp}/dn2cpp_suppresscost.XXXXXX")
trap 'rm -rf "$log_dir"' EXIT

set +e
_gate_run_argv
native=$(DN2CPP_GC_SUPPRESS_STATS=1 run_bounded "./$out/$project" \
    ${_GATE_RUN_ARGV[@]+"${_GATE_RUN_ARGV[@]}"} 2>"$log_dir/native.log"); native_code=$?
_gate_run_argv
expected=$(run_bounded dotnet "$_CG_APP" \
    ${_GATE_RUN_ARGV[@]+"${_GATE_RUN_ARGV[@]}"}); expected_code=$?
set -e
_gate_scratch_cleanup

assert_output "$native" "$expected"
assert_exit_code "$native_code" "$expected_code"

# A native Windows binary writes CRLF, which every number below would carry.
stats=$(LC_ALL=C tr -d '\r' <"$log_dir/native.log")

echo "$stats"

# The knob is opt-in and reported from the exit funnel, so its absence is
# indistinguishable from a cost of zero: pin the frame to exactly one summary.
if [ "$(grep -cF '=== dn2cpp GC suppress stats ===' <<<"$stats")" -ne 1 ]; then
    echo "FAIL: expected exactly one suppress-stats summary on stderr (knob not honoured?)" >&2
    exit 1
fi

# Both operands are the constants that produce the walk, read where they are
# declared rather than restated here: the sample suppresses (and then dequeues)
# `Count` victims, and the set is searched a chunk at a time. A search that stops
# at what it found costs a chunk per dequeue, so their product is the linear
# target; walking the whole set per dequeue costs the square of the set's size.
# Freezing the measured number instead would only track this machine's collector.
suppresses=$(sed -n 's/^ *private const int Count = \([0-9_]*\);.*/\1/p' \
    "samples/dotnet/$project/Program.cs" | LC_ALL=C tr -d '_')
chunk_entries=$(sed -n 's/^constexpr uint32_t kSuppressChunkEntries = \([0-9]*\);.*/\1/p' \
    runtime/core/dn2cpp_gc.cpp)
if [ -z "$suppresses" ] || [ -z "$chunk_entries" ]; then
    echo "FAIL: could not read the suppress count / chunk size the target is derived from" >&2
    exit 1
fi
limit=$((suppresses * chunk_entries))

walked=$(LC_ALL=C sed -n 's/^slots walked: *\([0-9][0-9]*\) *$/\1/p' <<<"$stats")
if [ -z "$walked" ]; then
    echo "FAIL: no 'slots walked' line in the suppress-stats summary" >&2
    exit 1
fi

ratio_x10=$(( (walked * 10) / limit ))
ratio="$((ratio_x10 / 10)).$((ratio_x10 % 10))"

echo "slots walked: $walked  (linear-scan target: $suppresses x $chunk_entries = $limit, ratio: ${ratio}x)"

echo
echo "Reading: 'slots walked' near or below the linear-scan target means the scan"
echo "cost tracks the work actually done (one chunk per dequeue). A ratio many"
echo "multiples above 1x means the scan is paying closer to the full set's size"
echo "per dequeue — i.e. cost grows with the square of the suppress set, not"
echo "linearly with it."
