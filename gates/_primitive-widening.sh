#!/usr/bin/env bash
# Primitive widening preserves the input RVA rows, source fallback and target masked shifts.
primitive_widening_contract_asserts() {
    local output="$1" label
    gate_output witness "$output" '== Primitive widening contract ==' --count 1 --policy crlf || return $?
    for label in public-widen public-identity public-reject; do
        gate_output witness "$output" "primitive-widening-$label:True" --count 1 --policy crlf || return $?
    done
    gate_output witness "$output" 'Primitive widening contract end' --count 1 --policy crlf
}

primitive_widening_asserts() {
    local output="$1" mode="$2" route
    local routes=(reflection)
    if [[ "$mode" == all ]]; then
        primitive_widening_contract_asserts "$output" || return $?
        routes+=(direct calli)
        for route in U8 I8 U16 I16 I32 U32 I64 instance generic-U8 generic-I8 generic-U16 generic-I16; do
            gate_output witness "$output" "narrow-enum-$route:True" --count 1 --policy crlf || return $?
        done
        gate_output witness "$output" 'Narrow enum argument routes end' --count 1 --policy crlf || return $?
        gate_output witness "$output" 'primitive-widening-direct-stack-normalization:True' --count 1 --policy crlf || return $?
        gate_output witness "$output" 'primitive-widening-calli-stack-normalization:True' --count 1 --policy crlf || return $?
        gate_output witness "$output" 'Primitive widening routes end' --count 1 --policy crlf || return $?
    fi
    for route in "${routes[@]}"; do
        gate_output witness "$output" "primitive-widening-$route-pairs:65536" --count 1 --policy crlf || return $?
        gate_output witness "$output" "primitive-widening-$route-true:690" --count 1 --policy crlf || return $?
        gate_output witness "$output" "primitive-widening-$route-hash:1131241327" --count 1 --policy crlf || return $?
        gate_output witness "$output" "primitive-widening-$route-edges:True" --count 1 --policy crlf || return $?
    done
    gate_output witness "$output" 'primitive-widening-reflection-row:True' --count 1 --policy crlf || return $?
    gate_output witness "$output" 'Primitive widening reflection end' --count 1 --policy crlf
}

primitive_widening_routes_gate() {
    local root="$1" original="samples/dotnet/SelfHostPrimSubset/bin/$CONFIG/$TFM" mode axis input out oracle flag
    shift
    mkdir -p "$root"
    for mode in all reflection; do
        flag=--primitive-widening
        [[ "$mode" != reflection ]] || flag+=-reflection
        oracle="$root/oracle-$mode"
        mkdir -p "$oracle"
        cp "$original/"* "$oracle/" || return $?
        dotnet "gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll" \
            "$flag-oracle" "$oracle/SelfHostPrimSubset.dll" "$(resolve_net10_corelib)" || return $?
        # CLR executes only local RVA data and public span calls; private helper routes are native-only.
        gate_capture_ok "$oracle/result" run_bounded dotnet "$oracle/SelfHostPrimSubset.dll" || return $?
        primitive_widening_asserts "$oracle/result.stdout" "$mode" || return $?
        input="$root/input-$mode"
        mkdir -p "$input"
        cp "$original/"* "$input/" || return $?
        dotnet "gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll" \
            "$flag" "$input/SelfHostPrimSubset.dll" "$(resolve_net10_corelib)" || return $?
        for axis in raw diet; do
            out="$root/$mode-$axis"
            local flags=("$@")
            [[ "$axis" != raw ]] || flags+=(--no-ildiet)
            invoke_cli "$input/SelfHostPrimSubset.dll" --auto-ref "${flags[@]}" -o "$out" || return $?
            if [[ "$mode" == all && " $* " != *" --no-shared-generics "* ]]; then
                local width
                for width in Byte SByte Int16 UInt16; do
                    if ! grep -q "^DN2CPP_NOINLINE int32_t EnumArguments451_Identity_TisCn${width}_.*(int32_t a0)" "$out"/generated*.cpp; then
                        echo "FAIL: missing shared narrow-enum identity body for $width" >&2
                        return 1
                    fi
                done
            fi
            local runner=()
            if [[ "${WASM:-0}" == 1 ]]; then
                compile_console_wasm "$out" PrimitiveWidening || return $?
                runner=(node "$out/PrimitiveWidening.js")
            else
                compile_console "$out" PrimitiveWidening || return $?
                runner=("$out/PrimitiveWidening$EXE_EXT")
            fi
            gate_capture_ok "$out/native" run_bounded "${runner[@]}" || return $?
            primitive_widening_asserts "$out/native.stdout" "$mode" || return $?
            gate_output compare "$oracle/result.stdout" "$out/native.stdout" --policy crlf || return $?
        done
    done
}
