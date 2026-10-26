#!/usr/bin/env bash
# Imports preserve generic storage and managed provider parameter/return/field identity.
hotupdate_signature_argument_roundtrip() {
    local base="$1" patch="$2" out="$3" address="$4" encoding metadata expected previous arm owner
    local int_owner='SignatureArgumentImports.Cell`1[System.Int32]'
    local string_owner='SignatureArgumentImports.Cell`1[System.String]'
    mkdir -p "$out"
    gate_capture_ok "$out/clr.raw" run_bounded dotnet "$patch"
    expected='== imported generic signature arguments ==
signature method imports
11
23
signature type imports
31
47
signature imports agree
31
47
signature named method imports
37
59
signature named type imports
61
73
signature named imports agree
83
97
imported generic signature arguments end'
    previous="$expected"
    expected+=$'\n'
    expected+='== imported generic fields ==
generic fields before initialization
4
generic field defaults
0
0
generic fields initialized once
6
generic fixed fields
17
29
generic field method agreement
17
29
generic typed static fields
41
generic static item
generic typed instance fields
67
generic instance item
generic instances retain constructor fields
43
53
generic signature fields
107
109
generic signature field method agreement
107
109
generic fields still initialized once
6
imported generic fields end'
    previous="$expected"
    expected+=$'\n'
    expected+='== imported managed providers ==
True
True
True
True
True
imported managed providers end'
    gate_assert_text "$out/clr.raw.stdout" "$expected"
    printf '%s' "$previous" > "$out/providers-before.expected"
    gate_output prefix "$out/clr.raw.stdout" "$out/providers-before.expected" \
        --before '== imported managed providers ==' --policy crlf
    previous="${previous%%$'\n== imported generic fields =='*}"
    printf '%s' "$previous" > "$out/fields-before.expected"
    gate_output prefix "$out/clr.raw.stdout" "$out/fields-before.expected" \
        --before '== imported generic fields ==' --policy crlf
    gate_output witness "$out/clr.raw.stdout" '== imported generic fields ==' \
        'imported generic fields end' --count 1 --policy crlf
    gate_capture_ok "$out/address" run_bounded dotnet "$address"
    gate_assert_text "$out/address.stdout" '1'
    for metadata in packed native; do
        arm="$out/$metadata"
        invoke_cli "$base" -r "$(locate_corelib)" --hotupdate-base --reflection-metadata "$int_owner=$metadata" \
            --reflection-metadata "$string_owner=$metadata" -o "$arm"
        local table=md_record_fldtab
        [ "$metadata" != native ] || table=md_native_fldtab
        for owner in Int32 String; do
            grep -qw "${table}_SignatureArgumentImports_Cell_$owner" "$arm"/generated*.cpp \
                || { echo "FAIL: generic field metadata was not emitted $metadata: $owner" >&2; return 1; }
        done
        compile_console "$arm" SignatureArgumentBase
        for encoding in register stack; do
            local flags=()
            [ "$encoding" != stack ] || flags=(--patch-stackcode)
            invoke_cli --emit-patch "$patch" --base-abi "$arm/base-abi.json" \
                ${flags[@]+"${flags[@]}"} -o "$arm/$encoding"
            gate_capture_ok "$arm/$encoding.raw" run_bounded "$arm/SignatureArgumentBase$EXE_EXT" \
                "$arm/$encoding/SignatureArgumentPatch.bpi"
            printf 'signature argument base ready\n' > "$arm/base-ready.expected"
            gate_output prefix "$arm/$encoding.raw.stdout" "$arm/base-ready.expected" \
                --before '== imported generic signature arguments ==' --policy crlf
            gate_output select "$arm/$encoding.raw.stdout" "$arm/$encoding.patch.stdout" \
                --from '== imported generic signature arguments ==' --policy crlf
            gate_output compare "$out/clr.raw.stdout" "$arm/$encoding.patch.stdout"
            "$(resolve_python)" gates/fixtures/hotupdate-import-identity/set-bpi-version.py \
                "$arm/$encoding/SignatureArgumentPatch.bpi" "$arm/$encoding/legacy.bpi" 2
            gate_capture_ok "$arm/$encoding.legacy" run_bounded "$arm/SignatureArgumentBase$EXE_EXT" "$arm/$encoding/legacy.bpi"
            gate_assert_text "$arm/$encoding.legacy.stdout" 'signature argument base ready
signature update rejected: BPI: unsupported format version'
            if invoke_cli --emit-patch "$address" --base-abi "$arm/base-abi.json" \
                ${flags[@]+"${flags[@]}"} -o "$arm/$encoding/address" > "$arm/$encoding.address.log" 2>&1; then
                echo "FAIL: taking a generic static field address unexpectedly baked" >&2
                return 1
            fi
            grep -Fq 'opcode Ldsflda is not supported yet' "$arm/$encoding.address.log" \
                || { cat "$arm/$encoding.address.log" >&2; return 1; }
        done
    done
    echo "OK (generic and provider imports, packed/native, register/stack)"
}
