#!/usr/bin/env bash
# The ResourceReader formatter boundary intentionally refuses unsupported deserialization.
resource_formatter_gate() {
    local root="$1" corelib route axis out
    local fixture="${DN2CPP_RESOURCE_FORMATTER_FIXTURE:-gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll}"
    local original="${DN2CPP_RESOURCE_FORMATTER_INPUT:-gates/fixtures/resource-formatter/bin/$CONFIG/$TFM}"
    local production="${DN2CPP_RESOURCE_FORMATTER_PRODUCTION:-src/Dn2Cpp.Transpiler/bin/$CONFIG/$TFM/Dn2Cpp.Transpiler.dll}"
    [[ ! -e "$root" ]] || { echo "Resource formatter output must be fresh: $root" >&2; return 1; }
    corelib=$(resolve_net10_corelib) || return $?
    if [[ -z "${DN2CPP_RESOURCE_FORMATTER_FIXTURE:-}" ]]; then build_gate_proj gates/fixtures/ldftn-local || return $?; fi
    if [[ -z "${DN2CPP_RESOURCE_FORMATTER_INPUT:-}" ]]; then build_gate_proj gates/fixtures/resource-formatter || return $?; fi
    mkdir -p "$root"
    dotnet "$fixture" --resource-formatter-guards "$production" "$corelib" "$root/model-input.dll" || return $?
    local routes="${DN2CPP_RESOURCE_FORMATTER_ROUTES:-direct pointer address reflection all}"
    local axes="${DN2CPP_RESOURCE_FORMATTER_AXES:-raw diet}"
    if [[ -n "${DN2CPP_RESOURCE_FORMATTER_ROUTES:-}${DN2CPP_RESOURCE_FORMATTER_AXES:-}${DN2CPP_RESOURCE_FORMATTER_EMIT_ONLY:-}" ]]; then
        echo "DIAGNOSTIC PARTIAL PROFILE: routes=$routes axes=$axes; not the complete resource formatter gate" >&2
    fi
    for route in $routes; do
        mkdir -p "$root/$route-input"
        cp -R "$original/." "$root/$route-input/" || return $?
        dotnet "$fixture" --resource-formatter "$root/$route-input/ResourceFormatter.dll" "$corelib" "$route" || return $?
        for axis in $axes; do
            out="$root/$route-$axis"
            local flags=()
            [[ "$axis" != raw ]] || flags+=(--no-ildiet)
            invoke_cli "$root/$route-input/ResourceFormatter.dll" --auto-ref "${flags[@]}" -o "$out" || return $?
            "$(resolve_python)" gates/fixtures/resource-formatter/check-generated.py "$out" --route "$route" || return $?
            [[ "$route" == all && "${DN2CPP_RESOURCE_FORMATTER_EMIT_ONLY:-0}" != 1 ]] || continue
            # Unlike CLR construction, dn2cpp deliberately refuses both externs.
            # This snapshot is a policy freeze, not a claim of constructor parity.
            local native_out
            native_out=$("$(resolve_python)" -c 'import os, sys; print(os.path.relpath(sys.argv[1]))' "$out") || return $?
            compile_console "$native_out" ResourceFormatter || return $?
            gate_capture_ok "$out/native" run_bounded "$out/ResourceFormatter$EXE_EXT" || return $?
            gate_output compare gates/fixtures/resource-formatter/expected.stdout "$out/native.stdout" || return $?
            gate_output byte-witness "$out/native.stdout" 'resource-formatter:end' --count any || return $?
        done
    done
    [[ "${DN2CPP_RESOURCE_FORMATTER_EMIT_ONLY:-0}" != 1 ]] || return 0
    resource_formatter_negative_gate "$root/negatives" "$original" "$fixture" "$corelib"
}
resource_formatter_negative_gate() {
    local root="$1" original="$2" fixture="$3" corelib="$4" route shape axis mode rc
    for route in direct address; do
        for shape in return byref streambyref modifier parammodifier instance generic varargs wrongtype arity spec; do
            mode="negative-$route-$shape"
            mkdir -p "$root/$mode/input"
            cp -R "$original/." "$root/$mode/input/" || return $?
            dotnet "$fixture" --resource-formatter "$root/$mode/input/ResourceFormatter.dll" "$corelib" "$mode" || return $?
            for axis in raw diet; do
                local flags=()
                [[ "$axis" != raw ]] || flags+=(--no-ildiet)
                rc=0
                invoke_cli "$root/$mode/input/ResourceFormatter.dll" --auto-ref "${flags[@]}" -o "$root/$mode/$axis" > "$root/$mode/$axis.log" 2>&1 || rc=$?
                printf '%s\n' "$rc" > "$root/$mode/$axis.status"
                [[ "$rc" == 2 ]] || return 1
                grep -Eq '(ResourceReader|resource formatter).*(exact MemberRef signature|exact supported declaration|no (generic )?method|has no intrinsic mapping yet)' "$root/$mode/$axis.log" || return 1
            done
        done
    done
}
resource_formatter_fresh_gate() {
    local generation
    mkdir -p artifacts || return $?
    generation=$(mktemp -d "artifacts/resource-formatter-${CONFIG}${DN2CPP_OUT_SUFFIX:-}.XXXXXXXX") || return $?
    resource_formatter_gate "$generation/run"
}

# A separate live CLR differential keeps the managed permit guard ahead of the externs.
resource_formatter_permit_gate() (
    unset -f gate_extra_asserts
    build_gate_proj gates/fixtures/resource-formatter-guards || return $?
    DN2CPP_SKIP_BUILD=1 DN2CPP_SAMPLE_PROJECT_DIR=gates/fixtures/resource-formatter-guards \
        corelib_diff_gate ResourceFormatterGuards --auto-ref --no-ildiet
)
