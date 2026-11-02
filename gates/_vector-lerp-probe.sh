#!/usr/bin/env bash
# Vector Lerp preserves the typed nonfused vector profile and exact callable identity.
probe_vector_lerp() {
    local root="$1" out profile axis emit
    local app="${DN2CPP_VECTOR_LERP_DLL:-gates/fixtures/vector-lerp/bin/$CONFIG/$TFM/VectorLerp.dll}"
    if [ -z "${DN2CPP_VECTOR_LERP_DLL:-}" ]; then
        build_gate_proj gates/fixtures/vector-lerp/VectorLerp.csproj || return 1
    fi
    mkdir -p "$root" || return 1
    out=$(mktemp -d "$root/lerp-XXXXXX") || return 1
    local clean=(env -u DOTNET_EnableHWIntrinsic -u DOTNET_EnableAVX -u DOTNET_EnableAVX2 -u DOTNET_EnableFMA
        -u COMPlus_EnableHWIntrinsic -u COMPlus_EnableAVX -u COMPlus_EnableAVX2 -u COMPlus_EnableFMA)
    for profile in default software; do
        local settings=()
        [ "$profile" != software ] || settings=(DOTNET_EnableHWIntrinsic=0)
        gate_capture_ok "$out/$profile" run_bounded "${clean[@]}" "${settings[@]}" dotnet "$app" || return 1
        [ ! -s "$out/$profile.stderr" ] || return 1
        gate_capture_ok "$out/$profile-contract" python3 gates/fixtures/vector-lerp/check-oracles.py \
            "$out/$profile.stdout" --profile "$profile" || return 1
    done
    for axis in raw diet; do
        emit=$(mktemp -d "$out/$axis-XXXXXX") || return 1
        local flags=() runner=()
        [ "$axis" != raw ] || flags+=(--no-ildiet)
        gate_capture_ok "$out/$axis-emit" invoke_cli "$app" -r "$_CG_CORELIB" --auto-ref "${flags[@]}" -o "$emit" || return 1
        if [ "$axis" = diet ]; then
            python3 - "$emit/ildiet" "$app" <<'PYDIET' || return 1
import sys
from pathlib import Path
sys.path.insert(0, "gates")
from ildiet_manifest import manifest
result, generation = manifest(sys.argv[1])
assert result.get("fallback") == "none" and result.get("cutsValidated") == "true"
assembly = next(a for a in result.findall("assembly") if a.get("name") == "VectorLerp")
assert assembly.get("mode") == "rewritten", "fixture was copied, not rewritten"
rewritten = Path(result.get("input"))
assert rewritten == generation / "VectorLerp.dll"
assert rewritten.read_bytes() != Path(sys.argv[2]).read_bytes(), "fixture bytes did not change"
PYDIET
        fi
        if [ "${WASM:-0}" = 1 ]; then
            compile_console_wasm "$emit" VectorLerp || return 1
            runner=(node "$emit/VectorLerp.js")
        else
            compile_console "$emit" VectorLerp || return 1
            runner=("$emit/VectorLerp$EXE_EXT")
        fi
        gate_capture_ok "$out/$axis-run" run_bounded "${runner[@]}" || return 1
        [ ! -s "$out/$axis-run.stderr" ] || return 1
        gate_capture_ok "$out/$axis-contract" python3 gates/fixtures/vector-lerp/check-oracles.py \
            "$out/$axis-run.stdout" --profile native --reference "$out/software.stdout" || return 1
    done
    vector_lerp_authority_probe "$out/authority"
}

vector_lerp_authority_probe() {
    local root="$1" out method route mutation position input case_out count=0 positives=0 upstream=0
    local fixture="${DN2CPP_VECTOR_LERP_FIXTURE_DLL:-gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll}"
    local compiler="${DN2CPP_VECTOR_LERP_COMPILER_DLL:-$(dirname "${DN2CPP_CLI_DLL:-src/Dn2Cpp.Cli/bin/$CONFIG/$TFM/dn2cpp.dll}")/Dn2Cpp.Transpiler.dll}"
    if [ -z "${DN2CPP_VECTOR_LERP_FIXTURE_DLL:-}" ]; then
        build_gate_proj gates/fixtures/ldftn-local/LdftnLocalFixture.csproj || return 1
    fi
    mkdir -p "$root" || return 1
    out=$(mktemp -d "$root/run-XXXXXX") || return 1
    gate_capture_ok "$out/guards" run_bounded dotnet "$fixture" --vector-lerp-guards \
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
            gate_capture_ok "$case_out/generate" run_bounded dotnet "$fixture" --vector-lerp-fixture \
                "$input" "$_CG_CORELIB" "$method:$route:valid" || return 1
            gate_capture_ok "$case_out/accept" invoke_cli "$input" -r "$_CG_CORELIB" --no-ildiet -o "$case_out/emitted" || return 1
            positives=$((positives + 1))
            printf '%s\n' "$method:$route:valid" >> "$out/accepted.txt" || return 1
            if [ "$method" = 128f ]; then
                case_out=$(mktemp -d "$out/$method-$route-valid-caller-XXXXXX") || return 1
                input="$case_out/input.dll"
                gate_capture_ok "$case_out/generate" run_bounded dotnet "$fixture" --vector-lerp-fixture                     "$input" "$_CG_CORELIB" "$method:$route:valid-caller" || return 1
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
                            "integer-$position" "scalar-$position" "decimal-$position" "methodvar-$position" "typevar-$position")
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
                gate_capture_ok "$case_out/generate" run_bounded dotnet "$fixture" --vector-lerp-fixture \
                    "$input" "$_CG_CORELIB" "$method:$route:$mutation" || return 1
                gate_capture "$case_out/reject" invoke_cli "$input" -r "$_CG_CORELIB" --no-ildiet -o "$case_out/emitted" || return 1
                if [ "$mutation" = spec-zero ]; then
                    # SRM rejects this invalid zero-arity blob before the family
                    # fence. Keep the exact upstream abort distinct from clean
                    # rejection; this does not claim decoder robustness.
                    grep -E '^vector-lerp-methodspec:2B[0-9A-F]{6}:target=0A[0-9A-F]{6}:raw=0A00$' "$case_out/generate.stdout" >/dev/null || return 1
                    if [ "$(cat "$case_out/reject.status")" = 134 ]; then
                        grep -Fx 'Unhandled exception. System.BadImageFormatException: Signature type sequence must have at least one element.' "$case_out/reject.stderr" >/dev/null || return 1
                        grep -F 'ReflectionSummarizer.DecodeCall' "$case_out/reject.stderr" >/dev/null || return 1
                        grep -F 'MethodSpecification.DecodeSignature' "$case_out/reject.stderr" >/dev/null || return 1
                        upstream=$((upstream + 1))
                        printf '%s\n' "$method:$route:$mutation" >> "$out/upstream-parser-rejected.txt" || return 1
                        continue
                    fi
                fi
                gate_output status "$(cat "$case_out/reject.status")" 2 || return 1
                if [ "$mutation" = spec-one ] || [ "$mutation" = spec-internal ]; then
                    grep -F 'Vector Lerp' "$case_out/reject.stderr" >/dev/null || return 1
                fi
                if ! grep -Ei 'Vector Lerp|System\.(Runtime\.Intrinsics\.Vector(64|128|256|512)|Numerics\.Vector).*Lerp' "$case_out/reject.stderr" >/dev/null; then
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
    [ "$positives" -eq 19 ] && [ "$((count + upstream))" -eq 359 ] && [ "$upstream" -le 3 ] || return 1
    printf 'vector-lerp-authority:accepted=%s:rejected=%s:upstream-parser-rejected=%s\n' "$positives" "$count" "$upstream" > "$out/summary.txt" || return 1
    cat "$out/summary.txt"
}

