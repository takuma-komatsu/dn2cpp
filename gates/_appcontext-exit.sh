#!/usr/bin/env bash
appcontext_exit_routes_gate() {
 local root="$1";shift
 local fixture="gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll" cli="${DN2CPP_CLI_DLL:-src/Dn2Cpp.Cli/bin/$CONFIG/$TFM/dn2cpp.dll}"
 local expected
 local original="samples/dotnet/SelfHostPrimSubset/bin/$CONFIG/$TFM" corelib input mode axis out route
 corelib=$(resolve_net10_corelib) || return $?
 mkdir -p "$root" || return $?
 dotnet "$fixture" --appcontext-exit-guards "$(dirname "$cli")/Dn2Cpp.Transpiler.dll" "$corelib" "$root/model-input.dll" || return $?
 input="$root/oracle-input";mkdir -p "$input";cp "$original/"* "$input/" || return $?
 dotnet "$fixture" --appcontext-exit-oracle "$input/SelfHostPrimSubset.dll" "$corelib" || return $?
 gate_capture_ok "$root/oracle" run_bounded dotnet "$input/SelfHostPrimSubset.dll" || return $?
 gate_output witness "$root/oracle.stdout" 'appcontext-public-base-directory:True' --count 1 --policy crlf || return $?
 expected="$root/oracle.stdout"
 if [[ "${WASM:-0}" == 1 ]];then
  # Wasm has no native executable path; its existing PAL returns an empty directory.
  expected="$root/wasm-public-policy.txt"
  printf '%s\n' '== AppContext public neighbor ==' 'appcontext-public-base-directory:False' 'AppContext public neighbor end' > "$expected" || return $?
 fi
 for mode in all reflection noalloc;do
  input="$root/$mode-input";mkdir -p "$input";cp "$original/"* "$input/" || return $?
  local flag=--appcontext-exit;[[ "$mode" == all ]] || flag+="-$mode"
  dotnet "$fixture" "$flag" "$input/SelfHostPrimSubset.dll" "$corelib" || return $?
  for axis in raw diet;do
   out="$root/$mode-$axis";local flags=("$@");[[ "$axis" != raw ]] || flags+=(--no-ildiet)
   invoke_cli "$input/SelfHostPrimSubset.dll" --auto-ref "${flags[@]}" -o "$out" || return $?
   if [[ "$mode" == reflection ]] && grep -Eq 'Program_(Direct|Pointer|DelegateRoute)_m[0-9]+\(' "$out/generated.h";then echo 'Direct exit roots leaked into reflection-only fixture' >&2;return 1;fi
   appcontext_exit_body_check "$out" || return $?
   local runner=()
   if [[ "${WASM:-0}" == 1 ]];then compile_console_wasm "$out" AppContextExit || return $?;runner=(node "$out/AppContextExit.js")
   else compile_console "$out" AppContextExit || return $?;runner=("$out/AppContextExit$EXE_EXT");fi
   gate_capture_ok "$out/native" run_bounded "${runner[@]}" || return $?
   local routes='direct calli delegate reflection';[[ "$mode" != reflection ]] || routes=reflection
   for route in $routes;do gate_output witness "$out/native.stdout" "appcontext-exit-$route:True" --count 1 --policy crlf || return $?;done
   gate_output witness "$out/native.stdout" 'appcontext-exit-continued:True' --count 1 --policy crlf || return $?
   gate_output prefix "$out/native.stdout" "$expected" --before "appcontext-exit-${routes%% *}:True" --policy crlf || return $?
  done
 done
}
appcontext_exit_negative_gate() {
 local root="$1" corelib mode axis input out
 local fixture="gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll"
 corelib=$(resolve_net10_corelib) || return $?
 for mode in return modifier argument instance generic address-return address-modifier address-generic;do
  input="$root/$mode-input";mkdir -p "$input";cp "samples/dotnet/SelfHostPrimSubset/bin/$CONFIG/$TFM/"* "$input/" || return $?
  dotnet "$fixture" --appcontext-exit-negative "$input/SelfHostPrimSubset.dll" "$corelib" "$mode" || return $?
  for axis in raw diet;do
   out="$root/$mode-$axis";local flags=();[[ "$axis" != raw ]] || flags+=(--no-ildiet)
   gate_capture "$out/emit" invoke_cli "$input/SelfHostPrimSubset.dll" --auto-ref "${flags[@]}" -o "$out/generated" || return $?
   gate_output status "$(cat "$out/emit.status")" 2 || return $?
   local diagnostic='.*AppContext process-exit hook requires its exact MemberRef signature.*'
   [[ "$mode" != argument ]] || diagnostic='.*System.AppContext: no method OnProcessExit/1.*'
   gate_output match-record "$out/emit.stderr" "$diagnostic" --count 1 --policy crlf || return $?
  done
 done
}

# The actual retained void wrapper must refuse before any shutdown callback.
appcontext_exit_body_check() {
 awk '
 /^void AppContext_OnProcessExit_m[0-9]+\(\)$/ {
   count++;
   if (getline <= 0 || $0 != "{") exit 1;
   if (getline <= 0 || $0 != "    dn2cpp_throw_platform_not_supported(\"AppContext process-exit orchestration is not supported\");") exit 1;
   if (getline <= 0 || $0 != "}") exit 1;
 }
 END { if (count != 1) exit 1 }
 ' "$1"/generated*.cpp
}
