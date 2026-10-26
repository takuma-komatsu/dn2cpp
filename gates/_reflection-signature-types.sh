#!/usr/bin/env bash
# Signature Types and closed generic arguments retain function-pointer identity.
generic_signature_argument_asserts() {
    local output="$1" line
    reflection_assert_lines "$output" 1 \
        '== generic function signature arguments ==' \
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
        'generic function signature arguments end'
}

reflection_signature_asserts() {
    local output="$1" line
    reflection_assert_lines "$output" 1 \
        '== reflection signature type handles ==' \
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
        'reflection signature type handles end'
}

gate_reflection_signature_prefix_asserts() {
    local out="$1" axis
    for axis in dotnet native; do
        if reflection_full_profile; then
            if [ "$axis" = dotnet ]; then
                gate_capture_ok "$out/signature-prefix.$axis.raw" reflection_oracle "$_CG_APP" before-reflection-signature-types
            else
                gate_capture_ok "$out/signature-prefix.$axis.raw" run_bounded "$out/ReflectInvoke$EXE_EXT" before-reflection-signature-types
            fi
            gate_output prefix "$out/virtual-delegate-full.$axis.raw.stdout" "$out/signature-prefix.$axis.raw.stdout" --before '== reflection signature type handles ==' --policy crlf
        fi
        reflection_signature_asserts "$out/virtual-delegate-full.$axis.raw.stdout"
        if reflection_full_profile; then
            if [ "$axis" = dotnet ]; then
                gate_capture_ok "$out/generic-signature-before.$axis.raw" reflection_oracle "$_CG_APP" before-generic-signature-arguments
            else
                gate_capture_ok "$out/generic-signature-before.$axis.raw" run_bounded "$out/ReflectInvoke$EXE_EXT" before-generic-signature-arguments
            fi
            gate_output prefix "$out/virtual-delegate-full.$axis.raw.stdout" "$out/generic-signature-before.$axis.raw.stdout" --before '== generic function signature arguments ==' --policy crlf
        fi
        generic_signature_argument_asserts "$out/virtual-delegate-full.$axis.raw.stdout"
    done
    if reflection_full_profile; then
        gate_output compare "$out/signature-prefix.dotnet.raw.stdout" "$out/signature-prefix.native.raw.stdout"
    fi
}

reflection_signature_diff_axes() {
    local axis out
    local axes=(packed native uncompressed trimmed unshared)
    if ! reflection_full_profile; then axes=(uncompressed); fi
    for axis in "${axes[@]}"; do
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
        reflection_build_once samples/dotnet/ReflectInvoke/ReflectionSignatureTypesOnly.csproj "$out/app" ${build_flags[@]+"${build_flags[@]}"}
        gate_capture_ok "$out/clr.raw" reflection_oracle "$out/app/ReflectionSignatureTypesOnly.dll"
        DN2CPP_STRICT_COMPLETION=1 invoke_cli "$out/app/ReflectionSignatureTypesOnly.dll" -r "$(locate_corelib)" \
            --no-ildiet --link-xml samples/dotnet/ReflectInvoke/keep-signature-shapes.xml \
            ${emit_flags[@]+"${emit_flags[@]}"} -o "$out" > "$out/emit.log" 2>&1
        compile_console "$out" ReflectionSignatureTypesOnly
        gate_capture_ok "$out/native.raw" run_bounded "$out/ReflectionSignatureTypesOnly$EXE_EXT"
        gate_output compare "$out/clr.raw.stdout" "$out/native.raw.stdout"
        reflection_signature_asserts "$out/native.raw.stdout"
        generic_signature_argument_asserts "$out/native.raw.stdout"
        gate_capture_ok "$out/generic-signature-before.clr.raw" reflection_oracle "$out/app/ReflectionSignatureTypesOnly.dll" before-generic-signature-arguments
        gate_capture_ok "$out/generic-signature-before.native.raw" run_bounded "$out/ReflectionSignatureTypesOnly$EXE_EXT" before-generic-signature-arguments
        gate_output prefix "$out/native.raw.stdout" "$out/generic-signature-before.clr.raw.stdout" --before '== generic function signature arguments ==' --policy crlf
        gate_output prefix "$out/native.raw.stdout" "$out/generic-signature-before.native.raw.stdout" --before '== generic function signature arguments ==' --policy crlf
        gate_capture_ok "$out/layout-boundaries.raw" run_bounded "$out/ReflectionSignatureTypesOnly$EXE_EXT" signature-layout-boundaries
        gate_capture_ok "$out/layout-boundaries.clr.raw" reflection_oracle "$out/app/ReflectionSignatureTypesOnly.dll" signature-layout-boundaries
        gate_output compare "$out/layout-boundaries.raw.stdout" "$out/layout-boundaries.clr.raw.stdout"
        for shape in pointer function; do
            gate_output witness "$out/layout-boundaries.raw.stdout" "signature $shape array allocation: none" --count any --policy crlf
        done
    done
}
