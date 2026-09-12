#!/usr/bin/env bash
# Existing DeClang: selective native flattening, isolated compiler logs, and Ninja invalidation.
source "$(dirname "$0")/_common.sh"
if [[ "$(uname -s)" != Darwin ]]; then
    gate_skip "DeClang export requires macOS"
fi
source "gates/_declang.sh"
ensure_declang_compiler
python3 "gates/declang_helper_checks.py" "$PWD" "$DN2CPP_DECLANG_COMPILER"
python3 "gates/declang_native_checks.py" "$PWD" "$DN2CPP_DECLANG_COMPILER"
