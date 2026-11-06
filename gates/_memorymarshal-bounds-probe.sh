#!/usr/bin/env bash
# Existing MemoryMarshal Read/Write reject short spans with the managed exception.
memorymarshal_bounds_asserts() {
    local out="$1" operation length
    gate_capture_ok "$out/marshal-before" run_bounded dotnet "$_CG_APP" before-marshal-bounds
    gate_output select "$out/native.stdout" "$out/marshal-prefix.stdout" --before '== MemoryMarshal bounds ==' --policy crlf
    gate_output compare "$out/marshal-prefix.stdout" "$out/marshal-before.stdout" --policy crlf
    for operation in read write; do
        for length in 0 3; do
            gate_output witness "$out/native.stdout" "$operation $length=length:Specified argument was out of the range of valid values. (Parameter 'length')" --count 1 --policy crlf
        done
    done
    gate_output witness "$out/native.stdout" 'unchanged=11,22,33,44' --count 2 --policy crlf
    gate_output witness "$out/native.stdout" 'exact=123456789' --count 1 --policy crlf
    gate_output witness "$out/native.stdout" 'MemoryMarshal bounds end' --count 1 --policy crlf
}
