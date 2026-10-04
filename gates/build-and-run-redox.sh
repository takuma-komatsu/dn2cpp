#!/usr/bin/env bash
# The real CAPCOM.REDox package: JSON/JSON5 token DOM parsing and mutation,
# UTF-8 and stream inputs, DOX conversion and cloning, reflection-based AOT
# object/collection/constructor binding, scalar conversions and a custom converter.
# AllowDynamicGenericConverters=false selects RE:Dox's own AOT fallback on both
# sides. The shipped IL is hash-pinned and stdout/stderr/status are diffed vs .NET.
source "$(dirname "$0")/_common.sh"

project=REDoxSample
pin=gates/expected/redox-dll.sha256
markers=gates/expected/redox-markers.txt
nuget_packages="$(nuget_global_packages_root)"
if [ ! -d "$nuget_packages/capcom.redox/1.0.0" ] \
    && ! curl -fsI --max-time 15 https://api.nuget.org/v3/index.json >/dev/null 2>&1; then
    gate_skip "CAPCOM.REDox 1.0.0 is not in the NuGet cache and nuget.org is unreachable"
fi
build_gate_proj "samples/dotnet/$project/$project.csproj"
appbin="samples/dotnet/$project/bin/$CONFIG/$TFM"
redox="$appbin/REDox.dll"
[ -f "$redox" ] || { echo "FAIL: REDox.dll did not restore to $redox" >&2; exit 1; }
actual=$(shasum -a 256 "$redox" | awk '{print $1}')
[ "$actual" = "$(cat "$pin")" ] \
    || { echo "FAIL: REDox.dll differs from the pinned package ($pin): $actual" >&2; exit 1; }

gate_extra_asserts() {
    local out="$1" symbol line
    assert_exit_code "$2" 0
    assert_exit_code "$3" 0
    [ ! -s "$out/expected.err" ] && [ ! -s "$out/native.err" ] \
        || { echo "FAIL: RE:Dox must run with empty stderr on both sides" >&2; return 1; }
    for line in 'JSON DOM end' 'JSON5 end' 'AOT serialization end' \
        'scalar converters end' 'custom converter end' 'REDox end'; do
        grep -Fxq -- "$line" "$out/native.out" \
            || { echo "FAIL: missing RE:Dox section witness: $line" >&2; return 1; }
    done
    while IFS= read -r symbol; do
        grep -qw -- "$symbol" "$out/generated.h" \
            || { echo "FAIL: the emitted tree lacks $symbol" >&2; return 1; }
    done < "$markers"
    local measure="$out-measure"
    invoke_cli "$_CG_APP" -r "$_CG_CORELIB" -r "$redox" --auto-ref \
        --measure -o "$measure" > "$measure.log" 2>&1
    [ -f "$measure/s0-gaps.tsv" ] && [ ! -s "$measure/s0-gaps.tsv" ] \
        || { echo "FAIL: RE:Dox must emit an empty gap report: $measure/s0-gaps.tsv" >&2; return 1; }
}

_CG_CORELIB_IN=$(resolve_net10_corelib)
DN2CPP_GATE_EXTRA_INPUTS="$pin $markers"
DN2CPP_GATE_EXTRA_CONTEXT="redox|cli:$(_gate_cli_hash)"
corelib_diff_split_gate "$project" -r "$redox" --auto-ref
