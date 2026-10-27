#!/usr/bin/env bash
# Retained MakeGenericType uses native AOT reflection; private Instantiate preserves pre-QCall checks.
make_generic_public_route_asserts() {
    local output="$1" route="$2" label
    for label in single reference pair constraint open repeated null-array nondefinition nondefinition-null arity null-element arity-before-element void byref pointer; do
        gate_output witness "$output" "make-generic-$route-$label:True" --count 1 --policy crlf || return $?
    done
}

make_generic_contract_asserts() {
    local output="$1" label
    gate_output witness "$output" '== Runtime type MakeGenericType contract ==' --count 1 --policy crlf || return $?
    make_generic_public_route_asserts "$output" public || return $?
    for label in planned-closure forwarded-closure; do
        gate_output witness "$output" "make-generic-$label:True" --count 1 --policy crlf || return $?
    done
    for label in null-receiver both-null; do
        gate_output witness "$output" "make-generic-public-$label:True" --count 1 --policy crlf || return $?
    done
    gate_output witness "$output" 'Runtime type MakeGenericType contract end' --count 1 --policy crlf
}

make_generic_public_asserts() {
    local output="$1" route label
    make_generic_contract_asserts "$output" || return $?
    for route in direct virtual pointer address virtual-address flowing-address; do
        make_generic_public_route_asserts "$output" "$route" || return $?
    done
    for label in direct-null-receiver pointer-null-receiver direct-both-null pointer-both-null virtual-both-null delegate-identity delegate-reflection delegate-reflection-null virtual-delegate-identity; do
        gate_output witness "$output" "make-generic-$label:True" --count 1 --policy crlf || return $?
    done
    gate_output witness "$output" 'Runtime type MakeGenericType public routes end' --count 1 --policy crlf
}

make_generic_private_route_asserts() {
    local output="$1" route="$2" label
    for label in scalar-valid scalar-null scalar-default scalar-both-null array-valid array-null array-empty array-null-element array-default array-default-null array-default-empty array-default-element not-definition constraint; do
        gate_output witness "$output" "make-generic-$route-$label:True" --count 1 --policy crlf || return $?
    done
}

make_generic_private_asserts() {
    local output="$1" mode="$2" route
    if [[ "$mode" == private ]]; then
        make_generic_contract_asserts "$output" || return $?
        for route in direct pointer delegate; do make_generic_private_route_asserts "$output" "$route" || return $?; done
        gate_output witness "$output" 'make-generic-private-delegate-identity:True' --count 1 --policy crlf || return $?
        gate_output witness "$output" 'Runtime type Instantiate private routes end' --count 1 --policy crlf || return $?
    fi
    make_generic_private_route_asserts "$output" reflection || return $?
    gate_output witness "$output" 'Runtime type Instantiate reflection end' --count 1 --policy crlf
}

make_generic_routes_gate() {
    local root="$1" original="samples/dotnet/SelfHostPrimSubset/bin/$CONFIG/$TFM" mode axis input out
    shift
    mkdir -p "$root"
    gate_capture_ok "$root/public-oracle" run_bounded dotnet "$original/SelfHostPrimSubset.dll" make-generic-public || return $?
    gate_capture_ok "$root/contract-oracle" run_bounded dotnet "$original/SelfHostPrimSubset.dll" make-generic-contract || return $?
    make_generic_public_asserts "$root/public-oracle.stdout" || return $?
    for mode in public private reflection; do
        input="$root/input-$mode"
        mkdir -p "$input"
        cp "$original/"* "$input/" || return $?
        dotnet "gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll" \
            "--make-generic-$mode" "$input/SelfHostPrimSubset.dll" "$(resolve_net10_corelib)" || return $?
        local axes=(raw diet)
        [[ "$mode" != public ]] || axes+=(coreonly-raw coreonly-diet)
        for axis in "${axes[@]}"; do
            out="$root/$mode-$axis"
            local flags=("$@") refs=(--auto-ref)
            [[ "$axis" != raw && "$axis" != coreonly-raw ]] || flags+=(--no-ildiet)
            if [[ "$axis" == coreonly-* ]]; then
                refs=(-r "$(resolve_net10_corelib)" --no-default-ref DnZlib --no-default-ref DnBrotli --no-default-ref DnHttp)
            fi
            invoke_cli "$input/SelfHostPrimSubset.dll" "${refs[@]}" "${flags[@]}" -o "$out" || return $?
            local runner=()
            if [[ "${WASM:-0}" == 1 ]]; then
                compile_console_wasm "$out" RuntimeMakeGeneric || return $?
                runner=(node "$out/RuntimeMakeGeneric.js")
            else
                compile_console "$out" RuntimeMakeGeneric || return $?
                runner=("$out/RuntimeMakeGeneric$EXE_EXT")
            fi
            gate_capture_ok "$out/native" run_bounded "${runner[@]}" || return $?
            if [[ "$mode" == public ]]; then
                make_generic_public_asserts "$out/native.stdout" || return $?
                gate_output compare "$root/public-oracle.stdout" "$out/native.stdout" --policy crlf || return $?
                gate_capture_ok "$out/boundary" run_bounded "${runner[@]}" native-boundary || return $?
                make_generic_public_asserts "$out/boundary.stdout" || return $?
                for label in virtual-address-null flowing-address-null; do
                    gate_output witness "$out/boundary.stdout" "make-generic-$label:True" --count 1 --policy crlf || return $?
                done
            else
                make_generic_private_asserts "$out/native.stdout" "$mode" || return $?
                if [[ "$mode" == private ]]; then
                    gate_output prefix "$out/native.stdout" "$root/contract-oracle.stdout" \
                        --before 'make-generic-direct-scalar-valid:True' --policy crlf || return $?
                fi
            fi
        done
    done
}
