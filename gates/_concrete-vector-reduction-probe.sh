#!/usr/bin/env bash
# Concrete reductions preserve numeric equality, bit predicates and real lane counts.
probe_concrete_vector_reductions() {
    local root="$1" out axis kind emit
    local fixture="${DN2CPP_CONCRETE_VECTOR_REDUCTION_FIXTURE_DLL:-gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll}"
    local compiler="${DN2CPP_CONCRETE_VECTOR_REDUCTION_COMPILER_DLL:-$(dirname "${DN2CPP_CLI_DLL:-src/Dn2Cpp.Cli/bin/$CONFIG/$TFM/dn2cpp.dll}")/Dn2Cpp.Transpiler.dll}"
    if [ -z "${DN2CPP_CONCRETE_VECTOR_REDUCTION_FIXTURE_DLL:-}" ]; then
        build_gate_proj gates/fixtures/ldftn-local/LdftnLocalFixture.csproj || return 1
    fi
    mkdir -p "$root" || return 1
    out=$(mktemp -d "$root/concrete-reductions-XXXXXX") || return 1
    gate_capture_ok "$out/guards" run_bounded dotnet "$fixture" --concrete-vector-reduction-guards "$compiler" "$_CG_CORELIB" "$out/model.dll" || return 1
    grep -E '^concrete-vector-reduction-guards:checks=[1-9][0-9]*$' "$out/guards.stdout" >/dev/null || return 1
    gate_capture_ok "$out/helper-generate" run_bounded dotnet "$fixture" --concrete-vector-reduction-fixture "$out/helpers.dll" "$_CG_CORELIB" || return 1
    gate_capture_ok "$out/public-generate" run_bounded dotnet "$fixture" --concrete-vector-reduction-parity-fixture "$out/public.dll" "$_CG_CORELIB" || return 1
    gate_capture_ok "$out/helpers-oracle" run_bounded env -u DOTNET_EnableHWIntrinsic -u COMPlus_EnableHWIntrinsic dotnet "$fixture" --concrete-vector-reduction-oracle || return 1
    gate_capture_ok "$out/helpers-nohw" run_bounded env -u COMPlus_EnableHWIntrinsic DOTNET_EnableHWIntrinsic=0 dotnet "$fixture" --concrete-vector-reduction-oracle || return 1
    gate_output compare "$out/helpers-oracle.stdout" "$out/helpers-nohw.stdout" --policy crlf || return 1
    [ "$(wc -l < "$out/helpers-oracle.stdout")" -eq 72 ] || return 1
    gate_capture_ok "$out/public-expected" run_bounded dotnet "$fixture" --concrete-vector-reduction-parity-oracle || return 1
    # Unlike the internal helper fixture, these public calls can execute directly
    # on CLR. This also executes the generated once/order/exception controls.
    gate_capture_ok "$out/public-oracle" run_bounded env -u DOTNET_EnableHWIntrinsic -u COMPlus_EnableHWIntrinsic dotnet "$out/public.dll" || return 1
    gate_capture_ok "$out/public-nohw" run_bounded env -u COMPlus_EnableHWIntrinsic DOTNET_EnableHWIntrinsic=0 dotnet "$out/public.dll" || return 1
    gate_output compare "$out/public-oracle.stdout" "$out/public-expected.stdout" --policy crlf || return 1
    gate_output compare "$out/public-oracle.stdout" "$out/public-nohw.stdout" --policy crlf || return 1
    [ "$(wc -l < "$out/public-oracle.stdout")" -eq 434 ] || return 1
    for kind in helpers public; do
        [ ! -s "$out/$kind-oracle.stderr" ] && [ ! -s "$out/$kind-nohw.stderr" ] || return 1
        for axis in raw diet; do
            emit=$(mktemp -d "$out/$kind-$axis-XXXXXX") || return 1
            local flags=() runner=() name
            [ "$axis" != raw ] || flags+=(--no-ildiet)
            name=ConcreteVectorReductions
            [ "$kind" != public ] || name=ConcreteVectorReductionParity
            gate_capture_ok "$out/$kind-$axis-emit" invoke_cli "$out/$kind.dll" -r "$_CG_CORELIB" --auto-ref "${flags[@]}" -o "$emit" || return 1
            if [ "$axis" = diet ]; then
                gate_python - "$emit/ildiet" "$out/$kind.dll" "$name" <<'PYDIET' || return 1
import sys
from pathlib import Path
sys.path.insert(0, "gates")
from ildiet_manifest import manifest
result, generation = manifest(sys.argv[1])
assert result.get("fallback") == "none" and result.get("cutsValidated") == "true"
assembly = next(a for a in result.findall("assembly") if a.get("name") == sys.argv[3])
assert assembly.get("mode") == "rewritten", "fixture was copied, not rewritten"
rewritten = Path(result.get("input"))
assert rewritten == generation / (sys.argv[3] + ".dll")
assert rewritten.read_bytes() != Path(sys.argv[2]).read_bytes(), "fixture bytes did not change"
PYDIET
            fi
            if [ "${WASM:-0}" = 1 ]; then
                compile_console_wasm "$emit" "$name" || return 1
                runner=(node "$emit/$name.js")
            else
                compile_console "$emit" "$name" || return 1
                runner=("$emit/$name$EXE_EXT")
            fi
            gate_capture_ok "$out/$kind-$axis-run" run_bounded "${runner[@]}" || return 1
            gate_output compare "$out/$kind-$axis-run.stdout" "$out/$kind-oracle.stdout" --policy crlf || return 1
            gate_output compare "$out/$kind-$axis-run.stderr" "$out/$kind-oracle.stderr" --policy crlf || return 1
        done
    done
}
