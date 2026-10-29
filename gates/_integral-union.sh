#!/usr/bin/env bash
integral_union_routes_gate() {
 local root="$1" original="$2";shift 2
 mkdir -p "$root" || return $?
 gate_capture_ok "$root/oracle" run_bounded dotnet "$original/IntegralUnion.dll" || return $?
 for axis in raw diet;do
  local out="$root/$axis" flags=("$@");[[ "$axis" != raw ]] || flags+=(--no-ildiet)
  invoke_cli "$original/IntegralUnion.dll" --auto-ref "${flags[@]}" -o "$out" || return $?
  python3 gates/fixtures/integral-union/check-generated.py "$out" --self-test || return $?
  local runner=()
  if [[ "${WASM:-0}" == 1 ]];then compile_console_wasm "$out" IntegralUnion || return $?;runner=(node "$out/IntegralUnion.js")
  else compile_console "$out" IntegralUnion || return $?;runner=("$out/IntegralUnion$EXE_EXT");fi
  gate_capture_ok "$out/native" run_bounded "${runner[@]}" || return $?
  gate_output compare "$root/oracle.stdout" "$out/native.stdout" --policy crlf || return $?
 done
}
integral_union_negative_gate() {
 local root="$1" original="$2";shift 2
 local fixture="gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll" corelib mode axis input out
 corelib=$(resolve_net10_corelib) || return $?
 for mode in unchanged float boolean char native-int pointer byref fnptr intrinsic nested-union byval-array missing-offset empty byref-like misaligned undersized invalid-size ref-overlap layout-ref-overlap;do
  input="$root/$mode-input";mkdir -p "$input";cp "$original/"* "$input/" || return $?
  dotnet "$fixture" --integral-union-negative "$input/IntegralUnionNegative.dll" "$corelib" "$mode" || return $?
  for axis in raw diet;do
   out="$root/$mode-$axis";local flags=("$@");[[ "$axis" != raw ]] || flags+=(--no-ildiet)
   gate_capture "$out/emit" invoke_cli "$input/IntegralUnionNegative.dll" --auto-ref "${flags[@]}" -o "$out/generated" || return $?
   if [[ "$mode" == unchanged || "$mode" == undersized ]];then gate_output status "$(cat "$out/emit.status")" 0 || return $?
   else
    gate_output status "$(cat "$out/emit.status")" 2 || return $?
    case "$mode" in
     misaligned) gate_output witness "$out/emit.stderr" "error: Bad.Field: [FieldOffset(1)] is not a multiple of the field's 4-byte alignment, which the emitted C++ layout cannot represent" --count 1 --policy crlf || return $?;;
     invalid-size) gate_output witness "$out/emit.stderr" "error: Bad: explicit Size=9 over fields ending at byte 8 gives a 9-byte struct, not a multiple of its 4-byte alignment, which the emitted C++ layout cannot represent" --count 1 --policy crlf || return $?;;
     layout-ref-overlap) gate_output witness "$out/emit.stderr" 'error: Bad.Field: a GC-reference field overlapping another field in a LayoutKind.Explicit type is not supported' --count 1 --policy crlf || return $?;;
     *) gate_output match-record "$out/emit.stderr" '.*Array[.]IndexOf<Bad> element equality is not supported yet [(]the element type has neither a typed Equals[(]T[)] nor an Equals[(]object[)] override[)].*' --count 1 --policy crlf || return $?;;
    esac
   fi
  done
 done
}
