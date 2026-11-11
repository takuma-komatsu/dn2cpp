#!/usr/bin/env bash
# CoreLib value-method and concrete element contracts beyond arithmetic parity.
vector_value_witnesses() {
    gate_output witness "$1" '-- vector value semantics --' 'vector value end' \
        'vector value collection integration passed' \
        'native-int counts and sequence lanes passed' \
        'native-int short/exact span and canaries passed' \
        'vector byte-span short/exact copy and canaries passed' \
        '-- fixed vector value semantics --' 'fixed vector value end' \
        'fixed vector 128 collection integration passed' \
        'fixed vector 256 collection integration passed' \
        '-- fixed width value smoke --' 'fixed64.float value/hash/callable passed' \
        'fixed512.double value/hash/callable passed' \
        'fixed64.enum.callable|exception=System.NotSupportedException|effects=' \
        'fixed512.enum.callable|exception=System.NotSupportedException|effects=' \
        'fixed width value smoke end' \
        '-- fixed vector unsupported semantics --' 'fixed vector unsupported end' \
        '-- vector unsupported semantics --' \
        'enum.typed|exception=System.NotSupportedException|effects=' \
        'ref-containing-struct.object-null|result=False|effects=' \
        '-- vector unsupported ABI --' \
        'abi.enum.delegate.marker|result=True|effects=' \
        'abi.body-enum-supported|result=False|effects=' \
        'abi.body-int-supported|result=True|effects=' \
        'vector unsupported ABI end' 'vector unsupported end' --count 1 --policy crlf
}

vector_section_witnesses() {
    vector_value_witnesses "$1"
    gate_output witness "$1" 'vector explicit end' \
        'retained fill byte and reference canaries passed' \
        'concrete vector delegate targets passed' 'vector multiply add estimate end' \
        '-- vector angle conversions end --' '-- vector trigonometry end --' \
        '-- vector predicate/copy complete --' '-- vector log end --' \
        '-- vector magnitude selection end --' '-- vector as end --' \
        'as.callable-enum|exception=System.NotSupportedException|effects=A' \
        'as.explicit-decimal|exception=System.NotSupportedException|effects=A' \
        'as.forwarded-decimal|exception=System.NotSupportedException|effects=A' \
        '-- concrete vector reductions end --' '-- vector probe complete --' --count 1 --policy crlf
    gate_output match-record "$1" 'block repeat and low-byte checks=[1-9][0-9]*' --count 1 --policy crlf
}

# The default Wasm image already contains this selector; no second emission or
# native build is needed for pointer-width-specific value and ABI coverage.
vector_value_wasm_gate() {
    local out="$1" app="$2"
    gate_capture_ok "$out/value-native" run_bounded node "$out/VectorProbe.js" value-semantics-only
    DOTNET_EnableAVX=0 gate_capture_ok "$out/value-oracle" run_bounded dotnet "$app" value-semantics-only
    gate_output compare "$out/value-native.stdout" "$out/value-oracle.stdout" --policy crlf
    vector_value_witnesses "$out/value-native.stdout"
    gate_output witness "$out/value-native.stderr" \
        'vector value target pointer=4 nint-count=4 independent-lanes=4' \
        'fixed vector target pointer=4 independent-native-lanes=4,8' --count 1 --policy crlf
}

# Vector4's JIT intrinsic omits the exception parameter name. Its managed
# fallback is the native contract; reuse the image with a strict section driver.
vector_concrete_elements_gate() {
    local out="$1" app="$2"
    gate_capture_ok "$out/concrete-native" run_bounded "./$out/VectorProbe$EXE_EXT" concrete-elements-only
    DOTNET_EnableHWIntrinsic=0 gate_capture_ok "$out/concrete-oracle" \
        run_bounded dotnet "$app" concrete-elements-only
    gate_output compare "$out/concrete-native.stdout" "$out/concrete-oracle.stdout" --policy crlf
    gate_output compare "$out/concrete-native.stderr" "$out/concrete-oracle.stderr" --policy crlf
    gate_output witness "$out/concrete-native.stdout" '-- concrete vector elements --' \
        'concrete vector delegate targets passed' --count 1 --policy crlf
    gate_output match-record "$out/concrete-native.stdout" \
        'concrete vector element bit/copy/bounds/order checks=[1-9][0-9]*' --count 1 --policy crlf
}
