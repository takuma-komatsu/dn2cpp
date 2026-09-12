#!/usr/bin/env bash
# Android DeClang APK packaging, selective application, and incremental rebuilds.
source "$(dirname "$0")/_common.sh"
if [ "$DN2CPP_OS" != macos ]; then
    gate_skip "Android DeClang editor export requires macOS"
fi
source "gates/_declang.sh"
ensure_declang_compiler
# Keep this lock-owning shell alive while the themed gate runs; its exit hook
# releases the standalone lock, and the child validates the inherited owner.
if [ "$(cat "$DN2CPP_SUITE_MACHINE_LOCK_DIR/pid" 2>/dev/null || true)" = "$$" ]; then
    export DN2CPP_MACHINE_LOCK_HELD=$$
fi
bash "$(dirname "$0")/build-and-run-godot-editor-export-android.sh"
