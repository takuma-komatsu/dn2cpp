#!/usr/bin/env bash
# Native-integer endian reads and parsing execute through C# -> C++ -> wasm32.
# The same generic fixtures run as CLR int/uint and Wasm nint/nuint: a host's
# native integer width must never choose the expected wasm32 range or storage.
# Pass --no-ildiet to exercise the original assembly instead of its rewritten copy.
source "$(dirname "$0")/_common.sh"

if [ "$#" -gt 1 ] || { [ "$#" -eq 1 ] && [ "$1" != --no-ildiet ]; }; then
    echo "usage: $0 [--no-ildiet]" >&2
    exit 2
fi

dn2cpp_emsdk_resolve --no-bundled
for tool in em++ emcmake node "$CMAKE" ninja dotnet; do
    if ! command -v "$tool" >/dev/null 2>&1; then
        gate_skip "$tool not found — required for native-integer generic math wasm32"
    fi
done
export WASM=1
DN2CPP_SAMPLE_PROJECT_DIR=gates/fixtures/generic-math-native-wasm
DN2CPP_GATE_EXTRA_INPUTS="${DN2CPP_GATE_EXTRA_INPUTS:-} $DN2CPP_SAMPLE_PROJECT_DIR"
build_gate_proj "$DN2CPP_SAMPLE_PROJECT_DIR/GenericMathNativeWasm.csproj"
_CG_CORELIB_IN=$(locate_corelib_cross_posix net10)
_corelib_gate_core GenericMathNativeWasm "artifacts/generic-math-native-wasm${DN2CPP_OUT_SUFFIX:-}" "$@"
if _corelib_gate_check "$_CG_OUT" "generic-math-native-wasm32|oracle32|$*|$_CG_CORELIB$(_gate_ctx_extras)"; then
    gate_cache_hit_msg
    exit 0
fi
gate_capture_ok "$_CG_OUT/clr" run_bounded dotnet "$_CG_APP" --oracle32
compile_console_wasm "$_CG_OUT" GenericMathNativeWasm
gate_capture_ok "$_CG_OUT/native" run_bounded node "$_CG_OUT/GenericMathNativeWasm.js"
gate_output compare "$_CG_OUT/native.stdout" "$_CG_OUT/clr.stdout" --policy crlf
for observation in native clr; do
    gate_output byte-witness "$_CG_OUT/$observation.stdout" --count 1 --policy crlf \
        'generic native width=4=True' \
        'signed endian native32 end' 'unsigned endian native32 end' \
        'signed parsing values native32 end' 'unsigned parsing values native32 end' \
        'signed parsing provider/null native32 end' 'unsigned parsing provider/null native32 end' \
        'signed parsing utf8 native32 end' 'unsigned parsing utf8 native32 end' \
        'signed parsing native32 end' 'unsigned parsing native32 end' 'generic native32 end'
done
cat "$_CG_OUT/native.stdout"
gate_cache_commit
