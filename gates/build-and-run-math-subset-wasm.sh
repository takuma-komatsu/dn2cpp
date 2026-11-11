#!/usr/bin/env bash
# Primitive BigMul facades execute through C# -> C++ -> wasm32. Native-int
# direct, function-pointer and delegate calls preserve halves, out aliases,
# canaries and null faults. A host CLR64 result is not a native32 oracle:
# fixed-width C# products and Python's unbounded products check the results.
# Pass --no-ildiet to exercise the original assembly instead of its rewritten copy.
source "$(dirname "$0")/_common.sh"

if [ "$#" -gt 1 ] || { [ "$#" -eq 1 ] && [ "$1" != --no-ildiet ]; }; then
    echo "usage: $0 [--no-ildiet]" >&2
    exit 2
fi

dn2cpp_emsdk_resolve --no-bundled
for tool in em++ emcmake node; do
    if ! command -v "$tool" >/dev/null 2>&1; then
        gate_skip "$tool not found: required for primitive BigMul wasm32"
    fi
done
export WASM=1
DN2CPP_SAMPLE_PROJECT_DIR=gates/fixtures/primitive-bigmul-wasm
DN2CPP_GATE_EXTRA_INPUTS="${DN2CPP_GATE_EXTRA_INPUTS:-} gates/fixtures/primitive-bigmul-wasm samples/dotnet/MathSubset/PrimitiveBigMulSubset.cs"
build_gate_proj "$DN2CPP_SAMPLE_PROJECT_DIR/PrimitiveBigMulWasm.csproj"
_CG_CORELIB_IN=$(locate_corelib_cross_posix net10)
_corelib_gate_core PrimitiveBigMulWasm "artifacts/primitive-bigmul-wasm${DN2CPP_OUT_SUFFIX:-}" "$@"
if _corelib_gate_check "$_CG_OUT" "primitive-bigmul-wasm32|preprocessing:$*|$_CG_CORELIB$(_gate_ctx_extras)"; then
    gate_cache_hit_msg
    exit 0
fi
compile_console_wasm "$_CG_OUT" PrimitiveBigMulWasm
gate_capture_ok "$_CG_OUT/native" run_bounded node "$_CG_OUT/PrimitiveBigMulWasm.js"
gate_output byte-witness "$_CG_OUT/native.stdout" --count 1 --policy crlf \
    'primitive native width=4' 'primitive fixed direct=True' \
    'primitive native direct/canary/alias/evaluation=True' \
    'primitive all-six address=True' 'primitive all-six delegate=True' \
    'primitive null output direct=NullReferenceException address=NullReferenceException' \
    'native32:route0:canary/alias/null=True' \
    'native32:route1:canary/alias/null=True' \
    'native32:route2:canary/alias/null=True' 'primitive native32 BigMul end'
gate_python gates/fixtures/primitive-bigmul-wasm/check-output.py "$_CG_OUT/native.stdout"
cat "$_CG_OUT/native.stdout"
gate_cache_commit
