#!/usr/bin/env bash
# Numeric Vector<T> value/boxed/callable semantics on actual wasm32 execution.
source "$(dirname "$0")/_common.sh"
source "$(dirname "$0")/vector-value-probe.sh"

dn2cpp_emsdk_resolve --no-bundled
for tool in em++ emcmake node; do
    command -v "$tool" >/dev/null 2>&1 || gate_skip "$tool not found — vector value Wasm not verified"
done
export WASM=1
unset SCALAR HIGHWAY DN2CPP_WASM_SIMD
corelib=$(locate_corelib_cross_posix net10)
build_proj samples/dotnet/VectorProbe/VectorProbe.csproj
_CG_APP="samples/dotnet/VectorProbe/bin/$CONFIG/$TFM/VectorProbe.dll"
out="$(_corelib_gate_out VectorProbe)-value-wasm${DN2CPP_OUT_SUFFIX:-}"
ctx="vector-value-wasm|fixed-vector-abi|AVX0|raw+diet|node|independent-native-lanes|$corelib|cli:$(_gate_cli_hash)"
if gate_cache_check "$out" "$ctx" "$corelib" "$_CG_APP" \
    "${_CG_APP%.dll}.runtimeconfig.json" "${_CG_APP%.dll}.deps.json" \
    gates/vector-value-probe.sh gates/fixtures/vector-value; then
    gate_cache_hit_msg
else
    vector_value_wasm_probe "$out"
    gate_cache_commit
fi
