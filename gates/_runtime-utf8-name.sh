#!/usr/bin/env bash
runtime_utf8_name_routes_gate() {
 local root="$1";shift
 local fixture="gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll"
 local cli="${DN2CPP_CLI_DLL:-src/Dn2Cpp.Cli/bin/$CONFIG/$TFM/dn2cpp.dll}"
 local original="samples/dotnet/SelfHostPrimSubset/bin/$CONFIG/$TFM" corelib mode axis input out route label
 corelib=$(resolve_net10_corelib) || return $?
 mkdir -p "$root" || return $?
 dotnet "$fixture" --runtime-utf8-name-guards "$(dirname "$cli")/Dn2Cpp.Transpiler.dll" "$corelib" "$root/model-input.dll" || return $?
 input="$root/oracle-input";mkdir -p "$input";cp "$original/"* "$input/" || return $?
 dotnet "$fixture" --runtime-utf8-name-oracle "$input/SelfHostPrimSubset.dll" "$corelib" || return $?
 gate_capture_ok "$root/oracle" run_bounded dotnet "$input/SelfHostPrimSubset.dll" || return $?
 for mode in direct all reflection noalloc;do
  input="$root/$mode-input";mkdir -p "$input";cp "$original/"* "$input/" || return $?
  local flag=--runtime-utf8-name;[[ "$mode" == all ]] || flag+="-$mode"
  dotnet "$fixture" "$flag" "$input/SelfHostPrimSubset.dll" "$corelib" || return $?
  for axis in raw diet;do
   out="$root/$mode-$axis";local flags=("$@");[[ "$axis" != raw ]] || flags+=(--no-ildiet)
   invoke_cli "$input/SelfHostPrimSubset.dll" --auto-ref "${flags[@]}" -o "$out" || return $?
   if [[ "$mode" == reflection ]] && grep -Eq 'Program_(Direct|Pointer|DelegateRoute)_m[0-9]+\(' "$out/generated.h";then echo 'Direct UTF8-name roots leaked' >&2;return 1;fi
   if [[ "$mode" == direct ]] && grep -Eq 'Program_(Address|Pointer|DelegateRoute|Reflect)_m[0-9]+\(' "$out/generated.h";then
    echo 'Address/reflection roots leaked into direct-only UTF8 name fixture' >&2;return 1
   fi
   cp gates/fixtures/runtime-utf8-name.cpp "$out/generated_utf8_name_checks.cpp" || return $?
   awk 'BEGIN { print "void dn2cpp_utf8_name_boundary_checks();" }
       { print }
       /^[[:space:]]*dn2cpp_runtime_init\(/ { print "    dn2cpp_utf8_name_boundary_checks();"; calls++ }
       END { if (calls != 1) exit 1 }' "$out/generated.cpp" > "$out/generated.checked.cpp.tmp" || return $?
   mv "$out/generated.checked.cpp.tmp" "$out/generated.cpp" || return $?
   local runner=()
   if [[ "${WASM:-0}" == 1 ]];then compile_console_wasm "$out" RuntimeUtf8Name || return $?;runner=(node "$out/RuntimeUtf8Name.js")
   else compile_console "$out" RuntimeUtf8Name || return $?;runner=("$out/RuntimeUtf8Name$EXE_EXT");fi
   gate_capture_ok "$out/native" run_bounded "${runner[@]}" || return $?
   gate_output witness "$out/native.stderr" "utf8-name-native-boundaries:passed" --count 1 --policy crlf || return $?
   local routes='direct calli delegate reflection';[[ "$mode" != reflection ]] || routes=reflection;[[ "$mode" != direct ]] || routes=direct
   for route in $routes;do for label in 0 1 2 3 4 5 6 7 8 9 10 11;do
    gate_output witness "$out/native.stdout" "utf8-name-$route-$label:True" --count 1 --policy crlf || return $?
   done;done
   if [[ "$mode" != direct ]];then
   gate_output witness "$out/native.stdout" 'utf8-name-reflection-shape:True' --count 1 --policy crlf || return $?
   fi
   # Public Name succeeds; private pointer-bearing value creation is unsupported.
   local marker=utf8-name-direct-0:True
   [[ "$mode" != reflection ]] || marker=utf8-name-reflection-shape:True
   gate_output prefix "$out/native.stdout" "$root/oracle.stdout" --before "$marker" --policy crlf || return $?
  done
 done
}
# Compile-only malformed MemberRefs; never run these artifacts.
runtime_utf8_name_negative_gate() {
 local root="$1" corelib mode axis input out
 local fixture="gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll"
 corelib=$(resolve_net10_corelib) || return $?
 for mode in return class-return byref modifier argument argument-byref instance generic address-class-return address-modifier address-generic;do
  input="$root/$mode-input";mkdir -p "$input";cp "samples/dotnet/SelfHostPrimSubset/bin/$CONFIG/$TFM/"* "$input/" || return $?
  dotnet "$fixture" --runtime-utf8-name-negative "$input/SelfHostPrimSubset.dll" "$corelib" "$mode" || return $?
  for axis in raw diet;do
   out="$root/$mode-$axis";local flags=();[[ "$axis" != raw ]] || flags+=(--no-ildiet)
   gate_capture "$out/emit" invoke_cli "$input/SelfHostPrimSubset.dll" --auto-ref "${flags[@]}" -o "$out/generated" || return $?
   if [[ "$axis" == diet && ( "$mode" == class-return || "$mode" == address-class-return ) ]]; then
    # Cecil may normalize CLASS to the real VALUETYPE. Prove the repaired signature,
    # rather than treating successful normalized input as malformed-input admission.
    gate_output status "$(cat "$out/emit.status")" 0 || return $?
    local normalized=()
    mapfile -t normalized < <(find "$out/generated/ildiet" -name SelfHostPrimSubset.dll)
    [[ "${#normalized[@]}" == 1 ]] || return 1
    gate_capture_ok "$out/normalization" run_bounded dotnet "$fixture" --runtime-utf8-name-normalization \
     "$input/SelfHostPrimSubset.dll" "${normalized[0]}" || return $?
    gate_output witness "$out/normalization.stdout" 'utf8-name-class-return-normalization:verified' --count 1 --policy crlf || return $?
   else
    gate_output status "$(cat "$out/emit.status")" 2 || return $?
    gate_output match-record "$out/emit.stderr" '.*RuntimeTypeHandle UTF8-name query requires its exact MemberRef signature.*' --count any --policy crlf || return $?
   fi
  done
 done
}
