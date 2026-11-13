#!/usr/bin/env bash
# CLI enum storage truncation and callable CoreLib enum metadata/formatting leaves.
narrow_enum_storage_asserts() {
    local out="$1" fixture="$1/narrow-storage" input="$1/narrow-storage-input.dll"
    gate_capture_ok "$out/narrow-before" run_bounded dotnet "$_CG_APP" before-narrow-storage
    gate_output select "$out/native.stdout" "$out/narrow-prefix.stdout" --before '== narrow enum storage ==' --policy crlf
    gate_output compare "$out/narrow-prefix.stdout" "$out/narrow-before.stdout" --policy crlf
    gate_output witness "$out/native.stdout" 'narrow enum storage end' --count 1 --policy crlf
    build_gate_proj gates/fixtures/narrow-enum-storage/NarrowEnumStorageFixture.csproj
    gate_capture_ok "$out/narrow-generate" run_bounded dotnet \
        "gates/fixtures/narrow-enum-storage/bin/$CONFIG/$TFM/NarrowEnumStorageFixture.dll" "$_CG_APP" "$input" "$_CG_CORELIB" "$DN2CPP_CLI_DLL"
    gate_output witness "$out/narrow-generate.stdout" 'enum-contract-guards=owned,non-generic,exact-signature,ildiet' --count 1 --policy crlf
    cp "${_CG_APP%.dll}.runtimeconfig.json" "${input%.dll}.runtimeconfig.json"
    gate_capture_ok "$out/narrow-oracle" run_bounded dotnet "$input"
    gate_capture_ok "$out/narrow-emit" invoke_cli "$input" -r "$_CG_CORELIB" --auto-ref --stripping-level high -o "$fixture"
    grep -q 'enum-raw-format=' "$out/narrow-emit.stdout" \
        || { echo "FAIL: private enum formatter did not exercise shared-body fallback" >&2; return 1; }
    grep -w -q 'dn2cpp_enum_format_raw_integer' "$fixture"/generated*.cpp \
        || { echo "FAIL: private enum formatter has no callable native body" >&2; return 1; }
    compile_console "$fixture" NarrowEnumStorage
    gate_capture_ok "$out/narrow-native" run_bounded "./$fixture/NarrowEnumStorage$EXE_EXT"
    gate_output compare "$out/narrow-native.stdout" "$out/narrow-oracle.stdout" --policy crlf
    gate_output witness "$out/narrow-native.stdout" 'enum private runtime contracts end' \
        'numeric-no-type:-1:FF' 'private-invalid-format=FormatException' --count 1 --policy crlf
    gate_output witness "$out/narrow-native.stdout" 'byte=0,0,0,0,0' --count 1 --policy crlf
    gate_output witness "$out/narrow-native.stdout" 'sbyte=-1,-1,-1,-1,-1' --count 1 --policy crlf
    gate_output witness "$out/narrow-native.stdout" 'ushort=0,0,0,0,0' --count 1 --policy crlf
    gate_output witness "$out/narrow-native.stdout" 'short=-1,-1,-1,-1,-1' --count 1 --policy crlf
    gate_output witness "$out/narrow-native.stdout" 'int=305419896,305419896,305419896,305419896,305419896' --count 1 --policy crlf
}
