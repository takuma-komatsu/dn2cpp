#!/usr/bin/env bash
# Boxed and indirect vector storage, independent of vector method support.
vector_storage_asserts() {
    local out="$1"
    DOTNET_EnableAVX="${DN2CPP_ORACLE_DOTNET_ENABLE_AVX:-0}" \
        gate_capture_ok "$out/storage-before" run_bounded dotnet "$_CG_APP" before-storage-safety
    gate_output select "$out/native.stdout" "$out/storage-prefix.stdout" --before '-- vector storage safety --' --policy crlf
    gate_output compare "$out/storage-prefix.stdout" "$out/storage-before.stdout" --policy crlf
    gate_output witness "$out/native.stdout" 'vector storage safety end' --count 1 --policy crlf
    gate_python gates/fixtures/vector-storage/check-generated.py "$out"
}
