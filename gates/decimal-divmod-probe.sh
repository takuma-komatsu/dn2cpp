#!/usr/bin/env bash
# The CLR runs only the public UInt128 oracle; rewritten private calls are native-only.
decimal_divmod_probe() {
    local out="$1" oracle="$2" axis emit
    local input="$out/divmod-input"
    mkdir -p "$input"
    cp "$_CG_APP" "${_CG_APP%.dll}.runtimeconfig.json" "${_CG_APP%.dll}.deps.json" "$input/"
    build_gate_proj gates/fixtures/ldftn-local/LdftnLocalFixture.csproj || return 1
    local fixture="gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll"
    local guards
    guards=$(mktemp -d "$out/divmod-guards.XXXXXX") || return 1
    gate_capture_ok "$out/divmod-guards" run_bounded dotnet "$fixture" --decimal-divmod-guards \
        "src/Dn2Cpp.Transpiler/bin/$CONFIG/$TFM/Dn2Cpp.Transpiler.dll" "$_CG_CORELIB" "$guards/model.dll" || return 1
    gate_capture_ok "$out/divmod-rewrite" run_bounded dotnet "$fixture" --decimal-divmod "$input/DecimalOps.dll" "$_CG_CORELIB" || return 1
    for axis in raw diet; do
        emit="$out/divmod-$axis"
        local flags=()
        [ "$axis" != raw ] || flags+=(--no-ildiet)
        gate_capture_ok "$out/divmod-$axis-emit" invoke_cli "$input/DecimalOps.dll" --auto-ref --link-xml gates/fixtures/decimal-divmod/retained.xml "${flags[@]}" -o "$emit" || return 1
        gate_capture_ok "$out/divmod-$axis-generated" python3 gates/fixtures/decimal-divmod/check-generated.py "$emit" || return 1
        compile_console "$emit" DecimalOps || return 1
        gate_capture_ok "$out/divmod-$axis-run" run_bounded "./$emit/DecimalOps$EXE_EXT" || return 1
        gate_output compare "$out/divmod-$axis-run.stdout" "$oracle" || return 1
        gate_output byte-witness "$out/divmod-$axis-run.stderr" 'decimal divmod native storage/ABI/canaries passed' --count 1 --policy crlf || return 1
        gate_output byte-witness "$out/divmod-$axis-run.stderr" 'decimal divmod retained ABI passed' --count 1 --policy crlf || return 1
    done
    local mode negative
    for mode in instance vararg generic return byvalue pointer modopt foreignref; do
        negative="$out/divmod-negative-$mode"
        mkdir -p "$negative"
        cp "$_CG_APP" "$negative/DecimalOps.dll"
        gate_capture_ok "$out/divmod-$mode-rewrite" run_bounded dotnet "$fixture" --decimal-divmod "$negative/DecimalOps.dll" "$_CG_CORELIB" "$mode" || return 1
        for axis in raw diet; do
            local flags=()
            [ "$axis" != raw ] || flags+=(--no-ildiet)
            gate_capture "$out/divmod-$mode-$axis-reject" invoke_cli "$negative/DecimalOps.dll" --auto-ref "${flags[@]}" -o "$negative/$axis" || return 1
            gate_output status "$(cat "$out/divmod-$mode-$axis-reject.status")" 2 || return 1
            if [ "$mode" = foreignref ]; then
                grep -F "input type 'System.Decimal' in assembly 'DecimalOps' conflicts with a CoreLib/runtime type identity" "$out/divmod-$mode-$axis-reject.stderr" >/dev/null || return 1
            else
                grep -F 'DecDivMod1E9' "$out/divmod-$mode-$axis-reject.stderr" >/dev/null || return 1
            fi
        done
    done
}
