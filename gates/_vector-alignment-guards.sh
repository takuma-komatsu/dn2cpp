#!/usr/bin/env bash
# Unsupported aligned DIM authority is scoped to the real caller and two edges.
probe_unsupported_aligned_dim_guards() {
    local root="$1" out operation pair width element route mutation case_out status
    local accepted=0 refused=0 parser=0
    local fixture="${DN2CPP_VECTOR_ALIGNMENT_FIXTURE_DLL:-gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll}"
    local compiler="${DN2CPP_VECTOR_ALIGNMENT_COMPILER_DLL:-$(dirname "${DN2CPP_CLI_DLL:-src/Dn2Cpp.Cli/bin/$CONFIG/$TFM/dn2cpp.dll}")/Dn2Cpp.Transpiler.dll}"
    if [ -z "${DN2CPP_VECTOR_ALIGNMENT_FIXTURE_DLL:-}" ]; then
        build_gate_proj gates/fixtures/ldftn-local/LdftnLocalFixture.csproj || return 1
    fi
    mkdir -p "$root" || return 1
    out=$(mktemp -d "$root/authority-XXXXXX") || return 1
    gate_capture_ok "$out/model" run_bounded dotnet "$fixture" --unsupported-aligned-dim-guards \
        "$compiler" "$_CG_CORELIB" "$out/model.dll" || return 1
    [ "$(grep -Ec '^unsupported-aligned-dim-authority:(Load|Store)Aligned:(0:Decimal|(64|128|256|512):(Char|Int128|UInt128)):raw=[1-9][0-9]*:warm=[1-9][0-9]*:references=[1-9][0-9]*:qualified=[1-9][0-9]*$' "$out/model.stdout")" -eq 26 ] || return 1
    grep -E '^unsupported-aligned-dim-guards:roots=26:checks=[1-9][0-9]*:raw=[1-9][0-9]*:warm=[1-9][0-9]*:references=[1-9][0-9]*:qualified=[1-9][0-9]*$' "$out/model.stdout" > "$out/model-summary.txt" || return 1
    for operation in LoadAligned StoreAligned; do
        for pair in 0:Decimal 64:Char 64:Int128 64:UInt128 128:Char 128:Int128 128:UInt128 256:Char 256:Int128 256:UInt128 512:Char 512:Int128 512:UInt128; do
            width=${pair%%:*}; element=${pair#*:}
            for route in direct address calli; do
                case_out=$(mktemp -d "$out/$operation-$width-$element-$route-XXXXXX") || return 1
                gate_capture_ok "$case_out/generate" run_bounded dotnet "$fixture" --unsupported-aligned-dim-reference-fixture \
                    "$case_out/input.dll" "$_CG_CORELIB" "$operation:$width:$element:$route:valid" || return 1
                gate_capture_ok "$case_out/accept" invoke_cli "$case_out/input.dll" -r "$_CG_CORELIB" --no-ildiet -o "$case_out/emitted" || return 1
                printf '%s\n' "$operation:$width:$element:$route" >> "$out/accepted.txt" || return 1
                accepted=$((accepted + 1))
            done
        done
        for pair in 0:Decimal 128:Char; do
            width=${pair%%:*}; element=${pair#*:}
            for route in direct address calli; do
                for mutation in instance explicit-this vararg generic return byref modifier parameter concrete methodvar var-index owner-modifier owner-self owner-element methodspec methodspec-zero; do
                    case_out=$(mktemp -d "$out/$operation-$width-$element-$route-$mutation-XXXXXX") || return 1
                    gate_capture_ok "$case_out/generate" run_bounded dotnet "$fixture" --unsupported-aligned-dim-reference-fixture \
                        "$case_out/input.dll" "$_CG_CORELIB" "$operation:$width:$element:$route:$mutation" || return 1
                    gate_capture "$case_out/reject" invoke_cli "$case_out/input.dll" -r "$_CG_CORELIB" --no-ildiet -o "$case_out/emitted" || return 1
                    status=$(cat "$case_out/reject.status")
                    if [ "$mutation" = methodspec-zero ] && [ "$status" = 134 ]; then
                        grep -F 'System.BadImageFormatException: Signature type sequence must have at least one element.' "$case_out/reject.stderr" >/dev/null || return 1
                        grep -F 'MethodSpecification.DecodeSignature' "$case_out/reject.stderr" >/dev/null || return 1
                        printf '%s\n' "$operation:$width:$element:$route:$mutation" >> "$out/upstream-parser-refused.txt" || return 1
                        parser=$((parser + 1))
                    else
                        gate_output status "$status" 2 || return 1
                        grep -Ei 'aligned|ISimdVector|generic|type argument|unbound' "$case_out/reject.stderr" >/dev/null || return 1
                        printf '%s\n' "$operation:$width:$element:$route:$mutation" >> "$out/refused.txt" || return 1
                        refused=$((refused + 1))
                    fi
                done
            done
        done
    done
    [ "$accepted" -eq 78 ] && [ "$((refused + parser))" -eq 192 ] && [ "$parser" -le 12 ] || return 1
    printf 'unsupported-aligned-dim-cli-authority:accepted=%s:refused=%s:upstream-parser-refused=%s\n' "$accepted" "$refused" "$parser" > "$out/summary.txt" || return 1
    cat "$out/model-summary.txt" >> "$out/summary.txt" || return 1
    cat "$out/summary.txt"
}
