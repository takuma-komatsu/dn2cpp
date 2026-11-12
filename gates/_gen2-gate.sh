#!/usr/bin/env bash
# Retained Gen2GcCallback refuses reflective and imported interpreter allocation
# before constructing the object. Register and stack patches share one base
# image; reference-only metadata and invalid IL require emission only. Rewritten
# assemblies are native-only and must never run on the CLR.

GEN2_GATE_INPUTS=(
    gates/_gen2-gate.sh
    gates/fixtures/gen2-callback/Gen2CallbackSubset.cs
    gates/fixtures/gen2-callback/check-minimal-metadata.py
    gates/fixtures/gen2-callback/link.xml
    gates/fixtures/gen2-callback/interpreter/Base/Gen2CallbackBase.csproj
    gates/fixtures/gen2-callback/interpreter/Base/Program.cs
    gates/fixtures/gen2-callback/interpreter/Patch/Gen2CallbackPatch.csproj
    gates/fixtures/gen2-callback/interpreter/Patch/Program.cs
    gates/fixtures/ldftn-local/LdftnLocalFixture.csproj
    gates/fixtures/ldftn-local/Program.cs
    gates/fixtures/ldftn-local/Gen2CallbackFixture.cs
)
DN2CPP_GATE_EXTRA_INPUTS="${DN2CPP_GATE_EXTRA_INPUTS:-} ${GEN2_GATE_INPUTS[*]}"
DN2CPP_GATE_EXTRA_CONTEXT="${DN2CPP_GATE_EXTRA_CONTEXT:-}|gen2:reflection,register,stack,reference-only,newobj,memberref|gen2-reference-only:System.Gen2GcCallback=native"

gen2_retained_boundary_gate() {
    local prefix="$1" root projects=gates/fixtures/gen2-callback/interpreter fixture corelib mode kind codec
    local flags=()
    fixture="gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll"
    corelib=$(resolve_net10_corelib)
    # The aggregate gate already prepared the shared Cecil driver. Only this
    # patch project is ours; its ProjectReference also builds the base.
    build_proj "$projects/Patch/Gen2CallbackPatch.csproj"
    mkdir -p "$(dirname "$prefix")"
    root=$(mktemp -d "${prefix}.XXXXXX")
    echo "Gen2 allocation artifacts: $root"
    : > "$root/empty.stdout"
    for mode in base patch minimal newobj memberref; do
        kind=Base
        [[ "$mode" != patch ]] || kind=Patch
        mkdir -p "$root/input/$mode"
        cp -R "$projects/$kind/bin/$CONFIG/$TFM/." "$root/input/$mode/"
        gate_capture_ok "$root/fixture-$mode" run_bounded dotnet "$fixture" \
            "--gen2-callback-$mode" "$root/input/$mode/Gen2Callback$kind.dll" "$corelib"
        gate_assert_text "$root/fixture-$mode.stdout" "gen2-fixture:$mode"
        gate_output compare "$root/fixture-$mode.stderr" "$root/empty.stdout"
    done
    gate_capture_ok "$root/base.transpile" invoke_cli "$root/input/base/Gen2CallbackBase.dll" \
        -r "$corelib" --auto-ref --hotupdate-base \
        --link-xml gates/fixtures/gen2-callback/link.xml -o "$root/base"
    compile_console "$root/base" Gen2CallbackBase
    for codec in register stack; do
        flags=()
        [[ "$codec" != stack ]] || flags=(--patch-stackcode)
        gate_capture_ok "$root/patch-$codec.emit" invoke_cli \
            --emit-patch "$root/input/patch/Gen2CallbackPatch.dll" \
            --base-abi "$root/base/base-abi.json" ${flags[@]+"${flags[@]}"} -o "$root/patch-$codec"
        gate_capture_ok "$root/native-$codec" run_bounded "$root/base/Gen2CallbackBase$EXE_EXT" \
            "$root/patch-$codec/Gen2CallbackPatch.bpi"
        gate_assert_text "$root/native-$codec.stdout" 'gen2-ctor0-reflection:True
gen2-ctor1-reflection:True
gen2-uninitialized:True
gen2-final-metadata:True
gen2-final-reflection-null-target:True
gen2-callbacks-untouched-reflection:True
gen2-interp-base-ready
gen2-interp-ctor0:True
gen2-interp-ordinary:True
gen2-interp-end
'
        gate_output compare "$root/native-$codec.stderr" "$root/empty.stdout"
    done
    gate_output compare "$root/native-register.stdout" "$root/native-stack.stdout"
    gate_output compare "$root/native-register.status" "$root/native-stack.status"

    # A type's full and reference-only metadata are mutually exclusive in one
    # image. Inspect the latter's allocation bit without another native build.
    gate_capture_ok "$root/minimal.transpile" invoke_cli "$root/input/minimal/Gen2CallbackBase.dll" \
        -r "$corelib" --auto-ref --reflection-metadata System.Gen2GcCallback=native -o "$root/minimal"
    gate_python gates/fixtures/gen2-callback/check-minimal-metadata.py "$root/minimal"
    for mode in newobj memberref; do
        gate_capture "$root/$mode" invoke_cli "$root/input/$mode/Gen2CallbackBase.dll" \
            -r "$corelib" --auto-ref -o "$root/$mode"
        gate_output status "$(cat "$root/$mode.status")" 2
    done
    grep -Fq 'newobj System.Gen2GcCallback — native allocation of Gen2 GC notification objects is not supported.' "$root/newobj.stderr" \
        || { cat "$root/newobj.stderr" >&2; return 1; }
    grep -Fxq 'error: Gen2GcCallback requires its exact MemberRef signature' "$root/memberref.stderr" \
        || { cat "$root/memberref.stderr" >&2; return 1; }
    echo 'OK (Gen2 allocation boundary: reflection, register/stack interpreter, reference-only metadata, newobj and MemberRef refusals)'
}
