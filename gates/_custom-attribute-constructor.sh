#!/usr/bin/env bash
# Public CAD base IL vs virtual native rows: constructor identity is not fabricated.
custom_attribute_constructor_routes_gate() {
    local root="$1"; shift
    local fixture="gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll"
    local cli="${DN2CPP_CLI_DLL:-src/Dn2Cpp.Cli/bin/$CONFIG/$TFM/dn2cpp.dll}"
    local original="samples/dotnet/SelfHostPrimSubset/bin/$CONFIG/$TFM" corelib mode axis input out label
    corelib=$(resolve_net10_corelib) || return $?
    dotnet "$fixture" --cad-constructor-guards "$(dirname "$cli")/Dn2Cpp.Transpiler.dll" "$corelib" || return $?
    for mode in public-oracle oracle base-oracle; do
        input="$root/$mode-input";mkdir -p "$input";cp "$original/"* "$input/" || return $?
        dotnet "$fixture" "--cad-constructor-$mode" "$input/SelfHostPrimSubset.dll" "$corelib" || return $?
        gate_capture_ok "$root/$mode" run_bounded dotnet "$input/SelfHostPrimSubset.dll" || return $?
    done
    for label in clr-exact-0 clr-exact-1 clr-invoke-0 clr-invoke-1 clr-no-construction selection-base selection-override selection-control-string selection-null custom-base custom-override custom-reflection custom-method reflection-null-target; do
        gate_output witness "$root/oracle.stdout" "cad-constructor-$label:True" --count 1 --policy crlf || return $?
    done
    for label in base-direct base-calli base-delegate base-function base-method base-dynamic closed-null-base; do
        gate_output witness "$root/base-oracle.stdout" "cad-constructor-$label:True" --count 1 --policy crlf || return $?
    done
    for mode in all reflection noalloc-base; do
        input="$root/$mode-input";mkdir -p "$input";cp "$original/"* "$input/" || return $?
        local flag=--cad-constructor;[[ "$mode" == all ]] || flag+="-$mode"
        dotnet "$fixture" "$flag" "$input/SelfHostPrimSubset.dll" "$corelib" || return $?
        for axis in raw diet; do
            out="$root/$mode-$axis";local flags=("$@");[[ "$axis" != raw ]] || flags+=(--no-ildiet)
            invoke_cli "$input/SelfHostPrimSubset.dll" --auto-ref "${flags[@]}" -o "$out" || return $?
            if [[ "$mode" == reflection ]]; then
                grep -Fq 'dn2cpp_attrdata_constructor_invoker' "$out"/generated*.cpp "$out/generated.h" || return $?
            else
                grep -Fq 'dn2cpp_attrdata_constructor_virtual(' "$out"/generated*.cpp "$out/generated.h" || return $?
            fi
            if [[ "$mode" == reflection ]] && grep -Eq 'ConstructorChecks_(BaseDirect|BasePointer|VirtualPointer|VirtualAddress)_m[0-9]+\(' "$out/generated.h"; then
                echo 'Direct CAD constructor routes leaked into reflection-only fixture' >&2;return 1
            fi
            # Used-slot reachability must not root every subclass or every virtual on an allocated class.
            if grep -Eq '(UnallocatedData_get_Constructor|OverrideData_UnrelatedVirtual)_m[0-9]+\(' "$out/generated.h"; then
                echo 'CAD constructor slot broadened unrelated virtual reachability' >&2;return 1
            fi
            if [[ "$mode" == reflection ]]; then
                grep -Eq 'OverrideData_get_Constructor_m[0-9]+\(' "$out/generated.h" || {
                    echo 'Reflection-only CAD invocation lost the allocated override body' >&2;return 1;
                }
            fi
            cp gates/fixtures/custom-attribute-constructor.cpp "$out/generated_cad_ctor_checks.cpp" || return $?
            awk '
                BEGIN { print "void dn2cpp_cad_constructor_boundary_checks();" }
                { print }
                /^[[:space:]]*dn2cpp_runtime_init\(/ { print "    dn2cpp_cad_constructor_boundary_checks();"; calls++ }
                END { if (calls != 1) exit 1 }
            ' "$out/generated.cpp" > "$out/generated.checked.cpp.tmp" || return $?
            mv "$out/generated.checked.cpp.tmp" "$out/generated.cpp" || return $?
            local runner=()
            if [[ "${WASM:-0}" == 1 ]];then compile_console_wasm "$out" CadConstructor || return $?;runner=(node "$out/CadConstructor.js")
            else compile_console "$out" CadConstructor || return $?;runner=("$out/CadConstructor$EXE_EXT");fi
            gate_capture_ok "$out/native" run_bounded "${runner[@]}" || return $?
            gate_output witness "$out/native.stderr" "cad-constructor-native-boundaries:passed" --count 1 --policy crlf || return $?
            if [[ "$mode" != reflection ]];then
                gate_output prefix "$out/native.stdout" "$root/public-oracle.stdout" --before 'cad-constructor-base-direct:True' --policy crlf || return $?
                for label in base-direct base-calli base-delegate base-function base-method base-dynamic closed-null-base custom-base custom-override custom-method selection-base selection-override selection-control-string selection-null selection-canonical-custom canonical-method-0 canonical-method-1 existing-data-0 existing-data-1 null-virtual policy-no-construction canonical-virtual-0 canonical-virtual-1 canonical-calli-0 canonical-calli-1 canonical-delegate-0 canonical-delegate-1 canonical-dynamic-0 canonical-dynamic-1; do
                    gate_output witness "$out/native.stdout" "cad-constructor-$label:True" --count 1 --policy crlf || return $?
                done
            fi
            for label in reflection-null-target custom-reflection canonical-reflection-0 canonical-reflection-1;do
                gate_output witness "$out/native.stdout" "cad-constructor-$label:True" --count 1 --policy crlf || return $?
            done
        done
    done
}
custom_attribute_constructor_negative_gate() {
    local root="$1" corelib mode input
    local fixture="gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll"
    corelib=$(resolve_net10_corelib) || return $?
    for mode in return-member return-object static argument generic modifier address-return-member address-modifier; do
        input="$root/$mode-input";mkdir -p "$input";cp "samples/dotnet/SelfHostPrimSubset/bin/$CONFIG/$TFM/"* "$input/" || return $?
        dotnet "$fixture" --cad-constructor-negative "$input/SelfHostPrimSubset.dll" "$corelib" "$mode" || return $?
        gate_capture "$root/$mode" invoke_cli "$input/SelfHostPrimSubset.dll" --auto-ref --no-ildiet -o "$root/$mode-output" || return $?
        gate_output status "$(cat "$root/$mode.status")" 2 || return $?
        gate_output match-record "$root/$mode.stderr" '.*(CustomAttributeData|get_Constructor).*' --count any --policy crlf || return $?
    done
    input="$root/noalloc-virtual-input";mkdir -p "$input";cp "samples/dotnet/SelfHostPrimSubset/bin/$CONFIG/$TFM/"* "$input/" || return $?
    dotnet "$fixture" --cad-constructor-noalloc-virtual "$input/SelfHostPrimSubset.dll" "$corelib" || return $?
    gate_capture "$root/noalloc-virtual" invoke_cli "$input/SelfHostPrimSubset.dll" --auto-ref --no-ildiet -o "$root/noalloc-virtual-output" || return $?
    gate_output status "$(cat "$root/noalloc-virtual.status")" 2 || return $?
    gate_output match-record "$root/noalloc-virtual.stderr" '.*NoAlloc.*dn2cpp_attrdata_constructor_virtual.*' --count any --policy crlf
}
