#!/usr/bin/env bash
# The real Lua-CSharp package: parsing and bytecode execution, closures, tables,
# metamethods, bit32/math/string/table libraries and generated LuaObject
# bindings, suspended async callbacks, coroutines, module caching and errors.
# Full standard-library registration reaches basic, I/O, OS and debug delegates.
# Hash-pinned shipped IL runs against the real CoreLib, with stdout, stderr and
# exit status diffed against .NET and an empty transpilation gap report required.
source "$(dirname "$0")/_common.sh"

project=LuaCSharpSample
pin=gates/expected/luacsharp-dlls.sha256
markers=gates/expected/luacsharp-markers.txt
nuget_packages="$(nuget_global_packages_root)"
if { [ ! -d "$nuget_packages/luacsharp/0.5.7" ] \
        || [ ! -d "$nuget_packages/luacsharp.annotations/0.5.7" ] \
        || [ ! -d "$nuget_packages/luacsharp.sourcegenerator/0.5.7" ]; } \
    && ! curl -fsI --max-time 15 https://api.nuget.org/v3/index.json >/dev/null 2>&1; then
    gate_skip "LuaCSharp 0.5.7 (runtime, annotations and generator) is not in the NuGet cache and nuget.org is unreachable"
fi
build_gate_proj "samples/dotnet/$project/$project.csproj"
appbin="samples/dotnet/$project/bin/$CONFIG/$TFM"
package_refs=()
while read -r want name; do
    [ -n "$want" ] || continue
    dll="$appbin/$name"
    [ -f "$dll" ] || { echo "FAIL: $name did not restore to $dll" >&2; exit 1; }
    actual=$(shasum -a 256 "$dll" | awk '{print $1}')
    [ "$actual" = "$want" ] \
        || { echo "FAIL: $name differs from the pinned package ($pin): $actual" >&2; exit 1; }
    package_refs+=(-r "$dll")
done < "$pin"

gate_extra_asserts() {
    local out="$1" symbol line before prefix
    assert_exit_code "$2" 0
    assert_exit_code "$3" 0
    [ ! -s "$out/expected.err" ] && [ ! -s "$out/native.err" ] \
        || { echo "FAIL: Lua-CSharp must run with empty stderr on both sides" >&2; return 1; }
    for line in 'language end' 'tables and libraries end' 'interop end' \
        'coroutines end' 'modules end' 'errors end' 'LuaCSharp end' 'Lua standard libraries end'; do
        grep -Fxq -- "$line" "$out/native.out" \
            || { echo "FAIL: missing Lua-CSharp section witness: $line" >&2; return 1; }
    done
    before=$(run_bounded dotnet "$_CG_APP" before-standard-libraries)
    prefix=$(awk '/^== Lua standard libraries ==$/ { exit } { print }' "$out/native.out")
    assert_output "$(strip_cr_win "$prefix")" "$(strip_cr_win "$before")"
    while IFS= read -r symbol; do
        [ -n "$symbol" ] || continue
        grep -qw -- "$symbol" "$out/generated.h" \
            || { echo "FAIL: the emitted tree lacks $symbol" >&2; return 1; }
    done < "$markers"
    local measure="$out-measure"
    invoke_cli "$_CG_APP" -r "$_CG_CORELIB" "${package_refs[@]}" --auto-ref \
        --measure -o "$measure" > "$measure.log" 2>&1
    [ -f "$measure/s0-gaps.tsv" ] && [ ! -s "$measure/s0-gaps.tsv" ] \
        || { echo "FAIL: Lua-CSharp must emit an empty gap report: $measure/s0-gaps.tsv" >&2; return 1; }
}

_CG_CORELIB_IN=$(resolve_net10_corelib)
DN2CPP_GATE_EXTRA_INPUTS="$pin $markers"
DN2CPP_GATE_EXTRA_CONTEXT="luacsharp|cli:$(_gate_cli_hash)"
corelib_diff_split_gate "$project" "${package_refs[@]}" --auto-ref
