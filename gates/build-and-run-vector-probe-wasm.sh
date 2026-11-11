#!/usr/bin/env bash
# IEEE vector arithmetic on EMU128 and Web SIMD, plus value-method and managed
# unsupported-element ABI parity on wasm32 using the same default image.
source "$(dirname "$0")/_common.sh"
source "$(dirname "$0")/_vector-floating-point.sh"
source "$(dirname "$0")/_vector-corelib-checks.sh"

dn2cpp_emsdk_resolve --no-bundled
for tool in em++ emcmake node; do
    command -v "$tool" >/dev/null 2>&1 || gate_skip "$tool not found — vector Wasm not verified"
done
export WASM=1
unset SCALAR HIGHWAY DN2CPP_WASM_SIMD
vector_floating_wasm_gate
