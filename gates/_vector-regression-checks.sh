#!/usr/bin/env bash
# Typed vector equality and angle conversion retain CLR value semantics.
source "$(dirname "${BASH_SOURCE[0]}")/_vector-storage-probe.sh"
DN2CPP_GATE_EXTRA_INPUTS="${DN2CPP_GATE_EXTRA_INPUTS:-} gates/_vector-regression-checks.sh gates/_vector-storage-probe.sh gates/fixtures/vector-storage/check-generated.py"
DN2CPP_GATE_EXTRA_CONTEXT="${DN2CPP_GATE_EXTRA_CONTEXT:-}|vector-storage-prefix:before-storage-safety|vector-value-prefix:before-value-semantics"

if declare -F gate_extra_asserts >/dev/null; then
    eval "$(declare -f gate_extra_asserts | sed '1s/gate_extra_asserts/vector_profile_asserts/')"
else
    vector_profile_asserts() { :; }
fi

gate_extra_asserts() {
    local out="$1"
    # Keep the profile callback outside an OR-list so its errexit checks stay active.
    vector_profile_asserts "$out"
    vector_storage_asserts "$out"
    DOTNET_EnableAVX="${DN2CPP_ORACLE_DOTNET_ENABLE_AVX:-0}" \
        gate_capture_ok "$out/value-before" run_bounded dotnet "$_CG_APP" before-value-semantics
    gate_output select "$out/native.stdout" "$out/value-prefix.stdout" \
        --before '-- vector existing value semantics --' --policy crlf
    gate_output compare "$out/value-prefix.stdout" "$out/value-before.stdout" --policy crlf
    gate_output witness "$out/native.stdout" '-- vector existing value semantics --' \
        'typed NaN=True:True:True:True:True' 'comparison NaN=False:False' \
        'degrees overflow=True:True:True:True:True' '-- vector existing value semantics end --' \
        --count 1 --policy crlf
}
