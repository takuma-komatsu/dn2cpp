#!/usr/bin/env bash

# Resolve only staged DeClang distributions; clang++ on PATH may be ordinary Clang.
ensure_declang_compiler() {
    local candidate selected="" suffix=""
    [ "${DN2CPP_OS:-}" != windows ] || suffix=".exe"
    if [ -z "${DN2CPP_DECLANG_COMPILER:-}" ]; then
        for candidate in "$PWD"/artifacts/declang-distribution/*/compiler/bin/clang++"$suffix"; do
            [ -f "$candidate" ] && [ -x "$candidate" ] || continue
            if [ -n "$selected" ]; then
                echo "FAIL: multiple DeClang distributions; set DN2CPP_DECLANG_COMPILER explicitly" >&2
                return 1
            fi
            selected="$candidate"
        done
        if [ -z "$selected" ]; then
            gate_skip "set DN2CPP_DECLANG_COMPILER to an existing DeClang C++ driver, or stage one under artifacts/declang-distribution/*/compiler/bin/clang++${suffix}"
        fi
        DN2CPP_DECLANG_COMPILER="$selected"
    fi
    if [ ! -f "$DN2CPP_DECLANG_COMPILER" ] || [ ! -x "$DN2CPP_DECLANG_COMPILER" ]; then
        echo "FAIL: DeClang compiler is not an executable file: $DN2CPP_DECLANG_COMPILER" >&2
        return 1
    fi
    # The editor gate reads this in child shells and Python processes.
    if [ "${DN2CPP_OS:-}" = windows ]; then
        DN2CPP_DECLANG_COMPILER="$(cygpath -am "$DN2CPP_DECLANG_COMPILER")"
    fi
    export DN2CPP_DECLANG_COMPILER
}

# Android builds use a newer LLVM baseline than the desktop distribution.
ensure_declang_android_compiler() {
    local candidate selected=""
    if [ "${DN2CPP_OS:-}" = macos ] && [ -z "${DN2CPP_DECLANG_COMPILER:-}" ]; then
        for candidate in "$PWD"/artifacts/declang-android-*/build/bin/clang++; do
            [ -f "$candidate" ] && [ -x "$candidate" ] || continue
            if [ -n "$selected" ]; then
                echo "FAIL: multiple Android DeClang builds; set DN2CPP_DECLANG_COMPILER explicitly" >&2
                return 1
            fi
            selected="$candidate"
        done
        if [ -n "$selected" ]; then
            DN2CPP_DECLANG_COMPILER="$selected"
        fi
    fi
    ensure_declang_compiler
}

# The distributed DeClang is based on Clang 16. Newer NDK libc++ headers use
# compiler builtins it does not have, so Android validation must use r26d.
ensure_declang_android_ndk() {
    local staged="$PWD/artifacts/declang-ndk/android-ndk-r26d"
    local revision='Pkg.Revision = 26.3.11579264'
    local version major
    version="$("$DN2CPP_DECLANG_COMPILER" --version 2>&1)"
    major="$(sed -n 's/^clang version \([0-9][0-9]*\).*/\1/p' <<<"$version")"
    major="${major%%$'\n'*}"
    [ -n "$major" ] \
        || { echo "FAIL: DeClang did not report a Clang version" >&2; return 1; }
    if [ "$major" -lt 19 ]; then
        if [ -f "$staged/build/cmake/android.toolchain.cmake" ]; then
            ANDROID_NDK_ROOT="$staged"
        elif [ -f "${ANDROID_NDK_ROOT:-}/build/cmake/android.toolchain.cmake" ] \
            && grep -qF "$revision" "$ANDROID_NDK_ROOT/source.properties"; then
            :
        else
            gate_skip "Clang $major based DeClang requires NDK r26d under artifacts/declang-ndk/android-ndk-r26d or ANDROID_NDK_ROOT"
        fi
    elif [ ! -f "${ANDROID_NDK_ROOT:-}/build/cmake/android.toolchain.cmake" ]; then
        gate_skip "set ANDROID_NDK_ROOT to an installed Android NDK"
    else
        :
    fi
    if [ "$DN2CPP_OS" = windows ]; then
        ANDROID_NDK_ROOT="$(cygpath -am "$ANDROID_NDK_ROOT")"
    fi
    export ANDROID_NDK_ROOT
}
