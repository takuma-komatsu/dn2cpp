#!/usr/bin/env bash
# Alignment helpers and retained DIM memory calls preserve alignment-first guards.
probe_vector_alignment() {
    local root="$1" out axis emit
    local fixture="${DN2CPP_VECTOR_ALIGNMENT_FIXTURE_DLL:-gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll}"
    local compiler="${DN2CPP_VECTOR_ALIGNMENT_COMPILER_DLL:-$(dirname "${DN2CPP_CLI_DLL:-src/Dn2Cpp.Cli/bin/$CONFIG/$TFM/dn2cpp.dll}")/Dn2Cpp.Transpiler.dll}"
    if [ -z "${DN2CPP_VECTOR_ALIGNMENT_FIXTURE_DLL:-}" ]; then
        build_gate_proj gates/fixtures/ldftn-local/LdftnLocalFixture.csproj || return 1
    fi
    mkdir -p "$root" || return 1
    out=$(mktemp -d "$root/alignment-XXXXXX") || return 1
    gate_capture_ok "$out/guards" run_bounded dotnet "$fixture" --vector-alignment-guards "$compiler" "$_CG_CORELIB" "$out/model.dll" || return 1
    grep -E '^vector-alignment-guards:checks=[1-9][0-9]*$' "$out/guards.stdout" >/dev/null || return 1
    gate_capture_ok "$out/generate" run_bounded dotnet "$fixture" --vector-alignment-fixture "$out/runtime.dll" "$_CG_CORELIB" || return 1
    # The oracle skips CLR visibility checks for internal/private properties. Both
    # profiles assert semantic relations rather than comparing host SIMD widths.
    gate_capture_ok "$out/oracle" run_bounded env -u DOTNET_EnableHWIntrinsic -u COMPlus_EnableHWIntrinsic dotnet "$fixture" --vector-alignment-oracle || return 1
    gate_capture_ok "$out/oracle-nohw" run_bounded env -u COMPlus_EnableHWIntrinsic DOTNET_EnableHWIntrinsic=0 dotnet "$fixture" --vector-alignment-oracle || return 1
    gate_output compare "$out/oracle.stdout" "$out/oracle-nohw.stdout" --policy crlf || return 1
    [ ! -s "$out/oracle.stderr" ] && [ ! -s "$out/oracle-nohw.stderr" ] || return 1
    [ "$(wc -l < "$out/oracle.stdout")" -eq 93 ] || return 1
    awk '/^vector-(alignment:helper:(direct|address|calli)|property:(0|64|128|256|512):(int|decimal):(Alignment|ElementCount|IsHardwareAccelerated):(direct|address|calli)):pass$/ { if (seen[$0]++) exit 1; next } { exit 1 } END { if (NR != 93) exit 1 }' "$out/oracle.stdout" || return 1
    # Preserve the existing helper output. The DIM block below reaches private
    # bridges through their actual closed interface defaults on the native axis.
    head -n 3 "$out/oracle.stdout" > "$out/native-expected.stdout" || return 1
    for axis in raw diet; do
        emit=$(mktemp -d "$out/$axis-XXXXXX") || return 1
        local flags=() runner=()
        [ "$axis" != raw ] || flags+=(--no-ildiet)
        gate_capture_ok "$out/$axis-emit" invoke_cli "$out/runtime.dll" -r "$_CG_CORELIB" --auto-ref "${flags[@]}" -o "$emit" || return 1
        if [ "$axis" = diet ]; then
            python3 - "$emit/ildiet" <<'PYHELPER' || return 1
import sys
sys.path.insert(0, "gates")
from ildiet_manifest import manifest
result, _ = manifest(sys.argv[1])
assert result.get("fallback") == "none" and result.get("cutsValidated") == "true"
assert next(a for a in result.findall("assembly") if a.get("name") == "VectorAlignment").get("mode") == "rewritten"
PYHELPER
        fi
        if [ "${WASM:-0}" = 1 ]; then
            compile_console_wasm "$emit" VectorAlignment || return 1
            runner=(node "$emit/VectorAlignment.js")
        else
            compile_console "$emit" VectorAlignment || return 1
            runner=("$emit/VectorAlignment$EXE_EXT")
        fi
        gate_capture_ok "$out/$axis-run" run_bounded "${runner[@]}" || return 1
        gate_output compare "$out/$axis-run.stdout" "$out/native-expected.stdout" --policy crlf || return 1
        gate_output compare "$out/$axis-run.stderr" "$out/oracle.stderr" --policy crlf || return 1
    done
    gate_capture_ok "$out/dim-guards" run_bounded dotnet "$fixture" --vector-alignment-dim-guards "$compiler" "$_CG_CORELIB" "$out/dim-model.dll" || return 1
    grep -E '^vector-alignment-dim-guards:checks=[1-9][0-9]*$' "$out/dim-guards.stdout" >/dev/null || return 1
    gate_capture_ok "$out/dim-generate" run_bounded dotnet "$fixture" --vector-alignment-dim-fixture "$out/dim.dll" "$_CG_CORELIB" || return 1
    gate_capture_ok "$out/dim-oracle" run_bounded env -u DOTNET_EnableHWIntrinsic -u COMPlus_EnableHWIntrinsic dotnet "$fixture" --vector-alignment-dim-oracle || return 1
    gate_capture_ok "$out/dim-oracle-nohw" run_bounded env -u COMPlus_EnableHWIntrinsic DOTNET_EnableHWIntrinsic=0 dotnet "$fixture" --vector-alignment-dim-oracle || return 1
    gate_output compare "$out/dim-oracle.stdout" "$out/dim-oracle-nohw.stdout" --policy crlf || return 1
    [ ! -s "$out/dim-oracle.stderr" ] && [ ! -s "$out/dim-oracle-nohw.stderr" ] || return 1
    awk '/^vector-alignment-dim:(0|64|128|256|512):(Byte|SByte|Int16|UInt16|Int32|UInt32|Int64|UInt64|IntPtr|UIntPtr|Single|Double):(direct|address|calli):[012]:pass$/ { if (seen[$0]++) exit 1; next } { exit 1 } END { if (NR != 540) exit 1 }' "$out/dim-oracle.stdout" || return 1
    cat "$out/oracle.stdout" "$out/dim-oracle.stdout" > "$out/combined-oracle.stdout" || return 1
    head -n 93 "$out/combined-oracle.stdout" > "$out/old-prefix.stdout" || return 1
    gate_output compare "$out/old-prefix.stdout" "$out/oracle.stdout" --policy crlf || return 1
    for axis in raw diet raw-unshared diet-unshared; do
        emit=$(mktemp -d "$out/dim-$axis-XXXXXX") || return 1
        local flags=() runner=()
        [[ "$axis" = diet* ]] || flags+=(--no-ildiet)
        [[ "$axis" != *-unshared ]] || flags+=(--no-shared-generics)
        gate_capture_ok "$out/dim-$axis-emit" invoke_cli "$out/dim.dll" -r "$_CG_CORELIB" --auto-ref "${flags[@]}" -o "$emit" || return 1
        gate_capture_ok "$out/dim-$axis-bodies" python3 - "$emit" "$axis" <<'PYDIM' || return 1
import re, sys
from pathlib import Path
sys.path.insert(0, "gates")
from ildiet_manifest import manifest
output = Path(sys.argv[1])
if sys.argv[2].startswith("diet"):
    result, generation = manifest(output / "ildiet")
    assert result.get("fallback") == "none" and result.get("cutsValidated") == "true"
    fixture = next(a for a in result.findall("assembly") if a.get("name") == "VectorAlignmentDim")
    assert fixture.get("mode") == "rewritten", "DIM fixture was copied, not rewritten"
    assert Path(result.get("input")) == generation / "VectorAlignmentDim.dll"
text = "\n".join(p.read_text() for p in [output / "generated.h", *sorted(output.glob("generated*.cpp"))])
defaults = re.findall(r"^// System\.Runtime\.Intrinsics\.ISimdVector_[^\n]+::(?:LoadAligned|StoreAligned)\n[^\n]+\n\{\n(.*?)^\}", text, re.M | re.S)
assert len(defaults) == 120, "actual closed DIM bodies are missing"
for body in defaults:
    assert "get_Alignment" in body and "ThrowHelper_ThrowAccessViolationException" in body and "dn2cpp_rem_unsigned" in body
    assert "dn2cpp_throw_invalid_operation" not in body
bridges = re.findall(r"^// System\.(?:Numerics|Runtime\.Intrinsics)\.Vector[^\n]+::System\.Runtime\.Intrinsics\.ISimdVector<[^\n]+\.get_Alignment$", text, re.M)
assert len(bridges) == 60, "exact managed MethodImpl bridges are missing"
assert re.search(r"^inline void ThrowHelper_ThrowAccessViolationException[^\n]+\n\{", text, re.M), "real exception body missing"
print("vector-alignment-dim-bodies:pass")
PYDIM
        if [ "${WASM:-0}" = 1 ]; then
            compile_console_wasm "$emit" VectorAlignmentDim || return 1
            runner=(node "$emit/VectorAlignmentDim.js")
        else
            compile_console "$emit" VectorAlignmentDim || return 1
            runner=("$emit/VectorAlignmentDim$EXE_EXT")
        fi
        gate_capture_ok "$out/dim-$axis-run" run_bounded "${runner[@]}" || return 1
        gate_output compare "$out/dim-$axis-run.stdout" "$out/dim-oracle.stdout" --policy crlf || return 1
        gate_output compare "$out/dim-$axis-run.stderr" "$out/dim-oracle.stderr" --policy crlf || return 1
    done
    cat "$out/raw-run.stdout" "$out/dim-raw-run.stdout" > "$out/combined-native.stdout" || return 1
    head -n 3 "$out/combined-native.stdout" > "$out/native-old-prefix.stdout" || return 1
    gate_output compare "$out/native-old-prefix.stdout" "$out/native-expected.stdout" --policy crlf || return 1
    local element mutation
    for element in char bool decimal int128 uint128; do
        gate_capture_ok "$out/unsupported-$element-generate" run_bounded dotnet "$fixture" --vector-alignment-dim-unsupported "$out/unsupported-$element.dll" "$_CG_CORELIB" "$element" || return 1
        gate_capture "$out/unsupported-$element" invoke_cli "$out/unsupported-$element.dll" -r "$_CG_CORELIB" --auto-ref --no-ildiet -o "$out/unsupported-$element-emit" || return 1
        gate_output status "$(cat "$out/unsupported-$element.status")" 2 || return 1
        grep -F 'ISimdVector.Alignment requires its exact CoreLib slot, numeric closure and concrete managed MethodImpl' "$out/unsupported-$element.stderr" >/dev/null || return 1
    done
    for mutation in vararg owner-modifier methodspec; do
        gate_capture_ok "$out/helper-$mutation-generate" run_bounded dotnet "$fixture" --vector-alignment-dim-helper-negative "$out/helper-$mutation.dll" "$_CG_CORELIB" "$mutation" || return 1
        gate_capture "$out/helper-$mutation" invoke_cli "$out/helper-$mutation.dll" -r "$_CG_CORELIB" --no-ildiet -o "$out/helper-$mutation-emit" || return 1
        gate_output status "$(cat "$out/helper-$mutation.status")" 2 || return 1
        grep -E 'ISimdVector.Alignment requires|Vector alignment dependencies are not generic methods|ThrowAccessViolationException requires' "$out/helper-$mutation.stderr" >/dev/null || return 1
    done
    source gates/_vector-alignment-guards.sh || return 1
    probe_unsupported_aligned_dim_guards "$out" || return 1
    probe_unsupported_aligned_dim_runtime "$out" "$fixture" || return 1
}


probe_unsupported_aligned_dim_runtime() {
    local out="$1" fixture="$2" axis kind emit
    local checker=gates/fixtures/ldftn-local/check-unsupported-aligned-dim.py
    gate_capture_ok "$out/unsupported-generate" run_bounded dotnet "$fixture" --unsupported-aligned-dim-fixture "$out/unsupported.dll" "$_CG_CORELIB" || return 1
    gate_capture_ok "$out/retained-generate" run_bounded dotnet "$fixture" --unsupported-aligned-dim-fixture "$out/retained.dll" "$_CG_CORELIB" retained-only || return 1
    gate_capture_ok "$out/unsupported-oracle" run_bounded env -u DOTNET_EnableHWIntrinsic -u COMPlus_EnableHWIntrinsic -u DOTNET_EnableAVX -u COMPlus_EnableAVX dotnet "$fixture" --unsupported-aligned-dim-oracle || return 1
    gate_capture_ok "$out/unsupported-oracle-software" run_bounded env -u COMPlus_EnableHWIntrinsic -u DOTNET_EnableAVX -u COMPlus_EnableAVX DOTNET_EnableHWIntrinsic=0 dotnet "$fixture" --unsupported-aligned-dim-oracle || return 1
    gate_capture_ok "$out/unsupported-oracle-128" run_bounded env -u DOTNET_EnableHWIntrinsic -u COMPlus_EnableHWIntrinsic DOTNET_EnableAVX=0 COMPlus_EnableAVX=0 dotnet "$fixture" --unsupported-aligned-dim-oracle || return 1
    local profile
    for profile in oracle oracle-software oracle-128; do
        [ ! -s "$out/unsupported-$profile.stderr" ] || return 1
        gate_capture_ok "$out/unsupported-$profile-check" python3 "$checker" "$out/unsupported-$profile.stdout" || return 1
    done
    gate_output compare "$out/unsupported-oracle-software.stdout" "$out/unsupported-oracle-128.stdout" --policy crlf || return 1
    # The default CLR may choose a wider Numerics target. The aligned16-not32
    # case deliberately distinguishes that profile; native uses supported byte Count.
    for axis in raw diet raw-unshared diet-unshared; do
        local flags=() runner=()
        [[ "$axis" = diet* ]] || flags+=(--no-ildiet)
        [[ "$axis" != *-unshared ]] || flags+=(--no-shared-generics)
        for kind in unsupported retained; do
            emit=$(mktemp -d "$out/$kind-$axis-XXXXXX") || return 1
            DN2CPP_STRICT_COMPLETION=1 gate_capture_ok "$out/$kind-$axis-emit" invoke_cli "$out/$kind.dll" -r "$_CG_CORELIB" --auto-ref "${flags[@]}" -o "$emit" || return 1
            if [[ "$axis" = diet* ]]; then
                gate_capture_ok "$out/$kind-$axis-diet" python3 - "$emit/ildiet" "$out/$kind.dll" "$out/$kind-$axis-diet-input.path" <<'PYUNSUPPORTED' || return 1
import hashlib, json, sys
from pathlib import Path
sys.path.insert(0, "gates")
from ildiet_manifest import manifest
result, generation = manifest(sys.argv[1])
assembly = next(a for a in result.findall("assembly") if a.get("name") == "UnsupportedAlignedDim")
rewritten = Path(result.get("input"))
assert rewritten == generation / "UnsupportedAlignedDim.dll"
original, output = Path(sys.argv[2]).read_bytes(), rewritten.read_bytes()
Path(sys.argv[3]).write_text(str(rewritten) + "\n")
assert result.get("fallback") == "none" and result.get("cutsValidated") == "true"
assert assembly.get("mode") == "rewritten"
print(json.dumps({"mode": assembly.get("mode"), "fallback": result.get("fallback"),
                  "input_sha256": hashlib.sha256(original).hexdigest(), "output_sha256": hashlib.sha256(output).hexdigest(),
                  "bytes_changed": original != output, "remapped_token_coverage_claimed": False}, sort_keys=True))
PYUNSUPPORTED
                gate_capture_ok "$out/$kind-$axis-input-check" run_bounded dotnet "$fixture" --unsupported-aligned-dim-inspect "$(cat "$out/$kind-$axis-diet-input.path")" || return 1
            fi
            gate_capture_ok "$out/$kind-$axis-bodies" python3 gates/fixtures/ldftn-local/check-unsupported-aligned-dim-bodies.py "$emit" || return 1
            if [ "${WASM:-0}" = 1 ]; then
                compile_console_wasm "$emit" UnsupportedAlignedDim || return 1
                runner=(node "$emit/UnsupportedAlignedDim.js")
            else
                compile_console "$emit" UnsupportedAlignedDim || return 1
                runner=("$emit/UnsupportedAlignedDim$EXE_EXT")
            fi
            gate_capture_ok "$out/$kind-$axis-run" run_bounded "${runner[@]}" || return 1
            [ ! -s "$out/$kind-$axis-run.stderr" ] || return 1
            if [ "$kind" = retained ]; then
                gate_capture_ok "$out/$kind-$axis-check" python3 "$checker" "$out/$kind-$axis-run.stdout" --retained-only || return 1
            else
                cat "$out/dim-$axis-run.stdout" "$out/$kind-$axis-run.stdout" > "$out/$kind-$axis-combined.stdout" || return 1
                gate_capture_ok "$out/$kind-$axis-check" python3 "$checker" "$out/$kind-$axis-combined.stdout" \
                    --prefix "$out/dim-oracle.stdout" --oracle "$out/unsupported-oracle-software.stdout" || return 1
            fi
        done
    done
}
