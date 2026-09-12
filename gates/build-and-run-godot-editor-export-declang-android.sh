#!/usr/bin/env bash
# Android DeClang APK packaging, selective application, and incremental rebuilds.
source "$(dirname "$0")/_common.sh"
if [ "$DN2CPP_OS" != macos ] && [ "$DN2CPP_OS" != windows ]; then
    gate_skip "Android DeClang editor export requires macOS or Windows"
fi
source "gates/_declang.sh"
ensure_declang_android_compiler
ensure_declang_android_ndk
export DN2CPP_EDITOR_EXPORT_ANDROID_OUT=gates/out-godot-editor-export-declang-android
# Keep this lock-owning shell alive while the themed gate runs; its exit hook
# releases the standalone lock, and the child validates the inherited owner.
if [ "$(cat "$DN2CPP_SUITE_MACHINE_LOCK_DIR/pid" 2>/dev/null || true)" = "$$" ]; then
    export DN2CPP_MACHINE_LOCK_HELD=$$
fi
bash "$(dirname "$0")/build-and-run-godot-editor-export-android.sh"
