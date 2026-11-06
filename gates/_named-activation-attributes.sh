#!/usr/bin/env bash
named_activation_attributes_routes_gate() {
 local root="$1";shift
 local fixture="gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll" cli="${DN2CPP_CLI_DLL:-src/Dn2Cpp.Cli/bin/$CONFIG/$TFM/dn2cpp.dll}"
 local original="samples/dotnet/SelfHostPrimSubset/bin/$CONFIG/$TFM" corelib input mode axis out route label
 corelib=$(resolve_net10_corelib) || return $?
 mkdir -p "$root" || return $?
 dotnet "$fixture" --named-attrs-guards "$(dirname "$cli")/Dn2Cpp.Transpiler.dll" "$corelib" "$root/model-input.dll" || return $?
 input="$root/oracle-input";mkdir -p "$input";cp "$original/"* "$input/" || return $?
 dotnet "$fixture" --named-attrs-oracle "$input/SelfHostPrimSubset.dll" "$corelib" || return $?
 gate_capture_ok "$root/oracle" run_bounded dotnet "$input/SelfHostPrimSubset.dll" || return $?
 local labels='null-attrs empty-attrs attrs-not-args attrs-before-missing-ctor empty-missing-ctor wrapped missing-assembly-first null-type-first empty-type-first missing-type-first nullable'
 for label in $labels;do gate_output witness "$root/oracle.stdout" "named-attrs-public-$label:True" --count 1 --policy crlf || return $?;done
 for mode in all reflection planner;do
  input="$root/$mode-input";mkdir -p "$input";cp "$original/"* "$input/" || return $?
  local flag=--named-attrs;[[ "$mode" == all ]] || flag+="-$mode"
  dotnet "$fixture" "$flag" "$input/SelfHostPrimSubset.dll" "$corelib" || return $?
  for axis in raw diet;do
   out="$root/$mode-$axis";local flags=("$@");[[ "$axis" != raw ]] || flags+=(--no-ildiet)
   [[ "$mode" == planner ]] || flags+=(--link-xml gates/fixtures/named-activation-attributes-opaque-ctors.xml)
   invoke_cli "$input/SelfHostPrimSubset.dll" --auto-ref "${flags[@]}" -o "$out" || return $?
   if [[ "$mode" == reflection ]] && grep -Eq 'Program_(Direct|Pointer|DelegateRoute|Producers|RunPlannerChecks)_m[0-9]+\(' "$out/generated.h";then echo 'Direct named attributes roots leaked' >&2;return 1;fi
   [[ "$mode" != planner ]] || named_activation_attributes_planner_check "$out" || return $?
   local runner=()
   if [[ "${WASM:-0}" == 1 ]];then compile_console_wasm "$out" NamedAttributes || return $?;runner=(node "$out/NamedAttributes.js")
   else compile_console "$out" NamedAttributes || return $?;runner=("$out/NamedAttributes$EXE_EXT");fi
   gate_capture_ok "$out/native" run_bounded "${runner[@]}" || return $?
   if [[ "$mode" == planner ]];then
    for label in zero;do gate_output witness "$out/native.stdout" "named-attrs-planner-$label:True" --count 1 --policy crlf || return $?;done
   else
    local routes='direct calli delegate reflection';[[ "$mode" != reflection ]] || routes=reflection
    for route in $routes;do for label in $labels native-null-assembly;do gate_output witness "$out/native.stdout" "named-attrs-$route-$label:True" --count 1 --policy crlf || return $?;done;done
    if [[ "$mode" == all ]];then for label in False True;do gate_output witness "$out/native.stdout" "named-attrs-producers-$label:True" --count 1 --policy crlf || return $?;done;fi
   fi
  done
 done
 input="$root/noalloc-input";mkdir -p "$input";cp "$original/"* "$input/" || return $?
 dotnet "$fixture" --named-attrs-noalloc "$input/SelfHostPrimSubset.dll" "$corelib" || return $?
 for axis in raw diet;do
  local flags=("$@");[[ "$axis" != raw ]] || flags+=(--no-ildiet)
  gate_capture "$root/noalloc-$axis" invoke_cli "$input/SelfHostPrimSubset.dll" --auto-ref --link-xml gates/fixtures/named-activation-attributes-opaque-ctors.xml "${flags[@]}" -o "$root/noalloc-$axis-output" || return $?
  gate_output status "$(cat "$root/noalloc-$axis.status")" 2 || return $?
  gate_output match-record "$root/noalloc-$axis.stderr" '^error: \[HotPath\(NoAlloc\)\] closure of NamedActivationAttributesSubset[.]Program::Direct allocates: System[.]Activator::CreateInstance emits dn2cpp_alloc\($' --count 1 --policy crlf || return $?
 done
}
named_activation_attributes_negative_gate() {
 local root="$1"; shift
 local corelib mode axis input out
 local fixture="gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll"
 corelib=$(resolve_net10_corelib) || return $?
 for mode in return modifier argument attrs-element attrs-byref instance generic address-return address-modifier address-generic;do
  input="$root/$mode-input";mkdir -p "$input";cp "samples/dotnet/SelfHostPrimSubset/bin/$CONFIG/$TFM/"* "$input/" || return $?
  dotnet "$fixture" --named-attrs-negative "$input/SelfHostPrimSubset.dll" "$corelib" "$mode" || return $?
  for axis in raw diet;do
   out="$root/$mode-$axis";local flags=("$@");[[ "$axis" != raw ]] || flags+=(--no-ildiet)
   gate_capture "$out/emit" invoke_cli "$input/SelfHostPrimSubset.dll" --auto-ref "${flags[@]}" -o "$out/generated" || return $?
   gate_output status "$(cat "$out/emit.status")" 2 || return $?
   gate_output match-record "$out/emit.stderr" '.*Named Activator construction requires its exact MemberRef signature.*' --count 1 --policy crlf || return $?
  done
 done
}

# Native selected bodies, not a promise of exact post-ILDiet CIL retention.
named_activation_attributes_planner_check() {
 local header="$1/generated.h"
 grep -Eq 'void PlannerOnly__ctor_m[0-9]+\(t_NamedActivationAttributesSubset_PlannerOnly\* a0\);' "$header" || return 1
 if grep -Eq 'PlannerOnly__ctor_m[0-9]+\(t_NamedActivationAttributesSubset_PlannerOnly\* a0,' "$header";then
  echo 'Activation attributes were treated as constructor arguments or unrelated constructor was rooted' >&2;return 1
 fi
}
