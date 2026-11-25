#!/usr/bin/env bash
# GC wrappers use the CLR oracle; collector capabilities are native-only.
# Buffer byte-copy parity is paired with an emitted barrier check. The existing
# gc-write-barrier gate owns the helper's deterministic incremental-GC proof.
DN2CPP_GATE_EXTRA_INPUTS="${DN2CPP_GATE_EXTRA_INPUTS:-} gates/_gc-collector-contracts.sh gates/check-gc-collector-bodies.py gates/fixtures/ldftn-local/BufferReferenceMoveFixture.cs"
DN2CPP_GATE_EXTRA_INPUTS="$DN2CPP_GATE_EXTRA_INPUTS gates/fixtures/ldftn-local/GcCoreLibInputFixture.cs gates/fixtures/ldftn-local/Program.cs gates/fixtures/ldftn-local/LdftnLocalFixture.csproj"
DN2CPP_GATE_EXTRA_CONTEXT="${DN2CPP_GATE_EXTRA_CONTEXT:-}|gc-collector:il-wrappers,finalizer-refusal|buffer-reference-moves:internal,batch,overlap,barrier,wrong-count,standalone-clr|gc-corelib-input:collect,memory,measure"

gc_collector_capture_raw_app() {
    local out="$1"
    # Culture preflight runs the original build, without either fixture rewrite.
    # Rewriting erases fallback bodies, so they must also key the cached verdict.
    gate_capture_ok "$out/buffer-unrewritten" run_bounded dotnet "$_CG_APP" || return 1
    DN2CPP_GATE_EXTRA_INPUTS="$DN2CPP_GATE_EXTRA_INPUTS $_CG_APP ${_CG_APP%.dll}.runtimeconfig.json ${_CG_APP%.dll}.deps.json $out/buffer-unrewritten.stdout"
}

gc_collector_prepare_app() {
    local out="$1"
    gate_capture_ok "$out/buffer-fixture" run_bounded dotnet \
        "gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll" \
        --buffer-reference-moves "$_CG_APP" "$_CG_CORELIB" || return 1
    gate_output byte-witness "$out/buffer-fixture.stdout" \
        'buffer-move-fixture:Internal:ref-byte,ref-byte,nuint' \
        'buffer-move-fixture:Batch:ref-byte,ref-byte,nuint' --count 1 --policy crlf
}

gc_collector_asserts() {
    local out="$1" label leaf
    gate_output compare "$out/buffer-unrewritten.stdout" "$out/oracle.stdout"
    gate_output witness "$out/native.stdout" 'GC collection overloads end' \
        'GC logical generations end' 'GC memory query entry points end' \
        'GC notification validation end' 'GC pause duration end' \
        'GC finalizer worker contract end' 'Buffer reference moves end' --count 1 --policy crlf
    gate_capture_ok "$out/gc-prefix" run_bounded dotnet "$_CG_APP" before-gc-contracts
    gate_output prefix "$out/native.stdout" "$out/gc-prefix.stdout" \
        --before '== GC collection overloads ==' --policy crlf
    gate_python gates/check-gc-collector-bodies.py "$out"
    gate_capture_ok "$out/gc-boundary" run_bounded "$out/SelfHostPrimSubset$EXE_EXT" gc-native-boundary
    for label in fullgc-register-direct fullgc-register-delegate fullgc-register-pointer fullgc-cancel-direct fullgc-cancel-delegate; do
        gate_output byte-witness "$out/gc-boundary.stdout" "$label:InvalidOperationException::This API is not available when the concurrent GC is enabled." --count 1 --policy crlf
    done
    for label in fullgc-approach-default fullgc-complete-default fullgc-approach-zero fullgc-complete-positive fullgc-approach-pointer fullgc-approach-time fullgc-complete-time fullgc-approach-fraction fullgc-complete-upper; do
        gate_output byte-witness "$out/gc-boundary.stdout" "$label:NotApplicable" --count 1 --policy crlf
    done
    gate_output byte-witness "$out/gc-boundary.stdout" 'GC notification unavailable end' 'gc-max-generation:0' \
        'gc-worker-refused:System.GC.RunFinalizers is a CoreCLR-owned worker entry point; dn2cpp owns finalization. Use GC.WaitForPendingFinalizers instead.' \
        'GC finalizer worker refusal end' --count 1 --policy crlf
    for leaf in internal batch; do
        local input="$out/buffer-negative-$leaf"
        mkdir -p "$input"
        cp "$_CG_APP" "$input/SelfHostPrimSubset.dll"
        gate_capture_ok "$input/fixture" run_bounded dotnet \
            "gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll" \
            "--buffer-reference-moves-negative-$leaf" "$input/SelfHostPrimSubset.dll" "$_CG_CORELIB"
        gate_capture "$input/refusal" invoke_cli "$input/SelfHostPrimSubset.dll" -r "$_CG_CORELIB" \
            --auto-ref --no-ildiet -o "$input/emitted"
        gate_output status "$(cat "$input/refusal.status")" 2
        label=Internal
        [ "$leaf" != batch ] || label=Batch
        gate_output match-record "$input/refusal.stderr" ".*System[.]Buffer::BulkMoveWithWriteBarrier$label.*Int32.*" --count 1 --policy crlf
    done
    gc_missing_corelib_gate "$out"
}

gc_missing_corelib_gate() {
    local out="$1" root mode input method signature gap source
    root=$(mktemp -d "$out/gc-missing-corelib.XXXXXX") || return 1
    echo "GC missing-CoreLib artifacts: $root"
    for mode in collect memory; do
        input="$root/$mode"
        mkdir -p "$input" || return 1
        cp "$_CG_APP" "$input/SelfHostPrimSubset.dll" || return 1
        gate_capture_ok "$input/fixture" run_bounded dotnet \
            "gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll" \
            "--gc-missing-corelib-$mode" "$input/SelfHostPrimSubset.dll" "$_CG_CORELIB" || return 1
        method=Collect
        signature='System.Void System.GC::Collect()'
        if [ "$mode" = memory ]; then
            method=GetTotalMemory
            signature='System.Int64 System.GC::GetTotalMemory(System.Boolean)'
        fi
        gate_assert_text "$input/fixture.stdout" "gc-missing-corelib-fixture:$mode:$signature" || return 1
        gate_assert_text "$input/fixture.stderr" '' || return 1
        # No -r or --auto-ref: measure records the same compiler refusal for
        # this unresolved MemberRef below ordinary AOT input validation.
        gate_capture_ok "$input/measure" invoke_cli "$input/SelfHostPrimSubset.dll" \
            --no-ildiet --measure -o "$input/measured" || return 1
        gap=$'compile\t<global>\tProgram.Main\tNotSupportedException\t'
        gap+="System.GC.$method requires CoreLib IL; use --auto-ref or -r <path/to/System.Private.CoreLib.dll>."
        gate_output witness "$input/measured/s0-gaps.tsv" "$gap" --count 1 --policy crlf || return 1
        gate_output match-record "$input/measure.stdout" \
            'dn2cpp --measure: [1-9][0-9]* compile-phase bodies attempted, [0-9]+ compiled OK, [1-9][0-9]* gaps total' \
            --count 1 --policy crlf || return 1
        for source in "$input/measured"/*.cpp "$input/measured"/*.h; do
            if [ -e "$source" ]; then
                echo "FAIL: --measure emitted C++: $source" >&2
                return 1
            fi
        done
        echo "OK: $method without CoreLib records the exact compile gap and emits no C++"
    done
}
