#!/usr/bin/env bash
# Retained unsupported ISimdVector aligned loads and stores, including argument faults.

vector_alignment_setup() {
    case "${DN2CPP_VECTOR_FULL:-0}" in
        0|1) DN2CPP_VECTOR_FULL=${DN2CPP_VECTOR_FULL:-0} ;;
        *) echo 'DN2CPP_VECTOR_FULL must be 0 or 1' >&2; return 1 ;;
    esac
    build_proj gates/fixtures/ldftn-local/LdftnLocalFixture.csproj
    prepare_cli
    vector_aligned_fixture="gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll"
    vector_aligned_checker=gates/fixtures/vector-alignment/check-unsupported-order.py
    vector_aligned_cli="$(_gate_cli_hash)"
    DN2CPP_GATE_EXTRA_INPUTS="${DN2CPP_GATE_EXTRA_INPUTS:-} $vector_aligned_fixture $vector_aligned_checker gates/_vector-alignment-probe.sh"
    DN2CPP_GATE_EXTRA_CONTEXT="${DN2CPP_GATE_EXTRA_CONTEXT:-}|unsupported-aligned:full=$DN2CPP_VECTOR_FULL|cli:$vector_aligned_cli"
}

vector_aligned_rewrite() {
    local app="$1" out="$2" prepared stable file
    shift 2
    mkdir -p "$out" || return 1
    prepared=$(mktemp -d "$out/unsupported-aligned-app.XXXXXX") || return 1
    gate_capture_ok "$prepared/rewrite" run_bounded dotnet "$vector_aligned_fixture" \
        --unsupported-aligned "$app" "$prepared/VectorProbe.dll" "$_CG_CORELIB" "$@" || return 1
    cp "${app%.dll}.runtimeconfig.json" "$prepared/VectorProbe.runtimeconfig.json" || return 1
    cp "${app%.dll}.deps.json" "$prepared/VectorProbe.deps.json" || return 1
    # Cache identity includes paths. Retain the rewrite evidence and publish at a stable path.
    stable="$out/unsupported-aligned-app"
    mkdir -p "$stable" || return 1
    for file in VectorProbe.dll VectorProbe.runtimeconfig.json VectorProbe.deps.json; do
        if ! cmp -s "$prepared/$file" "$stable/$file"; then
            mv "$prepared/$file" "$stable/$file" || return 1
        fi
    done
    _CG_APP="$stable/VectorProbe.dll"
}

gate_prepare_app() {
    [ "$1" = VectorProbe ] && [ -z "${DN2CPP_WASM_SIMD:-}" ] || return 0
    vector_aligned_rewrite "$_CG_APP" "$2"
}

vector_aligned_run() {
    local out="$1" reference="$2"; shift 2
    gate_capture_ok "$out/ordered-native" run_bounded "$@" unsupported-aligned-order
    gate_output compare "$out/ordered-native.stdout" "$reference/ordered-target.stdout" --policy crlf
    gate_output compare "$out/ordered-native.stderr" "$reference/ordered-clr-128.stderr" --policy crlf
}

vector_aligned_parity() {
    local out="$1" profile projection=()
    shift
    gate_capture_ok "$out/ordered-layout" run_bounded dotnet "$vector_aligned_fixture" \
        --unsupported-aligned-profile "$_CG_CORELIB" || return 1
    local clean=(env -u DOTNET_EnableHWIntrinsic -u COMPlus_EnableHWIntrinsic -u DOTNET_EnableAVX -u COMPlus_EnableAVX)
    local flags=()
    for profile in default software 128; do
        flags=()
        projection=()
        case "$profile" in
            software) flags=(DOTNET_EnableHWIntrinsic=0) ;;
            128) flags=(DOTNET_EnableAVX=0); projection=("$out/ordered-target.stdout") ;;
        esac
        gate_capture_ok "$out/ordered-clr-$profile" run_bounded "${clean[@]}" ${flags[@]+"${flags[@]}"} \
            dotnet "$_CG_APP" unsupported-aligned-order || return 1
        gate_python "$vector_aligned_checker" "$out/ordered-layout.stdout" "$out/ordered-clr-$profile.stdout" \
            ${projection[@]+"${projection[@]}"} || return 1
    done
    # On a wider host the default CLR may fault on a 16-aligned Numerics pointer.
    # The target uses the 128-bit profile; the checker uses each CLR's actual alignment.
    gate_output compare "$out/ordered-clr-128.stdout" "$out/ordered-clr-software.stdout" --policy crlf
    vector_aligned_run "$out" "$out" "$@"
}

vector_aligned_diet() {
    gate_python - "$1/ildiet/ildiet-result.xml" <<'PYDIET'
import sys
from xml.etree import ElementTree

root = ElementTree.parse(sys.argv[1]).getroot()
app = root.find("assembly[@name='VectorProbe']")
if root.get("fallback") != "none" or root.get("cutsValidated") != "true" or app is None or app.get("mode") != "rewritten":
    raise SystemExit(f"{sys.argv[1]}: expected the ILDiet-rewritten VectorProbe")
PYDIET
}

vector_aligned_full() {
    local base="$1" axis out
    if [ "$DN2CPP_VECTOR_FULL" != 1 ]; then
        echo 'Vector aligned defaults, extra raw/ILDiet and unshared builds: skipped (not selected); not counted as passed'
        return 0
    fi
    # The aggregate supplies shared ILDiet. The other compiler policies can use the
    # same built assembly with only its ordered section selected as the entry point.
    vector_aligned_diet "$base"
    local _CG_APP flags=() runner=()
    vector_aligned_rewrite "samples/dotnet/VectorProbe/bin/$CONFIG/$TFM/VectorProbe.dll" "$base/ordered-only" ordered-only
    for axis in raw-shared diet-unshared raw-unshared; do
        flags=()
        [[ "$axis" != raw-* ]] || flags+=(--no-ildiet)
        [[ "$axis" != *-unshared ]] || flags+=(--no-shared-generics)
        out="$base/ordered-$axis"
        mkdir -p "$out"
        if gate_cache_check "$out" "vector-aligned-order|axis:$axis|strict:1|cli:$vector_aligned_cli" \
                "$_CG_APP" "$_CG_CORELIB" "$vector_aligned_fixture" "$vector_aligned_checker" "$base/ordered-target.stdout" "$base/ordered-clr-128.stderr"; then
            gate_cache_hit_msg
            continue
        fi
        gate_capture_ok "$out/emit" invoke_cli "$_CG_APP" -r "$_CG_CORELIB" ${flags[@]+"${flags[@]}"} -o "$out"
        if [[ "$axis" = diet-* ]]; then vector_aligned_diet "$out"; fi
        if [ "${WASM:-0}" = 1 ]; then
            compile_console_wasm "$out" VectorProbe
            runner=(node "$out/VectorProbe.js")
        else
            compile_console "$out" VectorProbe
            runner=("./$out/VectorProbe$EXE_EXT")
        fi
        vector_aligned_run "$out" "$base" "${runner[@]}"
        gate_cache_commit
    done
}
