#!/usr/bin/env bash
# Exact ResourceManager type-name validation through calls, addresses and reflection.
resource_type_guard_gate() {
    local root="$1" corelib route axis out
    local fixture="${DN2CPP_RESOURCE_GUARD_FIXTURE:-gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll}"
    local original="${DN2CPP_RESOURCE_GUARD_INPUT:-gates/fixtures/resource-type-guard/bin/$CONFIG/$TFM}"
    local production="${DN2CPP_RESOURCE_GUARD_PRODUCTION:-src/Dn2Cpp.Transpiler/bin/$CONFIG/$TFM/Dn2Cpp.Transpiler.dll}"
    local diet="${DN2CPP_RESOURCE_GUARD_DIET:-src/ILDiet/bin/$CONFIG/$TFM/ILDiet.dll}"
    [[ ! -e "$root" ]] || { echo "Resource guard output must be fresh: $root" >&2; return 1; }
    corelib=$(resolve_net10_corelib) || return $?
    if [[ -z "${DN2CPP_RESOURCE_GUARD_FIXTURE:-}" ]]; then build_gate_proj gates/fixtures/ldftn-local || return $?; fi
    if [[ -z "${DN2CPP_RESOURCE_GUARD_INPUT:-}" ]]; then build_gate_proj gates/fixtures/resource-type-guard || return $?; fi
    mkdir -p "$root/guards"
    dotnet "$fixture" --resource-type-guard-guards "$production" "$corelib" "$root/guards/input.dll" "$diet" || return $?
    local default_routes="direct address reflection invoke all validate validate-oracle"
    local native_routes="all validate"
    # Public reader construction also exercises resource parsing and intrinsic static fields.
    # Keep that integration diagnostic explicit rather than treating a transpile as a native pass.
    if [[ "${DN2CPP_RESOURCE_GUARD_HEADER:-0}" == 1 ]]; then
        default_routes+=" header"
        native_routes+=" header"
        echo "Including public ResourceReader constructor diagnostic (requires native compilation and CLR parity)" >&2
    else
        echo "Public ResourceReader constructor diagnostic not requested; focused reader type validation only" >&2
    fi
    local routes="${DN2CPP_RESOURCE_GUARD_ROUTES:-$default_routes}"
    local axes="${DN2CPP_RESOURCE_GUARD_AXES:-raw diet}"
    if [[ -n "${DN2CPP_RESOURCE_GUARD_ROUTES:-}${DN2CPP_RESOURCE_GUARD_AXES:-}${DN2CPP_RESOURCE_GUARD_EMIT_ONLY:-}" ]]; then
        echo "DIAGNOSTIC PARTIAL PROFILE: routes=$routes axes=$axes; not the complete resource type guard gate" >&2
    fi
    for route in $routes; do
        mkdir -p "$root/$route-input"
        cp -R "$original/." "$root/$route-input/" || return $?
        dotnet "$fixture" --resource-type-guard "$root/$route-input/ResourceTypeGuard.dll" "$corelib" "$route" || return $?
        if [[ "$route" == reflection || "$route" == invoke || "$route" == header || "$route" == validate-oracle ]]; then
            gate_capture_ok "$root/$route-oracle" run_bounded dotnet "$root/$route-input/ResourceTypeGuard.dll" || return $?
        fi
        for axis in $axes; do
            out="$root/$route-$axis"
            local flags=()
            [[ "$axis" != raw ]] || flags+=(--no-ildiet)
            invoke_cli "$root/$route-input/ResourceTypeGuard.dll" --auto-ref "${flags[@]}" -o "$out" || return $?
            "$(resolve_python)" gates/fixtures/resource-type-guard/check-generated.py "$out" --route "$route" || return $?
        done
    done
    [[ "${DN2CPP_RESOURCE_GUARD_EMIT_ONLY:-0}" != 1 ]] || return 0
    cat "$root/reflection-oracle.stdout" "$root/reflection-oracle.stdout" "$root/reflection-oracle.stdout" \
        "$root/invoke-oracle.stdout" > "$root/expected.stdout"
    for axis in $axes; do
        for route in $native_routes; do
            out="$root/$route-$axis"
            compile_console "$out" ResourceTypeGuard || return $?
            gate_capture_ok "$out/native" run_bounded "$out/ResourceTypeGuard$EXE_EXT" || return $?
            local expected="$root/expected.stdout"
            [[ "$route" != header ]] || expected="$root/header-oracle.stdout"
            [[ "$route" != validate ]] || expected="$root/validate-oracle-oracle.stdout"
            gate_output compare "$expected" "$out/native.stdout" || return $?
            gate_output byte-witness "$out/native.stdout" "resource-type-$([[ "$route" == all ]] && echo guard || echo "$route"):end" --count any || return $?
        done
    done
    resource_type_guard_negative_gate "$root/negatives" "$original" "$fixture" "$corelib"
}

resource_type_guard_negative_gate() {
    local root="$1" original="$2" fixture="$3" corelib="$4" route shape axis mode rc
    for route in direct address; do
        for shape in return byref secondbyref modifier parammodifier instance generic varargs wrongtype arity spec; do
            mode="negative-$route-$shape"
            mkdir -p "$root/$mode/input"
            cp -R "$original/." "$root/$mode/input/" || return $?
            dotnet "$fixture" --resource-type-guard "$root/$mode/input/ResourceTypeGuard.dll" "$corelib" "$mode" || return $?
            for axis in raw diet; do
                local flags=()
                [[ "$axis" != raw ]] || flags+=(--no-ildiet)
                rc=0
                invoke_cli "$root/$mode/input/ResourceTypeGuard.dll" --auto-ref "${flags[@]}" -o "$root/$mode/$axis" > "$root/$mode/$axis.log" 2>&1 || rc=$?
                printf '%s\n' "$rc" > "$root/$mode/$axis.status"
                [[ "$rc" == 2 ]] || return 1
                grep -Eq 'ResourceManager.*(exact MemberRef signature|exact supported declaration|no (generic )?method|has no intrinsic mapping yet)' "$root/$mode/$axis.log" || return 1
            done
        done
    done
}

resource_type_guard_fresh_gate() {
    local generation
    mkdir -p artifacts || return $?
    generation=$(mktemp -d "artifacts/resource-type-guard-${CONFIG}${DN2CPP_OUT_SUFFIX:-}.XXXXXXXX") || return $?
    resource_type_guard_gate "$generation/run"
}
