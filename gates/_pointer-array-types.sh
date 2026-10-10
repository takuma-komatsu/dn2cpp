#!/usr/bin/env bash
# Pointer and function-pointer array layouts retain full signature identity and validation.
pointer_array_asserts() {
    local output="$1" line
    for line in '== pointer array types ==' 'pointer array types end' \
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
        'signature array MD clone clear: 4656/0'; do
        test "$(grep -Fxc -- "$line" "$output")" = 1 \
            || { echo "FAIL: pointer-array block witness missing or repeated: $output/$line" >&2; return 1; }
    done
}

pointer_array_isolated_routes() {
    local route out witness
    for route in from create jagged; do
        out="artifacts/reflect-invoke-pointer-array-route-$route"
        mkdir -p "$out"
        dotnet build samples/dotnet/ReflectInvoke/PointerArrayTypesOnly.csproj \
            -c "$CONFIG" --nologo -v q -p:PointerArrayRoute="$route" -o "$out/app"
        run_bounded dotnet "$out/app/PointerArrayTypesOnly.dll" > "$out/clr.raw.stdout"
        strip_cr_win_file "$out/clr.raw.stdout" > "$out/clr.stdout"
        DN2CPP_STRICT_COMPLETION=1 invoke_cli "$out/app/PointerArrayTypesOnly.dll" -r "$(locate_corelib)" \
            --no-ildiet -o "$out" > "$out/emit.log" 2>&1
        compile_console "$out" PointerArrayTypesOnly
        run_bounded "$out/PointerArrayTypesOnly$EXE_EXT" > "$out/native.raw.stdout"
        strip_cr_win_file "$out/native.raw.stdout" > "$out/native.stdout"
        diff -u "$out/clr.stdout" "$out/native.stdout"
        if [ "$route" = from ]; then
            witness='signature array composed FromArrayType: True/True'
        elif [ "$route" = create ]; then
            witness='signature array composed CreateInstance: True/True'
        else
            witness='signature array isolated function jagged: True/True'
        fi
        test "$(grep -Fxc -- "$witness" "$out/native.stdout")" = 1
    done
}

pointer_array_diff_axes() {
    pointer_array_isolated_routes
    local axis out
    for axis in packed native uncompressed trimmed unshared; do
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
        dotnet build samples/dotnet/ReflectInvoke/PointerArrayTypesOnly.csproj \
            -c "$CONFIG" --nologo -v q -o "$out/app"
        run_bounded dotnet "$out/app/PointerArrayTypesOnly.dll" > "$out/clr.raw.stdout"
        strip_cr_win_file "$out/clr.raw.stdout" > "$out/clr.stdout"
        DN2CPP_STRICT_COMPLETION=1 invoke_cli "$out/app/PointerArrayTypesOnly.dll" -r "$(locate_corelib)" \
            --no-ildiet ${flags[@]+"${flags[@]}"} -o "$out" > "$out/emit.log" 2>&1
        compile_console "$out" PointerArrayTypesOnly
        run_bounded "$out/PointerArrayTypesOnly$EXE_EXT" > "$out/native.raw.stdout"
        strip_cr_win_file "$out/native.raw.stdout" > "$out/native.stdout"
        diff -u "$out/clr.stdout" "$out/native.stdout"
        pointer_array_asserts "$out/native.stdout"
    done
}

gate_pointer_array_prefix_asserts() {
    local out="$1" axis
    for axis in dotnet native; do
        if [ "$axis" = dotnet ]; then
            run_bounded dotnet "$_CG_APP" before-pointer-array-types > "$out/pointer-array-prefix.$axis.raw.stdout"
        else
            run_bounded "$out/ReflectInvoke$EXE_EXT" before-pointer-array-types > "$out/pointer-array-prefix.$axis.raw.stdout"
        fi
        strip_cr_win_file "$out/pointer-array-prefix.$axis.raw.stdout" > "$out/pointer-array-prefix.$axis.stdout"
        awk '/^== pointer array types ==$/ { exit } { print }' \
            "$out/virtual-delegate-full.$axis.stdout" > "$out/pointer-array-old.$axis.stdout"
        diff -u "$out/pointer-array-prefix.$axis.stdout" "$out/pointer-array-old.$axis.stdout"
        pointer_array_asserts "$out/virtual-delegate-full.$axis.stdout"
    done
    diff -u "$out/pointer-array-prefix.dotnet.stdout" "$out/pointer-array-prefix.native.stdout"
}
