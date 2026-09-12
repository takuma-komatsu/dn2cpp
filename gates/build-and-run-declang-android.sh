#!/usr/bin/env bash
# Android DeClang compatibility and selective flattening in the linked AArch64 ELF.
source "$(dirname "$0")/_common.sh"
[[ "$DN2CPP_OS" == macos || "$DN2CPP_OS" == windows ]] || gate_skip "Android DeClang validation requires macOS or Windows"
source gates/_declang.sh
[ -f "${ANDROID_NDK_ROOT:-}/build/cmake/android.toolchain.cmake" ] \
    || gate_skip "set ANDROID_NDK_ROOT to an installed Android NDK"
PYTHON="$(resolve_python)" || gate_skip "Android DeClang validation requires Python"
$PYTHON gates/declang_discovery_checks.py "$PWD"
$PYTHON gates/declang_android_host_checks.py "$PWD"
ensure_declang_android_compiler
ensure_declang_android_ndk
$PYTHON gates/declang_android_checks.py "$PWD" "$DN2CPP_DECLANG_COMPILER"
DN2CPP_DECLANG_ANDROID=1 $PYTHON gates/declang_native_checks.py "$PWD" "$DN2CPP_DECLANG_COMPILER" \
    "$PWD/artifacts/declang-android-proof"
