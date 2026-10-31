#!/usr/bin/env bash
# Public floating vector estimates: software rounding, callable routes and CLR profiles.
vector_estimate_prefix() {
    local out="$1"
    gate_capture_ok "$out/estimate-before" run_bounded env DOTNET_EnableAVX=0 dotnet "$_CG_APP" before-vector-estimate || return 1
    gate_output select "$out/oracle.stdout" "$out/estimate-prefix.stdout" --before '-- vector multiply add estimate --' --policy crlf || return 1
    gate_output compare "$out/estimate-prefix.stdout" "$out/estimate-before.stdout" || return 1
    gate_output witness "$out/oracle.stdout" 'vector estimate ten direct delegate calli lane and order targets passed' --count 1 --policy crlf || return 1
}

vector_estimate_probe() {
    local out="$1/estimate" axis emit profile third_profile=no-avx2-fma
    local app="${DN2CPP_VECTOR_ESTIMATE_DLL:-gates/fixtures/vector-estimate/bin/$CONFIG/$TFM/VectorEstimate.dll}"
    if [ -z "${DN2CPP_VECTOR_ESTIMATE_DLL:-}" ]; then
        build_gate_proj gates/fixtures/vector-estimate/VectorEstimate.csproj || return 1
    fi
    mkdir -p "$out" || return 1
    # Clear inherited ISA overrides; the width-pinned aggregate oracle is not
    # the genuine default-JIT estimate oracle.
    local clean=(env -u DOTNET_EnableHWIntrinsic -u DOTNET_EnableAVX -u DOTNET_EnableAVX2 -u DOTNET_EnableFMA
        -u COMPlus_EnableHWIntrinsic -u COMPlus_EnableAVX -u COMPlus_EnableAVX2 -u COMPlus_EnableFMA)
    local third=(DOTNET_EnableAVX2=0)
    case "$(uname -m)" in
        x86_64|amd64|AMD64|i386|i486|i586|i686) ;;
        *) third=(DOTNET_EnableHWIntrinsic=0); third_profile=hardware-disabled-fallback;;
    esac
    printf '%s\n' "third-profile=$third_profile" "host=$(uname -m)" > "$out/profiles.txt" || return 1
    for profile in default nohw third; do
        local settings=()
        case "$profile" in nohw) settings=(DOTNET_EnableHWIntrinsic=0);; third) settings=("${third[@]}");; esac
        gate_capture_ok "$out/$profile" run_bounded "${clean[@]}" "${settings[@]}" dotnet "$app" || return 1
        gate_capture_ok "$out/$profile-capabilities" run_bounded "${clean[@]}" "${settings[@]}" dotnet "$app" --capabilities || return 1
    done
    grep -Fx 'fma=False' "$out/nohw-capabilities.stdout" >/dev/null || return 1
    grep -Fx 'fma=False' "$out/third-capabilities.stdout" >/dev/null || return 1
    gate_capture_ok "$out/oracle-contract" python3 gates/fixtures/vector-estimate/check-oracles.py \
        "$out/default.stdout" "$out/nohw.stdout" "$out/third.stdout" --third-profile "$third_profile" || return 1
    for axis in raw diet; do
        emit=$(mktemp -d "$out/$axis-XXXXXX") || return 1
        local flags=() runner=()
        [ "$axis" != raw ] || flags+=(--no-ildiet)
        gate_capture_ok "$out/$axis-emit" invoke_cli "$app" -r "$_CG_CORELIB" --auto-ref "${flags[@]}" -o "$emit" || return 1
        if [ "${WASM:-0}" = 1 ]; then
            compile_console_wasm "$emit" VectorEstimate || return 1
            runner=(node "$emit/VectorEstimate.js")
        else
            compile_console "$emit" VectorEstimate || return 1
            runner=("./$emit/VectorEstimate$EXE_EXT")
        fi
        gate_capture_ok "$out/$axis-run" run_bounded "${runner[@]}" || return 1
        gate_capture_ok "$out/$axis-contract" python3 gates/fixtures/vector-estimate/check-oracles.py \
            "$out/default.stdout" "$out/nohw.stdout" "$out/third.stdout" --third-profile "$third_profile" \
            --native "$out/$axis-run.stdout" || return 1
        gate_output compare "$out/$axis-run.stderr" "$out/nohw.stderr" --policy crlf || return 1
    done
    vector_estimate_authority_probe "$out/authority" || return 1
    probe_generic_vector_estimate "$1"
}

vector_estimate_authority_probe() {
    local root="$1" out method route mutation position input case_out count=0 positives=0 upstream=0
    local fixture="${DN2CPP_VECTOR_ESTIMATE_FIXTURE_DLL:-gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll}"
    local compiler="${DN2CPP_VECTOR_ESTIMATE_COMPILER_DLL:-$(dirname "${DN2CPP_CLI_DLL:-src/Dn2Cpp.Cli/bin/$CONFIG/$TFM/dn2cpp.dll}")/Dn2Cpp.Transpiler.dll}"
    if [ -z "${DN2CPP_VECTOR_ESTIMATE_FIXTURE_DLL:-}" ]; then
        build_gate_proj gates/fixtures/ldftn-local/LdftnLocalFixture.csproj || return 1
    fi
    mkdir -p "$root" || return 1
    out=$(mktemp -d "$root/run-XXXXXX") || return 1
    gate_capture_ok "$out/guards" run_bounded dotnet "$fixture" --vector-estimate-guards \
        "$compiler" "$_CG_CORELIB" "$out/model.dll" || return 1
    for method in 128f 64f 64d 128d 256f 256d 512f 512d nf nd; do
        for route in address direct calli; do
            # The numeric fixture executes all ten callable routes. These small
            # metadata fixtures independently pin import acceptance and rejection.
            if [ "$route" != direct ] && [ "$method" != 128f ] && [ "$method" != 512d ] && [ "$method" != nf ]; then
                continue
            fi
            case_out=$(mktemp -d "$out/$method-$route-valid-XXXXXX") || return 1
            input="$case_out/input.dll"
            gate_capture_ok "$case_out/generate" run_bounded dotnet "$fixture" --vector-estimate-fixture \
                "$input" "$_CG_CORELIB" "$method:$route:valid" || return 1
            gate_capture_ok "$case_out/accept" invoke_cli "$input" -r "$_CG_CORELIB" --no-ildiet -o "$case_out/emitted" || return 1
            positives=$((positives + 1))
            printf '%s\n' "$method:$route:valid" >> "$out/accepted.txt" || return 1
            if [ "$method" = 128f ]; then
                case_out=$(mktemp -d "$out/$method-$route-valid-caller-XXXXXX") || return 1
                input="$case_out/input.dll"
                gate_capture_ok "$case_out/generate" run_bounded dotnet "$fixture" --vector-estimate-fixture                     "$input" "$_CG_CORELIB" "$method:$route:valid-caller" || return 1
                gate_capture_ok "$case_out/accept" invoke_cli "$input" -r "$_CG_CORELIB" --no-ildiet -o "$case_out/emitted" || return 1
                positives=$((positives + 1))
                printf '%s\n' "$method:$route:valid-caller" >> "$out/accepted.txt" || return 1
            fi
            local mutations=()
            if [ "$route" = direct ]; then
                mutations=(instance vararg generic extra missing modopt-owner byref-owner generic-owner foreign-owner substitute generic-substitute)
                for position in result 0 1 2; do
                    mutations+=("width-$position" "element-$position" "byref-$position" "modopt-$position")
                    # The complete raw matrix is shared by every overload; one
                    # representative also exercises each remaining decoder shape.
                    if [ "$method" = 128f ]; then
                        mutations+=("pointer-$position" "pinned-$position" "modreq-$position" \
                            "integer-$position" "methodvar-$position" "typevar-$position")
                    fi
                done
            else
                mutations=(generic generic-owner byref-owner modopt-owner modopt-0 modreq-result substitute generic-substitute)
            fi
            if [ "$method" = 128f ]; then
                # Put the zero-arity address hole immediately after the generic
                # address regression rather than hiding it at the matrix tail.
                if [ "$route" = address ]; then
                    mutations=("${mutations[0]}" spec-zero spec-one spec-internal "${mutations[@]:1}")
                else
                    mutations+=(spec-zero spec-one spec-internal)
                fi
            fi
            for mutation in "${mutations[@]}"; do
                case_out=$(mktemp -d "$out/$method-$route-$mutation-XXXXXX") || return 1
                input="$case_out/input.dll"
                gate_capture_ok "$case_out/generate" run_bounded dotnet "$fixture" --vector-estimate-fixture \
                    "$input" "$_CG_CORELIB" "$method:$route:$mutation" || return 1
                gate_capture "$case_out/reject" invoke_cli "$input" -r "$_CG_CORELIB" --no-ildiet -o "$case_out/emitted" || return 1
                if [ "$mutation" = spec-zero ]; then
                    # SRM rejects this invalid zero-arity blob before the family
                    # fence. Keep the exact upstream abort distinct from clean
                    # rejection; this does not claim decoder robustness.
                    grep -E '^vector-estimate-methodspec:2B[0-9A-F]{6}:target=0A[0-9A-F]{6}:raw=0A00$' "$case_out/generate.stdout" >/dev/null || return 1
                    gate_output status "$(cat "$case_out/reject.status")" 134 || return 1
                    grep -Fx 'Unhandled exception. System.BadImageFormatException: Signature type sequence must have at least one element.' "$case_out/reject.stderr" >/dev/null || return 1
                    grep -F 'ReflectionSummarizer.DecodeCall' "$case_out/reject.stderr" >/dev/null || return 1
                    grep -F 'MethodSpecification.DecodeSignature' "$case_out/reject.stderr" >/dev/null || return 1
                    upstream=$((upstream + 1))
                    printf '%s\n' "$method:$route:$mutation" >> "$out/upstream-parser-rejected.txt" || return 1
                    continue
                fi
                gate_output status "$(cat "$case_out/reject.status")" 2 || return 1
                if [ "$mutation" = spec-one ] || [ "$mutation" = spec-internal ]; then
                    grep -F 'Vector MultiplyAddEstimate does not support method specifications' "$case_out/reject.stderr" >/dev/null || return 1
                fi
                if ! grep -Ei 'Vector MultiplyAddEstimate|System\.(Runtime\.Intrinsics\.Vector(64|128|256|512)|Numerics\.Vector).*MultiplyAddEstimate' "$case_out/reject.stderr" >/dev/null; then
                    case "$mutation" in
                        generic|generic-owner|methodvar-*|typevar-*|substitute|generic-substitute)
                            grep -Ei 'Type argument|generic parameter|generic type|unbound.*(type|method)' "$case_out/reject.stderr" >/dev/null || return 1;;
                        foreign-owner)
                            grep -E 'Foreign\.Vector\.Owner|External type reference.*could not be resolved' "$case_out/reject.stderr" >/dev/null || return 1;;
                        *) return 1;;
                    esac
                fi
                count=$((count + 1))
                printf '%s\n' "$method:$route:$mutation" >> "$out/rejected.txt" || return 1
            done
        done
    done
    [ "$positives" -eq 19 ] && [ "$count" -eq 348 ] && [ "$upstream" -eq 3 ] || return 1
    printf 'vector-estimate-authority:accepted=%s:rejected=%s:upstream-parser-rejected=%s\n' "$positives" "$count" "$upstream" > "$out/summary.txt" || return 1
    cat "$out/summary.txt"
}

# Unsupported closed generic estimates retain the managed Count exception.
probe_generic_vector_estimate() {
    local root="$1" out axis emit
    local fixture="${DN2CPP_GENERIC_VECTOR_ESTIMATE_FIXTURE_DLL:-${DN2CPP_VECTOR_ESTIMATE_FIXTURE_DLL:-gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll}}"
    local compiler="${DN2CPP_GENERIC_VECTOR_ESTIMATE_COMPILER_DLL:-${DN2CPP_VECTOR_ESTIMATE_COMPILER_DLL:-$(dirname "${DN2CPP_CLI_DLL:-src/Dn2Cpp.Cli/bin/$CONFIG/$TFM/dn2cpp.dll}")/Dn2Cpp.Transpiler.dll}}"
    if [ -z "${DN2CPP_GENERIC_VECTOR_ESTIMATE_FIXTURE_DLL:-${DN2CPP_VECTOR_ESTIMATE_FIXTURE_DLL:-}}" ]; then
        build_gate_proj gates/fixtures/ldftn-local/LdftnLocalFixture.csproj || return 1
    fi
    mkdir -p "$root" || return 1
    out=$(mktemp -d "$root/generic-estimate-XXXXXX") || return 1
    gate_capture_ok "$out/guards" run_bounded dotnet "$fixture" --generic-vector-estimate-guards \
        "$compiler" "$_CG_CORELIB" "$out/model.dll" || return 1
    grep -E '^generic-vector-estimate-guards:checks=[1-9][0-9]*$' "$out/guards.stdout" >/dev/null || return 1
    gate_capture_ok "$out/oracle" run_bounded dotnet "$fixture" --generic-vector-estimate-oracle || return 1
    gate_output witness "$out/oracle.stdout" '123' --count 18 --policy crlf || return 1
    gate_output witness "$out/oracle.stdout" 'System.NotSupportedException' --count 18 --policy crlf || return 1
    [ "$(wc -l < "$out/oracle.stdout")" -eq 54 ] || return 1
    gate_capture_ok "$out/generate" run_bounded dotnet "$fixture" --generic-vector-estimate-fixture \
        "$out/runtime.dll" "$_CG_CORELIB" all-runtime || return 1
    for axis in raw diet; do
        emit=$(mktemp -d "$out/$axis-XXXXXX") || return 1
        local flags=() runner=()
        [ "$axis" != raw ] || flags+=(--no-ildiet)
        gate_capture_ok "$out/$axis-emit" invoke_cli "$out/runtime.dll" -r "$_CG_CORELIB" --auto-ref "${flags[@]}" -o "$emit" || return 1
        if [ "${WASM:-0}" = 1 ]; then
            compile_console_wasm "$emit" GenericVectorEstimate || return 1
            runner=(node "$emit/GenericVectorEstimate.js")
        else
            compile_console "$emit" GenericVectorEstimate || return 1
            runner=("./$emit/GenericVectorEstimate$EXE_EXT")
        fi
        gate_capture_ok "$out/$axis-run" run_bounded "${runner[@]}" || return 1
        gate_output compare "$out/$axis-run.stdout" "$out/oracle.stdout" --policy crlf || return 1
        gate_output compare "$out/$axis-run.stderr" "$out/oracle.stderr" --policy crlf || return 1
    done
}
