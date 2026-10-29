#!/usr/bin/env bash
structural_pointer_routes_gate() {
 local root="$1" original="$2";shift 2
 mkdir -p "$root" || return $?
 gate_capture_ok "$root/oracle" run_bounded dotnet "$original/StructuralPointer.dll" || return $?
 for axis in raw diet;do
  local out="$root/$axis" flags=("$@");[[ "$axis" != raw ]] || flags+=(--no-ildiet)
  invoke_cli "$original/StructuralPointer.dll" --auto-ref "${flags[@]}" -o "$out" || return $?
  python3 gates/fixtures/structural-pointer/check-generated.py "$out" || return $?
  local runner=()
  if [[ "${WASM:-0}" == 1 ]];then compile_console_wasm "$out" StructuralPointer || return $?;runner=(node "$out/StructuralPointer.js")
  else compile_console "$out" StructuralPointer || return $?;runner=("$out/StructuralPointer$EXE_EXT");fi
  gate_capture_ok "$out/native" run_bounded "${runner[@]}" || return $?
  gate_output compare "$root/oracle.stdout" "$out/native.stdout" --policy crlf || return $?
 done
}
structural_pointer_negative_gate() {
 local root="$1" original="$2";shift 2
 local fixture="gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll" corelib mode axis input out
 corelib=$(resolve_net10_corelib) || return $?
 for mode in unchanged fnptr byref typed-reference union byref-like;do
  input="$root/$mode-input";mkdir -p "$input";cp "$original/"* "$input/" || return $?
  dotnet "$fixture" --structural-pointer-negative "$input/PointerNegative.dll" "$corelib" "$mode" || return $?
  for axis in raw diet;do
   out="$root/$mode-$axis";local flags=("$@");[[ "$axis" != raw ]] || flags+=(--no-ildiet)
   gate_capture "$out/emit" invoke_cli "$input/PointerNegative.dll" --auto-ref "${flags[@]}" -o "$out/generated" || return $?
   if [[ "$mode" == unchanged ]];then gate_output status "$(cat "$out/emit.status")" 0 || return $?
   else
    gate_output status "$(cat "$out/emit.status")" 2 || return $?
    gate_output match-record "$out/emit.stderr" '.*Array[.]IndexOf<Bad> element equality is not supported yet [(]the element type has neither a typed Equals[(]T[)] nor an Equals[(]object[)] override[)].*' --count 1 --policy crlf || return $?
   fi
  done
 done
}
