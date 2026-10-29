#!/usr/bin/env bash
# Retained GC-notification objects refuse allocation, including imported BPI newobj.
source "$(dirname "${BASH_SOURCE[0]}")/_gen2-callback.sh"
source "$(dirname "${BASH_SOURCE[0]}")/_gen2-interpreter.sh"
source "$(dirname "${BASH_SOURCE[0]}")/_gen2-memberref-negative.sh"

GEN2_GATE_INPUTS=(
    gates/_gen2-gate.sh gates/_gen2-callback.sh gates/_gen2-interpreter.sh gates/_gen2-memberref-negative.sh
    gates/fixtures/ldftn-local/Gen2CallbackNegativeFixture.cs
    gates/fixtures/gen2-callback/README.md
    gates/fixtures/gen2-callback/Entry.cs
    gates/fixtures/gen2-callback/Gen2Callback.csproj
    gates/fixtures/gen2-callback/Gen2CallbackSubset.cs
    gates/fixtures/gen2-callback/allocation-probe.cpp
    gates/fixtures/gen2-callback/link.xml
    gates/fixtures/gen2-callback/interpreter/Base/Gen2CallbackBase.csproj
    gates/fixtures/gen2-callback/interpreter/Base/Program.cs
    gates/fixtures/gen2-callback/interpreter/Patch/Gen2CallbackPatch.csproj
    gates/fixtures/gen2-callback/interpreter/Patch/Program.cs
    gates/fixtures/gen2-callback/interpreter/Sealed/Gen2CallbackSealed.csproj
    gates/fixtures/gen2-callback/interpreter/Sealed/Program.cs
    gates/fixtures/gen2-callback/interpreter/ObjectArgument/Gen2CallbackObjectArgument.csproj
    gates/fixtures/ldftn-local/LdftnLocalFixture.csproj
    gates/fixtures/ldftn-local/Program.cs
    gates/fixtures/ldftn-local/Gen2CallbackFixture.cs
    gates/fixtures/ldftn-local/Gen2CallbackGuards.cs
    gates/fixtures/ldftn-local/Gen2CallbackInterpreterFixture.cs
    gates/fixtures/ldftn-local/RuntimeTypeDefaultCtorFixture.cs
)

gen2_retained_boundary_gate() {
    local root="$1" fixture_root=gates/fixtures/gen2-callback
    local interpreter="$fixture_root/interpreter" fixture corelib model_root kind project name
    fixture="gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll"
    corelib=$(resolve_net10_corelib) || return $?
    build_proj gates/fixtures/ldftn-local/LdftnLocalFixture.csproj || return $?
    build_proj "$fixture_root/Gen2Callback.csproj" || return $?
    for project in Base/Gen2CallbackBase Patch/Gen2CallbackPatch Sealed/Gen2CallbackSealed ObjectArgument/Gen2CallbackObjectArgument; do
        build_proj "$interpreter/$project.csproj" || return $?
    done
    mkdir -p "$root"
    # The host gate has already run invoke_cli, so its selected compiler DLL is fixed.
    model_root=$(mktemp -d "$root/model-XXXXXX") || return $?
    gate_capture_ok "$model_root/guards" run_bounded dotnet "$fixture" --gen2-callback-guards \
        "$(dirname "$DN2CPP_CLI_DLL")/Dn2Cpp.Transpiler.dll" "$corelib" "$model_root/input.dll" || return $?
    gen2_callback_routes_gate "$root/all" "$fixture_root/bin/$CONFIG/$TFM" all || return $?
    gen2_callback_routes_gate "$root/reflection" "$fixture_root/bin/$CONFIG/$TFM" reflection || return $?
    local DN2CPP_GEN2_MEMBERREF_SMOKE=1
    gen2_memberref_negative_gate "$fixture_root/bin/$CONFIG/$TFM/Gen2Callback.dll" "$fixture" "$root/malformed-raw" --no-ildiet || return $?
    gen2_memberref_negative_gate "$fixture_root/bin/$CONFIG/$TFM/Gen2Callback.dll" "$fixture" "$root/malformed-diet" || return $?
    # Emission-only policy checks complement runtime calls in the all fixture.
    local mode axis
    for mode in newobj noalloc pointer-noalloc; do
        mkdir -p "$root/$mode/input"
        cp -R "$fixture_root/bin/$CONFIG/$TFM/." "$root/$mode/input/" || return $?
        dotnet "$fixture" "--gen2-callback-$mode" "$root/$mode/input/Gen2Callback.dll" "$corelib" || return $?
        for axis in raw diet; do
            local flags=() rc=0
            [[ "$axis" != raw ]] || flags=(--no-ildiet)
            invoke_cli "$root/$mode/input/Gen2Callback.dll" --auto-ref "${flags[@]}" -o "$root/$mode/$axis" > "$root/$mode/$axis.log" 2>&1 || rc=$?
            printf '%s\n' "$rc" > "$root/$mode/$axis.status"
            if [[ "$mode" == newobj ]]; then
                [[ "$rc" == 2 ]] || { echo 'FAIL: static Gen2 newobj was admitted' >&2; return 1; }
                grep -Fq 'newobj System.Gen2GcCallback — native allocation of Gen2 GC notification objects is not supported.' "$root/$mode/$axis.log" || return 1
            elif [[ "$mode" == pointer-noalloc ]]; then
                [[ "$rc" == 2 ]] || { echo 'FAIL: dynamic calli was admitted as NoAlloc' >&2; return 1; }
                for suffix in 0 1 Final; do
                    grep -Fq "[HotPath(NoAlloc)] closure of Gen2CallbackSubset.Program::Pointer$suffix dispatches dynamically: Gen2CallbackSubset.Program::Pointer$suffix emits calli" "$root/$mode/$axis.log" || return 1
                done
            else
                [[ "$rc" == 0 ]] || { cat "$root/$mode/$axis.log" >&2; return 1; }
            fi
        done
    done
    # Rewriting copies preserves the ordinary build outputs and never runs them on CLR.
    for kind in Base Patch Sealed ObjectArgument; do
        name="Gen2Callback$kind"
        mkdir -p "$root/input/$kind"
        cp -R "$interpreter/$kind/bin/$CONFIG/$TFM/." "$root/input/$kind/" || return $?
        local selector
        case "$kind" in
            Base) selector=base ;;
            Sealed) selector=sealed ;;
            Patch|ObjectArgument) selector=patch ;;
        esac
        dotnet "$fixture" "--gen2-interp-$selector" "$root/input/$kind/$name.dll" "$corelib" || return $?
    done
    gen2_interpreter_gate "$root/input/Base/Gen2CallbackBase.dll" \
        "$root/input/Patch/Gen2CallbackPatch.dll" "$root/input/Sealed/Gen2CallbackSealed.dll" \
        "$root/interpreter" --no-ildiet || return $?
    local encoding
    for encoding in register stack; do
        local flags=() rc=0
        [[ "$encoding" != stack ]] || flags=(--patch-stackcode)
        invoke_cli --emit-patch "$root/input/ObjectArgument/Gen2CallbackObjectArgument.dll" \
            --base-abi "$root/interpreter/base/base-abi.json" "${flags[@]}" \
            -o "$root/object-argument-$encoding" > "$root/object-argument-$encoding.log" 2>&1 || rc=$?
        printf '%s\n' "$rc" > "$root/object-argument-$encoding.status"
        [[ "$rc" == 2 ]] || { echo 'FAIL: unsupported BPI Object argument was admitted' >&2; return 1; }
        grep -Fq 'external call System.Gen2GcCallback..ctor argument type Object is not supported yet (scalars, string, and reference types only)' \
            "$root/object-argument-$encoding.log" || return 1
    done
    echo 'OK (retained Gen2 allocation boundary and BPI constructor imports)'
}
