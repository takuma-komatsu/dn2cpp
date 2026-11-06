#!/usr/bin/env bash
# Field-free raw-wait wrappers retain the existing QCall capability and binding policy.
wait_handle_raw_wait_gate() {
    local root="${1:-artifacts/wait-handle-raw-wait-$CONFIG}" corelib fixture mode selector input out route public
    corelib=$(locate_corelib)
    fixture="gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll"
    mkdir -p "$root/app" "$root/public"
    dotnet build samples/dotnet/ThreadingPrimitives/ThreadingPrimitives.csproj \
        -c "$CONFIG" --nologo -v q -p:BuildInParallel=false -o "$root/app" || return $?
    build_gate_proj gates/fixtures/ldftn-local/LdftnLocalFixture.csproj || return $?
    dotnet "$fixture" --wait-handle-raw-wait-guards \
        "src/Dn2Cpp.Cli/bin/$CONFIG/$TFM/Dn2Cpp.Transpiler.dll" "$corelib" \
        "src/ILDiet/bin/$CONFIG/$TFM/ILDiet.dll" "$root/app/ThreadingPrimitives.dll" || return $?
    cp "$root/app/"* "$root/public/" || return $?
    dotnet "$fixture" --wait-handle-raw-wait-public "$root/public/ThreadingPrimitives.dll" "$corelib" || return $?
    gate_capture_ok "$root/public/oracle" run_bounded dotnet "$root/public/ThreadingPrimitives.dll" || return $?
    for selector in boundary shim; do
        input="$root/$selector-input"
        mkdir -p "$input"
        cp "$root/app/"* "$input/" || return $?
        local rewrite=--wait-handle-raw-wait
        [ "$selector" != shim ] || rewrite=--wait-handle-raw-wait-shim
        dotnet "$fixture" "$rewrite" "$input/ThreadingPrimitives.dll" "$corelib" || return $?
        for mode in raw diet; do
            out="$root/$selector-$mode"
            local flags=() argv=()
            [ "$mode" == diet ] || flags+=(--no-ildiet)
            if [ "$selector" == shim ]; then
                flags+=(--direct-pinvoke 'QCall!WaitHandle_WaitMultipleIgnoringSyncContext')
                argv=(managed-binding)
            fi
            invoke_cli "$input/ThreadingPrimitives.dll" --auto-ref ${flags[@]+"${flags[@]}"} -o "$out" || return $?
            [ ! -e "$out/pinvoke-libs.txt" ] && [ ! -e "$out/pinvoke-symbols.txt" ] || return 1
            if [ "$mode" == diet ]; then
                gate_python - "$out/ildiet" "$corelib" <<'PY_DIET' || return $?
from pathlib import Path
import sys
sys.path.insert(0, "gates")
from ildiet_manifest import manifest
result, generation = manifest(sys.argv[1])
assemblies = {row.get("name"): row for row in result.findall("assembly")}
assert result.get("fallback") == "none" and result.get("cutsValidated") == "true"
assert assemblies["ThreadingPrimitives"].get("mode") == "rewritten"
assert Path(result.get("input")).name == "ThreadingPrimitives.dll"
assert assemblies["System.Private.CoreLib"].get("mode") == "copied"
assert (generation / "System.Private.CoreLib.dll").read_bytes() == Path(sys.argv[2]).read_bytes()
print("raw-wait-ildiet=rewritten-input,copied-CoreLib,no-fallback,validated-cuts")
PY_DIET
            fi
            compile_console "$out" WaitHandleRawWait || return $?
            gate_capture_ok "$out/native" run_bounded "$out/WaitHandleRawWait$EXE_EXT" ${argv[@]+"${argv[@]}"} || return $?
            gate_output select "$out/native.stdout" "$out/public-prefix.stdout" --before-prefix 'raw-wait-direct3:' --policy crlf || return $?
            gate_output compare "$out/public-prefix.stdout" "$root/public/oracle.stdout" || return $?
            for route in direct3 core3 direct4 pointer3 pointer-core3 pointer4 import-direct import-pointer public-wait; do
                local result=PlatformNotSupportedException:True
                if [ "$selector" == shim ]; then
                    result=managed-binding:2117
                    case "$route" in direct4|pointer4) result=managed-binding:1117 ;; import-direct|import-pointer) result=managed-binding:2517 ;; esac
                fi
                gate_output byte-witness "$out/native.stdout" "raw-wait-$route:$result" --count 1 --policy crlf || return $?
            done
            gate_output byte-witness "$out/native.stdout" 'raw-wait-null-array=ArgumentNullException/waitHandles' --count 1 --policy crlf || return $?
            gate_output byte-witness "$out/native.stdout" 'raw-wait existing QCall boundary end' --count 1 --policy crlf || return $?
        done
    done
}
