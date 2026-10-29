#!/usr/bin/env bash
# Real BPI imported constructors must refuse before target allocation in both engines.
gen2_interpreter_gate() {
    local base="$1" patch="$2" sealed="$3" out="$4"
    shift 4
    local flags=("$@") encoding
    mkdir -p "$out"
    # These three input DLLs have already been rewritten by the native-only fixture.
    invoke_cli "$base" -r "$(locate_corelib)" --auto-ref --hotupdate-base \
        --link-xml gates/fixtures/gen2-callback/link.xml "${flags[@]}" -o "$out/base" > "$out/base.transpile.log" 2>&1 || return $?
    gen2_callback_prepare_probe "$out/base" || return $?
    local DN2CPP_EXTRA_LINK_FLAGS="${DN2CPP_EXTRA_LINK_FLAGS:-}"
    if [[ "${DN2CPP_GEN2_ALLOC_COUNTERS:-0}" == 1 ]]; then
        DN2CPP_EXTRA_LINK_FLAGS+=' -Wl,--wrap=_Z12dn2cpp_allocm -Wl,--wrap=_Z25dn2cpp_register_finalizerP12Dn2CppObject'
    fi
    compile_console "$out/base" Gen2CallbackBase || return $?
    for encoding in register stack; do
        local patch_flags=()
        [ "$encoding" != stack ] || patch_flags=(--patch-stackcode)
        invoke_cli --emit-patch "$patch" --base-abi "$out/base/base-abi.json" \
            "${patch_flags[@]}" -o "$out/$encoding" > "$out/$encoding.emit.log" 2>&1 || return $?
        gate_capture_ok "$out/$encoding.native" run_bounded \
            "$out/base/Gen2CallbackBase$EXE_EXT" "$out/$encoding/Gen2CallbackPatch.bpi" || return $?
        gate_output witness "$out/$encoding.native.stdout" \
            'gen2-interp-ctor0:True' 'gen2-interp-end' --count 1 --policy crlf || return $?
        if [[ "${DN2CPP_GEN2_ALLOC_COUNTERS:-0}" == 1 ]]; then
            gate_output witness "$out/$encoding.native.stdout" 'gen2-probe-target-allocations:0' 'gen2-probe-target-registrations:0' --count 1 --policy crlf || return $?
        fi
        gate_output witness "$out/$encoding.native.stdout" 'gen2-interp-ordinary:True' --count 1 --policy crlf || return $?
        if grep -Fq 'gen2-interp-host-rejected:' "$out/$encoding.native.stdout"; then
            echo 'FAIL: patch did not execute the allocation fence' >&2
            return 1
        fi
        invoke_cli --emit-patch "$sealed" --base-abi "$out/base/base-abi.json" \
            "${patch_flags[@]}" -o "$out/$encoding/sealed" > "$out/$encoding.sealed.emit.log" 2>&1 || return $?
        gate_capture_ok "$out/$encoding.sealed.native" run_bounded \
            "$out/base/Gen2CallbackBase$EXE_EXT" "$out/$encoding/sealed/Gen2CallbackSealed.bpi" || return $?
        gate_output witness "$out/$encoding.sealed.native.stdout" \
            'gen2-interp-host-rejected:BPI bind: patch base type is not an inheritable class' --count 1 --policy crlf || return $?
        if grep -Fq 'gen2-sealed-unexpected-execution' "$out/$encoding.sealed.native.stdout"; then
            echo 'FAIL: sealed base patch executed' >&2
            return 1
        fi
    done
}
