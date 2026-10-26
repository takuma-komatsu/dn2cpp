#!/usr/bin/env bash
# Forward null facts skip unreachable formatters; backedges retain custom comparers.
set -euo pipefail
source "$(dirname "$0")/_common.sh"

DN2CPP_GATE_EXTRA_INPUTS="${DN2CPP_GATE_EXTRA_INPUTS:-} samples/dotnet/SharedGenerics/KnownNullJoinsOnly.csproj samples/dotnet/SharedGenerics/KnownNullJoinsOnlyProgram.cs samples/dotnet/SharedGenerics/KnownNullJoinSubset.cs samples/dotnet/SharedGenerics/Directory.Build.props gates/fixtures/known-null-joins/KnownNullJoinFixture.csproj gates/fixtures/known-null-joins/Program.cs"
build_gate_proj gates/fixtures/known-null-joins/KnownNullJoinFixture.csproj
_corelib_gate_core() {
    local project="$1" out="$2"
    _CG_OUT="$out"
    _CG_CORELIB=$(locate_corelib)
    _CG_EXTRA_REFERENCE_INPUTS=()
    dotnet build "samples/dotnet/SharedGenerics/$project.csproj" -c "$CONFIG" \
        --nologo -v q -o "$out/app"
    _CG_APP="$out/app/$project.dll"
    run_bounded dotnet "gates/fixtures/known-null-joins/bin/$CONFIG/$TFM/KnownNullJoinFixture.dll" "$_CG_APP"
    invoke_cli "$_CG_APP" -r "$_CG_CORELIB" "$null_join_sharing" -o "$out"
}
gate_extra_asserts() {
    local out="$1" native line
    gate_capture_ok "$out/assert-native-1" run_bounded "$out/KnownNullJoinsOnly$EXE_EXT"
    native="$out/assert-native-1.stdout"

    for line in '== known null stack joins ==' 'join-null-forward=values/values' \
        'mixed-comparer=False/True' 'backedge-comparer=False/True' \
        'known null stack joins end'; do
        gate_output witness "$native" "$line" --count any --policy crlf \
            || { echo "FAIL: null stack-join witness missing: $line" >&2; exit 1; }
    done
}
for null_join_sharing in --shared-generics --no-shared-generics; do
    DN2CPP_OUT_SUFFIX="-$CONFIG-${null_join_sharing#--}"
    DN2CPP_GATE_EXTRA_CONTEXT="sharing:$null_join_sharing"
    corelib_diff_gate KnownNullJoinsOnly
done
build_proj samples/dotnet/SharedGenerics/SharedGenerics.csproj
bucket="samples/dotnet/SharedGenerics/bin/$CONFIG/$TFM/SharedGenerics.dll"
prefix_out="artifacts/null-stack-join-prefix-$CONFIG"
mkdir -p "$prefix_out"
gate_capture_ok "$prefix_out/before" run_bounded dotnet "$bucket" before-null-stack-joins
gate_capture_ok "$prefix_out/full" run_bounded dotnet "$bucket"
gate_output select "$prefix_out/full.stdout" "$prefix_out/prefix.stdout" --before '== known null stack joins ==' --policy crlf
gate_output compare "$prefix_out/before.stdout" "$prefix_out/prefix.stdout"
echo 'PASS: known null stack joins'
