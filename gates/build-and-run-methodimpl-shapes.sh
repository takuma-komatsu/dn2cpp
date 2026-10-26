#!/usr/bin/env bash
# Ordinary generic interface, static-abstract and default bodies retain definition identity.
set -euo pipefail
source "$(dirname "$0")/_common.sh"

DN2CPP_GATE_EXTRA_INPUTS="${DN2CPP_GATE_EXTRA_INPUTS:-} samples/dotnet/SharedGenerics/MethodImplShapesOnly.csproj samples/dotnet/SharedGenerics/MethodImplShapesOnlyProgram.cs samples/dotnet/SharedGenerics/MethodImplShapeSubset.cs samples/dotnet/SharedGenerics/Directory.Build.props"
_corelib_gate_core() {
    local project="$1" out="$2"
    _CG_OUT="$out"
    _CG_CORELIB=$(locate_corelib)
    _CG_EXTRA_REFERENCE_INPUTS=()
    dotnet build "samples/dotnet/SharedGenerics/$project.csproj" -c "$CONFIG" \
        --nologo -v q -o "$out/app"
    _CG_APP="$out/app/$project.dll"
    invoke_cli "$_CG_APP" -r "$_CG_CORELIB" "$methodimpl_sharing" -o "$out"
}
gate_extra_asserts() {
    local out="$1" native line
    gate_capture_ok "$out/assert-native-1" run_bounded "$out/MethodImplShapesOnly$EXE_EXT"
    native="$out/assert-native-1.stdout"

    for line in '== ordinary MethodImpl definition shapes ==' \
        'methodimpl-interface-object=X/object/X/object' \
        'methodimpl-interface-string=X/object' \
        'methodimpl-single-default=default-X/single-object' \
        'methodimpl-single-string=default-X/single-object' \
        'methodimpl-derived-default=derived-X/derived-object' \
        'methodimpl-projected-object=projected-default-A/projected-B' \
        'methodimpl-projected-string=projected-default-A/projected-B' \
        'methodimpl-static-object=static-X/static-object/static-X' \
        'methodimpl-static-string=static-X/static-object' \
        'ordinary MethodImpl definition shapes end'; do
        gate_output witness "$native" "$line" --count any --policy crlf \
            || { echo "FAIL: MethodImpl definition witness missing: $line" >&2; exit 1; }
    done
}

for methodimpl_sharing in --shared-generics --no-shared-generics; do
    DN2CPP_OUT_SUFFIX="-$CONFIG-${methodimpl_sharing#--}"
    DN2CPP_GATE_EXTRA_CONTEXT="sharing:$methodimpl_sharing"
    corelib_diff_gate MethodImplShapesOnly
done

build_proj samples/dotnet/SharedGenerics/SharedGenerics.csproj
bucket="samples/dotnet/SharedGenerics/bin/$CONFIG/$TFM/SharedGenerics.dll"
prefix_out="artifacts/methodimpl-prefix-$CONFIG"
mkdir -p "$prefix_out"
gate_capture_ok "$prefix_out/before" run_bounded dotnet "$bucket" before-methodimpl-shapes
gate_capture_ok "$prefix_out/full" run_bounded dotnet "$bucket"
gate_output select "$prefix_out/full.stdout" "$prefix_out/prefix.stdout" --before '== ordinary MethodImpl definition shapes ==' --policy crlf
gate_output compare "$prefix_out/before.stdout" "$prefix_out/prefix.stdout"
echo 'PASS: ordinary MethodImpl definition shapes'
