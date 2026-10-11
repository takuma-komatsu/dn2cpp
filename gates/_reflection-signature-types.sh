#!/usr/bin/env bash
# Signature Types and closed generic arguments retain function-pointer identity.
generic_signature_argument_asserts() {
    local output="$1" line
    for line in '== generic function signature arguments ==' \
        'generic signature Empty addresses: True/True/True' \
        'generic signature Empty identity: True/True/False/True' \
        'generic signature body arrays: True/True/True' \
        'generic signature body types: True/True/True' \
        'generic signature class arguments: True/True/True/True' \
        'generic signature unequal: True/True/True' \
        'generic signature method arguments: True/True/True/False' \
        'generic signature type arguments: True/True' \
        'generic signature mangle shadow: True/True' \
        'generic signature collections: True/True/True/1/1' \
        'generic signature collection values: True/True' \
        'generic function signature arguments end'; do
        test "$(grep -Fxc -- "$line" "$output")" = 1 \
            || { echo "FAIL: generic signature argument witness missing or repeated: $output/$line" >&2; return 1; }
    done
}

reflection_signature_asserts() {
    local output="$1" line
    for line in '== reflection signature type handles ==' \
        'signature composition: True/True/True/True/True/True' \
        'signature negatives: False/False/False/False/False' \
        'signature static flags: False/False/True/True' \
        'signature assignment int-uint pointer: True/True' \
        'signature assignment nested int-uint pointer: False/False' \
        'signature assignment void-int pointer: False/False' \
        'signature assignment enum-int pointer: True/True' \
        'signature assignment int-uint byref: True/True' \
        'signature assignment object-string pointer: True/True' \
        'signature assignment string-object pointer: False/False' \
        'signature assignment object-string byref: True/True' \
        'signature assignment bool-byte pointer: False/False' \
        'signature assignment char-ushort pointer: False/False' \
        'signature assignment float-int pointer: False/False' \
        'signature assignment native-long pointer: False/False' \
        'signature assignment native-unsigned pointer: True/True' \
        'signature assignment int-uint pointer array: False/False' \
        'signature assignment object-pointer array: False/False' \
        'signature assignment nested object-pointer array: False/False' \
        'signature assignment array pointer: True/True' \
        'signature assignment nested object-string pointer: False/False' \
        'signature assignment function pointer identity: True/True' \
        'signature assignment function pointer signature: False/False' \
        'signature assignment nested function pointer: False/False' \
        'signature closed generic: True/True/True' \
        'signature runtime generic: True/True/True' \
        'signature Invoke unchanged: 8/8' \
        'signature Pointer.Box Invoke: 4656/4656/4656/4656/ArgumentException/ArgumentException' \
        'signature Pointer.Box fields: 4656/4656/4656/4656/ArgumentException/ArgumentException' \
        'signature declared constructor array: ConstructorInfo[]' \
        'signature declared constructor: True/False/True/System.Int32&' \
        'signature declared constructor: False/False/True/System.Int32*' \
        'signature declared constructors count: 2' \
        'signature declared constructors null: NullReferenceException' \
        'reflection signature type handles end'; do
        test "$(grep -Fxc -- "$line" "$output")" = 1 \
            || { echo "FAIL: signature type block witness missing or repeated: $output/$line" >&2; return 1; }
    done
}

gate_reflection_signature_prefix_asserts() {
    local out="$1" axis
    for axis in dotnet native; do
        if [ "$axis" = dotnet ]; then
            run_bounded dotnet "$_CG_APP" before-reflection-signature-types > "$out/signature-prefix.$axis.raw.stdout"
        else
            run_bounded "$out/ReflectInvoke$EXE_EXT" before-reflection-signature-types > "$out/signature-prefix.$axis.raw.stdout"
        fi
        strip_cr_win_file "$out/signature-prefix.$axis.raw.stdout" > "$out/signature-prefix.$axis.stdout"
        awk '/^== reflection signature type handles ==$/ { exit } { print }' \
            "$out/virtual-delegate-full.$axis.stdout" > "$out/signature-old.$axis.stdout"
        diff -u "$out/signature-prefix.$axis.stdout" "$out/signature-old.$axis.stdout"
        reflection_signature_asserts "$out/virtual-delegate-full.$axis.stdout"
        if [ "$axis" = dotnet ]; then
            run_bounded dotnet "$_CG_APP" before-generic-signature-arguments > "$out/generic-signature-before.$axis.raw.stdout"
        else
            run_bounded "$out/ReflectInvoke$EXE_EXT" before-generic-signature-arguments > "$out/generic-signature-before.$axis.raw.stdout"
        fi
        strip_cr_win_file "$out/generic-signature-before.$axis.raw.stdout" > "$out/generic-signature-before.$axis.stdout"
        awk '/^== generic function signature arguments ==$/ { exit } { print }' \
            "$out/virtual-delegate-full.$axis.stdout" > "$out/generic-signature-prefix.$axis.stdout"
        diff -u "$out/generic-signature-before.$axis.stdout" "$out/generic-signature-prefix.$axis.stdout"
        generic_signature_argument_asserts "$out/virtual-delegate-full.$axis.stdout"
    done
    diff -u "$out/signature-prefix.dotnet.stdout" "$out/signature-prefix.native.stdout"
}

reflection_signature_diff_axes() {
    local axis out
    for axis in packed native uncompressed trimmed unshared; do
        out="artifacts/reflect-invoke-signature-types-$axis"
        mkdir -p "$out"
        local emit_flags=() build_flags=()
        case "$axis" in
            packed) emit_flags=(--reflection-metadata 'ReflectionSignatureTypesSubset.Shapes=packed') ;;
            native) emit_flags=(--reflection-metadata 'ReflectionSignatureTypesSubset.Shapes=native') ;;
            uncompressed) emit_flags=(--no-metadata-compression) ;;
            trimmed) emit_flags=(--trim-reflection) ;;
            unshared) emit_flags=(--no-shared-generics); build_flags=(-p:SignatureTypesUnshared=true) ;;
        esac
        dotnet build samples/dotnet/ReflectInvoke/ReflectionSignatureTypesOnly.csproj \
            -c "$CONFIG" --nologo -v q ${build_flags[@]+"${build_flags[@]}"} -o "$out/app"
        run_bounded dotnet "$out/app/ReflectionSignatureTypesOnly.dll" > "$out/clr.raw.stdout"
        strip_cr_win_file "$out/clr.raw.stdout" > "$out/clr.stdout"
        DN2CPP_STRICT_COMPLETION=1 invoke_cli "$out/app/ReflectionSignatureTypesOnly.dll" -r "$(locate_corelib)" \
            --no-ildiet ${emit_flags[@]+"${emit_flags[@]}"} -o "$out" > "$out/emit.log" 2>&1
        compile_console "$out" ReflectionSignatureTypesOnly
        run_bounded "$out/ReflectionSignatureTypesOnly$EXE_EXT" > "$out/native.raw.stdout"
        strip_cr_win_file "$out/native.raw.stdout" > "$out/native.stdout"
        diff -u "$out/clr.stdout" "$out/native.stdout"
        reflection_signature_asserts "$out/native.stdout"
        generic_signature_argument_asserts "$out/native.stdout"
        run_bounded dotnet "$out/app/ReflectionSignatureTypesOnly.dll" before-generic-signature-arguments > "$out/generic-signature-before.clr.raw.stdout"
        run_bounded "$out/ReflectionSignatureTypesOnly$EXE_EXT" before-generic-signature-arguments > "$out/generic-signature-before.native.raw.stdout"
        awk '/^== generic function signature arguments ==$/ { exit } { print }' "$out/native.stdout" > "$out/generic-signature-prefix.stdout"
        diff -u <(strip_cr_win_file "$out/generic-signature-before.clr.raw.stdout") "$out/generic-signature-prefix.stdout"
        diff -u <(strip_cr_win_file "$out/generic-signature-before.native.raw.stdout") "$out/generic-signature-prefix.stdout"
        run_bounded "$out/ReflectionSignatureTypesOnly$EXE_EXT" signature-layout-boundaries > "$out/layout-boundaries.raw.stdout"
        strip_cr_win_file "$out/layout-boundaries.raw.stdout" > "$out/layout-boundaries.stdout"
        run_bounded dotnet "$out/app/ReflectionSignatureTypesOnly.dll" signature-layout-boundaries > "$out/layout-boundaries.clr.raw.stdout"
        diff -u <(strip_cr_win_file "$out/layout-boundaries.clr.raw.stdout") "$out/layout-boundaries.stdout"
        for shape in pointer function; do
            grep -Fxq "signature $shape array allocation: none" "$out/layout-boundaries.stdout"
        done
    done
}
