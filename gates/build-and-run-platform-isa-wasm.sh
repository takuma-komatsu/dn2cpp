#!/usr/bin/env bash
# The Wasm platform-ISA family's capability contract on the wasm32 axis, diffed
# against real .NET: PackedSimd (and every X86/Arm row) answers false in the
# default wasm build, and every family's representative instruction throws
# PlatformNotSupportedException — the oracle is real .NET under
# DOTNET_EnableHWIntrinsic=0, the one configuration where its own answer to the
# whole table is false. The supported arm of this gate (a DN2CPP_WASM_SIMD build
# whose PackedSimd rows answer true and whose instruction results diff against
# an unmasked oracle) lands with the PackedSimd lowering.
#
# Machines without the Emscripten toolchain (or node) skip; the runner reports
# the gate as SKIPPED rather than passed (gate_skip in _common.sh).
source "$(dirname "$0")/_common.sh"
source "$(dirname "$0")/_platform_isa.sh"

# --no-bundled: an -O-less link needs the -debug archives the frozen bundle lacks.
dn2cpp_emsdk_resolve --no-bundled
for tool in em++ emcmake node; do
    if ! command -v "$tool" >/dev/null 2>&1; then
        gate_skip "$tool not found — no Emscripten toolchain to build wasm with"
    fi
done

export WASM=1
# An inherited mask would narrow the run and record that answer under the
# key below.
unset DN2CPP_CPU_FEATURES DN2CPP_CPU_FEATURES_DIAG

project="$PLATFORM_ISA_PROJECT"
out="$(_corelib_gate_out "$project")-wasm"

# The wasm axis cross-targets Emscripten, so the CoreLib flavour follows the
# TARGET, not the host (docs/PORTING.md H6).
_CG_CORELIB_IN=$(locate_corelib_cross_posix net10)
_corelib_gate_core "$project" "$out"

if gate_cache_check "$out" "platform_isa_wasm|default|$_CG_CORELIB|argv:all|oracle:DOTNET_EnableHWIntrinsic=0" \
        "$_CG_APP" "${_CG_APP%.dll}.runtimeconfig.json" "${_CG_APP%.dll}.deps.json"; then
    gate_cache_hit_msg
    exit 0
fi

echo "== 4/4 Compiling to wasm and running the contract under node (exact diff vs real .NET, all families masked) =="
compile_console_wasm "$out" "$project"

# Exact code equality cannot hold across runtimes (node reports an abort as 1,
# .NET as 134): the sides must only FAIL ALIKE, both 0 or both nonzero.
set +e
native=$(run_bounded node "./$out/$project.js" all); native_code=$?
expected=$(DOTNET_EnableHWIntrinsic=0 run_bounded dotnet "$_CG_APP" all); expected_code=$?
set -e
# strip_cr_win on the ORACLE side: node never emits \r\n, the oracle does.
assert_output "$native" "$(strip_cr_win "$expected")"
if [ "$native_code" -eq 0 ] || [ "$expected_code" -eq 0 ]; then
    assert_exit_code "$native_code" "$expected_code"
fi

# The diff above would also pass if both sides printed nothing useful; these
# say the contract block was there and answered false throughout.
block=$(platform_isa_contract_block "$native")
n_true=$(grep -c '=True$' <<<"$block" || true)
if [ "$n_true" -ne 0 ]; then
    echo "FAIL: the default wasm build answered true for $n_true row(s); the SIMD arm is not built here" >&2
    exit 1
fi
n_packed=$(grep -c '^Wasm\.PackedSimd=False$' <<<"$block" || true)
if [ "$n_packed" -ne 1 ]; then
    echo "FAIL: expected exactly one 'Wasm.PackedSimd=False' row, found $n_packed" >&2
    exit 1
fi
echo "OK: every platform-ISA row false on the default wasm build, PackedSimd included"

gate_cache_commit
