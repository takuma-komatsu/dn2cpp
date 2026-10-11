#!/usr/bin/env bash
# Imports preserve generic storage and managed provider parameter/return/field identity.
hotupdate_signature_argument_roundtrip() {
    local base="$1" patch="$2" out="$3" address="$4" encoding metadata actual expected previous arm line owner
    local int_owner='SignatureArgumentImports.Cell`1[System.Int32]'
    local string_owner='SignatureArgumentImports.Cell`1[System.String]'
    mkdir -p "$out"
    run_bounded dotnet "$patch" > "$out/clr.raw.stdout"
    strip_cr_win_file "$out/clr.raw.stdout" > "$out/clr.stdout"
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
    assert_output "$(cat "$out/clr.stdout")" "$expected"
    actual=$(cat "$out/clr.stdout")
    assert_output "${actual%%$'\n== imported managed providers =='*}" "$previous"
    previous="${previous%%$'\n== imported generic fields =='*}"
    actual=$(cat "$out/clr.stdout")
    assert_output "${actual%%$'\n== imported generic fields =='*}" "$previous"
    for line in '== imported generic fields ==' 'imported generic fields end'; do
        [ "$(grep -Fxc -- "$line" "$out/clr.stdout")" = 1 ] \
            || { echo "FAIL: generic field block did not complete exactly once: $line" >&2; return 1; }
    done
    actual=$(run_bounded dotnet "$address")
    assert_output "$(strip_cr_win "$actual")" '1'
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
            run_bounded "$arm/SignatureArgumentBase$EXE_EXT" \
                "$arm/$encoding/SignatureArgumentPatch.bpi" > "$arm/$encoding.raw.stdout"
            strip_cr_win_file "$arm/$encoding.raw.stdout" > "$arm/$encoding.stdout"
            actual=$(head -n 1 "$arm/$encoding.stdout")
            assert_output "$actual" 'signature argument base ready'
            sed '1d' "$arm/$encoding.stdout" > "$arm/$encoding.patch.stdout"
            diff -u "$out/clr.stdout" "$arm/$encoding.patch.stdout"
            "$(resolve_python)" gates/fixtures/hotupdate-import-identity/set-bpi-version.py \
                "$arm/$encoding/SignatureArgumentPatch.bpi" "$arm/$encoding/legacy.bpi" 2
            actual=$(run_bounded "$arm/SignatureArgumentBase$EXE_EXT" "$arm/$encoding/legacy.bpi")
            assert_output "$(strip_cr_win "$actual")" 'signature argument base ready
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
