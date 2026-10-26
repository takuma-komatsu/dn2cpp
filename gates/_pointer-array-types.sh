#!/usr/bin/env bash
# Pointer and function-pointer array layouts retain full signature identity and validation.
pointer_array_asserts() {
    local output="$1" line
    reflection_assert_lines "$output" 1 \
        '== pointer array types ==' 'pointer array types end' \
        'pointer array rejected entries: 0' 'pointer array null entries: 1' \
        'pointer array allocation: System.Int32*[]/2/4656/True' \
        'function array allocation: System.Int32()[]/2/9024/True' \
        'pointer array jagged: System.Int32*[][]/1/4656/True' \
        'signature array identities: True/True/True/True/True/True' \
        'signature array generic: True/True/True/True/True' \
        'signature array empty: True/True/True' \
        'signature array return: True/True/True' \
        'signature array byref writeback: 1/17760' \
        'signature array function writeback: 1/26496' \
        'signature array fields: True/True/True/True' \
        'signature array dynamic convention: True' \
        'signature array dynamic jagged: True/True' \
        'signature array pinned data: 4656' \
        'signature array MD default: 0/0/0/0' \
        'signature array byte length: ArgumentException' \
        'signature array MD clone clear: 4656/0'
}

pointer_array_isolated_routes() {
    local route out witness
    for route in from create jagged; do
        out="artifacts/reflect-invoke-pointer-array-route-$route"
        mkdir -p "$out"
        reflection_build_once samples/dotnet/ReflectInvoke/PointerArrayTypesOnly.csproj "$out/app" -p:PointerArrayRoute="$route"
        gate_capture_ok "$out/clr.raw" reflection_oracle "$out/app/PointerArrayTypesOnly.dll"
        DN2CPP_STRICT_COMPLETION=1 invoke_cli "$out/app/PointerArrayTypesOnly.dll" -r "$(locate_corelib)" \
            --no-ildiet -o "$out" > "$out/emit.log" 2>&1
        compile_console "$out" PointerArrayTypesOnly
        gate_capture_ok "$out/native.raw" run_bounded "$out/PointerArrayTypesOnly$EXE_EXT"
        gate_output compare "$out/clr.raw.stdout" "$out/native.raw.stdout"
        if [ "$route" = from ]; then
            witness='signature array composed FromArrayType: True/True'
        elif [ "$route" = create ]; then
            witness='signature array composed CreateInstance: True/True'
        else
            witness='signature array isolated function jagged: True/True'
        fi
        gate_output witness "$out/native.raw.stdout" "$witness" --count 1 --policy crlf
    done
}

pointer_array_diff_axes() {
    pointer_array_isolated_routes
    local axis out
    local axes=(packed native uncompressed trimmed unshared)
    if ! reflection_full_profile; then axes=(unshared); fi
    for axis in "${axes[@]}"; do
        out="artifacts/reflect-invoke-pointer-arrays-$axis"
        mkdir -p "$out"
        local flags=()
        case "$axis" in
            packed) flags=(--reflection-metadata 'ReflectInvokeValidationSubset.PointerArrayTarget=packed') ;;
            native) flags=(--reflection-metadata 'ReflectInvokeValidationSubset.PointerArrayTarget=native') ;;
            uncompressed) flags=(--no-metadata-compression) ;;
            trimmed) flags=(--trim-reflection) ;;
            unshared) flags=(--no-shared-generics) ;;
        esac
        reflection_build_once samples/dotnet/ReflectInvoke/PointerArrayTypesOnly.csproj "$out/app"
        gate_capture_ok "$out/clr.raw" reflection_oracle "$out/app/PointerArrayTypesOnly.dll"
        DN2CPP_STRICT_COMPLETION=1 invoke_cli "$out/app/PointerArrayTypesOnly.dll" -r "$(locate_corelib)" \
            --no-ildiet ${flags[@]+"${flags[@]}"} -o "$out" > "$out/emit.log" 2>&1
        compile_console "$out" PointerArrayTypesOnly
        gate_capture_ok "$out/native.raw" run_bounded "$out/PointerArrayTypesOnly$EXE_EXT"
        gate_output compare "$out/clr.raw.stdout" "$out/native.raw.stdout"
        pointer_array_asserts "$out/native.raw.stdout"
    done
}

gate_pointer_array_prefix_asserts() {
    local out="$1" axis
    for axis in dotnet native; do
        if reflection_full_profile; then
            if [ "$axis" = dotnet ]; then
                gate_capture_ok "$out/pointer-array-prefix.$axis.raw" reflection_oracle "$_CG_APP" before-pointer-array-types
            else
                gate_capture_ok "$out/pointer-array-prefix.$axis.raw" run_bounded "$out/ReflectInvoke$EXE_EXT" before-pointer-array-types
            fi
            gate_output prefix "$out/virtual-delegate-full.$axis.raw.stdout" "$out/pointer-array-prefix.$axis.raw.stdout" --before '== pointer array types ==' --policy crlf
        fi
        pointer_array_asserts "$out/virtual-delegate-full.$axis.raw.stdout"
    done
    if reflection_full_profile; then
        gate_output compare "$out/pointer-array-prefix.dotnet.raw.stdout" "$out/pointer-array-prefix.native.raw.stdout"
    fi
}
