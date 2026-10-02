#!/usr/bin/env bash
# Same-width integer and enum interfaces retain their own constrained dispatch slots.
set -euo pipefail
source "$(dirname "$0")/_common.sh"

DN2CPP_GATE_EXTRA_INPUTS="${DN2CPP_GATE_EXTRA_INPUTS:-} samples/dotnet/SharedGenerics/WidthTypedSlotsOnly.csproj samples/dotnet/SharedGenerics/WidthTypedSlotsOnlyProgram.cs samples/dotnet/SharedGenerics/WidthJoinSubset.cs samples/dotnet/SharedGenerics/Directory.Build.props"
_corelib_gate_core() {
    local project="$1" out="$2"
    _CG_OUT="$out"
    _CG_CORELIB=$(locate_corelib)
    _CG_EXTRA_REFERENCE_INPUTS=()
    dotnet build "samples/dotnet/SharedGenerics/$project.csproj" -c "$CONFIG" \
        --nologo -v q -o "$out/app"
    _CG_APP="$out/app/$project.dll"
    invoke_cli "$_CG_APP" -r "$_CG_CORELIB" "$width_slot_sharing" -o "$out"
}
gate_extra_asserts() {
    local out="$1" native line
    native=$(run_bounded "$out/WidthTypedSlotsOnly$EXE_EXT")
    native=$(strip_cr_win "$native")
    for line in '== same-width constrained interface slots ==' \
        'width typed slots=True,True,False/1,-1' \
        'width typed comparer=True:3/True:40' \
        'same-width constrained interface slots end'; do
        grep -Fxq -- "$line" <<< "$native" \
            || { echo "FAIL: same-width interface witness missing: $line" >&2; exit 1; }
    done
}
for width_slot_sharing in --shared-generics --no-shared-generics; do
    DN2CPP_OUT_SUFFIX="-$CONFIG-${width_slot_sharing#--}"
    DN2CPP_GATE_EXTRA_CONTEXT="sharing:$width_slot_sharing"
    corelib_diff_gate WidthTypedSlotsOnly
done
build_proj samples/dotnet/SharedGenerics/SharedGenerics.csproj
bucket="samples/dotnet/SharedGenerics/bin/$CONFIG/$TFM/SharedGenerics.dll"
prefix_out="artifacts/width-typed-slots-prefix-$CONFIG"
mkdir -p "$prefix_out"
run_bounded dotnet "$bucket" before-width-typed-slots > "$prefix_out/before.stdout"
run_bounded dotnet "$bucket" > "$prefix_out/full.stdout"
sed '/^== same-width constrained interface slots ==/,$d' "$prefix_out/full.stdout" > "$prefix_out/prefix.stdout"
diff -u <(strip_cr_win_file "$prefix_out/before.stdout") <(strip_cr_win_file "$prefix_out/prefix.stdout")

echo 'PASS: same-width constrained interface slots'
