#!/usr/bin/env bash
# Closed generic imports distinguish nested function signatures with the same ABI.
hotupdate_signature_argument_roundtrip() {
    local base="$1" patch="$2" out="$3" encoding actual expected
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
    assert_output "$(cat "$out/clr.stdout")" "$expected"
    invoke_cli "$base" --hotupdate-base -o "$out"
    compile_console "$out" SignatureArgumentBase
    for encoding in register stack; do
        local flags=()
        [ "$encoding" != stack ] || flags=(--patch-stackcode)
        invoke_cli --emit-patch "$patch" --base-abi "$out/base-abi.json" \
            ${flags[@]+"${flags[@]}"} -o "$out/$encoding"
        run_bounded "$out/SignatureArgumentBase$EXE_EXT" \
            "$out/$encoding/SignatureArgumentPatch.bpi" > "$out/$encoding.raw.stdout"
        strip_cr_win_file "$out/$encoding.raw.stdout" > "$out/$encoding.stdout"
        actual=$(head -n 1 "$out/$encoding.stdout")
        assert_output "$actual" 'signature argument base ready'
        sed '1d' "$out/$encoding.stdout" > "$out/$encoding.patch.stdout"
        diff -u "$out/clr.stdout" "$out/$encoding.patch.stdout"
        "$(resolve_python)" gates/fixtures/hotupdate-import-identity/set-bpi-version.py \
            "$out/$encoding/SignatureArgumentPatch.bpi" "$out/$encoding/legacy.bpi" 2
        actual=$(run_bounded "$out/SignatureArgumentBase$EXE_EXT" "$out/$encoding/legacy.bpi")
        assert_output "$(strip_cr_win "$actual")" 'signature argument base ready
signature update rejected: BPI: unsupported format version'
    done
    echo "OK (closed generic function-signature imports, register and stack)"
}
