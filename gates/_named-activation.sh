#!/usr/bin/env bash
named_activation_routes_gate() {
 local root="$1";shift
 local fixture="gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll" cli="${DN2CPP_CLI_DLL:-src/Dn2Cpp.Cli/bin/$CONFIG/$TFM/dn2cpp.dll}"
 local original="samples/dotnet/SelfHostPrimSubset/bin/$CONFIG/$TFM" corelib input mode axis out route label
 corelib=$(resolve_net10_corelib) || return $?
 mkdir -p "$root" || return $?
 dotnet "$fixture" --named-activation-guards "$(dirname "$cli")/Dn2Cpp.Transpiler.dll" "$corelib" "$root/model-input.dll" || return $?
 input="$root/oracle-input";mkdir -p "$input";cp "$original/"* "$input/" || return $?
 dotnet "$fixture" --named-activation-oracle "$input/SelfHostPrimSubset.dll" "$corelib" || return $?
 gate_capture_ok "$root/oracle" run_bounded dotnet "$input/SelfHostPrimSubset.dll" || return $?
 local labels='class int nullable sz-array md-array null-type empty-type empty-assembly-first missing-assembly-first missing-type private missing-ctor throwing'
 for label in $labels;do gate_output witness "$root/oracle.stdout" "named-activation-public-$label:True" --count 1 --policy crlf || return $?;done
 gate_output witness "$root/oracle.stdout" 'named-activation-domain-null:True' --count 1 --policy crlf || return $?
 for mode in all reflection planner;do
  input="$root/$mode-input";mkdir -p "$input";cp "$original/"* "$input/" || return $?
  local flag=--named-activation;[[ "$mode" == all ]] || flag+="-$mode"
  dotnet "$fixture" "$flag" "$input/SelfHostPrimSubset.dll" "$corelib" || return $?
  for axis in raw diet;do
   out="$root/$mode-$axis";local flags=("$@");[[ "$axis" != raw ]] || flags+=(--no-ildiet)
   # Never pass opaque constructor roots into the independent literal planner lane.
   [[ "$mode" == planner ]] || flags+=(--link-xml gates/fixtures/named-activation-opaque-ctors.xml)
   invoke_cli "$input/SelfHostPrimSubset.dll" --auto-ref "${flags[@]}" -o "$out" || return $?
   if [[ "$mode" == planner && "$axis" == diet ]];then
    local normalized=()
    mapfile -t normalized < <(find "$out/ildiet" -name SelfHostPrimSubset.dll)
    [[ "${#normalized[@]}" == 1 ]] || return 1
    dotnet "$fixture" --named-activation-planner-metadata "${normalized[0]}" || return $?
   fi
   if [[ "$mode" == reflection ]] && grep -Eq 'Program_(Direct|Pointer|DelegateRoute|ProducerRoute|RunPlannerChecks)_m[0-9]+\(' "$out/generated.h";then echo 'Direct named activation roots leaked' >&2;return 1;fi
   if [[ "$mode" == planner ]];then named_activation_planner_body_check "$out" || return $?;fi
   local runner=()
   if [[ "${WASM:-0}" == 1 ]];then compile_console_wasm "$out" NamedActivation || return $?;runner=(node "$out/NamedActivation.js")
   else compile_console "$out" NamedActivation || return $?;runner=("$out/NamedActivation$EXE_EXT");fi
   gate_capture_ok "$out/native" run_bounded "${runner[@]}" || return $?
   if [[ "$mode" == planner ]];then
    gate_output witness "$out/native.stdout" 'named-activation-planner:True' --count 1 --policy crlf || return $?
   else
    local routes='direct calli delegate reflection';[[ "$mode" != reflection ]] || routes=reflection
    for route in $routes;do for label in $labels native-null-assembly;do gate_output witness "$out/native.stdout" "named-activation-$route-$label:True" --count 1 --policy crlf || return $?;done;done
    if [[ "$mode" == all ]];then gate_output witness "$out/native.stdout" 'named-activation-domain-null:True' --count 1 --policy crlf || return $?;for label in False True;do gate_output witness "$out/native.stdout" "named-activation-producers-$label:True" --count 1 --policy crlf || return $?;done;fi
   fi
  done
 done
 named_activation_noalloc_gate "$root" "$@"
}
named_activation_noalloc_gate() {
 local root="$1";shift
 local fixture="gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll"
 local original="samples/dotnet/SelfHostPrimSubset/bin/$CONFIG/$TFM" corelib input axis
 corelib=$(resolve_net10_corelib) || return $?
 input="$root/noalloc-input";mkdir -p "$input";cp "$original/"* "$input/" || return $?
 dotnet "$fixture" --named-activation-noalloc "$input/SelfHostPrimSubset.dll" "$corelib" || return $?
 for axis in raw diet;do
  local flags=("$@");[[ "$axis" != raw ]] || flags+=(--no-ildiet)
  gate_capture "$root/noalloc-$axis" invoke_cli "$input/SelfHostPrimSubset.dll" --auto-ref --link-xml gates/fixtures/named-activation-opaque-ctors.xml "${flags[@]}" -o "$root/noalloc-$axis-output" || return $?
  gate_output status "$(cat "$root/noalloc-$axis.status")" 2 || return $?
  gate_output match-record "$root/noalloc-$axis.stderr" '^error: \[HotPath\(NoAlloc\)\] closure of NamedActivationSubset[.]Program::Direct allocates: System[.]Activator::CreateInstance emits dn2cpp_alloc\($' --count 1 --policy crlf || return $?
 done
}

named_activation_negative_gate() {
 local root="$1" corelib mode axis input out
 local fixture="gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll"
 corelib=$(resolve_net10_corelib) || return $?
 for mode in return modifier argument args-element args-byref instance generic address-return address-modifier address-generic;do
  input="$root/$mode-input";mkdir -p "$input";cp "samples/dotnet/SelfHostPrimSubset/bin/$CONFIG/$TFM/"* "$input/" || return $?
  dotnet "$fixture" --named-activation-negative "$input/SelfHostPrimSubset.dll" "$corelib" "$mode" || return $?
  for axis in raw diet;do
   out="$root/$mode-$axis";local flags=();[[ "$axis" != raw ]] || flags+=(--no-ildiet)
   gate_capture "$out/emit" invoke_cli "$input/SelfHostPrimSubset.dll" --auto-ref "${flags[@]}" -o "$out/generated" || return $?
   gate_output status "$(cat "$out/emit.status")" 2 || return $?
   gate_output match-record "$out/emit.stderr" '.*Named Activator construction requires its exact MemberRef signature.*' --count 1 --policy crlf || return $?
  done
 done
}

named_activation_planner_body_check() {
 grep -Eq 'void PlannerOnly__ctor_m[0-9]+\(t_NamedActivationSubset_PlannerOnly\* a0\);' "$1/generated.h" || return 1
 if grep -Eq 'PlannerOnly__ctor_m[0-9]+\([^)]*,' "$1/generated.h";then
  echo 'Literal named planner emitted an unrelated constructor arity' >&2;return 1
 fi
}
