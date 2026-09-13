#!/usr/bin/env bash
# macOS DeClang: selective native flattening, isolated logs, and Ninja invalidation.
source "$(dirname "$0")/_common.sh"
if [[ "$DN2CPP_OS" == linux || "$DN2CPP_OS" == windows ]]; then
    gate_expected_partial "Linux and Windows desktop targets are outside this DeClang integration. Android arm64-v8a compilation from these hosts is covered by gates/build-and-run-declang-android.sh; desktop compilation, execution and machine-code comparison are covered by gates/build-and-run-declang.sh on macOS."
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
