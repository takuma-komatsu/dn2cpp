#!/usr/bin/env bash
default_principal_dependencies_gate() {
 local parent="$1" rewritten_input="$2";shift 2
 local fixture="gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll"
 local cli="${DN2CPP_CLI_DLL:-src/Dn2Cpp.Cli/bin/$CONFIG/$TFM/dn2cpp.dll}" corelib claims root plan axis mode out
 corelib=$(resolve_net10_corelib) || return $?
 claims="$(dirname "$corelib")/System.Security.Claims.dll"
 mkdir -p "$parent" || return $?
 root=$(mktemp -d "$parent/dependency-XXXXXX") || return $?
 plan="$root/plan"
 gate_capture_ok "$root/loadset" dotnet "$fixture" --default-principal-loadset "$(dirname "$cli")/Dn2Cpp.Transpiler.dll" "$rewritten_input" "$corelib" "$plan" || return $?
 local refs=() without_claims=() path
 while IFS= read -r path;do
  refs+=(-r "$path")
  [[ "$path" == "$claims" ]] || without_claims+=(-r "$path")
 done < "$plan/refs.txt"
 # The recorded plan must contain the actual implementation used by subsequent controls.
 grep -Fxq "$claims" "$plan/refs.txt" || return 1
 printf '%s\n' "${refs[@]}" > "$root/explicit-arguments.txt"
 for axis in raw diet;do
  local flags=("$@");[[ "$axis" != raw ]] || flags+=(--no-ildiet)
  out="$root/valid-$axis"
  gate_capture_ok "$out/emit" invoke_cli "$rewritten_input" "${refs[@]}" "${flags[@]}" -o "$out/generated" || return $?
  # Intentionally no --auto-ref in these controls.
  out="$root/missing-$axis"
  gate_capture "$out/emit" invoke_cli "$rewritten_input" "${without_claims[@]}" "${flags[@]}" -o "$out/generated" || return $?
  gate_output status "$(cat "$out/emit.status")" 2 || return $?
  gate_output match-record "$out/emit.stderr" '.*default principal requires the System[.]Security[.]Claims implementation assembly.*' --count 1 --policy crlf || return $?
 done
 for mode in unchanged wrong-return wrong-name duplicate lookalike-assembly;do
  mkdir -p "$root/copy-$mode" || return $?
  local copy="$root/copy-$mode/System.Security.Claims.dll"
  gate_capture_ok "$root/copy-$mode/rewrite" dotnet "$fixture" --default-principal-target-copy "$claims" "$copy" "$mode" || return $?
  shasum -a 256 "$claims" "$copy" > "$root/copy-$mode/identity.sha256"
  for axis in raw diet;do
   local flags=("$@");[[ "$axis" != raw ]] || flags+=(--no-ildiet)
   for resolve in explicit auto;do
    # Auto case checks malformed same-name explicit target is authoritative.
    [[ "$mode" != lookalike-assembly || "$resolve" != auto ]] || continue
    out="$root/$mode-$resolve-$axis";local auto=();[[ "$resolve" != auto ]] || auto+=(--auto-ref)
    gate_capture "$out/emit" invoke_cli "$rewritten_input" "${without_claims[@]}" -r "$copy" "${auto[@]}" "${flags[@]}" -o "$out/generated" || return $?
    if [[ "$mode" == unchanged ]];then gate_output status "$(cat "$out/emit.status")" 0 || return $?
    else
     gate_output status "$(cat "$out/emit.status")" 2 || return $?
     local diagnostic='.*default principal requires the exact managed GenericPrincipal[.]GetDefaultInstance factory.*'
     [[ "$mode" != lookalike-assembly ]] || diagnostic='.*default principal requires the System[.]Security[.]Claims implementation assembly.*'
     gate_output match-record "$out/emit.stderr" "$diagnostic" --count 1 --policy crlf || return $?
    fi
   done
  done
 done
 for axis in raw diet;do
  local flags=("$@");[[ "$axis" != raw ]] || flags+=(--no-ildiet)
  out="$root/unused-$axis"
  gate_capture_ok "$out/emit" invoke_cli "$plan/UnusedPrincipalProbe.dll" --auto-ref "${flags[@]}" -o "$out/generated" || return $?
  if grep -Eq 'GenericPrincipal_(GetDefaultInstance|_ctor)_m[0-9]+\(' "$out/generated/generated.h";then echo 'Unused accessor globally rooted principal factory' >&2;return 1;fi
  # Missing target does not reject an unrelated app without --auto-ref either.
  local unused_refs=()
  while IFS= read -r path;do [[ "$path" == "$claims" ]] || unused_refs+=(-r "$path");done < "$plan/unused-refs.txt"
  gate_capture_ok "$root/unused-missing-$axis/emit" invoke_cli "$plan/UnusedPrincipalProbe.dll" "${unused_refs[@]}" "${flags[@]}" -o "$root/unused-missing-$axis/generated" || return $?
 done
 printf '%s\n' "$root"
}
