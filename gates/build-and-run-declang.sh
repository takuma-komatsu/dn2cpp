#!/usr/bin/env bash
# Existing DeClang: selective native flattening, isolated compiler logs, and Ninja invalidation.
source "$(dirname "$0")/_common.sh"
if [ "$DN2CPP_OS" = windows ]; then
    gate_expected_partial "native desktop DeClang has no reachable state on Windows because this integration supports only macOS desktop and Android arm64-v8a targets. The Windows-hosted compiler path is asserted by gates/build-and-run-declang-android.sh; the desktop compile, execution and machine-code comparison are asserted by this gate, gates/build-and-run-declang.sh, on macOS."
    exit 0
fi
if [ "$DN2CPP_OS" != macos ]; then
    gate_skip "desktop DeClang validation requires macOS"
fi
source "gates/_declang.sh"
ensure_declang_compiler
PYTHON="$(resolve_python)" || gate_skip "DeClang validation requires Python"
"$PYTHON" "gates/declang_helper_checks.py" "$PWD" "$DN2CPP_DECLANG_COMPILER"
"$PYTHON" "gates/declang_native_checks.py" "$PWD" "$DN2CPP_DECLANG_COMPILER"
