#!/usr/bin/env bash
# A worker remains active at module shutdown and completes before handles are released.
source "$(dirname "$0")/_common.sh"

[ "${UNREALSHARP_BACKEND:-}" = Dn2Cpp ] || gate_skip "Pending worker shutdown requires the native UnrealSharp package"
work="${UNREALSHARP_RESULT_DIR:-$PWD/artifacts/unrealsharp-package-smoke/pending-shutdown}"
mkdir -p "$work"
work=$(cd "$work" && pwd -P)
export UNREALSHARP_RESULT_DIR="$work"
export DN2CPP_SMOKE_PENDING_FILE="$work/pending.txt"
export DN2CPP_SMOKE_LATE_CALLBACK_FILE="$work/late-callback.txt"
rm -f "$DN2CPP_SMOKE_PENDING_FILE" "$DN2CPP_SMOKE_LATE_CALLBACK_FILE"
bash "$(dirname "$0")/run-unrealsharp-package-smoke.sh"
assert_output "$(cat "$DN2CPP_SMOKE_PENDING_FILE")" $'worker-started\nshutdown-worker-pending\nworker-completed-handle-alive'
assert_output "$(cat "$DN2CPP_SMOKE_LATE_CALLBACK_FILE")" $'abi-shapes=7\nmanaged-control-entry\nlate-callback-returned'
if grep -E 'Exception during InvokeDelegate|native callback failed|shutdown timed out|Native shutdown failed|UnrealSharp callback failure' "$work/stdout.log" "$work/run.log" 2>/dev/null; then
    echo "error: callback or shutdown boundary failure" >&2
    exit 1
fi
echo "unrealsharp pending worker and late callback shutdown OK"
