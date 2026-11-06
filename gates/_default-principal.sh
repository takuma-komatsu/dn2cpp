#!/usr/bin/env bash
# Fixture binary already built by owner; input is standalone probe with no Claims reference.
default_principal_routes_gate() {
 local root="$1" original="$2";shift 2
 local fixture="gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll" corelib mode axis input out
 corelib=$(resolve_net10_corelib) || return $?
 for mode in all reflection policy;do
  input="$root/$mode-input";mkdir -p "$input";cp "$original/"* "$input/" || return $?
  local flag=--default-principal;[[ "$mode" == all ]] || flag+="-$mode"
  dotnet "$fixture" "$flag" "$input/ImplicitPrincipalProbe.dll" "$corelib" || return $?
  for axis in raw diet;do
   out="$root/$mode-$axis";local flags=("$@");[[ "$axis" != raw ]] || flags+=(--no-ildiet)
   # No Claims -r: this is the implicit dependency proof.
   invoke_cli "$input/ImplicitPrincipalProbe.dll" --auto-ref "${flags[@]}" -o "$out" || return $?
   if [[ "$mode" == reflection ]] && grep -Eq 'Program_(Direct|Pointer|Address|DelegateRoute|Produced|PolicyChecks)_m[0-9]+\(' "$out/generated.h";then echo 'Direct roots leaked into reflection-only lane' >&2;return 1;fi
   grep -Eq 'GenericPrincipal_GetDefaultInstance_m[0-9]+\(' "$out/generated.h" || return 1
   local runner=()
   if [[ "${WASM:-0}" == 1 ]];then compile_console_wasm "$out" DefaultPrincipal || return $?;runner=(node "$out/DefaultPrincipal.js")
   else compile_console "$out" DefaultPrincipal || return $?;runner=("$out/DefaultPrincipal$EXE_EXT");fi
   gate_capture_ok "$out/native" run_bounded "${runner[@]}" || return $?
   if [[ "$mode" == policy ]];then
    for label in none windows unauthenticated explicit second-set;do gate_output witness "$out/native.stdout" "default-principal-policy-$label:True" --count 1 --policy crlf || return $?;done
   else
    local routes='direct calli delegate reflection';[[ "$mode" != reflection ]] || routes=reflection
    for route in $routes;do for marker in False True;do gate_output witness "$out/native.stdout" "default-principal-$route-$marker:True" --count 1 --policy crlf || return $?;done;done
    if [[ "$mode" == all ]];then for label in 0 1;do gate_output witness "$out/native.stdout" "default-principal-producer-$label:True" --count 1 --policy crlf || return $?;done;fi
   fi
  done
 done
}
default_principal_noalloc_gate() {
 local root="$1" original="$2";shift 2
 local fixture="gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll" corelib input="$root/input" axis
 corelib=$(resolve_net10_corelib) || return $?
 mkdir -p "$input";cp "$original/"* "$input/" || return $?
 dotnet "$fixture" --default-principal-noalloc "$input/ImplicitPrincipalProbe.dll" "$corelib" || return $?
 for axis in raw diet;do
  local flags=("$@");[[ "$axis" != raw ]] || flags+=(--no-ildiet)
  gate_capture "$root/$axis" invoke_cli "$input/ImplicitPrincipalProbe.dll" --auto-ref "${flags[@]}" -o "$root/$axis-output" || return $?
  gate_output status "$(cat "$root/$axis.status")" 2 || return $?
  gate_output match-record "$root/$axis.stderr" '^\[HotPath\(NoAlloc\)\] closure of DefaultPrincipalSubset[.]Program::Direct allocates: System[.]Security[.]Claims[.]ClaimsIdentity::[.]ctor emits dn2cpp_alloc\($' --count 1 --policy crlf || return $?
  gate_output match-record "$root/$axis.stderr" '^  call chain: DefaultPrincipalSubset[.]Program[.]Direct -> System[.]Security[.]Principal[.]GenericPrincipal[.]GetDefaultInstance -> System[.]Security[.]Principal[.]GenericIdentity[.][.]ctor -> System[.]Security[.]Claims[.]ClaimsIdentity[.][.]ctor -> System[.]Security[.]Claims[.]ClaimsIdentity[.][.]ctor$' --count 1 --policy crlf || return $?
 done
}
default_principal_negative_gate() {
 local root="$1" original="$2"; shift 2
 local corelib input mode axis out fixture="gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll"
 corelib=$(resolve_net10_corelib) || return $?
 for mode in return parameter byref modifier instance generic address-return address-parameter address-byref address-modifier address-generic;do
  input="$root/$mode-input";mkdir -p "$input";cp "$original/"* "$input/" || return $?
  dotnet "$fixture" --default-principal-negative "$input/ImplicitPrincipalProbe.dll" "$corelib" "$mode" || return $?
  for axis in raw diet;do
   out="$root/$mode-$axis";local flags=("$@");[[ "$axis" != raw ]] || flags+=(--no-ildiet)
   gate_capture "$out/emit" invoke_cli "$input/ImplicitPrincipalProbe.dll" --auto-ref "${flags[@]}" -o "$out/generated" || return $?
   gate_output status "$(cat "$out/emit.status")" 2 || return $?
   gate_output match-record "$out/emit.stderr" '.*default principal accessor requires its exact MemberRef signature.*' --count 1 --policy crlf || return $?
  done
 done
}

default_principal_guards_gate() {
 local root="$1" corelib
 mkdir -p "$root" || return $?
 root=$(mktemp -d "$root/run-XXXXXX") || return $?
 corelib=$(resolve_net10_corelib) || return $?
 dotnet "gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll" --default-principal-guards "$(dirname "${DN2CPP_CLI_DLL:-src/Dn2Cpp.Cli/bin/$CONFIG/$TFM/dn2cpp.dll}")/Dn2Cpp.Transpiler.dll" "$corelib" "$(dirname "$corelib")/System.Security.Claims.dll" "$root/model-input.dll"
}
