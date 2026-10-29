#!/usr/bin/env bash
named_activation_flags_routes_gate() {
 local root="$1";shift
 local fixture="gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll" cli="${DN2CPP_CLI_DLL:-src/Dn2Cpp.Cli/bin/$CONFIG/$TFM/dn2cpp.dll}"
 local original="samples/dotnet/SelfHostPrimSubset/bin/$CONFIG/$TFM" corelib input mode axis out route label
 corelib=$(resolve_net10_corelib) || return $?
 mkdir -p "$root" || return $?
 dotnet "$fixture" --named-flags-guards "$(dirname "$cli")/Dn2Cpp.Transpiler.dll" "$corelib" "$root/model-input.dll" || return $?
 input="$root/oracle-input";mkdir -p "$input";cp "$original/"* "$input/" || return $?
 dotnet "$fixture" --named-flags-oracle "$input/SelfHostPrimSubset.dll" "$corelib" || return $?
 gate_capture_ok "$root/oracle" run_bounded dotnet "$input/SelfHostPrimSubset.dll" || return $?
 local labels='default int-args ignorecase case-sensitive private wrapped unwrapped culture empty-attrs attrs missing-assembly-first null-type-first missing-type-first nullable'
 for label in $labels default-binder;do gate_output witness "$root/oracle.stdout" "named-flags-public-$label:True" --count 1 --policy crlf || return $?;done
 for mode in all reflection planner;do
  input="$root/$mode-input";mkdir -p "$input";cp "$original/"* "$input/" || return $?
  local flag=--named-flags;[[ "$mode" == all ]] || flag+="-$mode"
  dotnet "$fixture" "$flag" "$input/SelfHostPrimSubset.dll" "$corelib" || return $?
  for axis in raw diet;do
   out="$root/$mode-$axis";local flags=("$@");[[ "$axis" != raw ]] || flags+=(--no-ildiet)
   [[ "$mode" == planner ]] || flags+=(--link-xml gates/fixtures/named-activation-flags-opaque-ctors.xml)
   invoke_cli "$input/SelfHostPrimSubset.dll" --auto-ref "${flags[@]}" -o "$out" || return $?
   if [[ "$mode" == reflection ]] && grep -Eq 'Program_(Direct|Pointer|DelegateRoute|Producers|RunPlannerChecks)_m[0-9]+\(' "$out/generated.h";then echo 'Direct named flags roots leaked' >&2;return 1;fi
   [[ "$mode" != planner ]] || named_activation_flags_planner_check "$out" || return $?
   local runner=()
   if [[ "${WASM:-0}" == 1 ]];then compile_console_wasm "$out" NamedFlags || return $?;runner=(node "$out/NamedFlags.js")
   else compile_console "$out" NamedFlags || return $?;runner=("$out/NamedFlags$EXE_EXT");fi
   gate_capture_ok "$out/native" run_bounded "${runner[@]}" || return $?
   if [[ "$mode" == planner ]];then
    for label in int private;do gate_output witness "$out/native.stdout" "named-flags-planner-$label:True" --count 1 --policy crlf || return $?;done
   else
    local routes='direct calli delegate reflection';[[ "$mode" != reflection ]] || routes=reflection
    for route in $routes;do for label in $labels native-null-assembly native-binder attrs-before-binder;do gate_output witness "$out/native.stdout" "named-flags-$route-$label:True" --count 1 --policy crlf || return $?;done;done
    if [[ "$mode" == all ]];then for label in False True;do gate_output witness "$out/native.stdout" "named-flags-producers-$label:True" --count 1 --policy crlf || return $?;done;fi
   fi
  done
 done
 input="$root/noalloc-input";mkdir -p "$input";cp "$original/"* "$input/" || return $?
 dotnet "$fixture" --named-flags-noalloc "$input/SelfHostPrimSubset.dll" "$corelib" || return $?
 for axis in raw diet;do
  local flags=("$@");[[ "$axis" != raw ]] || flags+=(--no-ildiet)
  gate_capture "$root/noalloc-$axis" invoke_cli "$input/SelfHostPrimSubset.dll" --auto-ref --link-xml gates/fixtures/named-activation-flags-opaque-ctors.xml "${flags[@]}" -o "$root/noalloc-$axis-output" || return $?
  gate_output status "$(cat "$root/noalloc-$axis.status")" 2 || return $?
  gate_output match-record "$root/noalloc-$axis.stderr" '^error: \[HotPath\(NoAlloc\)\] closure of NamedActivationFlagsSubset[.]Program::Direct allocates: System[.]Activator::CreateInstance emits dn2cpp_alloc\($' --count 1 --policy crlf || return $?
 done
}
named_activation_flags_negative_gate() {
 local root="$1" corelib mode axis input out
 local fixture="gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll"
 corelib=$(resolve_net10_corelib) || return $?
 for mode in return modifier argument flags binder culture args-element args-byref instance generic address-return address-modifier address-generic;do
  input="$root/$mode-input";mkdir -p "$input";cp "samples/dotnet/SelfHostPrimSubset/bin/$CONFIG/$TFM/"* "$input/" || return $?
  dotnet "$fixture" --named-flags-negative "$input/SelfHostPrimSubset.dll" "$corelib" "$mode" || return $?
  for axis in raw diet;do
   out="$root/$mode-$axis";local flags=();[[ "$axis" != raw ]] || flags+=(--no-ildiet)
   gate_capture "$out/emit" invoke_cli "$input/SelfHostPrimSubset.dll" --auto-ref "${flags[@]}" -o "$out/generated" || return $?
   gate_output status "$(cat "$out/emit.status")" 2 || return $?
   gate_output match-record "$out/emit.stderr" '.*Named Activator construction requires its exact MemberRef signature.*' --count 1 --policy crlf || return $?
  done
 done
}

# Known argument length narrows arity, not same-arity overload types.
named_activation_flags_planner_check() {
 local header="$1/generated.h"
 grep -Eq 'void PlannerInt__ctor_m[0-9]+\(t_NamedActivationFlagsSubset_PlannerInt\* a0, int32_t a1\);' "$header" || return 1
 grep -Eq 'void PlannerPrivate__ctor_m[0-9]+\(t_NamedActivationFlagsSubset_PlannerPrivate\* a0\);' "$header" || return 1
 if grep -Eq 'PlannerInt__ctor_m[0-9]+\(t_NamedActivationFlagsSubset_PlannerInt\* a0\)|PlannerPrivate__ctor_m[0-9]+\([^)]*,' "$header";then
  echo 'Named constructor planner retained an unrelated visibility/arity' >&2;return 1
 fi
}
