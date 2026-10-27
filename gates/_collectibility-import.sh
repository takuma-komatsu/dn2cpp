#!/usr/bin/env bash
# The private QCall keeps import binding authority under its intrinsic owner.
collectibility_import_gate() {
    local root="${1:-artifacts/collectibility-import-$CONFIG}" corelib input fixture mode out route selector archive
    corelib=$(locate_corelib)
    fixture="gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll"
    mkdir -p "$root/app"
    dotnet build samples/dotnet/ReflectTypes/OrdinaryReflectionTypeLeaves.csproj \
        -c "$CONFIG" --nologo -v q -p:BuildInParallel=false -o "$root/app" || return $?
    build_gate_proj gates/fixtures/ldftn-local/LdftnLocalFixture.csproj || return $?
    input="$root/input"
    mkdir -p "$input"
    cp "$root/app/"* "$input/" || return $?
    dotnet "$fixture" --collectibility-import "$input/OrdinaryReflectionTypeLeaves.dll" "$corelib" || return $?
    local configure=("$CMAKE") cmake_args=()
    if [ -n "${WASM:-}" ]; then configure=(emcmake "$CMAKE")
    elif [ -n "${CMAKE_CXX_COMPILER:-}" ]; then cmake_args+=(-DCMAKE_CXX_COMPILER="$CMAKE_CXX_COMPILER"); fi
    mkdir -p "$root/native-library"
    _cmake_step "$root/native-library/configure.log" 'configuring collectibility import fixture' \
        "${configure[@]}" -S gates/fixtures/collectibility-import -B "$root/native-library" -G Ninja \
        "${cmake_args[@]}" || return $?
    _cmake_step "$root/native-library/build.log" 'building collectibility import fixture' \
        "$CMAKE" --build "$root/native-library" || return $?
    archive=$(cat "$root/native-library/archive.txt")
    [ -f "$archive" ] || return 1
    for mode in raw diet native; do
        local flags=()
        [ "$mode" == diet ] || flags+=(--no-ildiet)
        [ "$mode" != native ] || flags+=(--no-metadata-compression)
        for selector in refusal direct; do
            out="$root/$mode-$selector"
            local binding=() argv=()
            if [ "$selector" == direct ]; then
                binding=(--direct-pinvoke 'QCall!RuntimeTypeHandle_IsCollectible')
                argv=(native-binding)
            fi
            invoke_cli "$input/OrdinaryReflectionTypeLeaves.dll" --auto-ref "${flags[@]}" \
                "${binding[@]}" -o "$out" || return $?
            if [ "$selector" == direct ]; then
                gate_output witness "$out/pinvoke-libs.txt" QCall --count 1 --policy crlf || return $?
                gate_output witness "$out/pinvoke-symbols.txt" $'QCall\tRuntimeTypeHandle_IsCollectible' --count 1 --policy crlf || return $?
                printf '%s\n' "$archive" > "$out/native-assets.txt"
            else
                [ ! -e "$out/pinvoke-libs.txt" ] && [ ! -e "$out/pinvoke-symbols.txt" ] || return 1
            fi
            if [ -n "${WASM:-}" ]; then
                compile_console_wasm "$out" CollectibilityImport || return $?
                gate_capture_ok "$out/native" run_bounded node "$out/CollectibilityImport.js" "${argv[@]}" || return $?
            else
                compile_console "$out" CollectibilityImport || return $?
                gate_capture_ok "$out/native" run_bounded "$out/CollectibilityImport$EXE_EXT" "${argv[@]}" || return $?
            fi
            for route in direct pointer; do
                local label=refusal
                [ "$selector" != direct ] || label=binding
                gate_output byte-witness "$out/native.stdout" "collectibility-import-$route-$label:True" --count 1 --policy crlf || return $?
            done
            gate_output byte-witness "$out/native.stdout" 'collectibility-import-reflection-row-unavailable:True' --count 1 --policy crlf || return $?
            gate_output byte-witness "$out/native.stdout" 'collectibility import routes end' --count 1 --policy crlf || return $?
        done
    done
    # Struct-valued NativeImplementation parameters are an existing rejected ABI.
    # Direct calls keep the detailed mismatch; address wrappers report no forwarder.
    # Both must reject instead of using either successful no-shim route above.
    for route in direct pointer; do
        input="$root/shim-$route-input"
        mkdir -p "$input"
        cp "$root/app/"* "$input/" || return $?
        local fixture_flag=--collectibility-import-shim
        [ "$route" != pointer ] || fixture_flag=--collectibility-import-shim-pointer
        dotnet "$fixture" "$fixture_flag" "$input/OrdinaryReflectionTypeLeaves.dll" "$corelib" || return $?
        for selector in refusal direct; do
            out="$root/shim-$route-$selector"
            local binding=()
            [ "$selector" != direct ] || binding=(--direct-pinvoke 'QCall!RuntimeTypeHandle_IsCollectible')
            gate_capture "$out/emit" invoke_cli "$input/OrdinaryReflectionTypeLeaves.dll" --auto-ref \
                --no-ildiet "${binding[@]}" -o "$out/generated" || return $?
            gate_output status "$(cat "$out/emit.status")" 2 || return $?
            if [ "$route" == direct ]; then
                gate_output match-record "$out/emit.stderr" \
                    'error: .*\[NativeImplementation CollectibilityImportRoutes\.Program\.Managed\]: parameter 0 native shape mismatch.*' \
                    --count 1 --policy crlf || return $?
            else
                gate_output byte-witness "$out/emit.stderr" \
                    "error: taking the address of System.RuntimeTypeHandle::IsCollectible (delegate method group / function pointer over a [DllImport]): the import's shape is not marshallable, so no forwarder body can be synthesized" \
                    --count 1 --policy crlf || return $?
            fi
        done
    done
}
