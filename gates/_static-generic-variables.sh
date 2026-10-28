#!/usr/bin/env bash
static_generic_variables_routes_gate() {
 local root="$1";shift
 local fixture="gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll"
 local cli="${DN2CPP_CLI_DLL:-src/Dn2Cpp.Cli/bin/$CONFIG/$TFM/dn2cpp.dll}"
 local original="samples/dotnet/SelfHostPrimSubset/bin/$CONFIG/$TFM" corelib mode axis input out route label
 corelib=$(resolve_net10_corelib) || return $?
 mkdir -p "$root" || return $?
 dotnet "$fixture" --static-generic-variables-guards "$(dirname "$cli")/Dn2Cpp.Transpiler.dll" "$corelib" "$root/model-input.dll" || return $?
 input="$root/oracle-input";mkdir -p "$input";cp "$original/"* "$input/" || return $?
 dotnet "$fixture" --static-generic-variables-oracle "$input/SelfHostPrimSubset.dll" "$corelib" || return $?
 gate_capture_ok "$root/oracle" run_bounded dotnet "$input/SelfHostPrimSubset.dll" || return $?
 for mode in all reflection noalloc;do
  input="$root/$mode-input";mkdir -p "$input";cp "$original/"* "$input/" || return $?
  local flag=--static-generic-variables;[[ "$mode" == all ]] || flag+="-$mode"
  dotnet "$fixture" "$flag" "$input/SelfHostPrimSubset.dll" "$corelib" || return $?
  for axis in raw diet;do
   out="$root/$mode-$axis";local flags=("$@");[[ "$axis" != raw ]] || flags+=(--no-ildiet)
   invoke_cli "$input/SelfHostPrimSubset.dll" --auto-ref "${flags[@]}" -o "$out" || return $?
   if [[ "$mode" == reflection ]] && grep -Eq 'Program_(Direct|Pointer|DelegateRoute)_m[0-9]+\(' "$out/generated.h";then echo 'Direct static generic-variable roots leaked' >&2;return 1;fi
   cp gates/fixtures/runtime-static-generic-vars.cpp "$out/generated_static_generic_checks.cpp" || return $?
   awk 'BEGIN { print "void dn2cpp_static_generic_vars_boundary_checks();" }
       { print }
       /^[[:space:]]*dn2cpp_runtime_init\(/ { print "    dn2cpp_static_generic_vars_boundary_checks();"; calls++ }
       END { if (calls != 1) exit 1 }' "$out/generated.cpp" > "$out/generated.checked.cpp.tmp" || return $?
   mv "$out/generated.checked.cpp.tmp" "$out/generated.cpp" || return $?
   local runner=()
   if [[ "${WASM:-0}" == 1 ]];then compile_console_wasm "$out" StaticGenericVariables || return $?;runner=(node "$out/StaticGenericVariables.js")
   else compile_console "$out" StaticGenericVariables || return $?;runner=("$out/StaticGenericVariables$EXE_EXT");fi
   gate_capture_ok "$out/native" run_bounded "${runner[@]}" || return $?
   gate_output witness "$out/native.stderr" "static-generic-vars-native-boundaries:passed" --count 1 --policy crlf || return $?
   local routes='direct calli delegate reflection';[[ "$mode" != reflection ]] || routes=reflection
   for route in $routes;do for label in 0 1 2 3 4 5 6 7 8 9 10 11 12 13 14 15 16 17 18 19 null;do
    gate_output witness "$out/native.stdout" "static-generic-$route-$label:True" --count 1 --policy crlf || return $?
   done;done
   # Public property oracle is separate from private null safety policy.
   gate_output prefix "$out/native.stdout" "$root/oracle.stdout" --before "static-generic-${routes%% *}-0:True" --policy crlf || return $?
  done
 done
}
# Compile-only malformed MemberRefs; never run these artifacts.
static_generic_variables_negative_gate() {
 local root="$1" corelib mode axis input out
 local fixture="gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll"
 corelib=$(resolve_net10_corelib) || return $?
 for mode in return return-byref return-modifier argument argument-byref argument-modifier instance generic address-return address-return-modifier address-argument-modifier address-generic;do
  input="$root/$mode-input";mkdir -p "$input";cp "samples/dotnet/SelfHostPrimSubset/bin/$CONFIG/$TFM/"* "$input/" || return $?
  dotnet "$fixture" --static-generic-variables-negative "$input/SelfHostPrimSubset.dll" "$corelib" "$mode" || return $?
  for axis in raw diet;do
   out="$root/$mode-$axis";local flags=();[[ "$axis" != raw ]] || flags+=(--no-ildiet)
   gate_capture "$out/emit" invoke_cli "$input/SelfHostPrimSubset.dll" --auto-ref "${flags[@]}" -o "$out/generated" || return $?
   gate_output status "$(cat "$out/emit.status")" 2 || return $?
   gate_output match-record "$out/emit.stderr" '.*RuntimeTypeHandle static generic-variable query requires its exact MemberRef signature.*' --count any --policy crlf || return $?
  done
 done
}
