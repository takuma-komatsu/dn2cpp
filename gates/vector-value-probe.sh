#!/usr/bin/env bash
# Numeric Vector<T> value equality/hash, independent IEEE oracle and real tuple bodies.
# Sourced after _common.sh. Native wrappers select Highway or SCALAR explicitly.
vector_value_probe() {
    local out="$1" axis emit hardware=1
    [ -z "${SCALAR:-}" ] || hardware=0
    gate_capture_ok "$out/value-oracle" run_bounded env DOTNET_EnableAVX=0 dotnet "$_CG_APP" vector-value-only || return 1
    python3 gates/fixtures/vector-value/compare-output.py "$out/value-oracle.stdout" || return 1
    gate_capture_ok "$out/unsupported-oracle" run_bounded env DOTNET_EnableAVX=0 dotnet "$_CG_APP" vector-unsupported-only || return 1
    python3 gates/fixtures/vector-unsupported/compare-output.py "$out/unsupported-oracle.stdout" || return 1
    python3 gates/fixtures/vector-value/compare-output.py "$out/oracle.stdout" --full || return 1
    for axis in raw diet; do
        emit=$(mktemp -d "$out/value-$axis-XXXXXX") || return 1
        local flags=()
        [ "$axis" != raw ] || flags+=(--no-ildiet)
        gate_capture_ok "$out/value-$axis-emit" invoke_cli "$_CG_APP" -r "$_CG_CORELIB" --auto-ref \
            "${flags[@]}" -o "$emit" || return 1
        gate_capture_ok "$out/value-$axis-generated" python3 gates/fixtures/vector-value/check-generated.py "$emit" || return 1
        gate_capture_ok "$out/value-$axis-unsupported-generated" python3 gates/fixtures/vector-unsupported/check-generated.py "$emit" || return 1
        gate_capture_ok "$out/value-$axis-unsupported-layouts" python3 gates/fixtures/vector-unsupported/check-layouts.py "$emit" --abi || return 1
        compile_console "$emit" VectorProbe || return 1
        gate_capture_ok "$out/value-$axis-run" run_bounded "./$emit/VectorProbe$EXE_EXT" vector-value-only || return 1
        gate_output compare "$out/value-$axis-run.stdout" "$out/value-oracle.stdout" --policy crlf || return 1
        python3 gates/fixtures/vector-value/compare-output.py "$out/value-$axis-run.stdout" || return 1
        [ "$(grep -Ec "^vector value actual tuple equality/hash passed hw=$hardware pointer=(4|8)$" "$out/value-$axis-run.stderr")" = 1 ] || return 1
        gate_capture_ok "$out/value-$axis-full" run_bounded "./$emit/VectorProbe$EXE_EXT" || return 1
        gate_output compare "$out/value-$axis-full.stdout" "$out/oracle.stdout" --policy crlf || return 1
        python3 gates/fixtures/vector-value/compare-output.py "$out/value-$axis-full.stdout" --full || return 1
        gate_capture_ok "$out/value-$axis-unsupported-run" run_bounded "./$emit/VectorProbe$EXE_EXT" vector-unsupported-only || return 1
        gate_capture_ok "$out/value-$axis-unsupported-compare" vector_unsupported_compare_target \
            "$out/value-$axis-unsupported-run.stdout" "$out/unsupported-oracle.stdout" \
            "$out/value-$axis-run.stderr" "$out/value-$axis-numeric-support-differences.json" || return 1
    done
    if [ -z "${SCALAR:-}" ]; then
        vector_value_metadata_probe "$out" || return 1
        vector_unsupported_metadata_probe "$out" || return 1
    fi
}

# Explicit opt-in by the serial orchestrator; never treat a native compile as Wasm.
vector_value_wasm_probe() (
    local out="$1" axis emit
    dn2cpp_emsdk_resolve --no-bundled
    for tool in em++ emcmake node; do
        command -v "$tool" >/dev/null 2>&1 || gate_skip "$tool not found — vector value Wasm not verified"
    done
    export WASM=1
    unset SCALAR HIGHWAY DN2CPP_WASM_SIMD
    local corelib
    corelib=$(locate_corelib_cross_posix net10)
    mkdir -p "$out"
    gate_capture_ok "$out/value-oracle" run_bounded env DOTNET_EnableAVX=0 dotnet "$_CG_APP" vector-value-only || return 1
    python3 gates/fixtures/vector-value/compare-output.py "$out/value-oracle.stdout" || return 1
    gate_capture_ok "$out/unsupported-oracle" run_bounded env DOTNET_EnableAVX=0 dotnet "$_CG_APP" vector-unsupported-only || return 1
    python3 gates/fixtures/vector-unsupported/compare-output.py "$out/unsupported-oracle.stdout" || return 1
    for axis in raw diet; do
        emit=$(mktemp -d "$out/value-wasm-$axis-XXXXXX") || return 1
        local flags=()
        [ "$axis" != raw ] || flags+=(--no-ildiet)
        gate_capture_ok "$out/value-wasm-$axis-emit" invoke_cli "$_CG_APP" -r "$corelib" --auto-ref \
            "${flags[@]}" -o "$emit" || return 1
        gate_capture_ok "$out/value-wasm-$axis-generated" python3 gates/fixtures/vector-value/check-generated.py "$emit" || return 1
        gate_capture_ok "$out/value-wasm-$axis-unsupported-generated" python3 gates/fixtures/vector-unsupported/check-generated.py "$emit" || return 1
        gate_capture_ok "$out/value-wasm-$axis-unsupported-layouts" python3 gates/fixtures/vector-unsupported/check-layouts.py "$emit" --abi || return 1
        compile_console_wasm "$emit" VectorProbe || return 1
        gate_capture_ok "$out/value-wasm-$axis-run" run_bounded node "./$emit/VectorProbe.js" vector-value-only || return 1
        gate_output compare "$out/value-wasm-$axis-run.stdout" "$out/value-oracle.stdout" --policy crlf || return 1
        python3 gates/fixtures/vector-value/compare-output.py "$out/value-wasm-$axis-run.stdout" || return 1
        [ "$(grep -Ec '^vector value actual tuple equality/hash passed hw=(0|1) pointer=4$' "$out/value-wasm-$axis-run.stderr")" = 1 ] || return 1
        [ "$(grep -Ec '^vector value target pointer=4 nint-count=[0-9]+ independent-lanes=4$' "$out/value-wasm-$axis-run.stderr")" = 1 ] || return 1
        gate_capture_ok "$out/value-wasm-$axis-unsupported-run" run_bounded node "./$emit/VectorProbe.js" vector-unsupported-only || return 1
        gate_capture_ok "$out/value-wasm-$axis-unsupported-compare" vector_unsupported_compare_target \
            "$out/value-wasm-$axis-unsupported-run.stdout" "$out/unsupported-oracle.stdout" \
            "$out/value-wasm-$axis-run.stderr" "$out/value-wasm-$axis-numeric-support-differences.json" || return 1
    done
)

# Genuine unsupported closures are scope tests, not malformed-IL execution tests.
vector_value_metadata_probe() {
    local out mode input
    mkdir -p "$1"
    out=$(mktemp -d "$1/value-metadata-XXXXXX") || return 1
    echo "vector value metadata evidence: $out"
    local fixture="${DN2CPP_VECTOR_VALUE_FIXTURE_DLL:-gates/fixtures/vector-value/bin/$CONFIG/$TFM/VectorValueFixture.dll}"
    if [ -z "${DN2CPP_VECTOR_VALUE_FIXTURE_DLL:-}" ]; then
        build_gate_proj gates/fixtures/vector-value/VectorValueFixture.csproj || return 1
    fi
    local compiler="${DN2CPP_VECTOR_VALUE_COMPILER_DLL:-$(dirname "${DN2CPP_CLI_DLL:-src/Dn2Cpp.Cli/bin/$CONFIG/$TFM/dn2cpp.dll}")/Dn2Cpp.Transpiler.dll}"
    gate_capture_ok "$out/guards" run_bounded dotnet "$fixture" guard "$compiler" "$_CG_CORELIB" "$out/all-closures.dll" || return 1
    gate_capture_ok "$out/context-cache" run_bounded dotnet "$fixture" context "$compiler" "$_CG_CORELIB" "$out/context-cache.dll" || return 1
    local owner member
    for owner in var mvar; do
        for member in typed hash; do
            gate_capture_ok "$out/context-spec-$owner-$member" run_bounded dotnet "$fixture" context-spec \
                "$compiler" "$_CG_CORELIB" "$out/context-spec-$owner-$member.dll" "$owner" "$member" || return 1
        done
    done
    for mode in valid object-valid hash-valid constrained-equatable constrained-object constrained-hash boxed-valid; do
        input="$out/$mode.dll"
        gate_capture_ok "$out/$mode-generate" run_bounded dotnet "$fixture" generate "$mode" "$_CG_CORELIB" "$input" || return 1
        gate_capture_ok "$out/$mode-reference" run_bounded dotnet "$fixture" reference "$compiler" "$_CG_CORELIB" "$input" accept || return 1
    done
    for mode in static explicit-this vararg generic byref-result return-modopt wrong-result \
        byref-input modopt modreq wrong-closed wrong-input extra missing trailing owner-trailing \
        methodspec methodspec-zero constrained-foreign-equatable constrained-wrong-equatable constrained-foreign-object \
        boxed-modopt boxed-trailing boxed-owner-trailing boxed-methodspec boxed-methodspec-zero \
        constrained-equatable-modopt constrained-equatable-trailing constrained-equatable-owner-trailing \
        constrained-object-modopt constrained-object-trailing constrained-receiver-modopt constrained-receiver-trailing object-static object-explicit-this object-vararg \
        object-generic object-byref-result object-return-modopt object-wrong-result \
        hash-static hash-explicit-this hash-vararg hash-generic hash-byref-result hash-return-modopt hash-wrong-result; do
        input="$out/$mode.dll"
        gate_capture_ok "$out/$mode-generate" run_bounded dotnet "$fixture" generate "$mode" "$_CG_CORELIB" "$input" || return 1
        gate_capture_ok "$out/$mode-reference" run_bounded dotnet "$fixture" reference "$compiler" "$_CG_CORELIB" "$input" reject || return 1
        # Never execute these fixtures on CLR: only the compiler consumes malformed metadata.
        gate_capture "$out/$mode-reject" invoke_cli "$input" -r "$_CG_CORELIB" --no-ildiet -o "$out/$mode-emitted" || return 1
        vector_value_check_rejection "$mode" "$out/$mode-reject" || return 1
    done
    for mode in bool char decimal int128 uint128 enum struct open fixed-owner foreign-owner; do
        input="$out/scope-$mode.dll"
        gate_capture_ok "$out/scope-$mode-generate" run_bounded dotnet "$fixture" generate "$mode" "$_CG_CORELIB" "$input" || return 1
        gate_capture_ok "$out/scope-$mode-reference" run_bounded dotnet "$fixture" reference "$compiler" "$_CG_CORELIB" "$input" reject || return 1
    done
}

# Zero-argument MethodSpecs are invalid ECMA signatures. The SRM reflection scan
# can reject them before the feature guard; this is not a managed runtime result.
vector_value_check_rejection() {
    local mode="$1" evidence="$2" status
    status=$(cat "$evidence.status")
    if { [ "$mode" = methodspec-zero ] || [ "$mode" = boxed-methodspec-zero ]; } && [ "$status" = 134 ]; then
        grep -Fx 'Unhandled exception. System.BadImageFormatException: Signature type sequence must have at least one element.' \
            "$evidence.stderr" >/dev/null || return 1
        grep -F 'at System.Reflection.Metadata.MethodSpecification.DecodeSignature' "$evidence.stderr" >/dev/null || return 1
        grep -F 'at Dn2Cpp.ReflectionSummarizer.DecodeCall' "$evidence.stderr" >/dev/null || return 1
        echo "vector value $mode: malformed MethodSpec signature rejected before family adapter (134), not feature refusal (2)"
        return 0
    fi
    gate_output status "$status" 2 || return 1
    local foreign_identity=''
    case "$mode" in
        constrained-foreign-equatable) foreign_identity='System.IEquatable`1' ;;
        constrained-foreign-object) foreign_identity='System.Object' ;;
    esac
    if [ -n "$foreign_identity" ]; then
        grep -F "input type '$foreign_identity' in assembly 'VectorValueGuardInput' conflicts with a CoreLib/runtime type identity" \
            "$evidence.stderr" >/dev/null || return 1
        echo "vector value $mode: refused by input identity before family validator (2)"
        return 0
    fi
    grep -F 'Vector<T> value methods require an exact supported closed CoreLib declaration and unmodified call signature' \
        "$evidence.stderr" >/dev/null || return 1
}
