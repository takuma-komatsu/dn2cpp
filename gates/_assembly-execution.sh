#!/usr/bin/env bash
assembly_execution_routes_gate() {
 local root="$1";shift
 local fixture="gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll" cli="${DN2CPP_CLI_DLL:-src/Dn2Cpp.Cli/bin/$CONFIG/$TFM/dn2cpp.dll}"
 local original="samples/dotnet/SelfHostPrimSubset/bin/$CONFIG/$TFM" corelib input mode axis out route
 corelib=$(resolve_net10_corelib) || return $?
 mkdir -p "$root" || return $?
 dotnet "$fixture" --assembly-execution-guards "$(dirname "$cli")/Dn2Cpp.Transpiler.dll" "$corelib" "$root/model-input.dll" || return $?
 input="$root/oracle-input";mkdir -p "$input";cp "$original/"* "$input/" || return $?
 dotnet "$fixture" --assembly-execution-oracle "$input/SelfHostPrimSubset.dll" "$corelib" || return $?
 gate_capture_ok "$root/oracle" run_bounded dotnet "$input/SelfHostPrimSubset.dll" || return $?
 for label in linked null-name;do gate_output witness "$root/oracle.stdout" "assembly-execution-public-$label:True" --count 1 --policy crlf || return $?;done
 for mode in all reflection noalloc;do
  input="$root/$mode-input";mkdir -p "$input";cp "$original/"* "$input/" || return $?
  local flag=--assembly-execution;[[ "$mode" == all ]] || flag+="-$mode"
  dotnet "$fixture" "$flag" "$input/SelfHostPrimSubset.dll" "$corelib" || return $?
  for axis in raw diet;do
   out="$root/$mode-$axis";local flags=("$@");[[ "$axis" != raw ]] || flags+=(--no-ildiet)
   invoke_cli "$input/SelfHostPrimSubset.dll" --auto-ref "${flags[@]}" -o "$out" || return $?
   if [[ "$mode" == reflection ]] && grep -Eq 'Program_(Direct|Pointer|DelegateRoute|ProducerRoute|ProducerChecks)_m[0-9]+\(' "$out/generated.h";then echo 'Direct assembly-execution roots leaked into reflection-only fixture' >&2;return 1;fi
   assembly_execution_body_check "$out" || return $?
   local runner=()
   if [[ "${WASM:-0}" == 1 ]];then compile_console_wasm "$out" AssemblyExecution || return $?;runner=(node "$out/AssemblyExecution.js")
   else compile_console "$out" AssemblyExecution || return $?;runner=("$out/AssemblyExecution$EXE_EXT");fi
   gate_capture_ok "$out/native" run_bounded "${runner[@]}" || return $?
   local routes='direct calli delegate reflection';[[ "$mode" != reflection ]] || routes=reflection
   for route in $routes;do for assembly in null app core;do for args in 0 1;do
    gate_output witness "$out/native.stdout" "assembly-execution-$route-$assembly-$args:True" --count 1 --policy crlf || return $?
   done;done;done
   if [[ "$mode" != reflection ]];then
    for assembly in null app;do for nonnull in False True;do for throwing in False True;do
     gate_output witness "$out/native.stdout" "assembly-execution-producers-$assembly-$nonnull-$throwing:True" --count 1 --policy crlf || return $?
    done;done;done
   fi
   gate_output prefix "$out/native.stdout" "$root/oracle.stdout" --before "assembly-execution-${routes%% *}-null-0:True" --policy crlf || return $?
  done
 done
}
assembly_execution_negative_gate() {
 local root="$1" corelib mode axis input out
 local fixture="gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll"
 corelib=$(resolve_net10_corelib) || return $?
 for mode in return modifier argument args-element args-byref instance generic address-return address-modifier address-generic;do
  input="$root/$mode-input";mkdir -p "$input";cp "samples/dotnet/SelfHostPrimSubset/bin/$CONFIG/$TFM/"* "$input/" || return $?
  dotnet "$fixture" --assembly-execution-negative "$input/SelfHostPrimSubset.dll" "$corelib" "$mode" || return $?
  for axis in raw diet;do
   out="$root/$mode-$axis";local flags=();[[ "$axis" != raw ]] || flags+=(--no-ildiet)
   gate_capture "$out/emit" invoke_cli "$input/SelfHostPrimSubset.dll" --auto-ref "${flags[@]}" -o "$out/generated" || return $?
   gate_output status "$(cat "$out/emit.status")" 2 || return $?
   gate_output match-record "$out/emit.stderr" '.*AppDomain assembly execution requires its exact MemberRef signature.*' --count 1 --policy crlf || return $?
  done
 done
}

# Validate the retained int-return wrapper, not an unrelated whole-file message.
assembly_execution_body_check() {
 awk '
 /^int32_t AppDomain_ExecuteAssembly_m[0-9]+\(const char\* a0, Dn2CppArrayRef\* a1\)$/ {
   count++;
   if (getline <= 0 || $0 != "{") exit 1;
   if (getline <= 0 || $0 != "    [[maybe_unused]] int32_t t0;") exit 1;
   if (getline <= 0 || $0 != "    (void)(a1);") exit 1;
   if (getline <= 0 || $0 != "    if (a0 == nullptr) dn2cpp_throw_null_reference();") exit 1;
   if (getline <= 0 || $0 != "    dn2cpp_throw_platform_not_supported(\"AppDomain assembly entrypoint execution is not supported\");") exit 1;
   if (getline <= 0 || $0 != "    t0 = 0;") exit 1;
   if (getline <= 0 || $0 != "    return t0;") exit 1;
   if (getline <= 0 || $0 != "}") exit 1;
 }
 END { if (count != 1) exit 1 }
 ' "$1"/generated*.cpp
}
