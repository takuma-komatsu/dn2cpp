#!/usr/bin/env bash
gchandle_equality_routes_gate() {
 local root="$1" original="$2";shift 2
 mkdir -p "$root" || return $?
 gate_capture_ok "$root/oracle" run_bounded dotnet "$original/GCHandleEquality.dll" || return $?
 for axis in raw diet;do
  local out="$root/$axis" flags=("$@");[[ "$axis" != raw ]] || flags+=(--no-ildiet)
  invoke_cli "$original/GCHandleEquality.dll" --auto-ref "${flags[@]}" -o "$out" || return $?
  python3 gates/fixtures/gchandle-equality/check-generated.py "$out" || return $?
  local runner=()
  if [[ "${WASM:-0}" == 1 ]];then compile_console_wasm "$out" GCHandleEquality || return $?;runner=(node "$out/GCHandleEquality.js")
  else compile_console "$out" GCHandleEquality || return $?;runner=("$out/GCHandleEquality$EXE_EXT");fi
  gate_capture_ok "$out/native" run_bounded "${runner[@]}" || return $?
  gate_output compare "$root/oracle.stdout" "$out/native.stdout" --policy crlf || return $?
 done
}

gchandle_equality_guards_gate() {
 local root="$1" model corelib
 mkdir -p "$root" || return $?
 model=$(mktemp -d "$root/model-XXXXXX") || return $?
 corelib=$(resolve_net10_corelib) || return $?
 gate_capture_ok "$model/guards" run_bounded dotnet "gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll" --gchandle-equality-guards "$(dirname "${DN2CPP_CLI_DLL:-src/Dn2Cpp.Cli/bin/$CONFIG/$TFM/dn2cpp.dll}")/Dn2Cpp.Transpiler.dll" "$corelib" "$model/GuardInput.dll" || return $?
 gate_output witness "$model/guards.stdout" 'gchandle-equality-guards:10' --count 1 --policy crlf || return $?
}
