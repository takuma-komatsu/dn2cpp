#!/usr/bin/env bash
path_activation_routes_gate() {
 local root="$1"; shift
 local fixture="gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll"
 local cli="${DN2CPP_CLI_DLL:-src/Dn2Cpp.Cli/bin/$CONFIG/$TFM/dn2cpp.dll}" original="samples/dotnet/SelfHostPrimSubset/bin/$CONFIG/$TFM"
 local corelib input mode axis out label route
 corelib=$(resolve_net10_corelib) || return $?
 mkdir -p "$root" || return $?
 dotnet "$fixture" --path-activation-guards "$(dirname "$cli")/Dn2Cpp.Transpiler.dll" "$corelib" "$root/model-input.dll" || return $?
 input="$root/oracle-input"; mkdir -p "$input"; cp "$original/"* "$input/" || return $?
 dotnet "$fixture" --path-activation-oracle "$input/SelfHostPrimSubset.dll" "$corelib" || return $?
 gate_capture_ok "$root/oracle" run_bounded dotnet "$input/SelfHostPrimSubset.dll" || return $?
 for label in known null empty missing null-type missing-type; do
  gate_output witness "$root/oracle.stdout" "path-public-$label:True" --count 1 --policy crlf || return $?
 done
 for mode in all reflection noalloc; do
  input="$root/$mode-input"; mkdir -p "$input"; cp "$original/"* "$input/" || return $?
  local flag=--path-activation; [[ "$mode" == all ]] || flag+="-$mode"
  dotnet "$fixture" "$flag" "$input/SelfHostPrimSubset.dll" "$corelib" || return $?
  for axis in raw diet; do
   out="$root/$mode-$axis"; local flags=("$@"); [[ "$axis" != raw ]] || flags+=(--no-ildiet)
   invoke_cli "$input/SelfHostPrimSubset.dll" --auto-ref "${flags[@]}" -o "$out" || return $?
   if [[ "$mode" == reflection ]] && grep -Eq 'Program_(Direct|Pointer|DelegateRoute|Producers|DomainChecks)_m[0-9]+\(' "$out/generated.h"; then
    echo 'Direct path activation roots leaked into reflection fixture' >&2; return 1
   fi
   path_activation_body_check "$out" || return $?
   # Neither target constructor is needed by the refusal boundary.
   if grep -Eq 'void (Probe|ObjectHandle)__ctor_m[0-9]+\(' "$out/generated.h"; then
    echo 'Refusal unexpectedly retained target/ObjectHandle constructor' >&2; return 1
   fi
   local runner=()
   if [[ "${WASM:-0}" == 1 ]]; then compile_console_wasm "$out" PathActivation || return $?; runner=(node "$out/PathActivation.js")
   else compile_console "$out" PathActivation || return $?; runner=("$out/PathActivation$EXE_EXT"); fi
   gate_capture_ok "$out/native" run_bounded "${runner[@]}" || return $?
   local routes='direct calli delegate reflection'; [[ "$mode" != reflection ]] || routes=reflection
   for route in $routes; do for label in 0 1 2 3 4; do
    gate_output witness "$out/native.stdout" "path-activation-$route-$label:True" --count 1 --policy crlf || return $?
   done; done
   if [[ "$mode" != reflection ]]; then
    for label in domain-forward domain-null-receiver producers-0 producers-1 producers-2; do
     gate_output witness "$out/native.stdout" "path-activation-$label:True" --count 1 --policy crlf || return $?
    done
   fi
  done
 done
}
path_activation_negative_gate() {
 local root="$1"; shift
 local corelib mode axis input out fixture="gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll"
 corelib=$(resolve_net10_corelib) || return $?
 for mode in return modifier argument array byref instance generic address-return address-modifier address-byref address-generic; do
  input="$root/$mode-input"; mkdir -p "$input"; cp "samples/dotnet/SelfHostPrimSubset/bin/$CONFIG/$TFM/"* "$input/" || return $?
  dotnet "$fixture" --path-activation-negative "$input/SelfHostPrimSubset.dll" "$corelib" "$mode" || return $?
  for axis in raw diet; do
   out="$root/$mode-$axis"; local flags=("$@"); [[ "$axis" != raw ]] || flags+=(--no-ildiet)
   gate_capture "$out/emit" invoke_cli "$input/SelfHostPrimSubset.dll" --auto-ref "${flags[@]}" -o "$out/generated" || return $?
   gate_output status "$(cat "$out/emit.status")" 2 || return $?
   # Match the narrow production resolver diagnostic, not any compilation failure.
   gate_output match-record "$out/emit.stderr" '.*Activator path activation requires its exact MemberRef signature.*' --count 1 --policy crlf || return $?
  done
 done
}

path_activation_body_check() {
 python3 - "$1" <<'PY'
import pathlib,re,sys
root=pathlib.Path(sys.argv[1])
pattern=r't_System_Runtime_Remoting_ObjectHandle\* Activator_CreateInstanceFrom_m\d+\(Dn2CppString\* a0, Dn2CppString\* a1\)\n\{([^{}]*)\n\}'
bodies=[]
for path in root.glob('generated*.cpp'):
 bodies += re.findall(pattern,path.read_text())
assert len(bodies)==1, f'expected one exact path activation wrapper, got {len(bodies)}'
body=' '.join(bodies[0].split())
expected='[[maybe_unused]] t_System_Runtime_Remoting_ObjectHandle* t0; (void)(a0); (void)(a1); dn2cpp_throw_platform_not_supported("Assembly.LoadFrom is not available in AOT-compiled code (no assembly loading)"); t0 = nullptr; return t0;'
assert body==expected, 'path activation wrapper is not the exact no-payload/no-allocation refusal body: '+body
print('path-activation-body:exact-refusal')
PY
}
