#!/usr/bin/env bash
# Genuine unmapped Vector<T> value semantics; malformed metadata never runs on CLR.
# Sourced after _common.sh. The caller owns sample/CLI builds and serialization.
vector_unsupported_probe() {
    local out="$1" axis emit
    mkdir -p "$out"
    gate_capture_ok "$out/unsupported-oracle" run_bounded env DOTNET_EnableAVX=0 dotnet "$_CG_APP" vector-unsupported-only || return 1
    python3 gates/fixtures/vector-unsupported/compare-output.py "$out/unsupported-oracle.stdout" || return 1
    gate_capture_ok "$out/unsupported-numeric-oracle" run_bounded env DOTNET_EnableAVX=0 dotnet "$_CG_APP" vector-value-only || return 1
    python3 gates/fixtures/vector-value/compare-output.py "$out/unsupported-numeric-oracle.stdout" || return 1
    gate_capture_ok "$out/unsupported-full-oracle" run_bounded env DOTNET_EnableAVX=0 dotnet "$_CG_APP" || return 1
    python3 gates/fixtures/vector-value/compare-output.py "$out/unsupported-full-oracle.stdout" --full || return 1
    for axis in raw diet; do
        emit=$(mktemp -d "$out/unsupported-$axis-XXXXXX") || return 1
        local flags=()
        [ "$axis" != raw ] || flags+=(--no-ildiet)
        gate_capture_ok "$out/unsupported-$axis-emit" invoke_cli "$_CG_APP" -r "$_CG_CORELIB" --auto-ref "${flags[@]}" -o "$emit" || return 1
        gate_capture_ok "$out/unsupported-$axis-generated" python3 gates/fixtures/vector-unsupported/check-generated.py "$emit" || return 1
        gate_capture_ok "$out/unsupported-$axis-layouts" python3 gates/fixtures/vector-unsupported/check-layouts.py "$emit" --abi || return 1
        local command
        if [ -n "${WASM:-}" ]; then
            compile_console_wasm "$emit" VectorProbe || return 1
            command=(node "./$emit/VectorProbe.js")
        else
            compile_console "$emit" VectorProbe || return 1
            command=("./$emit/VectorProbe$EXE_EXT")
        fi
        gate_capture_ok "$out/unsupported-$axis-run" run_bounded "${command[@]}" vector-unsupported-only || return 1
        python3 gates/fixtures/vector-unsupported/compare-output.py "$out/unsupported-$axis-run.stdout" || return 1
        gate_output compare "$out/unsupported-$axis-run.stdout" "$out/unsupported-oracle.stdout" --policy crlf || return 1
        gate_capture_ok "$out/unsupported-$axis-numeric" run_bounded "${command[@]}" vector-value-only || return 1
        gate_output compare "$out/unsupported-$axis-numeric.stdout" "$out/unsupported-numeric-oracle.stdout" --policy crlf || return 1
        gate_capture_ok "$out/unsupported-$axis-full" run_bounded "${command[@]}" || return 1
        gate_output compare "$out/unsupported-$axis-full.stdout" "$out/unsupported-full-oracle.stdout" --policy crlf || return 1
    done
}

# The actual numeric run binds this backend exception; output is never normalized.
vector_unsupported_compare_target() {
    local actual="$1" oracle="$2" hardware="$3" receipt="$4"
    if grep -Eq '^vector value actual tuple equality/hash passed hw=0 pointer=(4|8)$' "$hardware"; then
        python3 gates/fixtures/vector-unsupported/compare-output.py "$actual" --backend hw0 \
            --oracle "$oracle" --hardware-evidence "$hardware" --difference-receipt "$receipt" || return 1
    else
        grep -Eq '^vector value actual tuple equality/hash passed hw=1 pointer=(4|8)$' "$hardware" || return 1
        python3 gates/fixtures/vector-unsupported/compare-output.py "$actual" --oracle "$oracle" || return 1
        gate_output compare "$actual" "$oracle" --policy crlf || return 1
    fi
}

vector_unsupported_metadata_probe() {
    local out fixture compiler mode input suffix declaration target
    mkdir -p "$1"
    out=$(mktemp -d "$1/unsupported-metadata-XXXXXX") || return 1
    echo "vector unsupported metadata evidence: $out"
    fixture="${DN2CPP_VECTOR_UNSUPPORTED_FIXTURE_DLL:-gates/fixtures/vector-unsupported/bin/$CONFIG/$TFM/VectorUnsupportedFixture.dll}"
    if [ -z "${DN2CPP_VECTOR_UNSUPPORTED_FIXTURE_DLL:-}" ]; then
        build_gate_proj gates/fixtures/vector-unsupported/VectorUnsupportedFixture.csproj || return 1
    fi
    compiler="${DN2CPP_VECTOR_UNSUPPORTED_COMPILER_DLL:-$(dirname "${DN2CPP_CLI_DLL:-src/Dn2Cpp.Cli/bin/$CONFIG/$TFM/dn2cpp.dll}")/Dn2Cpp.Transpiler.dll}"
    sha256sum "$_CG_CORELIB" > "$out/corelib-before.sha256" || return 1
    gate_capture_ok "$out/declarations" run_bounded dotnet "$fixture" declarations "$_CG_CORELIB" || return 1
    for mode in valid object-valid hash-valid decimal enum struct constrained-equatable constrained-object constrained-hash boxed-valid count-valid helper-valid mvar-context helper-context-mvar; do
        input="$out/$mode.dll"
        gate_capture_ok "$out/$mode-generate" run_bounded dotnet "$fixture" generate "$mode" "$_CG_CORELIB" "$input" || return 1
        gate_capture_ok "$out/$mode-reference" run_bounded dotnet "$fixture" reference "$compiler" "$_CG_CORELIB" "$input" accept || return 1
    done
    for mode in bool char open string nullable generic-struct; do
        input="$out/$mode.dll"
        gate_capture_ok "$out/$mode-generate" run_bounded dotnet "$fixture" generate "$mode" "$_CG_CORELIB" "$input" || return 1
        gate_capture_ok "$out/$mode-reference" run_bounded dotnet "$fixture" reference "$compiler" "$_CG_CORELIB" "$input" scope || return 1
    done
    local negatives=(helper-foreign-owner foreign-owner static explicit-this vararg generic byref-result return-modopt wrong-result
        byref-input modopt modreq wrong-closed wrong-input extra missing trailing owner-trailing
        methodspec methodspec-zero constrained-foreign-equatable constrained-wrong-equatable constrained-foreign-object
        boxed-modopt boxed-trailing boxed-owner-trailing boxed-methodspec boxed-methodspec-zero
        constrained-equatable-modopt constrained-equatable-trailing constrained-equatable-owner-trailing
        constrained-object-modopt constrained-object-trailing constrained-receiver-modopt constrained-receiver-trailing
        object-static object-explicit-this object-vararg object-generic object-byref-result object-return-modopt object-wrong-result
        hash-static hash-explicit-this hash-vararg hash-generic hash-byref-result hash-return-modopt hash-wrong-result)
    for suffix in instance explicit-this vararg generic wrong-result return-modopt return-modreq return-pinned extra trailing owner-trailing owner-modopt owner-modreq owner-pinned methodspec methodspec-zero; do
        negatives+=("count-$suffix")
    done
    for suffix in instance explicit-this vararg wrong-result return-modopt return-modreq return-pinned extra trailing spec-modopt spec-modreq spec-pinned spec-byref spec-trailing spec-arity spec-zero owner-spec owner-class-spec owner-modopt-spec owner-modreq-spec; do
        negatives+=("helper-$suffix")
    done
    for mode in "${negatives[@]}"; do
        input="$out/$mode.dll"
        gate_capture_ok "$out/$mode-generate" run_bounded dotnet "$fixture" generate "$mode" "$_CG_CORELIB" "$input" || return 1
        gate_capture_ok "$out/$mode-reference" run_bounded dotnet "$fixture" reference "$compiler" "$_CG_CORELIB" "$input" reject || return 1
        case "$mode" in
            helper-owner-class-spec|helper-owner-modopt-spec|helper-owner-modreq-spec)
                gate_capture "$out/$mode-cli-refusal" invoke_cli "$input" -r "$_CG_CORELIB" --no-ildiet -o "$out/$mode-emitted" || return 1
                gate_output status "$(cat "$out/$mode-cli-refusal.status")" 2 || return 1
                grep -F 'Vector unsupported-type guard requires its exact closed CoreLib declaration and unmodified MethodSpec' "$out/$mode-cli-refusal.stderr" >/dev/null || return 1
                ;;
        esac
    done
    declaration="$out/serialized-baseline/System.Private.CoreLib.dll"
    gate_capture_ok "$out/serialized-baseline-copy" run_bounded dotnet "$fixture" declaration-copy baseline "$_CG_CORELIB" "$declaration" || return 1
    for target in valid constrained-equatable boxed-valid count-valid helper-valid; do
        gate_capture_ok "$out/serialized-baseline-$target-reference" run_bounded dotnet "$fixture" reference "$compiler" "$declaration" "$out/$target.dll" accept || return 1
    done
    for mode in owner-flags owner-constraint interface-flags interface-constraint helper-flags helper-constraint helper-public helper-impl count-instance count-impl; do
        declaration="$out/$mode/System.Private.CoreLib.dll"
        gate_capture_ok "$out/$mode-copy" run_bounded dotnet "$fixture" declaration-copy "$mode" "$_CG_CORELIB" "$declaration" || return 1
        case "$mode" in
            owner-*) target=valid;;
            interface-*) target=constrained-equatable;;
            helper-*) target=helper-valid;;
            count-*) target=count-valid;;
        esac
        # Private CoreLib is metadata input to the compiler only, never a CLR runtime.
        gate_capture_ok "$out/declaration-$mode-reference" run_bounded dotnet "$fixture" reference "$compiler" "$declaration" "$out/$target.dll" reject || return 1
        if [[ "$mode" == interface-* ]]; then
            gate_capture_ok "$out/declaration-$mode-boxed-reference" run_bounded dotnet "$fixture" reference "$compiler" "$declaration" "$out/boxed-valid.dll" reject || return 1
        fi
    done
    sha256sum "$_CG_CORELIB" > "$out/corelib-after.sha256" || return 1
    cmp "$out/corelib-before.sha256" "$out/corelib-after.sha256" || return 1
}
