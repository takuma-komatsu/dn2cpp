#!/usr/bin/env bash
# Typed vector values, integer selection and callable reinterprets retain CLR semantics.
source "$(dirname "${BASH_SOURCE[0]}")/_vector-storage-probe.sh"
DN2CPP_GATE_EXTRA_INPUTS="${DN2CPP_GATE_EXTRA_INPUTS:-} gates/_vector-regression-checks.sh gates/_vector-storage-probe.sh gates/fixtures/vector-storage/check-generated.py"
DN2CPP_GATE_EXTRA_CONTEXT="${DN2CPP_GATE_EXTRA_CONTEXT:-}|vector-storage-prefix:before-storage-safety|vector-value-prefix:before-value-semantics"
DN2CPP_GATE_EXTRA_CONTEXT="$DN2CPP_GATE_EXTRA_CONTEXT|vector-integer-selection-prefix:before-integer-selection|vector-as:callable"

gate_extra_asserts() {
    local out="$1"
    vector_storage_asserts "$out"
    DOTNET_EnableAVX="${DN2CPP_ORACLE_DOTNET_ENABLE_AVX:-0}" \
        gate_capture_ok "$out/value-before" run_bounded dotnet "$_CG_APP" before-value-semantics
    gate_output select "$out/native.stdout" "$out/value-prefix.stdout" \
        --before '-- vector existing value semantics --' --policy crlf
    gate_output compare "$out/value-prefix.stdout" "$out/value-before.stdout" --policy crlf
    gate_output witness "$out/native.stdout" '-- vector existing value semantics --' \
        'typed NaN=True:True:True:True:True' 'comparison NaN=False:False' \
        'degrees overflow=False:False:False:False:False' '-- vector existing value semantics end --' \
        --count 1 --policy crlf
    vector_floating_prefix "$out"
    DOTNET_EnableAVX="${DN2CPP_ORACLE_DOTNET_ENABLE_AVX:-0}" \
        gate_capture_ok "$out/integer-selection-before" run_bounded dotnet "$_CG_APP" before-integer-selection
    gate_output select "$out/native.stdout" "$out/integer-selection-prefix.stdout" \
        --before '-- vector integer selection --' --policy crlf
    gate_output compare "$out/integer-selection-prefix.stdout" "$out/integer-selection-before.stdout"
    gate_output byte-witness "$out/native.stdout" '-- vector integer selection --' \
        'select int.MaxMagnitude=[-3,-2147483648,5,5]' \
        'select int.MinMagnitude=[2,1,-5,-5]' \
        'select int.MaxMagnitudeNumber=[-3,-2147483648,5,5]' \
        'select int.MinMagnitudeNumber=[2,1,-5,-5]' \
        'select int.callable.MaxNumber=[2,1,5,5]' \
        'select int.MinNumber=[-3,-2147483648,-5,-5]' \
        'select long.MinMagnitude=9223372036854775807' \
        'select ulong.MaxMagnitude=18446744073709551615' \
        '-- vector integer selection end --' '-- vector as callable --' \
        'as callable roundtrip=True' '-- vector as callable end --' --count 1 --policy crlf
}
