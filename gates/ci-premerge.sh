#!/usr/bin/env bash
# Preserve the strict runner's result and bind its receipt to the dispatched tree.
set -euo pipefail
cd "$(dirname "$0")/.."

: "${DN2CPP_PREMERGE_LOGROOT:?pre-merge evidence directory required}"
disk_pid=
stage=

finish() {
    rc=$?
    trap - EXIT
    if [ -n "$disk_pid" ]; then
        kill "$disk_pid" 2>/dev/null || true
        wait "$disk_pid" 2>/dev/null || true
    fi
    printf '%s\n' "$rc" > "$DN2CPP_PREMERGE_LOGROOT/$stage.exit"
    if [ "$stage" = premerge ]; then
        date -u +%FT%TZ > "$DN2CPP_PREMERGE_LOGROOT/premerge.finished"
    fi
    exec 1>&3 2>&4
    cat "$DN2CPP_PREMERGE_LOGROOT/$stage.stdout"
    cat "$DN2CPP_PREMERGE_LOGROOT/$stage.stderr" >&2
    exit "$rc"
}

capture() {
    stage=$1
    case "$stage" in identity|sdk|setup|premerge) ;; *) exit 2 ;; esac
    exec 3>&1 4>&2
    trap finish EXIT
    trap 'exit 130' INT
    trap 'exit 143' TERM
    exec > "$DN2CPP_PREMERGE_LOGROOT/$stage.stdout" 2> "$DN2CPP_PREMERGE_LOGROOT/$stage.stderr"
}

verify_identity() {
    local head
    head=$(git rev-parse HEAD)
    [[ "$EXPECTED_HEAD" =~ ^[0-9a-f]{40}$ && "$head" == "$EXPECTED_HEAD" && "$head" == "$GITHUB_SHA" ]] || {
        echo "checkout, expected_head and dispatched workflow commit differ" >&2
        return 1
    }
    test -z "$(git status --porcelain=v1)"
}

case "${1:-}" in
    identity)
        capture identity
        git rev-parse HEAD HEAD^{tree} > "$DN2CPP_PREMERGE_LOGROOT/source-identity.txt"
        verify_identity
        ;;
    capture)
        capture "$2"
        shift 2
        "$@" 3>&- 4>&-
        ;;
    run)
        capture premerge
        verify_identity
        [[ "$CONFIG" = Debug && "$JOBS" = 2 && "$CMAKE_BUILD_PARALLEL_LEVEL" = 2 ]] || {
            echo 'error: hosted pre-merge requires Debug with parallelism 2' >&2
            exit 1
        }
        source gates/_python.sh
        _gate_python_resolve
        # A single process can stop immediately, without Bash deferring a trap
        # until df/tail finishes or leaving a sleep child holding evidence open.
        "${_GATE_PYTHON_ARGV[@]}" - "$DN2CPP_PREMERGE_LOGROOT" "$GITHUB_WORKSPACE" "$RUNNER_TEMP" \
            >&3 2>&4 3>&- 4>&- <<'PY' &
import datetime
import os
from pathlib import Path
import shutil
import sys
import time

root = Path(sys.argv[1])
with (root / 'disk-periodic.log').open('a', encoding='utf-8') as disk:
    while True:
        disk.write(datetime.datetime.now(datetime.timezone.utc).isoformat() + '\n')
        for path in sys.argv[2:]:
            usage = shutil.disk_usage(path)
            disk.write(f'disk {path}: total={usage.total} used={usage.used} free={usage.free}\n')
        disk.flush()
        with (root / 'premerge.stdout').open('rb') as stream:
            stream.seek(0, os.SEEK_END)
            stream.seek(max(0, stream.tell() - 65536))
            preview = b''.join(stream.read(65536).splitlines(keepends=True)[-8:])
        sys.stdout.buffer.write(preview)
        sys.stdout.buffer.flush()
        time.sleep(60)
PY
        disk_pid=$!
        date -u +%FT%TZ > "$DN2CPP_PREMERGE_LOGROOT/premerge.started"
        printf '%s\n' 'CONFIG=Debug JOBS=2 CMAKE_BUILD_PARALLEL_LEVEL=2 bash gates/pre-merge.sh --skip-godot' \
            > "$DN2CPP_PREMERGE_LOGROOT/premerge.command"
        bash gates/pre-merge.sh --skip-godot 3>&- 4>&-
        ;;
    outcome)
        valid_receipt=0
        receipt="$DN2CPP_PREMERGE_LOGROOT/_receipt.txt"
        if [ "$PREMERGE_OUTCOME" = success ]; then
            short_head=$(git rev-parse --short HEAD)
            if [ "$(git rev-parse HEAD)" = "$EXPECTED_HEAD" ] \
                && [ "$EXPECTED_HEAD" = "$GITHUB_SHA" ] \
                && [ "$(cat "$DN2CPP_PREMERGE_LOGROOT/premerge.exit" 2>/dev/null)" = 0 ] \
                && [ -s "$receipt" ] \
                && grep -Fxq 'dn2cpp NON-GODOT PRE-MERGE PASSED' "$receipt" \
                && grep -Fxq 'configs: Debug' "$receipt" \
                && awk -v sha="$short_head" '$1 == "commit:" && $NF == sha { found=1 } END { exit !found }' "$receipt"; then
                valid_receipt=1
            fi
        fi
        if [ "$valid_receipt" = 1 ]; then
            cat "$receipt" >> "$GITHUB_STEP_SUMMARY"
        else
            echo 'No completed, valid pre-merge receipt; this is not a pass.' >> "$GITHUB_STEP_SUMMARY"
        fi
        if [ "$PREMERGE_OUTCOME" = success ] && [ "$valid_receipt" != 1 ]; then
            echo 'error: successful step lacks the matching Debug non-Godot receipt' \
                | tee "$DN2CPP_PREMERGE_LOGROOT/receipt-validation-error.txt" >&2
            exit 1
        fi
        ;;
    *)
        echo 'usage: ci-premerge.sh identity|capture STAGE COMMAND...|run|outcome' >&2
        exit 2
        ;;
esac
