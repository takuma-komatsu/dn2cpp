#!/usr/bin/env bash
# Selective DeClang export runs the desktop game's existing GC and callback assertions.
source "$(dirname "$0")/_common.sh"
if [ "$DN2CPP_OS" = windows ]; then
    gate_expected_partial "a desktop DeClang editor export has no reachable state on Windows because Windows desktop targets are outside this DeClang integration. The Windows-hosted editor and compiler path is asserted by gates/build-and-run-godot-editor-export-declang-android.sh; the desktop export and game execution are asserted by this gate, gates/build-and-run-godot-editor-export-declang.sh, on macOS."
    exit 0
fi
if [ "$DN2CPP_OS" != macos ]; then
    gate_skip "desktop DeClang editor export requires macOS"
fi
source "gates/_declang.sh"
ensure_declang_compiler
export DN2CPP_EDITOR_EXPORT_OUT=gates/out-godot-editor-export-declang
# Keep this lock-owning shell alive while the themed gate runs; its exit hook
# releases the standalone lock, and the child validates the inherited owner.
if [ "$(cat "$DN2CPP_SUITE_MACHINE_LOCK_DIR/pid" 2>/dev/null || true)" = "$$" ]; then
    export DN2CPP_MACHINE_LOCK_HELD=$$
fi
bash "$(dirname "$0")/build-and-run-godot-editor-export.sh"
