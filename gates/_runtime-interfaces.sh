#!/usr/bin/env bash
runtime_interfaces_routes_gate() {
 local root="$1";shift
 local fixture="gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll"
 local cli="${DN2CPP_CLI_DLL:-src/Dn2Cpp.Cli/bin/$CONFIG/$TFM/dn2cpp.dll}"
 local original="samples/dotnet/SelfHostPrimSubset/bin/$CONFIG/$TFM" corelib mode axis input out route label
 corelib=$(resolve_net10_corelib) || return $?
 mkdir -p "$root" || return $?
 dotnet "$fixture" --runtime-interfaces-guards "$(dirname "$cli")/Dn2Cpp.Transpiler.dll" "$corelib" "$root/model-input.dll" || return $?
 input="$root/oracle-input";mkdir -p "$input";cp "$original/"* "$input/" || return $?
 dotnet "$fixture" --runtime-interfaces-oracle "$input/SelfHostPrimSubset.dll" "$corelib" || return $?
 gate_capture_ok "$root/oracle" run_bounded dotnet "$input/SelfHostPrimSubset.dll" || return $?
 for mode in all reflection;do
  input="$root/$mode-input";mkdir -p "$input";cp "$original/"* "$input/" || return $?
  local flag=--runtime-interfaces;[[ "$mode" == all ]] || flag+="-$mode"
  dotnet "$fixture" "$flag" "$input/SelfHostPrimSubset.dll" "$corelib" || return $?
  for axis in raw diet;do
   out="$root/$mode-$axis";local flags=("$@");[[ "$axis" != raw ]] || flags+=(--no-ildiet)
   invoke_cli "$input/SelfHostPrimSubset.dll" --auto-ref "${flags[@]}" -o "$out" || return $?
   if [[ "$mode" == reflection ]] && grep -Eq 'Program_(Direct|Pointer|DelegateRoute)_m[0-9]+\(' "$out/generated.h";then echo 'Direct interface roots leaked' >&2;return 1;fi
   cp gates/fixtures/runtime-interfaces.cpp "$out/generated_interfaces_checks.cpp" || return $?
   awk 'BEGIN { print "void dn2cpp_interfaces_boundary_checks();" }
       { print }
       /^[[:space:]]*dn2cpp_runtime_init\(/ { print "    dn2cpp_interfaces_boundary_checks();"; calls++ }
       END { if (calls != 1) exit 1 }' "$out/generated.cpp" > "$out/generated.checked.cpp.tmp" || return $?
   mv "$out/generated.checked.cpp.tmp" "$out/generated.cpp" || return $?
   local runner=()
   if [[ "${WASM:-0}" == 1 ]];then compile_console_wasm "$out" RuntimeInterfaces || return $?;runner=(node "$out/RuntimeInterfaces.js")
   else compile_console "$out" RuntimeInterfaces || return $?;runner=("$out/RuntimeInterfaces$EXE_EXT");fi
   gate_capture_ok "$out/native" run_bounded "${runner[@]}" || return $?
   gate_output witness "$out/native.stderr" "interfaces-native-boundaries:passed" --count 1 --policy crlf || return $?
   local routes='direct calli delegate reflection';[[ "$mode" != reflection ]] || routes=reflection
   for route in $routes;do for label in 0 1 2 3 4 5 6 7 8 formal null empty-unmutated;do
    gate_output witness "$out/native.stdout" "interfaces-$route-$label:True" --count 1 --policy crlf || return $?
   done;done
   # Preserve unsorted CLR/native sequence evidence. The native retained-row engine
   # has ordering/substitution limits; private results must match that public engine.
   grep '^interfaces-sequence-' "$out/native.stdout" > "$out/native-sequence.txt" || return $?
   grep '^interfaces-sequence-' "$root/oracle.stdout" > "$root/clr-sequence.txt" || return $?
   sed '/^interfaces-sequence-/,$d' "$root/oracle.stdout" > "$root/oracle-parity.txt" || return $?
   gate_output prefix "$out/native.stdout" "$root/oracle-parity.txt" --before 'interfaces-sequence-0:' --policy crlf || return $?
   if [[ -n "${DN2CPP_INTERFACES_NATIVE_BASELINE:-}" ]]; then
    gate_output compare "$DN2CPP_INTERFACES_NATIVE_BASELINE" "$out/native-sequence.txt" --policy crlf || return $?
   fi
  done
 done
 input="$root/noalloc-input";mkdir -p "$input";cp "$original/"* "$input/" || return $?
 dotnet "$fixture" --runtime-interfaces-noalloc "$input/SelfHostPrimSubset.dll" "$corelib" || return $?
 for axis in raw diet;do
  local flags=("$@");[[ "$axis" != raw ]] || flags+=(--no-ildiet)
  gate_capture "$root/noalloc-$axis" invoke_cli "$input/SelfHostPrimSubset.dll" --auto-ref "${flags[@]}" -o "$root/noalloc-$axis-output" || return $?
  gate_output status "$(cat "$root/noalloc-$axis.status")" 2 || return $?
  gate_output witness "$root/noalloc-$axis.stderr" 'error: [HotPath(NoAlloc)] closure of RuntimeInterfacesSubset.Program::Direct allocates: RuntimeInterfacesSubset.Program::Direct emits dn2cpp_runtime_type_get_interfaces(' --count 1 --policy crlf || return $?
 done
}
# The raw sibling remains import-owned. Transpile only: never link or execute it.
# baseline_cli must be the pre-adapter published compiler with its reference siblings.
runtime_interfaces_raw_import_gate() {
    local root="$1" baseline_cli="$2" corelib route binding axis input out compiler side status
    local fixture="gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll"
    local current_cli="${DN2CPP_CLI_DLL:-src/Dn2Cpp.Cli/bin/$CONFIG/$TFM/dn2cpp.dll}"
    [[ -f "$baseline_cli" ]] || { echo 'Raw import authority requires the pre-adapter compiler baseline' >&2; return 1; }
    corelib=$(resolve_net10_corelib) || return $?
    for route in direct address; do
        input="$root/$route-input"; mkdir -p "$input"
        cp "samples/dotnet/SelfHostPrimSubset/bin/$CONFIG/$TFM/"* "$input/" || return $?
        local flag=--runtime-interfaces-raw-import
        [[ "$route" != address ]] || flag+=-address
        dotnet "$fixture" "$flag" "$input/SelfHostPrimSubset.dll" "$corelib" || return $?
        for binding in default direct; do
            for axis in raw diet; do
                local flags=() bind=()
                [[ "$axis" != raw ]] || flags+=(--no-ildiet)
                [[ "$binding" != direct ]] || bind+=(--direct-pinvoke 'QCall!RuntimeTypeHandle_GetInterfaces')
                for side in baseline current; do
                    compiler="$baseline_cli"; [[ "$side" != current ]] || compiler="$current_cli"
                    out="$root/$route-$binding-$axis-$side"
                    gate_capture "$out/emit" run_bounded dotnet "$compiler" "$input/SelfHostPrimSubset.dll" --auto-ref "${flags[@]}" "${bind[@]}" -o "$out/generated" || return $?
                    status=$(cat "$out/emit.status")
                    [[ "$status" == 0 || "$status" == 2 ]] || return 1
                    if grep -Fq 'dn2cpp_runtime_type_get_interfaces(' "$out/generated"/generated*.cpp 2>/dev/null; then
                        echo 'Managed GetInterfaces boundary hijacked raw import' >&2; return 1
                    fi
                    if [[ "$status" == 2 ]]; then
                        grep '^error:' "$out/emit.stderr" > "$out/import-error.txt" || return $?
                        grep -Eq 'GetInterfaces|RuntimeTypeHandle' "$out/import-error.txt" || return $?
                    elif [[ "$binding" == direct ]]; then
                        gate_output witness "$out/generated/pinvoke-symbols.txt" $'QCall\tRuntimeTypeHandle_GetInterfaces' --count 1 --policy crlf || return $?
                        gate_output witness "$out/generated/pinvoke-libs.txt" QCall --count 1 --policy crlf || return $?
                    fi
                done
                local before="$root/$route-$binding-$axis-baseline" after="$root/$route-$binding-$axis-current"
                gate_output status "$(cat "$after/emit.status")" "$(cat "$before/emit.status")" || return $?
                if [[ $(cat "$after/emit.status") == 2 ]]; then
                    gate_output compare "$before/import-error.txt" "$after/import-error.txt" --policy crlf || return $?
                else
                    # Preserve manifest presence and contents, including the unbound policy.
                    local manifest
                    for manifest in pinvoke-symbols.txt pinvoke-libs.txt; do
                        if [[ -f "$before/generated/$manifest" ]]; then
                            gate_output compare "$before/generated/$manifest" "$after/generated/$manifest" --policy crlf || return $?
                        else [[ ! -e "$after/generated/$manifest" ]] || return 1; fi
                    done
                fi
            done
        done
    done
}
# Compile-only malformed MemberRefs; never run these artifacts.
runtime_interfaces_negative_gate() {
 local root="$1" corelib mode axis input out
 local fixture="gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll"
 corelib=$(resolve_net10_corelib) || return $?
 for mode in return return-byref return-modifier argument argument-byref argument-modifier instance generic address-return address-return-modifier address-argument-modifier address-generic;do
  input="$root/$mode-input";mkdir -p "$input";cp "samples/dotnet/SelfHostPrimSubset/bin/$CONFIG/$TFM/"* "$input/" || return $?
  dotnet "$fixture" --runtime-interfaces-negative "$input/SelfHostPrimSubset.dll" "$corelib" "$mode" || return $?
  for axis in raw diet;do
   out="$root/$mode-$axis";local flags=();[[ "$axis" != raw ]] || flags+=(--no-ildiet)
   gate_capture "$out/emit" invoke_cli "$input/SelfHostPrimSubset.dll" --auto-ref "${flags[@]}" -o "$out/generated" || return $?
   gate_output status "$(cat "$out/emit.status")" 2 || return $?
   gate_output match-record "$out/emit.stderr" '.*RuntimeTypeHandle interfaces query requires its exact MemberRef signature.*' --count any --policy crlf || return $?
  done
 done
}
