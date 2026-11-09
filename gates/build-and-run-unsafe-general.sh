#!/usr/bin/env bash
# Consolidated Unsafe/MemoryMarshal arbitrary-type gate: proves the transpiler's
# System.Runtime.CompilerServices.Unsafe and System.Runtime.InteropServices.MemoryMarshal
# intrinsic interceptions are correct over arbitrary element/struct types and arbitrary
# memory backings (stackalloc, heap arrays, struct fields), not just int. Covers
# Unsafe.As / AsRef / Add / Subtract / ByteOffset / SizeOf / AreSame / NullRef /
# IsNullRef / Read / Write / ReadUnaligned / WriteUnaligned / CopyBlock / InitBlock
# / Unbox (mutate a box's payload in place through the returned ref),
# and MemoryMarshal.Cast / AsBytes / GetReference / GetArrayDataReference / Read /
# Write / CreateSpan / CreateReadOnlySpan over multi-field structs, double/long, and
# byte-backed reinterprets. Also proves [StructLayout] layout control:
# LayoutKind.Explicit unions (all-offset-0 and disjoint offsets), explicit Size,
# Pack=1/Pack=8 packing, and an explicit union nested in a packed sequential
# struct (the godot_variant shape), checked by sizeof / byte punning / AsBytes.
# Reinterpret byte order is host-endian — the gate diffs the
# transpiled binary against real .NET on the same machine, so they agree.
# Typed references retain their exact declared type and writable location,
# including shared generic arguments and invalid refanyval casts.
# WASM=1 runs the same checks under node with the Web collector.
# Diffed exact vs real .NET.
source "$(dirname "$0")/_common.sh"

typedref_shared=1
for argument in "$@"; do
    case "$argument" in
        --no-shared-generics) typedref_shared=0 ;;
    esac
done

gate_capture_diff_files() {
    local out="$1" output="$3" tail="$1/assert-typedref-tail.stdout"
    gate_capture_ok "$out/assert-before-typedref" run_bounded dotnet "$_CG_APP" before-typed-references || return 1
    gate_output prefix "$output" "$out/assert-before-typedref.stdout" \
        --before '== typed references ==' --policy crlf || return 1
    gate_output byte-witness "$output" 'typedref complete' --policy crlf || return 1
    gate_output select "$output" "$tail" --from 'typedref complete' --policy crlf || return 1
    gate_assert_text "$tail" 'typedref complete'
}

gate_extra_asserts() {
    gate_python - "$1" "$typedref_shared" <<'PY'
from pathlib import Path
import re
import sys

source = "\n".join(p.read_text() for p in Path(sys.argv[1]).glob("generated*.cpp"))
body = re.search(r"// UnsafeGeneral.TypedReferenceSubset.Program::HeapReference\n.*?\n}", source, re.S)
assert body, "Missing typed reference GC witness"
reference = re.search(r"(\w+) = Dn2CppTypedReference\{", body[0])
assert reference, "Missing interior typed reference"
assert re.search(r"= Program_Collect_m\d+\(\);\s+DN2CPP_WEB_GC_LIVENESS\(\(" +
                 re.escape(reference[1]) + r"\)\.f__value\);", body[0]), \
    "Pending typed reference must survive the collecting argument call on Web"
if sys.argv[2] == "1":
    for method in ("ReferenceType", "ReplaceShared", "ReadShared", "MatrixType", "ReadMatrix"):
        body = re.search(r"\bProgram_" + method + r"_TisCnRef_m\d+\([^\n]+\)\n\{.*?\n\}", source, re.S)
        assert body and "__rgctx[" in body[0], "Typed reference generic witness must use the shared body: " + method
PY
}

if [ "${WASM:-0}" = 1 ]; then
    dn2cpp_emsdk_resolve --no-bundled
    for tool in em++ emcmake node; do
        command -v "$tool" >/dev/null 2>&1 \
            || gate_skip "$tool not found — no Emscripten toolchain to build wasm with"
    done
    export WASM=1
    wasm_corelib_diff_gate UnsafeGeneral "$@"
    gate_capture_diff_files "$_CG_OUT" UnsafeGeneral "$_CG_OUT/native.stdout"
    gate_extra_asserts "$_CG_OUT"
else
    corelib_diff_gate UnsafeGeneral "$@"
fi

(
    unset -f gate_capture_diff_files gate_extra_asserts
    DN2CPP_SAMPLE_PROJECT_DIR=gates/fixtures/typed-reference-erasure
    if [ "${WASM:-0}" = 1 ]; then
        _CG_CORELIB_IN=$(locate_corelib_cross_posix net10)
        out=artifacts/typedreferenceerasure-wasm
        _corelib_gate_core TypedReferenceErasure "$out" System.Collections.Concurrent "$@"
        compile_console_wasm "$out" TypedReferenceErasure
        gate_capture_ok "$out/native" run_bounded node "$out/TypedReferenceErasure.js"
        gate_output compare "$out/native.stdout" gates/expected/typed-reference-erasure.txt --policy crlf
    else
        corelib_freeze_gate TypedReferenceErasure gates/expected/typed-reference-erasure.txt \
            System.Collections.Concurrent "$@"
    fi
)

(
    unset -f gate_capture_diff_files gate_extra_asserts
    DN2CPP_SAMPLE_PROJECT_DIR=gates/fixtures/typed-reference-signature
    base="artifacts/typedreferencesignature${DN2CPP_OUT_SUFFIX:-}"
    if [ "${WASM:-0}" = 1 ]; then
        base="$base-wasm"
        _CG_CORELIB_IN=$(locate_corelib_cross_posix net10)
    fi
    _corelib_gate_core TypedReferenceSignature "$base" "$@"
    gate_capture_ok "$base/oracle" run_bounded dotnet "$_CG_APP"
    for mode in ordinary trimmed; do
        out="$base"
        if [ "$mode" = trimmed ]; then
            out="$base-trimmed"
            invoke_cli "$_CG_APP" -r "$_CG_CORELIB" --trim-reflection "$@" -o "$out"
        fi
        if [ "${WASM:-0}" = 1 ]; then
            compile_console_wasm "$out" TypedReferenceSignature
            runner=(node "$out/TypedReferenceSignature.js")
        else
            compile_console "$out" TypedReferenceSignature
            runner=("./$out/TypedReferenceSignature$EXE_EXT")
        fi
        gate_capture_ok "$out/native" run_bounded "${runner[@]}"
        gate_output compare "$out/native.stdout" "$base/oracle.stdout" --policy crlf
        gate_output byte-witness "$out/native.stdout" 'typedref signature complete' --policy crlf
        if [ "$mode" = trimmed ]; then
            gate_capture_ok "$out/members" run_bounded "${runner[@]}" members
            gate_output byte-witness "$out/members.stdout" \
                'typedref signature members=PlatformNotSupportedException' --policy crlf
        fi
    done
)
