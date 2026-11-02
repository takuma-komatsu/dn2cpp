#!/usr/bin/env bash
# Exact fixed-width vector value metadata admission and contextual cache guards.
# Caller owns serial builds and supplies fresh output roots.
vector_fixed_value_metadata_probe() {
    local out="$1" width mode input route owner member
    local fixture="${DN2CPP_FIXED_VECTOR_VALUE_FIXTURE_DLL:-gates/fixtures/vector-fixed-value/bin/$CONFIG/$TFM/FixedVectorValueFixture.dll}"
    if [ -z "${DN2CPP_FIXED_VECTOR_VALUE_FIXTURE_DLL:-}" ]; then
        build_gate_proj gates/fixtures/vector-fixed-value/FixedVectorValueFixture.csproj || return 1
    fi
    local compiler="${DN2CPP_FIXED_VECTOR_VALUE_COMPILER_DLL:-$(dirname "${DN2CPP_CLI_DLL:-src/Dn2Cpp.Cli/bin/$CONFIG/$TFM/dn2cpp.dll}")/Dn2Cpp.Transpiler.dll}"
    mkdir -p "$out" || return 1
    for width in 64 128 256 512; do
        local wout="$out/width-$width"
        mkdir -p "$wout" || return 1
        gate_capture_ok "$wout/guards" run_bounded dotnet "$fixture" "$width" guard "$compiler" "$_CG_CORELIB" "$wout/all.dll" || return 1
        for route in direct boxed; do
            gate_capture_ok "$wout/context-$route" run_bounded dotnet "$fixture" "$width" context "$compiler" "$_CG_CORELIB" "$wout/context-$route.dll" "$route" || return 1
        done
        for owner in var mvar; do
            for member in typed hash; do
                gate_capture_ok "$wout/context-spec-$owner-$member" run_bounded dotnet "$fixture" "$width" context-spec "$compiler" "$_CG_CORELIB" "$wout/context-spec-$owner-$member.dll" "$owner" "$member" direct || return 1
            done
            gate_capture_ok "$wout/context-spec-boxed-$owner" run_bounded dotnet "$fixture" "$width" context-spec "$compiler" "$_CG_CORELIB" "$wout/context-spec-boxed-$owner.dll" "$owner" typed boxed || return 1
        done
        for mode in valid object-valid hash-valid constrained-equatable constrained-object constrained-hash boxed-valid; do
            input="$wout/$mode.dll"
            gate_capture_ok "$wout/$mode-generate" run_bounded dotnet "$fixture" "$width" generate "$mode" "$_CG_CORELIB" "$input" || return 1
            gate_capture_ok "$wout/$mode-reference" run_bounded dotnet "$fixture" "$width" reference "$compiler" "$_CG_CORELIB" "$input" accept || return 1
        done
        for mode in static explicit-this vararg generic byref-result return-modopt wrong-result \
            byref-input modopt modreq wrong-closed wrong-width wrong-family wrong-input extra missing trailing owner-trailing \
            methodspec methodspec-zero constrained-foreign-equatable constrained-wrong-equatable constrained-foreign-object \
            boxed-modopt boxed-trailing boxed-owner-trailing boxed-methodspec boxed-methodspec-zero \
            constrained-equatable-modopt constrained-equatable-trailing constrained-equatable-owner-trailing \
            constrained-object-modopt constrained-object-trailing constrained-receiver-modopt constrained-receiver-trailing \
            object-static object-explicit-this object-vararg object-generic object-byref-result object-return-modopt object-wrong-result \
            hash-static hash-explicit-this hash-vararg hash-generic hash-byref-result hash-return-modopt hash-wrong-result; do
            input="$wout/$mode.dll"
            gate_capture_ok "$wout/$mode-generate" run_bounded dotnet "$fixture" "$width" generate "$mode" "$_CG_CORELIB" "$input" || return 1
            gate_capture_ok "$wout/$mode-reference" run_bounded dotnet "$fixture" "$width" reference "$compiler" "$_CG_CORELIB" "$input" reject || return 1
            gate_capture "$wout/$mode-reject" invoke_cli "$input" -r "$_CG_CORELIB" --no-ildiet -o "$wout/$mode-emitted" || return 1
            vector_value_check_rejection "$mode" "$wout/$mode-reject" FixedVectorValueGuardInput || return 1
        done
        for mode in decimal int128 uint128 enum struct; do
            for member in "" object- hash- constrained-object- constrained-equatable- boxed-; do
                input="$wout/managed-$member$mode.dll"
                gate_capture_ok "$wout/managed-$member$mode-generate" run_bounded dotnet "$fixture" "$width" generate "$member$mode" "$_CG_CORELIB" "$input" || return 1
                gate_capture_ok "$wout/managed-$member$mode-reference" run_bounded dotnet "$fixture" "$width" reference "$compiler" "$_CG_CORELIB" "$input" managed || return 1
            done
        done
        for mode in bool char open string nullable generic-struct byref-like foreign-owner; do
            input="$wout/scope-$mode.dll"
            gate_capture_ok "$wout/scope-$mode-generate" run_bounded dotnet "$fixture" "$width" generate "$mode" "$_CG_CORELIB" "$input" || return 1
            gate_capture_ok "$wout/scope-$mode-reference" run_bounded dotnet "$fixture" "$width" reference "$compiler" "$_CG_CORELIB" "$input" reject || return 1
        done
        gate_capture_ok "$wout/mixed-generate" run_bounded dotnet "$fixture" "$width" mixed-generate "$_CG_CORELIB" "$wout/mixed.dll" || return 1
        gate_capture_ok "$wout/mixed-require" run_bounded dotnet "$fixture" "$width" mixed "$compiler" "$_CG_CORELIB" "$wout/mixed.dll" require || return 1
    done
}

# Fixed owners use distinct Count guards; only the auxiliary fixture family changes.
vector_fixed_managed_guard_probe() {
    local out="$1" fixture compiler width suffix mode input
    fixture="${DN2CPP_VECTOR_UNSUPPORTED_FIXTURE_DLL:-gates/fixtures/vector-unsupported/bin/$CONFIG/$TFM/VectorUnsupportedFixture.dll}"
    if [ -z "${DN2CPP_VECTOR_UNSUPPORTED_FIXTURE_DLL:-}" ]; then
        build_gate_proj gates/fixtures/vector-unsupported/VectorUnsupportedFixture.csproj || return 1
    fi
    compiler="${DN2CPP_FIXED_VECTOR_VALUE_COMPILER_DLL:-$(dirname "${DN2CPP_CLI_DLL:-src/Dn2Cpp.Cli/bin/$CONFIG/$TFM/dn2cpp.dll}")/Dn2Cpp.Transpiler.dll}"
    mkdir -p "$out" || return 1
    for width in 64 128 256 512; do
        local wout="$out/width-$width"
        mkdir -p "$wout" || return 1
        gate_capture_ok "$wout/declarations" run_bounded dotnet "$fixture" fixed "$width" declarations "$_CG_CORELIB" || return 1
        for mode in count-valid helper-valid helper-context-mvar; do
            input="$wout/$mode.dll"
            gate_capture_ok "$wout/$mode-generate" run_bounded dotnet "$fixture" fixed "$width" generate "$mode" "$_CG_CORELIB" "$input" || return 1
            gate_capture_ok "$wout/$mode-reference" run_bounded dotnet "$fixture" fixed "$width" reference "$compiler" "$_CG_CORELIB" "$input" accept || return 1
        done
        local negatives=(helper-foreign-owner)
        for suffix in instance explicit-this vararg generic wrong-result return-modopt return-modreq return-pinned extra trailing owner-trailing owner-modopt owner-modreq owner-pinned methodspec methodspec-zero; do
            negatives+=("count-$suffix")
        done
        for suffix in instance explicit-this vararg wrong-result return-modopt return-modreq return-pinned extra trailing spec-modopt spec-modreq spec-pinned spec-byref spec-trailing spec-arity spec-zero owner-spec owner-class-spec owner-modopt-spec owner-modreq-spec; do
            negatives+=("helper-$suffix")
        done
        for mode in "${negatives[@]}"; do
            input="$wout/$mode.dll"
            gate_capture_ok "$wout/$mode-generate" run_bounded dotnet "$fixture" fixed "$width" generate "$mode" "$_CG_CORELIB" "$input" || return 1
            gate_capture_ok "$wout/$mode-reference" run_bounded dotnet "$fixture" fixed "$width" reference "$compiler" "$_CG_CORELIB" "$input" reject || return 1
        done
    done
    local declaration="$out/serialized-baseline/System.Private.CoreLib.dll" target group
    gate_capture_ok "$out/serialized-baseline-copy" run_bounded dotnet "$fixture" fixed 64 declaration-copy baseline "$_CG_CORELIB" "$declaration" || return 1
    for width in 64 128 256 512; do
        local wout="$out/width-$width"
        for target in count-valid helper-valid; do
            gate_capture_ok "$wout/serialized-baseline-$target-reference" run_bounded dotnet "$fixture" fixed "$width" reference "$compiler" "$declaration" "$wout/$target.dll" accept || return 1
        done
        for mode in owner-flags owner-constraint count-instance count-impl; do
            declaration="$wout/$mode/System.Private.CoreLib.dll"
            gate_capture_ok "$wout/$mode-copy" run_bounded dotnet "$fixture" fixed "$width" declaration-copy "$mode" "$_CG_CORELIB" "$declaration" || return 1
            gate_capture_ok "$wout/declaration-$mode-reference" run_bounded dotnet "$fixture" fixed "$width" reference "$compiler" "$declaration" "$wout/count-valid.dll" reject || return 1
        done
        declaration="$out/serialized-baseline/System.Private.CoreLib.dll"
    done
    for group in 64 128; do
        for mode in helper-flags helper-constraint helper-public helper-impl; do
            declaration="$out/helper-$group-$mode/System.Private.CoreLib.dll"
            gate_capture_ok "$out/helper-$group-$mode-copy" run_bounded dotnet "$fixture" fixed "$group" declaration-copy "$mode" "$_CG_CORELIB" "$declaration" || return 1
            for width in 64 128 256 512; do
                local expected=reject
                if { [ "$group" = 64 ] && [ "$width" != 64 ]; } || { [ "$group" = 128 ] && [ "$width" = 64 ]; }; then expected=accept; fi
                gate_capture_ok "$out/width-$width/declaration-$group-$mode-reference" run_bounded dotnet "$fixture" fixed "$width" reference "$compiler" "$declaration" "$out/width-$width/helper-valid.dll" "$expected" || return 1
            done
        done
    done
}
