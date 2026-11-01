#!/usr/bin/env bash
# Exact generic magnitude declarations reject malformed and unsupported closures.
vector_magnitude_authority_probe() {
    local root="$1" out operation width element route mutation input case_out count=0 positives=0 upstream=0
    local fixture="${DN2CPP_VECTOR_MAGNITUDE_FIXTURE_DLL:-gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll}"
    local compiler="${DN2CPP_VECTOR_MAGNITUDE_COMPILER_DLL:-$(dirname "${DN2CPP_CLI_DLL:-src/Dn2Cpp.Cli/bin/$CONFIG/$TFM/dn2cpp.dll}")/Dn2Cpp.Transpiler.dll}"
    if [ -z "${DN2CPP_VECTOR_MAGNITUDE_FIXTURE_DLL:-}" ]; then
        build_gate_proj gates/fixtures/ldftn-local/LdftnLocalFixture.csproj || return 1
    fi
    mkdir -p "$root" || return 1
    out=$(mktemp -d "$root/run-XXXXXX") || return 1
    gate_capture_ok "$out/guards" run_bounded dotnet "$fixture" --vector-magnitude-guards \
        "$compiler" "$_CG_CORELIB" "$out/model.dll" || return 1
    [ "$(grep -Ec '^vector-magnitude-guards:(Max|Min)Magnitude(Number)?:width=(0|64|128|256|512):checks=[0-9]+$' "$out/guards.stdout")" -eq 20 ] || return 1
    for operation in MaxMagnitude MaxMagnitudeNumber MinMagnitude MinMagnitudeNumber; do
        for width in 0 64 128 256 512; do
            for element in float double; do
                for route in direct address calli; do
                    case_out=$(mktemp -d "$out/$operation-$width-$element-$route-valid-XXXXXX") || return 1
                    input="$case_out/input.dll"
                    gate_capture_ok "$case_out/generate" run_bounded dotnet "$fixture" --vector-magnitude-fixture \
                        "$input" "$_CG_CORELIB" "$operation:$width:$element:$route:valid" || return 1
                    gate_capture_ok "$case_out/accept" invoke_cli "$input" -r "$_CG_CORELIB" --no-ildiet -o "$case_out/emitted" || return 1
                    positives=$((positives + 1))
                    printf '%s\n' "$operation:$width:$element:$route:valid" >> "$out/accepted.txt" || return 1
                done
            done
        done
        for route in direct address calli; do
            case_out=$(mktemp -d "$out/$operation-mvar-$route-XXXXXX") || return 1
            input="$case_out/input.dll"
            gate_capture_ok "$case_out/generate" run_bounded dotnet "$fixture" --vector-magnitude-fixture \
                "$input" "$_CG_CORELIB" "$operation:128:float:$route:valid-mvar" || return 1
            gate_capture_ok "$case_out/accept" invoke_cli "$input" -r "$_CG_CORELIB" --no-ildiet -o "$case_out/emitted" || return 1
            positives=$((positives + 1))
            printf '%s\n' "$operation:128:float:$route:valid-mvar" >> "$out/accepted.txt" || return 1
            for mutation in instance vararg arity nongeneric concrete typevar methodvar-one extra missing byref-result modopt-0 modreq-1 pointer-1 width modopt-owner byref-owner generic-owner foreign-owner zero two trailing-spec trailing-reference reference primitive boolean byreflike nullable byref-argument pointer-argument modopt-argument bare bare-concrete; do
                case_out=$(mktemp -d "$out/$operation-$route-$mutation-XXXXXX") || return 1
                input="$case_out/input.dll"
                gate_capture_ok "$case_out/generate" run_bounded dotnet "$fixture" --vector-magnitude-fixture \
                    "$input" "$_CG_CORELIB" "$operation:128:float:$route:$mutation" || return 1
                gate_capture "$case_out/reject" invoke_cli "$input" -r "$_CG_CORELIB" --no-ildiet -o "$case_out/emitted" || return 1
                if [ "$mutation" = zero ] && [ "$(cat "$case_out/reject.status")" = 134 ]; then
                    grep -E '^vector-magnitude-methodspec:2B[0-9A-F]{6}:raw=0A00$' "$case_out/generate.stdout" >/dev/null || return 1
                    grep -Fx 'Unhandled exception. System.BadImageFormatException: Signature type sequence must have at least one element.' "$case_out/reject.stderr" >/dev/null || return 1
                    grep -F 'ReflectionSummarizer.DecodeCall' "$case_out/reject.stderr" >/dev/null || return 1
                    grep -F 'MethodSpecification.DecodeSignature' "$case_out/reject.stderr" >/dev/null || return 1
                    upstream=$((upstream + 1))
                    printf '%s\n' "$operation:128:float:$route:$mutation" >> "$out/upstream-parser-rejected.txt" || return 1
                    continue
                fi
                gate_output status "$(cat "$case_out/reject.status")" 2 || return 1
                if ! grep -Ei 'Magnitude|generic parameter|generic type|Type argument|unbound.*(type|method)|Foreign.Vector.Owner|External type reference.*could not be resolved' "$case_out/reject.stderr" >/dev/null; then
                    [ "$mutation" = foreign-owner ] || return 1
                    grep -Fx "error: Generic method's declaring type System.Numerics.Vector did not resolve to a loaded class" "$case_out/reject.stderr" >/dev/null || return 1
                fi
                count=$((count + 1))
                printf '%s\n' "$operation:128:float:$route:$mutation" >> "$out/rejected.txt" || return 1
            done
            for element in int uint long ulong byte sbyte short ushort nint nuint bool char object decimal enum struct refs int128 uint128; do
                case_out=$(mktemp -d "$out/$operation-$route-$element-XXXXXX") || return 1
                input="$case_out/input.dll"
                gate_capture_ok "$case_out/generate" run_bounded dotnet "$fixture" --vector-magnitude-fixture \
                    "$input" "$_CG_CORELIB" "$operation:128:$element:$route:valid" || return 1
                gate_capture "$case_out/reject" invoke_cli "$input" -r "$_CG_CORELIB" --no-ildiet -o "$case_out/emitted" || return 1
                gate_output status "$(cat "$case_out/reject.status")" 2 || return 1
                grep -F 'Vector magnitude selection requires' "$case_out/reject.stderr" >/dev/null || return 1
                count=$((count + 1))
                printf '%s\n' "$operation:128:$element:$route:valid" >> "$out/rejected.txt" || return 1
            done
        done
    done
    [ "$positives" -eq 132 ] && [ "$((count + upstream))" -eq 612 ] && [ "$upstream" -le 12 ] || return 1
    printf 'vector-magnitude-authority:accepted=%s:rejected=%s:upstream-parser-rejected=%s\n' "$positives" "$count" "$upstream" > "$out/summary.txt" || return 1
    cat "$out/summary.txt"
}
