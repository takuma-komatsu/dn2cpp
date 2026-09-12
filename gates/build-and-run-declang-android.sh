#!/usr/bin/env bash
# Android DeClang compatibility and selective flattening in the linked AArch64 ELF.
source "$(dirname "$0")/_common.sh"
[[ "$(uname -s)" == Darwin ]] || gate_skip "Android DeClang validation requires macOS"
[ -f "${ANDROID_NDK_ROOT:-}/build/cmake/android.toolchain.cmake" ] \
    || gate_skip "set ANDROID_NDK_ROOT to an installed Android NDK"
source gates/_declang.sh
ensure_declang_compiler
python3 gates/declang_android_checks.py "$PWD" "$DN2CPP_DECLANG_COMPILER"
DN2CPP_DECLANG_ANDROID=1 python3 gates/declang_native_checks.py "$PWD" "$DN2CPP_DECLANG_COMPILER" \
    "$PWD/artifacts/declang-android-proof"
