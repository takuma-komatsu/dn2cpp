#!/usr/bin/env bash
# The rewritten private CoreLib fixture is native-only, never a CLR oracle.
gen2_callback_prepare_probe() {
 local out="$1"
 python3 - "$out" "${DN2CPP_GEN2_ALLOC_COUNTERS:-0}" <<'PY'
import pathlib,re,sys
out=pathlib.Path(sys.argv[1]);p=out/'generated.cpp';s=p.read_text()
(out/'generated-before-probe.txt').write_text(s)
assert s.count('    dn2cpp_runtime_init();')==1 and s.count('        dn2cpp_main_exit(0);')==1
s='void gen2_allocation_probe_start();\nvoid gen2_allocation_probe_finish();\n'+s
s=s.replace('    dn2cpp_runtime_init();','    dn2cpp_runtime_init();\n    gen2_allocation_probe_start();')
s=s.replace('        dn2cpp_main_exit(0);','        gen2_allocation_probe_finish();\n        dn2cpp_main_exit(0);');p.write_text(s)
probe=pathlib.Path('gates/fixtures/gen2-callback/allocation-probe.cpp').read_text()
metadata='\n'.join(p.read_text() for p in out.glob('generated*.cpp') if p.name!='generated_gen2_allocation_probe.cpp')
rows=re.findall(r'^const Dn2CppTypeInfo ti_System_Gen2GcCallback = \{ (.*?) \};$',metadata,re.M)
assert len(rows)==1 and 'DN2CPP_TF_SEALED' in rows[0]
finalizers=re.findall(r'\(void\(\*\)\(Dn2CppObject\*\)\)\&(Gen2GcCallback_Finalize_m[0-9]+)',rows[0])
assert len(finalizers)==1 and 'Dn2CppMetadataHandle<Dn2CppTypeReflection>::from_raw(' in rows[0]
probe='#define DN2CPP_GEN2_FINALIZER '+finalizers[0]+'\n'+probe
if sys.argv[2]=='1':probe='#define DN2CPP_GEN2_ALLOC_COUNTERS 1\n'+probe
symbols=re.findall(r'void (Program_RunSuppliedDelegateChecks_m[0-9]+)\(Dn2CppObject\* a0\);', (out/'generated.h').read_text())
assert len(symbols)<=1
if symbols:probe='#define DN2CPP_GEN2_SUPPLIED_DELEGATES '+symbols[0]+'\n'+probe
(out/'generated_gen2_allocation_probe.cpp').write_text(probe)
PY
}
gen2_callback_routes_gate() {
 local root="$1" original="$2" mode="${3:-all}";shift 3
 local fixture="gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll" flag="--gen2-callback" corelib input out axis
 [[ "$mode" == all ]] || flag="--gen2-callback-$mode"
 corelib=$(resolve_net10_corelib) || return $?
 input="$root/input";mkdir -p "$input";cp "$original/"* "$input/" || return $?
 dotnet "$fixture" "$flag" "$input/Gen2Callback.dll" "$corelib" || return $?
 for axis in raw diet;do
  out="$root/$axis";local flags=("$@");[[ "$axis" != raw ]] || flags+=(--no-ildiet)
  [[ "$mode" != reflection ]] || flags+=(--link-xml gates/fixtures/gen2-callback/link.xml)
  invoke_cli "$input/Gen2Callback.dll" --auto-ref "${flags[@]}" -o "$out" || return $?
  local DN2CPP_EXTRA_LINK_FLAGS="${DN2CPP_EXTRA_LINK_FLAGS:-}" runner=()
  if [[ "$mode" == all || "$mode" == reflection ]];then
   gen2_callback_prepare_probe "$out" || return $?
   if [[ "${DN2CPP_GEN2_ALLOC_COUNTERS:-0}" == 1 ]];then
    # Opt-in test instrumentation uses the GNU/wasm linker wrapper ABI. Normal
    # cross-platform gates still run the metadata/clone/finalizer boundary probe.
    DN2CPP_EXTRA_LINK_FLAGS+=' -Wl,--wrap=_Z12dn2cpp_allocm -Wl,--wrap=_Z25dn2cpp_register_finalizerP12Dn2CppObject'
   fi
  fi
  if [[ "${WASM:-0}" == 1 ]];then compile_console_wasm "$out" Gen2Callback || return $?;runner=(node "$out/Gen2Callback.js")
  else compile_console "$out" Gen2Callback || return $?;runner=("$out/Gen2Callback$EXE_EXT");fi
  gate_capture_ok "$out/native" run_bounded "${runner[@]}" || return $?
  if grep -Fq ':False' "$out/native.stdout";then echo 'Gen2 assertion failed' >&2;return 1;fi
  if [[ "$mode" == all ]];then gate_output witness "$out/native.stdout" 'gen2-native-end:23' 'gen2-register-policy-no-callback:True' --count 1 --policy crlf || return $?;fi
  if [[ "$mode" == all || "$mode" == reflection ]];then
   gate_output witness "$out/native.stdout" 'gen2-ctor0-reflection:True' 'gen2-ctor1-reflection:True' 'gen2-uninitialized:True' 'gen2-probe-retained-finalizer-and-clone-refusal:True' --count 1 --policy crlf || return $?
   if [[ "${DN2CPP_GEN2_ALLOC_COUNTERS:-0}" == 1 ]];then
    gate_output witness "$out/native.stdout" 'gen2-probe-target-allocations:0' 'gen2-probe-target-registrations:0' 'gen2-probe-ordinary-allocation-registration:True' --count 1 --policy crlf || return $?
   fi
  fi
 done
}
